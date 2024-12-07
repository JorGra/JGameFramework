using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JG.Tools.SceneManagement;


public class SceneLoadHelper : MonoBehaviour
{

    SceneLoader loader;

    public void LoadSceneGroup(string sceneGroupName)
    {
        if(!loader)
            loader = SceneLoader.Instance;

        loader?.LoadSceneGroup(sceneGroupName);
    }
}
