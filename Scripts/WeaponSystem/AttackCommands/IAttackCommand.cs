using System.Collections;
using System.Collections.Generic;
using JG.Flyweights;
using UnityEngine;

public interface IAttackCommand 
{
    void Execute(Vector3 spawnPosition, Quaternion spawnRotation);
}

public class ProjectileCommand : IAttackCommand
{

    private readonly ProjectileSettings settings;

    public ProjectileCommand(ProjectileSettings settings)
    {
        this.settings = settings;
    }


    public void Execute(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        FlyweightFactory.Spawn(settings, spawnPosition, spawnRotation);
    }
}
