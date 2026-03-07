using System;
using System.Text;
using System.Threading;
using UnityEngine;

namespace JG.GameContent.Debugging
{
    public struct ConsoleLogEntry
    {
        public string Message;
        public string Stacktrace;
        public LogType Type;
        public float Timestamp;
        public int Count;
    }

    public sealed class ConsoleLogBuffer
    {
        readonly ConsoleLogEntry[] _entries;
        readonly int _capacity;
        int _head;
        int _count;
        int _version;

        int _errorCount;
        int _warningCount;
        int _logCount;

        readonly object _lock = new();

        public int Version => _version;
        public int Count { get { lock (_lock) return _count; } }
        public int ErrorCount => _errorCount;
        public int WarningCount => _warningCount;
        public int LogCount => _logCount;

        public ConsoleLogBuffer(int capacity = 1000)
        {
            _capacity = Mathf.Max(capacity, 16);
            _entries = new ConsoleLogEntry[_capacity];
        }

        public void Add(string message, string stacktrace, LogType type)
        {
            lock (_lock)
            {
                // Duplicate stacking: compare with last entry
                if (_count > 0)
                {
                    int lastIdx = (_head + _count - 1) % _capacity;
                    ref var last = ref _entries[lastIdx];
                    if (last.Type == type && string.Equals(last.Message, message, StringComparison.Ordinal))
                    {
                        last.Count++;
                        Interlocked.Increment(ref _version);
                        return;
                    }
                }

                // Track severity counts
                switch (type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        Interlocked.Increment(ref _errorCount);
                        break;
                    case LogType.Warning:
                        Interlocked.Increment(ref _warningCount);
                        break;
                    default:
                        Interlocked.Increment(ref _logCount);
                        break;
                }

                if (_count < _capacity)
                {
                    int idx = (_head + _count) % _capacity;
                    _entries[idx] = new ConsoleLogEntry
                    {
                        Message = message,
                        Stacktrace = stacktrace,
                        Type = type,
                        Timestamp = Time.realtimeSinceStartup,
                        Count = 1
                    };
                    _count++;
                }
                else
                {
                    // Overwrite oldest
                    _entries[_head] = new ConsoleLogEntry
                    {
                        Message = message,
                        Stacktrace = stacktrace,
                        Type = type,
                        Timestamp = Time.realtimeSinceStartup,
                        Count = 1
                    };
                    _head = (_head + 1) % _capacity;
                }

                Interlocked.Increment(ref _version);
            }
        }

        public ConsoleLogEntry Get(int index)
        {
            lock (_lock)
            {
                if (index < 0 || index >= _count)
                    return default;
                return _entries[(_head + index) % _capacity];
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _head = 0;
                _count = 0;
                Interlocked.Exchange(ref _errorCount, 0);
                Interlocked.Exchange(ref _warningCount, 0);
                Interlocked.Exchange(ref _logCount, 0);
                Interlocked.Increment(ref _version);
            }
        }

        public string CopyAll(bool includeStacktraces)
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _count; i++)
                {
                    var e = _entries[(_head + i) % _capacity];
                    var prefix = e.Type switch
                    {
                        LogType.Error or LogType.Exception or LogType.Assert => "[ERROR]",
                        LogType.Warning => "[WARN]",
                        _ => "[INFO]"
                    };
                    var countSuffix = e.Count > 1 ? $" (x{e.Count})" : "";
                    sb.AppendLine($"{prefix} [{e.Timestamp:F3}] {e.Message}{countSuffix}");

                    if (includeStacktraces && !string.IsNullOrEmpty(e.Stacktrace))
                        sb.AppendLine(e.Stacktrace);
                }
                return sb.ToString();
            }
        }
    }
}
