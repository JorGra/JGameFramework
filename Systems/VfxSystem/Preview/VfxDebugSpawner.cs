using JG.GameContent;
using UnityEngine;

namespace JG.Vfx.Preview
{
    /// <summary>
    /// In-game test helper: spawns a ParticleSystemDef from the ContentCatalogue.
    /// Trigger via the component context menu (Respawn), or enable spawnOnStart.
    /// </summary>
    public class VfxDebugSpawner : MonoBehaviour
    {
        [IdReference(typeof(ParticleSystemDef))]
        public string defId;

        [Tooltip("Spawn immediately on Start. Requires mods to be loaded by then.")]
        public bool spawnOnStart = true;

        private ParticleSystem _current;

        private void Start()
        {
            if (spawnOnStart)
                Respawn();
        }

        [ContextMenu("Respawn")]
        public void Respawn()
        {
            if (_current != null)
                Destroy(_current.gameObject);

            if (!ContentCatalogue.Instance.TryGet<ParticleSystemDef>(defId, out var def))
            {
                Debug.LogWarning($"[VfxDebugSpawner] ParticleSystemDef '{defId}' not found. Are mods loaded?");
                return;
            }

            _current = ParticleSystemBuilder.Build(def, transform, ResolveDef);
        }

        private static ParticleSystemDef ResolveDef(string id)
        {
            return ContentCatalogue.Instance.TryGet<ParticleSystemDef>(id, out var def) ? def : null;
        }
    }
}
