using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JG.GameContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace JG.Vfx.Preview
{
    /// <summary>
    /// Entry point of the WebGL particle preview player. Lives on a GameObject
    /// named "VfxPreview" (SendMessage target). Receives payloads from the
    /// hosting page via postMessage -> VfxJsonBridge.jslib -> ApplyPayload.
    ///
    /// Payload is either a raw ParticleSystemDef JSON object, or a wrapper:
    /// { "def": { ... }, "textures": { "&lt;texturePath&gt;": "&lt;base64 png&gt;" } }
    /// </summary>
    public class VfxPreviewBootstrap : MonoBehaviour
    {
        public const string GameObjectName = "VfxPreview";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void JGVfx_RegisterMessageListener();
        [DllImport("__Internal")] private static extern void JGVfx_PostStatus(string json);
#endif

        [SerializeField, Tooltip("Optional def JSON applied on start, for testing in the editor.")]
        [TextArea(5, 30)]
        private string initialJson;

        private ParticleSystem _ps;
        private readonly List<Texture2D> _runtimeTextures = new();

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Converters =
            {
                new UnityScriptableObjectConverter(),
                new UnityColorJsonConverter(),
                new UnityVector2JsonConverter(),
                new UnityVector3JsonConverter()
            },
            // Lenient: the CMS may push partially valid JSON while the user is typing.
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });

        private void Awake()
        {
            if (gameObject.name != GameObjectName)
                gameObject.name = GameObjectName;

            _ps = GetComponentInChildren<ParticleSystem>();
            if (_ps == null)
            {
                var child = new GameObject("PreviewParticleSystem");
                child.transform.SetParent(transform, false);
                _ps = child.AddComponent<ParticleSystem>();
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            JGVfx_RegisterMessageListener();
#endif
        }

        private void Start()
        {
            if (!string.IsNullOrWhiteSpace(initialJson))
                ApplyPayload(initialJson);
        }

        /// <summary>Called via SendMessage from the jslib bridge, and usable directly in tests.</summary>
        public void ApplyPayload(string payload)
        {
            try
            {
                var root = JObject.Parse(payload);
                var defToken = root["def"] as JObject ?? root;
                var texturesToken = root["textures"] as JObject;

                var def = defToken.ToObject<ParticleSystemDef>(Serializer);
                if (def == null)
                {
                    PostStatus(false, "Payload contained no particle def.");
                    return;
                }

                ReplaceRuntimeTextures(def, texturesToken);
                ParticleSystemBuilder.ClearMaterialCache();
                ParticleSystemBuilder.ApplyTo(def, _ps);
                PostStatus(true, $"Applied def '{def.Id}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VfxPreview] Failed to apply payload: {ex}");
                PostStatus(false, ex.Message);
            }
        }

        private void ReplaceRuntimeTextures(ParticleSystemDef def, JObject textures)
        {
            foreach (var tex in _runtimeTextures)
                if (tex != null)
                    Destroy(tex);
            _runtimeTextures.Clear();

            var path = def.renderer?.texturePath;
            if (textures == null || string.IsNullOrWhiteSpace(path))
                return;

            var base64 = textures[path]?.ToString();
            if (string.IsNullOrWhiteSpace(base64))
                return;

            var tex2D = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!tex2D.LoadImage(Convert.FromBase64String(base64)))
            {
                Destroy(tex2D);
                PostStatus(false, $"Could not decode texture '{path}'.");
                return;
            }

            tex2D.name = path;
            _runtimeTextures.Add(tex2D);
            def.renderer.texture = tex2D;
        }

        private void PostStatus(bool ok, string message)
        {
            var json = new JObject { ["ok"] = ok, ["message"] = message }.ToString(Formatting.None);
#if UNITY_WEBGL && !UNITY_EDITOR
            JGVfx_PostStatus(json);
#else
            Debug.Log($"[VfxPreview] status: {json}");
#endif
        }
    }
}
