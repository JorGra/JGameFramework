using System;
using UnityEngine;

namespace JGameFramework.Saving
{
    /// <summary>
    /// Opaque identifier for a logical save case (e.g. "meta", "profile", "run").
    /// Define cases as static readonly SaveCaseId fields using nameof() to avoid typos.
    /// </summary>
    [Serializable]
    public struct SaveCaseId : IEquatable<SaveCaseId>
    {
        [SerializeField] private string value;

        public string Value => value ?? string.Empty;

        public SaveCaseId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public bool Equals(SaveCaseId other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is SaveCaseId other && Equals(other);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        public override string ToString() => Value;

        public static implicit operator string(SaveCaseId id) => id.Value;
    }
}
