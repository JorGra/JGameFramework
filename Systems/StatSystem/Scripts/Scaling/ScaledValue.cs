using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace JG.Scaling
{
    [Serializable]
    [JsonConverter(typeof(ScaledValueJsonConverter))]
    public struct ScaledValue
    {
        [SerializeField] private float baseValue;
        [SerializeField] private List<ScalingTerm> scaling;

        public float Base => baseValue;
        public IReadOnlyList<ScalingTerm> Scaling => scaling ?? (IReadOnlyList<ScalingTerm>)Array.Empty<ScalingTerm>();
        public bool HasScaling => scaling != null && scaling.Count > 0;

        public ScaledValue(float baseValue)
        {
            this.baseValue = baseValue;
            scaling = null;
        }

        public ScaledValue(float baseValue, IEnumerable<ScalingTerm> terms)
        {
            this.baseValue = baseValue;
            scaling = terms != null ? new List<ScalingTerm>(terms) : null;
        }

        public float Evaluate() => baseValue;

        public float Evaluate(IStatProvider stats)
        {
            if (stats == null || !HasScaling) return baseValue;

            float sum = baseValue;
            float product = 1f;
            for (int i = 0; i < scaling.Count; i++)
            {
                var t = scaling[i];
                float s = stats.GetStat(t.Stat);
                switch (t.Mode)
                {
                    case ScalingMode.Sum:
                        sum += s * t.Factor;
                        break;
                    case ScalingMode.Product:
                        product *= 1f + s * t.Factor;
                        break;
                }
            }
            return sum * product;
        }

        public static implicit operator ScaledValue(float v) => new ScaledValue(v);
    }
}
