using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides access to the global StatRegistry. The StatRegistry asset must be placed
/// in a Resources folder and named "StatRegistry".
/// </summary>
public static class StatRegistryProvider
{
    private static StatRegistry instance;

    public static StatRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<StatRegistry>("StatRegistry");
                if (instance == null)
                {
                    Debug.LogError("StatRegistry asset not found in the Resources folder. Please create one and name it 'StatRegistry'.");
                }
            }
            return instance;
        }
    }
}