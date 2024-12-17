using System;


public class StatModifier : IDisposable
{
    public StatType StatType { get; set; }
    public IOperationStrategy Strategy { get; }


    public bool MarkedForRemoval { get; set; }

    public event Action<StatModifier> OnDispose = delegate { };

    readonly CountdownTimer timer;

    public StatModifier(StatType statType, IOperationStrategy strategy, float duration)
    {
        this.StatType = statType;
        this.Strategy = strategy;

        if (duration <= 0f)
            return;


        timer = new CountdownTimer(duration);
        timer.OnTimerStop += () => MarkedForRemoval = true;
        timer.Start();
    }

    public void Update(float deltaTime) => timer?.Tick(deltaTime);

    public void Handle(object sender, Query query)
    {
        if (query.StatType == StatType)
        {
            query.Value = Strategy.Calculate(query.Value);
        }
    }

    public void Dispose()
    {
        OnDispose.Invoke(this);
    }
}
