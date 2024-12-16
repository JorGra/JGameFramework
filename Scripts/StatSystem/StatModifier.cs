using System;


public enum OperatorType
{
    Add,
    Multiply,
}



public class BasicStatModifier : StatModifier
{
    readonly StatType statType;
    readonly Func<float, float> operation;

    public BasicStatModifier(StatType statType, Func<float, float> operation, float duration) : base(duration)
    {
        this.statType = statType;
        this.operation = operation;
    }
    public override void Handle(object sender, Query query)
    {
        if (query.StatType == statType)
        {
            query.Value += operation(query.Value);
        }
    }
}

public abstract class StatModifier : IDisposable
{
    public bool MarkedForRemoval { get; set; }

    public event Action<StatModifier> OnDisposed = delegate { };

    readonly CountdownTimer timer;

    protected StatModifier(float duration)
    {
        if (duration <= 0f)
            return;


        timer = new CountdownTimer(duration);
        timer.OnTimerStop += () => MarkedForRemoval = true;
        timer.Start();
    }

    public void Update(float deltaTime) => timer?.Tick(deltaTime);

    public abstract void Handle(object sender, Query query);
    public void Dispose()
    {
        OnDisposed.Invoke(this);
    }
}
