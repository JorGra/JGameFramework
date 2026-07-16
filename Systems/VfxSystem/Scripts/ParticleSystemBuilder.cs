using System.Collections.Generic;
using UnityEngine;

namespace JG.Vfx
{
    /// <summary>
    /// Applies a <see cref="ParticleSystemDef"/> to a live ParticleSystem.
    /// Shared by the game runtime and the WebGL preview player.
    /// </summary>
    public static class ParticleSystemBuilder
    {
        static readonly Dictionary<(string baseId, int textureId, Color tint), Material> MaterialCache = new();

        public static void ClearMaterialCache()
        {
            foreach (var mat in MaterialCache.Values)
                if (mat != null)
                    Object.Destroy(mat);
            MaterialCache.Clear();
        }

        /// <summary>Creates a new GameObject with a ParticleSystem configured from the def.</summary>
        public static ParticleSystem Build(ParticleSystemDef def, Transform parent = null)
        {
            var go = new GameObject($"Vfx_{def.Id}");
            if (parent != null)
                go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ApplyTo(def, ps);
            return ps;
        }

        /// <summary>Reconfigures an existing ParticleSystem from the def. Stops and clears it first.</summary>
        public static void ApplyTo(ParticleSystemDef def, ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ApplyMain(ps, def.main ?? new MainModuleDef());
            ApplyEmission(ps, def.emission);
            ApplyShape(ps, def.shape);
            ApplyVelocityOverLifetime(ps, def.velocityOverLifetime);
            ApplyColorOverLifetime(ps, def.colorOverLifetime);
            ApplySizeOverLifetime(ps, def.sizeOverLifetime);
            ApplyRotationOverLifetime(ps, def.rotationOverLifetime);
            ApplyTextureSheetAnimation(ps, def.textureSheetAnimation);
            ApplyRenderer(ps, def.renderer ?? new RendererModuleDef());

            ps.Play(true);
        }

        static ParticleSystem.MinMaxCurve Curve(MinMaxCurveDef def, float fallback) =>
            def?.ToMinMaxCurve() ?? new ParticleSystem.MinMaxCurve(fallback);

        static void ApplyMain(ParticleSystem ps, MainModuleDef def)
        {
            var main = ps.main;
            main.duration = def.duration;
            main.loop = def.looping;
            main.prewarm = def.prewarm;
            main.startDelay = Curve(def.startDelay, 0f);
            main.startLifetime = Curve(def.startLifetime, 5f);
            main.startSpeed = Curve(def.startSpeed, 5f);
            main.startSize = Curve(def.startSize, 1f);
            main.startRotation = MultiplyCurve(Curve(def.startRotation, 0f), Mathf.Deg2Rad);
            main.startColor = def.startColor?.ToMinMaxGradient()
                              ?? new ParticleSystem.MinMaxGradient(Color.white);
            main.gravityModifier = Curve(def.gravityModifier, 0f);
            main.simulationSpace = def.simulationSpace;
            main.simulationSpeed = def.simulationSpeed;
            main.maxParticles = def.maxParticles;
        }

        static void ApplyEmission(ParticleSystem ps, EmissionModuleDef def)
        {
            var emission = ps.emission;
            emission.enabled = def != null;
            if (def == null)
                return;

            emission.rateOverTime = Curve(def.rateOverTime, 10f);
            emission.rateOverDistance = Curve(def.rateOverDistance, 0f);

            if (def.bursts != null && def.bursts.Count > 0)
            {
                var bursts = new ParticleSystem.Burst[def.bursts.Count];
                for (int i = 0; i < def.bursts.Count; i++)
                {
                    var b = def.bursts[i];
                    bursts[i] = new ParticleSystem.Burst(b.time, Curve(b.count, 10f), b.cycles, b.interval)
                    {
                        probability = b.probability
                    };
                }
                emission.SetBursts(bursts);
            }
            else
            {
                emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            }
        }

        static void ApplyShape(ParticleSystem ps, ShapeModuleDef def)
        {
            var shape = ps.shape;
            shape.enabled = def != null;
            if (def == null)
                return;

            shape.shapeType = def.shapeType;
            shape.angle = def.angle;
            shape.radius = def.radius;
            shape.radiusThickness = def.radiusThickness;
            shape.arc = def.arc;
            shape.position = def.position;
            shape.rotation = def.rotation;
            shape.scale = def.scale;
            shape.randomDirectionAmount = def.randomDirectionAmount;
            shape.sphericalDirectionAmount = def.sphericalDirectionAmount;
        }

        static void ApplyVelocityOverLifetime(ParticleSystem ps, VelocityOverLifetimeModuleDef def)
        {
            var vel = ps.velocityOverLifetime;
            vel.enabled = def != null;
            if (def == null)
                return;

            vel.x = Curve(def.x, 0f);
            vel.y = Curve(def.y, 0f);
            vel.z = Curve(def.z, 0f);
            vel.radial = Curve(def.radial, 0f);
            vel.speedModifier = Curve(def.speedModifier, 1f);
            vel.space = def.space;
        }

        static void ApplyColorOverLifetime(ParticleSystem ps, ColorOverLifetimeModuleDef def)
        {
            var col = ps.colorOverLifetime;
            col.enabled = def?.color != null;
            if (def?.color == null)
                return;

            col.color = def.color.ToMinMaxGradient();
        }

        static void ApplySizeOverLifetime(ParticleSystem ps, SizeOverLifetimeModuleDef def)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = def?.size != null;
            if (def?.size == null)
                return;

            size.size = def.size.ToMinMaxCurve();
        }

        static void ApplyRotationOverLifetime(ParticleSystem ps, RotationOverLifetimeModuleDef def)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = def?.z != null;
            if (def?.z == null)
                return;

            rot.z = MultiplyCurve(def.z.ToMinMaxCurve(), Mathf.Deg2Rad);
        }

        static void ApplyTextureSheetAnimation(ParticleSystem ps, TextureSheetAnimationModuleDef def)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = def != null;
            if (def == null)
                return;

            tsa.numTilesX = def.tilesX;
            tsa.numTilesY = def.tilesY;
            tsa.frameOverTime = def.frameOverTime?.ToMinMaxCurve()
                                ?? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            tsa.startFrame = Curve(def.startFrame, 0f);
            tsa.cycleCount = def.cycles;
        }

        static void ApplyRenderer(ParticleSystem ps, RendererModuleDef def)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
                return;

            renderer.renderMode = def.renderMode;
            if (def.renderMode == ParticleSystemRenderMode.Stretch)
            {
                renderer.lengthScale = def.lengthScale;
                renderer.velocityScale = def.speedScale;
            }

            renderer.minParticleSize = def.minParticleSize;
            renderer.maxParticleSize = def.maxParticleSize;
            if (!string.IsNullOrEmpty(def.sortingLayer))
                renderer.sortingLayerName = def.sortingLayer;
            renderer.sortingOrder = def.sortingOrder;
            renderer.sortingFudge = def.sortingFudge;

            var material = ResolveMaterial(def);
            if (material != null)
                renderer.sharedMaterial = material;
        }

        static Material ResolveMaterial(RendererModuleDef def)
        {
            var library = VfxMaterialLibrary.Instance;
            if (library == null || !library.TryResolve(def.baseMaterial, out var baseMat))
            {
                Debug.LogWarning($"[Vfx] Base material '{def.baseMaterial}' not found in VfxMaterialLibrary.");
                return null;
            }

            bool untouched = def.texture == null && def.tint == Color.white;
            if (untouched)
                return baseMat;

            var key = (def.baseMaterial, def.texture != null ? def.texture.GetInstanceID() : 0, def.tint);
            if (MaterialCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var mat = new Material(baseMat);
            if (def.texture != null)
                mat.mainTexture = def.texture;
            if (def.tint != Color.white)
                mat.color = baseMat.color * def.tint;

            MaterialCache[key] = mat;
            return mat;
        }

        static ParticleSystem.MinMaxCurve MultiplyCurve(ParticleSystem.MinMaxCurve curve, float factor)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(curve.constant * factor);
                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(curve.constantMin * factor, curve.constantMax * factor);
                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * factor, curve.curve);
                case ParticleSystemCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * factor, curve.curveMin, curve.curveMax);
                default:
                    return curve;
            }
        }
    }
}
