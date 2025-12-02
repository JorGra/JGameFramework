using UnityEngine;

public interface IInventoryContext
{
    bool TryGet<TService>(out TService service);

}
