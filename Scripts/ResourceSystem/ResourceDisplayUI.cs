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

    private void OnEnable()
    {
        if (resource != null && iconImage != null)
            iconImage.sprite = resource.icon;

        amountText.text = ResourceManager.Instance.GetResourceAmount(playerId, resource.Id).ToString();

        this.SubscribeEvent<ResourceChangedEvent>(OnResourceChanged);
    }

    private void OnResourceChanged(ResourceChangedEvent e)
    {
        if (e.PlayerId == playerId && e.ResourceId == resource.Id)
        {
            amountText.text = e.NewAmount.ToString();
        }
    }
}
