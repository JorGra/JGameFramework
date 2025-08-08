using JG.Tools;
using System.Collections.Generic;

public class ResourceManager : Singleton<ResourceManager>
{

    private readonly Dictionary<int, PlayerResourceInventory> playerInventories
        = new Dictionary<int, PlayerResourceInventory>();

    public void RegisterPlayer(int playerId)
    {
        if (!playerInventories.ContainsKey(playerId))
            playerInventories[playerId] = new PlayerResourceInventory(playerId);
    }

    public void AddResource(int playerId, string resourceId, int amount)
    {
        EnsurePlayerExists(playerId);
        playerInventories[playerId].AddResource(resourceId, amount);

        EventBus<ResourceChangedEvent>.Raise(
            new ResourceChangedEvent(playerId, resourceId, playerInventories[playerId].GetAmount(resourceId))
        );
    }

    public void AddResourceToAll(string resourceId, int amount)
    {
        foreach (var playerId in playerInventories.Keys)
        {
            AddResource(playerId, resourceId, amount);
            EventBus<ResourceChangedEvent>.Raise(
    new ResourceChangedEvent(playerId, resourceId, playerInventories[playerId].GetAmount(resourceId))
);
        }
    }

    public bool RemoveResource(int playerId, string resourceId, int amount)
    {
        EnsurePlayerExists(playerId);
        bool success = playerInventories[playerId].RemoveResource(resourceId, amount);

        if (success)
        {
            EventBus<ResourceChangedEvent>.Raise(
                new ResourceChangedEvent(playerId, resourceId, playerInventories[playerId].GetAmount(resourceId))
            );
        }

        return success;
    }

    public int GetResourceAmount(int playerId, string resourceId)
    {
        EnsurePlayerExists(playerId);
        return playerInventories[playerId].GetAmount(resourceId);
    }

    private void EnsurePlayerExists(int playerId)
    {
        if (!playerInventories.ContainsKey(playerId))
            playerInventories[playerId] = new PlayerResourceInventory(playerId);
    }
}

public struct ResourceChangedEvent : IEvent
{
    public int PlayerId;
    public string ResourceId;
    public int NewAmount;

    public ResourceChangedEvent(int playerId, string resourceId, int newAmount)
    {
        PlayerId = playerId;
        ResourceId = resourceId;
        NewAmount = newAmount;
    }
}

//public class ExampleShop : MonoBehaviour
//{
//    public ResourceDef coins = new ResourceDef("coins", "Coins");

//    private void Start()
//    {
//        // Register two couch co-op players
//        ResourceManager.Instance.RegisterPlayer(0);
//        ResourceManager.Instance.RegisterPlayer(1);

//        // Give player 0 some coins
//        ResourceManager.Instance.AddResource(0, coins, 100);

//        // Try buying something
//        bool bought = ResourceManager.Instance.RemoveResource(0, coins, 30);
//        Debug.Log(bought ? "Purchase successful!" : "Not enough coins!");
//    }
//}