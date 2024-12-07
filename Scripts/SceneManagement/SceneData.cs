using System;
using System.Collections;
using System.Collections.Generic;
using Eflatun.SceneReference;
using UnityEngine;
using System.Linq;

namespace JG.Tools.SceneManagement
{
    [Serializable]
    public class SceneGroup
    {
        public string GroupName = "New Scene Group";
        public List<SceneData> Scenes;

        public string FindSceneNameByType(SceneType sceneType)
        {
            return Scenes.FirstOrDefault(sceneData => sceneData.Type == sceneType)?.Name;
        }
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public string Name => Reference.Name;
        public SceneType Type;
    }

    public enum SceneType
    {
        ActiveScene,
        MainMenu,
        Management,
        UserInterface,
        HUD,
        Environment,
        Tooling,
    }
}
