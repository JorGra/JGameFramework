using JG.Tools.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SceneLoadHelper : MonoBehaviour
{

    SceneLoader loader;

    [SerializeField] bool fadeIn = true;
    [SerializeField] bool fadeOut = true;

    public void LoadSceneGroup(string sceneGroupName)
    {
        if (!loader)
            loader = SceneLoader.Instance;

        loader?.LoadSceneGroup(sceneGroupName, fadeIn, fadeOut);
    }
}
