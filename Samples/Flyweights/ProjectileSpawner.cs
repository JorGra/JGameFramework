using System.Collections;
using System.Collections.Generic;
using JG.Flyweights;
using JG.Samples;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{

    public ProjectileSettings projectileSettings;

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
