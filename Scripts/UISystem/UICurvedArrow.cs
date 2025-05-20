using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace JG.UI
{
    /// <summary>
    /// Connects two UI elements with a curved, dotted arrow in a Screen-Space Overlay canvas.
    /// Assign a dot prefab (and optional head sprite) in the Inspector, then call
    /// <see cref="SetEndpoints"/> to link the RectTransforms and <see cref="SetColor"/> to tint.
    /// </summary>
    public class UICurvedArrow : MonoBehaviour
    {
        #region Inspector ---------------------------------------------------

        [Header("Endpoints")]
        [SerializeField] private RectTransform startRect;
        [SerializeField] private RectTransform endRect;

        [Header("Arrow Head (optional)")]
        [Tooltip("Sprite placed at the arrow’s end. Author it pointing +X if offset = 0°.")]
        [SerializeField] private Image headImage;

        [Tooltip("Extra rotation (degrees) applied after tangent alignment.")]
        [Range(-180f, 180f)]
        [SerializeField] private float headRotationOffset = 0f;

        [Header("Dot Settings")]
        [Tooltip("Prefab of a simple UI Image used for each dot of the trail.")]
        [SerializeField] private Image dotPrefab;

        [Min(1f)][SerializeField] private float dotSpacing = 40f;
        [Min(1)][SerializeField] private int initialPoolSize = 32;

        [Tooltip("Keep the last dot at least this far (units) from the head.")]
        [Min(0f)]
        [SerializeField] private float headDotGap = 20f;

        [Header("Curve Shape")]
        [SerializeField] private Curves.CurveType curveType = Curves.CurveType.CatmullRomAlpha;
        [Range(0f, 1f)][SerializeField] private float catmullAlpha = 0.5f;
        [Range(0f, 1f)][SerializeField] private float bendStrength = 0.15f;
        [Range(0f, 1f)][SerializeField] private float controlPointAlong = 0.25f;
        [Min(4)][SerializeField] private int samplesPerSeg = 10;

        [Header("Appearance")]
        [SerializeField] private Color arrowColor = Color.white;

        #endregion

        readonly List<Image> activeDots = new();
        ObjectPool<Image> dotPool;
        Curves workCurve;

        // ------------------------------------------------------------------

        void Awake()
        {
            if (!dotPrefab)
            {
                Debug.LogError($"{name} ➜ Dot Prefab is missing.", this);
                enabled = false;
                return;
            }

            dotPool = new ObjectPool<Image>(
                CreateDot,
                img => img.gameObject.SetActive(true),
                img => img.gameObject.SetActive(false),
                img => Destroy(img.gameObject),
                collectionCheck: false,
                defaultCapacity: initialPoolSize,
                maxSize: 256);

            for (int i = 0; i < initialPoolSize; i++)
                dotPool.Release(dotPool.Get());

            workCurve = gameObject.AddComponent<Curves>();
            workCurve.hideFlags = HideFlags.HideAndDontSave;
            workCurve.enabled = false;
            workCurve.isLoop = false;
        }

        void LateUpdate() => Refresh();

        // ----------------------------- API --------------------------------

        public void SetEndpoints(RectTransform start, RectTransform end)
        {
            startRect = start;
            endRect = end;
            Refresh();
        }

        public void SetColor(Color color)
        {
            arrowColor = color;
            foreach (var dot in activeDots) dot.color = arrowColor;
            if (headImage) headImage.color = arrowColor;
        }

        public void Refresh()
        {
            if (!startRect || !endRect) return;

            Vector3 p0 = startRect.position;
            Vector3 p3 = endRect.position;
            Vector3 dir = p3 - p0;
            if (dir.sqrMagnitude < 1e-4f) return;

            // -------- build curve
            Vector3 perp = Vector3.Cross(dir.normalized, Vector3.forward);
            float bend = dir.magnitude * bendStrength;
            float cpL = Mathf.Clamp01(controlPointAlong);

            workCurve.Clear();
            workCurve.curveType = curveType;
            workCurve.catmullAlpha = catmullAlpha;
            workCurve.segmentsPerCurve = samplesPerSeg;

            workCurve.AddControlPoint(p0);
            workCurve.AddControlPoint(p0 + dir * cpL + perp * bend);
            workCurve.AddControlPoint(p3 - dir * cpL + perp * bend);
            workCurve.AddControlPoint(p3);

            // -------- recycle previous dots
            foreach (var d in activeDots) dotPool.Release(d);
            activeDots.Clear();

            // -------- spawn dots along the curve
            float travelled = 0f;
            Vector3 prev = p0;
            SpawnDot(prev);

            int segCount = workCurve.SegmentCount;
            List<Vector3> samples = new();

            for (int seg = 0; seg < segCount; seg++)
            {
                for (int s = 1; s <= samplesPerSeg; s++)
                {
                    Vector3 p = CurvesUtils.EvaluateCurve(workCurve, seg, s / (float)samplesPerSeg);
                    samples.Add(p);

                    travelled += Vector3.Distance(prev, p);

                    if (travelled >= dotSpacing)
                    {
                        // Skip if we’re too close to the head
                        if (Vector3.Distance(p, p3) > headDotGap)
                        {
                            SpawnDot(p);
                            travelled = 0f;
                        }
                    }

                    prev = p;
                }
            }

            // -------- arrow head
            if (headImage)
            {
                Vector3 tangent = samples.Count >= 2 ? samples[^1] - samples[^2] : dir;
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + headRotationOffset;

                RectTransform h = headImage.rectTransform;
                h.position = p3;
                h.rotation = Quaternion.Euler(0, 0, angle);
                headImage.color = arrowColor;

                if (!headImage.gameObject.activeSelf)
                    headImage.gameObject.SetActive(true);
            }
        }

        // --------------------------- helpers ------------------------------

        Image CreateDot()
        {
            Image img = Instantiate(dotPrefab, transform);
            img.name = $"Dot_{img.GetInstanceID()}";
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            return img;
        }

        void SpawnDot(Vector3 worldPos)
        {
            Image d = dotPool.Get();
            activeDots.Add(d);
            d.rectTransform.position = worldPos;
            d.color = arrowColor;
        }
    }
}
