using System;
using JG.GameContent;
using UnityEngine;

namespace JG.Scaling
{
    [Serializable]
    public struct ScalingTerm
    {
        [IdReference(typeof(StatDef))]
        [SerializeField] private string stat;
        [SerializeField] private ScalingMode mode;
        [SerializeField] private float factor;

        public string Stat => stat;
        public ScalingMode Mode => mode;
        public float Factor => factor;

        public ScalingTerm(string stat, ScalingMode mode, float factor)
        {
            this.stat = stat;
            this.mode = mode;
            this.factor = factor;
        }
    }
}
