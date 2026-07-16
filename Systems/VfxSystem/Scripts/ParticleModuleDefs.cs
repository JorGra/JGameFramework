using System;
using System.Collections.Generic;
using UnityEngine;

namespace JG.Vfx
{
    /// <summary>
    /// Module defs mirror Unity's ParticleSystem modules. A null module on
    /// <see cref="ParticleSystemDef"/> means the module is disabled.
    /// All rotation/angle values are in degrees.
    /// </summary>
    [Serializable]
    public class MainModuleDef
    {
        public float duration = 5f;
        public bool looping = true;
        public bool prewarm;
        public MinMaxCurveDef startDelay;
        public MinMaxCurveDef startLifetime = MinMaxCurveDef.FromConstant(5f);
        public MinMaxCurveDef startSpeed = MinMaxCurveDef.FromConstant(5f);
        public MinMaxCurveDef startSize = MinMaxCurveDef.FromConstant(1f);
        public MinMaxCurveDef startRotation;
        public MinMaxGradientDef startColor;
        public MinMaxCurveDef gravityModifier;
        public ParticleSystemSimulationSpace simulationSpace = ParticleSystemSimulationSpace.Local;
        public float simulationSpeed = 1f;
        public int maxParticles = 1000;
    }

    [Serializable]
    public class BurstDef
    {
        public float time;
        public MinMaxCurveDef count = MinMaxCurveDef.FromConstant(10f);
        public int cycles = 1;
        public float interval = 0.01f;
        public float probability = 1f;
    }

    [Serializable]
    public class EmissionModuleDef
    {
        public MinMaxCurveDef rateOverTime = MinMaxCurveDef.FromConstant(10f);
        public MinMaxCurveDef rateOverDistance;
        public List<BurstDef> bursts;
    }

    [Serializable]
    public class ShapeModuleDef
    {
        public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Cone;
        public float angle = 25f;
        public float radius = 1f;
        public float radiusThickness = 1f;
        public float arc = 360f;
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.one;
        public float randomDirectionAmount;
        public float sphericalDirectionAmount;
    }

    [Serializable]
    public class VelocityOverLifetimeModuleDef
    {
        public MinMaxCurveDef x;
        public MinMaxCurveDef y;
        public MinMaxCurveDef z;
        public MinMaxCurveDef radial;
        public MinMaxCurveDef speedModifier;
        public ParticleSystemSimulationSpace space = ParticleSystemSimulationSpace.Local;
    }

    [Serializable]
    public class ColorOverLifetimeModuleDef
    {
        public MinMaxGradientDef color;
    }

    [Serializable]
    public class SizeOverLifetimeModuleDef
    {
        public MinMaxCurveDef size;
    }

    [Serializable]
    public class RotationOverLifetimeModuleDef
    {
        /// <summary>Angular velocity around Z in degrees per second.</summary>
        public MinMaxCurveDef z;
    }

    [Serializable]
    public class TextureSheetAnimationModuleDef
    {
        public int tilesX = 1;
        public int tilesY = 1;
        public MinMaxCurveDef frameOverTime;
        public MinMaxCurveDef startFrame;
        public int cycles = 1;
    }

    [Serializable]
    public class RendererModuleDef
    {
        public ParticleSystemRenderMode renderMode = ParticleSystemRenderMode.Billboard;

        /// <summary>Id into the <see cref="VfxMaterialLibrary"/> (e.g. "additive", "alphaBlend").</summary>
        public string baseMaterial = "additive";

        public string texturePath;
        [AssetFromPath(nameof(texturePath), optional: true)]
        public Texture2D texture;

        public Color tint = Color.white;

        /// <summary>Stretch render mode only.</summary>
        public float lengthScale = 2f;
        /// <summary>Stretch render mode only.</summary>
        public float speedScale;

        public float minParticleSize;
        public float maxParticleSize = 0.5f;
        public string sortingLayer = "Foreground";
        public int sortingOrder;
        public float sortingFudge;
    }
}
