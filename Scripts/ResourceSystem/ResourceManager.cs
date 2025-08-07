using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<int, PlayerResourceInventory> playerInventories = new Dictionary<int, PlayerResourceInventory>();

    public event Action<int, string, int> OnResourceChanged; // playerId, resourceId, newAmount

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterPlayer(int playerId)
    {
        if (!playerInventories.ContainsKey(playerId))
            playerInventories[playerId] = new PlayerResourceInventory(playerId);
    }

    public void AddResource(int playerId, Resource resource, int amount)
    {
        EnsurePlayerExists(playerId);
        playerInventories[playerId].AddResource(resource.id, amount);
        OnResourceChanged?.Invoke(playerId, resource.id, playerInventories[playerId].GetAmount(resource.id));
    }

    public bool RemoveResource(int playerId, Resource resource, int amount)
    {
        EnsurePlayerExists(playerId);
        bool success = playerInventories[playerId].RemoveResource(resource.id, amount);
        if (success)
            OnResourceChanged?.Invoke(playerId, resource.id, playerInventories[playerId].GetAmount(resource.id));
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

//public class ExampleShop : MonoBehaviour
//{
//    public Resource coins = new Resource("coins", "Coins");

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