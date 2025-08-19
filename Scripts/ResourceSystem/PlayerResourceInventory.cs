using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerResourceInventory
{
    public int playerId;
    private Dictionary<string, int> resourceAmounts = new Dictionary<string, int>();

    public PlayerResourceInventory(int playerId)
    {
        this.playerId = playerId;
    }

    public int GetAmount(string resourceId)
    {
        return resourceAmounts.TryGetValue(resourceId, out var amount) ? amount : 0;
    }

    public void AddResource(string resourceId, int amount)
    {
        if (!resourceAmounts.ContainsKey(resourceId))
            resourceAmounts[resourceId] = 0;

        resourceAmounts[resourceId] += amount;
        if (resourceAmounts[resourceId] < 0) resourceAmounts[resourceId] = 0;
    }

    public bool RemoveResource(string resourceId, int amount)
    {
        if (GetAmount(resourceId) < amount) return false;
        resourceAmounts[resourceId] -= amount;
        return true;
    }

    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(resourceAmounts);
    }
}
