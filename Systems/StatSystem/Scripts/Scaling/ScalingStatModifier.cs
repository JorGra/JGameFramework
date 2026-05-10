using System;

namespace JG.Scaling
{
    /// <summary>
    /// Stat modifier whose magnitude is computed lazily from a <see cref="ScaledValue"/>
    /// against the live <see cref="IStatProvider"/> at query time.
    /// Falls back to <c>ScaledValue.Base</c> when no provider is supplied.
    /// </summary>
    public sealed class ScalingStatModifier : StatModifier
    {
        private readonly ScaledValue scaled;
        private readonly OperatorType opType;

        public ScalingStatModifier(string statKey, ScaledValue value, OperatorType op, float duration)
            : base(statKey, BuildBaseStrategy(value, op), duration)
        {
            scaled = value;
            opType = op;
        }

        public ScaledValue Value => scaled;
        public OperatorType Operator => opType;

        public override IOperationStrategy ResolveStrategy(IStatProvider provider)
        {
            if (provider == null || !scaled.HasScaling) return Strategy;
            float v = scaled.Evaluate(provider);
            return BuildStrategy(opType, v);
        }

        static IOperationStrategy BuildBaseStrategy(ScaledValue value, OperatorType op)
            => BuildStrategy(op, value.Base);

        static IOperationStrategy BuildStrategy(OperatorType op, float v) => op switch
        {
            OperatorType.Add => new AddOperation(v),
            OperatorType.Multiply => new MultiplyOperation(v),
            OperatorType.Percentage => new PercentageOperation(v),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
        };
    }
}
