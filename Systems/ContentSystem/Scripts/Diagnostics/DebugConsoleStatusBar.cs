using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.GameContent.Debugging
{
    public sealed class DebugConsoleStatusBar : UIPanel
    {
        [SerializeField] ConsoleLogCapture logCapture;
        [SerializeField] DebugConsolePanel consolePanel;
        [SerializeField] TMP_Text errorCountText;
        [SerializeField] TMP_Text warningCountText;
        [SerializeField] TMP_Text infoCountText;
        [SerializeField] Button barButton;
        [SerializeField] Image errorIcon;
        [SerializeField] Image warningIcon;
        [SerializeField] Image infoIcon;

        int _lastErrorCount;
        float _errorPulseTimer;
        int _lastClickFrame = -1;

        static readonly Color ErrorPulse = new(1f, 0.3f, 0.3f, 1f);
        static readonly Color ErrorNormal = new(0.8f, 0.2f, 0.2f, 1f);
        static readonly Color WarningNormal = new(0.9f, 0.75f, 0.1f, 1f);
        static readonly Color InfoNormal = new(0.5f, 0.7f, 0.9f, 1f);

        void Awake()
        {
            if (barButton != null)
                barButton.onClick.AddListener(OnBarClicked);

            // Status bar starts visible
            IsOpen = true;
        }

        void Update()
        {
            if (logCapture == null || logCapture.Buffer == null) return;

            var buffer = logCapture.Buffer;

            if (errorCountText != null) errorCountText.text = buffer.ErrorCount.ToString();
            if (warningCountText != null) warningCountText.text = buffer.WarningCount.ToString();
            if (infoCountText != null) infoCountText.text = buffer.LogCount.ToString();

            // Error pulse effect
            if (buffer.ErrorCount > _lastErrorCount)
            {
                _errorPulseTimer = 1f;
                _lastErrorCount = buffer.ErrorCount;
            }

            if (errorIcon != null)
            {
                if (_errorPulseTimer > 0f)
                {
                    _errorPulseTimer -= Time.unscaledDeltaTime * 2f;
                    errorIcon.color = Color.Lerp(ErrorNormal, ErrorPulse, _errorPulseTimer);
                }
                else
                {
                    errorIcon.color = ErrorNormal;
                }
            }

            if (warningIcon != null) warningIcon.color = WarningNormal;
            if (infoIcon != null) infoIcon.color = InfoNormal;
        }

        void OnBarClicked()
        {
            if (consolePanel == null) return;
            // Guard against duplicate EventSystem clicks (EventSystem + MultiplayerEventSystem)
            if (_lastClickFrame == Time.frameCount) return;
            _lastClickFrame = Time.frameCount;

            if (consolePanel.IsOpen)
                consolePanel.Close();
            else
                consolePanel.Open();
        }
    }
}
