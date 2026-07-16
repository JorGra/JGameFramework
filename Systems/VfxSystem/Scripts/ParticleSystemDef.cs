using System;
using System.Collections.Generic;
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

        [Tooltip("Other particle systems spawned as children when this one is built - e.g. an explosion combining a ring and sparks.")]
        public List<SubSystemDef> subSystems;
    }

    /// <summary>
    /// Reference to another <see cref="ParticleSystemDef"/> played together with
    /// the parent system (spawned as a child GameObject when the parent is built).
    /// </summary>
    [Serializable]
    public class SubSystemDef
    {
        [IdReference(typeof(ParticleSystemDef))]
        public string id;

        [Tooltip("Seconds added to the child's start delay.")]
        public float delay;

        [Tooltip("Local position offset relative to the parent system.")]
        public Vector3 offset = Vector3.zero;
    }
}
