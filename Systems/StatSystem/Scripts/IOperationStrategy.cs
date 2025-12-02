using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IOperationStrategy
{
    float Calculate(float value);
}

public class AddOperation : IOperationStrategy
{
    readonly float value;

    public AddOperation(float value)
    {
        this.value = value;
    }

    public float Calculate(float value) => value + this.value;
}

public class MultiplyOperation : IOperationStrategy
{
    readonly float value;
    public MultiplyOperation(float value)
    {
        this.value = value;
    }
    public float Calculate(float value) => value * this.value;
}

public class PercentageOperation : IOperationStrategy
{
    readonly float value;
    public PercentageOperation(float value)
    {
        this.value = value;
    }
    public float Calculate(float value) => value * (1 + this.value / 100);
}