using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackCommand 
{
    void Execute(Vector3 spawnPosition, Quaternion spawnRotation);
}
