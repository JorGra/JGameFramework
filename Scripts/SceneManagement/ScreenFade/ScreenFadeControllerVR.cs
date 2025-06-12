using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles VR-style full-screen fades. Listens for <see cref="FadeRequestEvent"/> via EventBus
/// and performs fade-in / fade-out transitions.
/// Creates a spherical gradient mesh that covers the camera view.
/// </summary>
public class ScreenFadeControllerVR : MonoBehaviour
{
    /* ────────────────────────────────────────────
     *  Inspector fields
     * ────────────────────────────────────────── */
    [SerializeField] private Material fadeMaterial;

    [Header("Fade Defaults")]
    [SerializeField] private float defaultFadeDuration = 1f;
    [SerializeField] private Color defaultFadeColor = Color.black;

    [Header("Gradient Settings")]
    [SerializeField] private Color fadeColor = new(0f, 0f, 0f, 1f);
    [SerializeField] private int renderQueue = 4000;

    /* ────────────────────────────────────────────
     *  Internal mesh & material
     * ────────────────────────────────────────── */
    private MeshRenderer gradientMeshRenderer;
    private MeshFilter gradientMeshFilter;
    private Material gradientMaterial;

    private readonly List<Vector3> verts = new();
    private readonly List<int> indices = new();
    private const int N = 5;

    /* ────────────────────────────────────────────
     *  Runtime state
     * ────────────────────────────────────────── */
    private Coroutine fadeCoroutine;
    private bool isFading;

    /// <summary>True while a fade coroutine is executing.</summary>
    public bool IsFading => isFading;

    /* Shader property cache */
    private static readonly int FadeColorProperty = Shader.PropertyToID("_FadeColor");

    /* Event-bus binding */
    private EventBinding<FadeRequestEvent> fadeRequestBinding;

    /* ────────────────────────────────────────────
     *  Unity lifecycle
     * ────────────────────────────────────────── */
    private void Awake()
    {
        CreateFadeMesh();
    }

    private void OnEnable()
    {
        fadeRequestBinding = new EventBinding<FadeRequestEvent>(OnFadeRequestReceived);
        EventBus<FadeRequestEvent>.Register(fadeRequestBinding);
    }

    private void OnDisable()
    {
        EventBus<FadeRequestEvent>.Deregister(fadeRequestBinding);
    }

    private void Start()
    {
        SetFadeAmount(1f);
        SetFadeColor(defaultFadeColor);
        FadeOut(1f, defaultFadeColor, null); // Start fully opaque
    }

    /* ────────────────────────────────────────────
     *  Event-bus callback
     * ────────────────────────────────────────── */
    private void OnFadeRequestReceived(FadeRequestEvent e)
    {
        if (isFading && !e.ForceReset) return;           // Ignore if already fading
        if (isFading && e.ForceReset && fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);                // Cancel current fade
        }

        if (e.FadeIn)
        {
            FadeIn(e.Duration, e.ColorOverride, null, e.ForceReset);
        }
        else
        {
            FadeOut(e.Duration, e.ColorOverride, null, e.ForceReset);
        }
    }

    /* ────────────────────────────────────────────
     *  Public API
     * ────────────────────────────────────────── */
    /// <summary>Begin fade-in (opaque ➜ clear).</summary>
    public void FadeIn(float duration = -1f, Color? color = null,
                       UnityAction onComplete = null, bool forceReset = false)
    {
        StartFade(0f, 1f, duration, color, onComplete, forceReset);
    }

    /// <summary>Begin fade-out (clear ➜ opaque).</summary>
    public void FadeOut(float duration = -1f, Color? color = null,
                        UnityAction onComplete = null, bool forceReset = false)
    {
        StartFade(1f, 0f, duration, color, onComplete, forceReset);
    }

    /* ────────────────────────────────────────────
     *  Core fade logic
     * ────────────────────────────────────────── */
    private void StartFade(float startValue, float endValue,
                           float duration, Color? color,
                           UnityAction onComplete, bool forceReset)
    {
        if (isFading && !forceReset) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (duration < 0f) duration = defaultFadeDuration;
        if (color.HasValue) SetFadeColor(color.Value);

        fadeCoroutine = StartCoroutine(FadeCoroutine(startValue, endValue, duration, onComplete));
    }

    private IEnumerator FadeCoroutine(float startValue, float endValue,
                                      float duration, UnityAction onComplete)
    {
        isFading = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAmount(Mathf.Lerp(startValue, endValue, t));
            yield return null;
        }

        SetFadeAmount(endValue);
        isFading = false;
        onComplete?.Invoke();
    }

    /* ────────────────────────────────────────────
     *  Instant controls
     * ────────────────────────────────────────── */
    /// <summary>Set fade amount immediately (0-1).</summary>
    public void SetFadeAmount(float amount) => SetAlpha(amount);

    /// <summary>Override fade colour immediately.</summary>
    public void SetFadeColor(Color color) => fadeMaterial.SetColor(FadeColorProperty, color);

    /// <summary>Instantly set fade amount (and optional colour) cancelling any coroutine.</summary>
    public void SetInstantFade(float amount, Color? color = null)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        SetFadeAmount(amount);
        if (color.HasValue) SetFadeColor(color.Value);
    }

    /* ────────────────────────────────────────────
     *  Gradient mesh helpers
     * ────────────────────────────────────────── */
    private void SetAlpha(float alpha)
    {
        Color c = fadeColor;
        c.a = alpha;
        bool visible = c.a > 0f;

        if (gradientMaterial != null)
        {
            gradientMaterial.color = c;
            gradientMaterial.renderQueue = renderQueue;
            gradientMeshRenderer.material = gradientMaterial;
            gradientMeshRenderer.enabled = visible;
        }
    }

    private void CreateFadeMesh()
    {
        gradientMaterial = new Material(Shader.Find("JG/ScreenFade"));
        gradientMeshFilter = gameObject.AddComponent<MeshFilter>();
        gradientMeshRenderer = gameObject.AddComponent<MeshRenderer>();

        if(LayerMask.NameToLayer("ScreenFade") == -1)
            Debug.LogError("Layer 'ScreenFade' not found.");
        gradientMeshRenderer.gameObject.layer = LayerMask.NameToLayer("ScreenFade");

        CreateModel();
    }

    private void CreateModel()
    {
        /* Build vertex cube */
        for (float i = -N / 2f; i <= N / 2f; i++)
        {
            for (float j = -N / 2f; j <= N / 2f; j++)
            {
                verts.Add(new Vector3(i, j, -N / 2f));
            }
        }
        for (float i = -N / 2f; i <= N / 2f; i++)
        {
            for (float j = -N / 2f; j <= N / 2f; j++)
            {
                verts.Add(new Vector3(N / 2f, j, i));
            }
        }
        for (float i = -N / 2f; i <= N / 2f; i++)
        {
            for (float j = -N / 2f; j <= N / 2f; j++)
            {
                verts.Add(new Vector3(i, N / 2f, j));
            }
        }
        for (float i = -N / 2f; i <= N / 2f; i++)
        {
            for (float j = -N / 2f; j <= N / 2f; j++)
            {
                verts.Add(new Vector3(-N / 2f, j, i));
            }
        }
        for (float i = -N / 2f; i <= N / 2f; i++)
        {
            for (float j = -N / 2f; j <= N / 2f; j++)
            {
                verts.Add(new Vector3(i, j, N / 2f));
            }
        }
        for (float i = -N / 2f; i <= N / 2f; i++)
        {
            for (float j = -N / 2f; j <= N / 2f; j++)
            {
                verts.Add(new Vector3(i, -N / 2f, j));
            }
        }

        /* Normalise vertices to sphere shape */
        for (int i = 0; i < verts.Count; i++)
        {
            verts[i] = verts[i].normalized * 0.7f;
        }

        /* Build index buffer */
        CreateMakePos(0);
        CreateMakePos(1);
        CreateMakePos(2);
        OtherMakePos(3);
        OtherMakePos(4);
        OtherMakePos(5);

        /* Create mesh */
        Mesh mesh = new Mesh
        {
            vertices = verts.ToArray(),
            triangles = indices.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        /* Invert normals & triangles for inside-view */
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        int[] tris = mesh.triangles;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int t = tris[i];
            tris[i] = tris[i + 2];
            tris[i + 2] = t;
        }
        mesh.triangles = tris;

        gradientMeshFilter.mesh = mesh;
    }

    /// <summary>Create indices for the first three faces.</summary>
    private void CreateMakePos(int num)
    {
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                int index = j * (N + 1) + (N + 1) * (N + 1) * num + i;
                int up = (j + 1) * (N + 1) + (N + 1) * (N + 1) * num + i;
                indices.AddRange(new[] { index, index + 1, up + 1 });
                indices.AddRange(new[] { index, up + 1, up });
            }
        }
    }

    /// <summary>Create indices for the remaining three faces.</summary>
    private void OtherMakePos(int num)
    {
        for (int i = 0; i < N + 1; i++)
        {
            for (int j = 0; j < N + 1; j++)
            {
                if (i != N && j != N)
                {
                    int index = j * (N + 1) + (N + 1) * (N + 1) * num + i;
                    int up = (j + 1) * (N + 1) + (N + 1) * (N + 1) * num + i;
                    indices.AddRange(new[] { index, up + 1, index + 1 });
                    indices.AddRange(new[] { index, up, up + 1 });
                }
            }
        }
    }
}


/// <summary>
/// Payload for requesting a screen-fade handled by <see cref="ScreenFadeControllerVR"/>.
/// Raise with <c>EventBus&lt;FadeRequestEvent&gt;.Raise(…)</c>.
/// </summary>
public class FadeRequestEvent : IEvent
{
    /// <summary>True = fade-in (opaque ➜ clear); False = fade-out (clear ➜ opaque).</summary>
    public bool FadeIn { get; }

    /// <summary>
    /// Duration in seconds.  
    /// Values &lt; 0 fall back to <c>defaultFadeDuration</c> on the controller.
    /// </summary>
    public float Duration { get; }

    /// <summary>
    /// Optional colour override for this fade.  
    /// <see langword="null"/> ➜ use controller’s <c>defaultFadeColor</c>.
    /// </summary>
    public Color? ColorOverride { get; }

    /// <summary>
    /// When <c>true</c> cancels any current fade before starting this one.
    /// </summary>
    public bool ForceReset { get; }

    public FadeRequestEvent(bool fadeIn,
                            float duration = -1f,
                            Color? colorOverride = null,
                            bool forceReset = false)
    {
        FadeIn = fadeIn;
        Duration = duration;
        ColorOverride = colorOverride;
        ForceReset = forceReset;
    }
}
