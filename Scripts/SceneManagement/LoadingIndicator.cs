using DG.Tweening;
using TMPro;
using UnityEngine;

public class LoadingIndicator : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private RectTransform loadingFill; // New image that will scale for progress
    [SerializeField] private GameObject loadingImage; // The spinning loading image
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        container.SetActive(false);
    }

    public void SetLoadingIndicator(float val, string text = null)
    {
        // Clamp value between 0 and 1 to avoid over-scaling
        val = Mathf.Clamp01(val);

        // Scale the loadingFill image on the x-axis from 0 to 1
        loadingFill.DOScaleX(val, 0.5f); // Gradually change the scale on x-axis over 0.5 seconds

        if (text != null && !string.IsNullOrEmpty(text))
            this.text.text = text;
    }

    public void ToggleLoadingScreen(bool enable)
    {
        if (enable)
        {
            container.SetActive(true);
            loadingFill.localScale = new Vector3(0, 1, 1); // Start from zero width for fill
            // Fade in and pop in effect
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1, 1.5f);
            container.transform.localScale = Vector3.zero;
            container.transform.DOScale(1, 0.5f).SetEase(Ease.OutBack);

            loadingImage.transform.DOLocalRotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360)
                                .SetLoops(-1, LoopType.Incremental);
        }
        else
        {
            // Fade out and pop out effect
            canvasGroup.DOFade(0, 0.5f).OnComplete(() => container.SetActive(false));
            container.transform.DOScale(0, 0.5f).SetEase(Ease.InBack);

            loadingImage.transform.DOKill();
        }
    }
}
