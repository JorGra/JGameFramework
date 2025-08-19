using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceDisplayUI : MonoBehaviour
{
    [Header("Setup")]
    public int playerId;
    public ResourceDef resource;
    public Image iconImage;
    public TMP_Text amountText;

    private EventBinding<ResourceChangedEvent> resourceChangedBinding;

    private void OnEnable()
    {
        // Set initial values
        if (resource != null && iconImage != null)
            iconImage.sprite = resource.icon;

        amountText.text = ResourceManager.Instance.GetResourceAmount(playerId, resource.Id).ToString();

        // Subscribe to event bus
        resourceChangedBinding = new EventBinding<ResourceChangedEvent>(OnResourceChanged);
        EventBus<ResourceChangedEvent>.Register(resourceChangedBinding);
    }

    private void OnDisable()
    {
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
    }

    private void OnResourceChanged(ResourceChangedEvent e)
    {
        if (e.PlayerId == playerId && e.ResourceId == resource.Id)
        {
            amountText.text = e.NewAmount.ToString();
        }
    }
}
