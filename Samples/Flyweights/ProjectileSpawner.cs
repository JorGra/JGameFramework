using System.Collections;
using System.Collections.Generic;
using JG.Flyweights;
using UnityEngine;


namespace JG.Samples
{

public class ProjectileSpawner : MonoBehaviour
{

    public JG.Samples.ProjectileSettings projectileSettings;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var p = FlyweightFactory.Spawn(projectileSettings);

        }
    }
}

}