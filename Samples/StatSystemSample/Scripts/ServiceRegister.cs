using UnityEngine;
using UnityServiceLocator;

namespace JG.Samples { 
public class ServiceRegister : MonoBehaviour
{
    // Start is called before the first frame update
    private void Awake()
    {
        ServiceLocator.ForSceneOf(this).Register<IStatModifierFactory>(new StatModifierFactory());
    }
}

}