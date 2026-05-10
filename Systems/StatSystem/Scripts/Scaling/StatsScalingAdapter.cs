using System.Collections.Generic;

namespace JG.Scaling
{
    public sealed class StatsScalingAdapter : IStatProvider
    {
        private readonly IReadOnlyDictionary<string, float> values;

        public StatsScalingAdapter(IReadOnlyDictionary<string, float> values)
        {
            this.values = values ?? new Dictionary<string, float>();
        }

        public float GetStat(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0f;
            return values.TryGetValue(key, out var v) ? v : 0f;
        }
    }
}
