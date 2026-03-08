using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JG.GameContent.Debugging
{
    public sealed class DebugConsoleStatusBar : UIPanel
    {
        [SerializeField] ConsoleLogCapture logCapture;
        [SerializeField] DebugConsolePanel consolePanel;
        [SerializeField] DebugCommandPanel commandPanel;
        [SerializeField] TMP_Text errorCountText;
        [SerializeField] TMP_Text warningCountText;
        [SerializeField] TMP_Text infoCountText;
        [SerializeField] Button barButton;
        [SerializeField] Button commandPanelButton;
        [SerializeField] Image errorIcon;
        [SerializeField] Image warningIcon;
        [SerializeField] Image infoIcon;

        int _lastErrorCount;
        float _errorPulseTimer;
        int _lastClickFrame = -1;
        int _lastCmdClickFrame = -1;

        static readonly Color ErrorPulse = new(1f, 0.3f, 0.3f, 1f);
        static readonly Color ErrorNormal = new(0.8f, 0.2f, 0.2f, 1f);
        static readonly Color WarningNormal = new(0.9f, 0.75f, 0.1f, 1f);
        static readonly Color InfoNormal = new(0.5f, 0.7f, 0.9f, 1f);

        void Awake()
        {
            if (barButton != null)
                barButton.onClick.AddListener(OnBarClicked);
            if (commandPanelButton != null)
                commandPanelButton.onClick.AddListener(OnCommandPanelClicked);
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

        /// <summary>
        /// Show or hide the status bar. When hiding, also closes the console panel.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (visible)
            {
                gameObject.SetActive(true);
                IsOpen = true;
            }
            else
            {
                if (consolePanel != null && consolePanel.IsOpen)
                    consolePanel.Close();
                if (commandPanel != null && commandPanel.IsOpen)
                    commandPanel.Close();
                IsOpen = false;
                gameObject.SetActive(false);
            }
        }

        void OnBarClicked()
        {
            if (consolePanel == null) return;
            if (_lastClickFrame == Time.frameCount) return;
            _lastClickFrame = Time.frameCount;

            if (consolePanel.IsOpen)
                consolePanel.Close();
            else
                consolePanel.Open();
        }

        void OnCommandPanelClicked()
        {
            if (commandPanel == null) return;
            if (_lastCmdClickFrame == Time.frameCount) return;
            _lastCmdClickFrame = Time.frameCount;

            if (commandPanel.IsOpen)
                commandPanel.Close();
            else
                commandPanel.Open();
        }
    }
}
