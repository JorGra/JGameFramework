using UnityEngine;

namespace JG.GameContent.Debugging
{
    public sealed class ConsoleLogCapture : MonoBehaviour
    {
        [SerializeField] int bufferCapacity = 1000;

        public ConsoleLogBuffer Buffer { get; private set; }
        public bool Paused { get; set; }

        void Awake()
        {
            Buffer = new ConsoleLogBuffer(bufferCapacity);
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        void OnDisable()
        {
            Application.logMessageReceivedThreaded -= OnLogMessage;
        }

        void OnLogMessage(string condition, string stacktrace, LogType type)
        {
            if (Paused) return;
            if (string.IsNullOrEmpty(condition)) return;
            Buffer.Add(condition, stacktrace, type);
        }
    }
}
