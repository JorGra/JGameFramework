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
        #region Enums

        public enum BendDirection
        {
            Auto,      // Automatically choose based on positions
            Up,        // Bend upward
            Down,      // Bend downward
            Left,      // Bend left
            Right,     // Bend right
            Custom     // Use custom angle
        }

        #endregion

        #region Inspector Fields

        [Header("Endpoints")]
        [SerializeField] private RectTransform startRect;
        [SerializeField] private RectTransform endRect;

        [Header("Arrow Head")]
        [Tooltip("Sprite placed at the arrow's end. Author it pointing +X if offset = 0°.")]
        [SerializeField] private Image headImage;

        [Tooltip("Extra rotation (degrees) applied after tangent alignment.")]
        [Range(-180f, 180f)]
        [SerializeField] private float headRotationOffset = 0f;

        [Header("Dot Settings")]
        [Tooltip("Prefab of a simple UI Image used for each dot of the trail.")]
        [SerializeField] private Image dotPrefab;

        [Min(1f)]
        [SerializeField] private float dotSpacing = 40f;

        [Min(1)]
        [SerializeField] private int initialPoolSize = 32;

        [Tooltip("Keep the last dot at least this far (units) from the head.")]
        [Min(0f)]
        [SerializeField] private float headDotGap = 20f;

        [Header("Curve Shape")]
        [SerializeField] private Curves.CurveType curveType = Curves.CurveType.CatmullRomAlpha;

        [Range(0f, 1f)]
        [SerializeField] private float catmullAlpha = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float bendStrength = 0.15f;

        [Range(0f, 1f)]
        [SerializeField] private float controlPointAlong = 0.25f;

        [Min(4)]
        [SerializeField] private int samplesPerSeg = 10;

        [Header("Bend Direction")]
        [SerializeField] private BendDirection bendDirection = BendDirection.Auto;

        [Tooltip("Custom bend angle in degrees (only used when Bend Direction is Custom)")]
        [Range(-180f, 180f)]
        [SerializeField] private float customBendAngle = 90f;

        [Tooltip("Flip the bend direction")]
        [SerializeField] private bool flipBend = false;

        [Header("Appearance")]
        [SerializeField] private Color arrowColor = Color.white;

        #endregion

        #region Private Fields

        private readonly List<Image> activeDots = new();
        private ObjectPool<Image> dotPool;
        private Curves workCurve;
        private bool isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (isInitialized)
            {
                Refresh();
            }
        }

        private void LateUpdate()
        {
            if (isInitialized && startRect && endRect)
            {
                Refresh();
            }
        }

        private void OnDestroy()
        {
            dotPool?.Dispose();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Sets the start and end points for the arrow.
        /// </summary>
        public void SetEndpoints(RectTransform start, RectTransform end)
        {
            startRect = start;
            endRect = end;
            Refresh();
        }

        /// <summary>
        /// Sets the color of the arrow (dots and head).
        /// </summary>
        public void SetColor(Color color)
        {
            arrowColor = color;
            ApplyColor();
        }

        /// <summary>
        /// Sets the bend direction for the arrow.
        /// </summary>
        public void SetBendDirection(BendDirection direction, float customAngle = 90f)
        {
            bendDirection = direction;
            customBendAngle = customAngle;
            Refresh();
        }

        /// <summary>
        /// Toggles the bend direction flip.
        /// </summary>
        public void SetFlipBend(bool flip)
        {
            flipBend = flip;
            Refresh();
        }

        /// <summary>
        /// Forces a refresh of the arrow.
        /// </summary>
        public void Refresh()
        {
            if (!isInitialized || !startRect || !endRect) return;

            BuildCurve();
            UpdateDots();
            UpdateArrowHead();
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            if (!ValidateComponents()) return;

            InitializeDotPool();
            InitializeCurve();

            isInitialized = true;
        }

        private bool ValidateComponents()
        {
            if (!dotPrefab)
            {
                Debug.LogError($"[{name}] Dot Prefab is missing.", this);
                enabled = false;
                return false;
            }
            return true;
        }

        private void InitializeDotPool()
        {
            dotPool = new ObjectPool<Image>(
                createFunc: CreateDot,
                actionOnGet: img => img.gameObject.SetActive(true),
                actionOnRelease: img => img.gameObject.SetActive(false),
                actionOnDestroy: img => Destroy(img.gameObject),
                collectionCheck: false,
                defaultCapacity: initialPoolSize,
                maxSize: 256
            );

            // Pre-warm the pool
            var tempDots = new List<Image>();
            for (int i = 0; i < initialPoolSize; i++)
            {
                tempDots.Add(dotPool.Get());
            }
            foreach (var dot in tempDots)
            {
                dotPool.Release(dot);
            }
        }

        private void InitializeCurve()
        {
            workCurve = gameObject.AddComponent<Curves>();
            workCurve.hideFlags = HideFlags.HideAndDontSave;
            workCurve.enabled = false;
            workCurve.isLoop = false;
        }

        private void BuildCurve()
        {
            Vector3 p0 = startRect.position;
            Vector3 p3 = endRect.position;
            Vector3 dir = p3 - p0;

            if (dir.sqrMagnitude < 1e-4f) return;

            // Calculate bend direction
            Vector3 bendVector = CalculateBendVector(dir);
            float bendMagnitude = dir.magnitude * bendStrength;

            // Apply flip if needed
            if (flipBend)
            {
                bendVector = -bendVector;
            }

            // Configure curve
            workCurve.Clear();
            workCurve.curveType = curveType;
            workCurve.catmullAlpha = catmullAlpha;
            workCurve.segmentsPerCurve = samplesPerSeg;

            // Add control points
            float cpOffset = Mathf.Clamp01(controlPointAlong);
            workCurve.AddControlPoint(p0);
            workCurve.AddControlPoint(p0 + dir * cpOffset + bendVector * bendMagnitude);
            workCurve.AddControlPoint(p3 - dir * cpOffset + bendVector * bendMagnitude);
            workCurve.AddControlPoint(p3);
        }

        private Vector3 CalculateBendVector(Vector3 direction)
        {
            Vector3 bendVector = Vector3.zero;

            switch (bendDirection)
            {
                case BendDirection.Auto:
                    // Default perpendicular bend
                    bendVector = Vector3.Cross(direction.normalized, Vector3.forward).normalized;
                    if (bendVector.sqrMagnitude < 0.01f)
                    {
                        bendVector = Vector3.up;
                    }
                    break;

                case BendDirection.Up:
                    bendVector = Vector3.up;
                    break;

                case BendDirection.Down:
                    bendVector = Vector3.down;
                    break;

                case BendDirection.Left:
                    bendVector = Vector3.left;
                    break;

                case BendDirection.Right:
                    bendVector = Vector3.right;
                    break;

                case BendDirection.Custom:
                    float angleRad = customBendAngle * Mathf.Deg2Rad;
                    bendVector = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f).normalized;
                    break;
            }

            return bendVector;
        }

        private void UpdateDots()
        {
            // Clear existing dots
            ReleaseDots();

            // Generate curve samples and place dots
            float distanceTravelled = 0f;
            Vector3 previousPoint = startRect.position;
            Vector3 endPoint = endRect.position;

            SpawnDot(previousPoint);

            int segmentCount = workCurve.SegmentCount;

            for (int seg = 0; seg < segmentCount; seg++)
            {
                for (int sample = 1; sample <= samplesPerSeg; sample++)
                {
                    float t = sample / (float)samplesPerSeg;
                    Vector3 point = CurvesUtils.EvaluateCurve(workCurve, seg, t);

                    distanceTravelled += Vector3.Distance(previousPoint, point);

                    if (distanceTravelled >= dotSpacing)
                    {
                        float distanceToEnd = Vector3.Distance(point, endPoint);
                        if (distanceToEnd > headDotGap)
                        {
                            SpawnDot(point);
                            distanceTravelled = 0f;
                        }
                    }

                    previousPoint = point;
                }
            }
        }

        private void UpdateArrowHead()
        {
            if (!headImage) return;

            Vector3 endPoint = endRect.position;

            // Calculate tangent at the end of the curve
            Vector3 tangent = CalculateEndTangent();
            float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + headRotationOffset;

            // Update head transform
            RectTransform headTransform = headImage.rectTransform;
            headTransform.position = endPoint;
            headTransform.rotation = Quaternion.Euler(0, 0, angle);

            // Update appearance
            headImage.color = arrowColor;
            if (!headImage.gameObject.activeSelf)
            {
                headImage.gameObject.SetActive(true);
            }
        }

        private Vector3 CalculateEndTangent()
        {
            int lastSeg = workCurve.SegmentCount - 1;
            if (lastSeg < 0) return Vector3.right;

            Vector3 beforeLast = CurvesUtils.EvaluateCurve(workCurve, lastSeg, 0.98f);
            Vector3 last = CurvesUtils.EvaluateCurve(workCurve, lastSeg, 1f);

            Vector3 tangent = last - beforeLast;
            return tangent.sqrMagnitude > 0.001f ? tangent.normalized : Vector3.right;
        }

        private void ReleaseDots()
        {
            foreach (var dot in activeDots)
            {
                dotPool.Release(dot);
            }
            activeDots.Clear();
        }

        private Image CreateDot()
        {
            Image img = Instantiate(dotPrefab, transform);
            img.name = $"Dot_{img.GetInstanceID()}";
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            return img;
        }

        private void SpawnDot(Vector3 worldPos)
        {
            Image dot = dotPool.Get();
            activeDots.Add(dot);
            dot.rectTransform.position = worldPos;
            dot.color = arrowColor;
        }

        private void ApplyColor()
        {
            foreach (var dot in activeDots)
            {
                dot.color = arrowColor;
            }

            if (headImage)
            {
                headImage.color = arrowColor;
            }
        }

        #endregion
    }
}