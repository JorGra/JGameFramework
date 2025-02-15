using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A profile that defines the base stat values for a unit.
/// A unit can have only some of the available stats.
/// </summary>
[CreateAssetMenu(menuName = "Gameplay/Stats/Unit Stats Profile", fileName = "NewUnitStatsProfile")]
public class StatsProfile : ScriptableObject
{
    [Serializable]
    public struct StatEntry
    {
        [Tooltip("Reference to the global stat definition.")]
        public StatDefinition statDefinition;

        [Tooltip("The base value for this unit for the given stat.")]
        public float baseValue;
    }

    [Tooltip("List of stat entries that define this unit's base stats.")]
    public List<StatEntry> statEntries = new List<StatEntry>();
}