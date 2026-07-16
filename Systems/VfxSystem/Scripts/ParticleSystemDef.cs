using JG.GameContent;
using UnityEngine;

namespace JG.Vfx
{
    /// <summary>
    /// Data-driven Unity ParticleSystem definition. Authored as JSON
    /// (ModCreator or by hand), applied at runtime via <see cref="ParticleSystemBuilder"/>.
    /// A null module means that module is disabled.
    /// </summary>
    [ContentFolder("Vfx")]
    [CreateAssetMenu(menuName = "JGameFramework/Vfx/Particle System Def")]
    public class ParticleSystemDef : ContentDef
    {
        public MainModuleDef main = new();
        public EmissionModuleDef emission = new();
        public ShapeModuleDef shape;
        public VelocityOverLifetimeModuleDef velocityOverLifetime;
        public ColorOverLifetimeModuleDef colorOverLifetime;
        public SizeOverLifetimeModuleDef sizeOverLifetime;
        public RotationOverLifetimeModuleDef rotationOverLifetime;
        public TextureSheetAnimationModuleDef textureSheetAnimation;
        public RendererModuleDef renderer = new();
    }
}
