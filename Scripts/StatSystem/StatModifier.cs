using System;

public class StatModifier : IDisposable
{
    public string StatKey { get; private set; }
    public IOperationStrategy Strategy { get; }
    public bool MarkedForRemoval { get; set; }

    public event Action<StatModifier> OnDispose = delegate { };

    private readonly CountdownTimer timer;

    public StatModifier(string statKey, IOperationStrategy strategy, float duration)
    {
        if (string.IsNullOrWhiteSpace(statKey))
            throw new ArgumentException("statKey cannot be null/empty", nameof(statKey));
        StatKey = statKey;
        Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        if (duration > 0f)
        {
            timer = new CountdownTimer(duration);
            timer.OnTimerStop += () => MarkedForRemoval = true;
            timer.Start();
        }
    }

    public void Update(float deltaTime) => timer?.Tick(deltaTime);

    public void Handle(object sender, Query query)
    {
        if (query != null &&
            string.Equals(query.StatKey, StatKey, StringComparison.OrdinalIgnoreCase))
        {
            query.Value = Strategy.Calculate(query.Value);
        }
    }

    public void Dispose() => OnDispose.Invoke(this);
}
