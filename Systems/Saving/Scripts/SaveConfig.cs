using System;
using System.Collections.Generic;
using UnityEngine;

namespace JGameFramework.Saving
{
    [CreateAssetMenu(fileName = "SaveConfig", menuName = "JGameFramework/Saving/SaveConfig")]
    public class SaveConfig : ScriptableObject
    {
        [SerializeField] private List<SaveCaseConfig> cases = new();
        [SerializeField] private string defaultSlotId = "slot_default";

        public IReadOnlyList<SaveCaseConfig> Cases => cases;
        public string DefaultSlotId => defaultSlotId;
    }

    [Serializable]
    public struct SaveCaseConfig
    {
        public SaveCaseId CaseId;
        public bool Cached;
    }
}
