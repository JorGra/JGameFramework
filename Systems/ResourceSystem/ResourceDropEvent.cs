using UnityEngine;

public struct ResourceDropEvent : IEvent
{
    public string ResourceId;
    public int Amount;
    public Vector3 WorldPosition;
    public int? PlayerId;
}
