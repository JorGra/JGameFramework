using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JG.GameContent.Diagnostics
{
    public sealed class DiagnosticReport : IDiagnosticSink
    {
        readonly ConcurrentBag<ContentDiagnostic> _items = new();

        public void Report(ContentDiagnostic diagnostic)
        {
            if (diagnostic != null)
                _items.Add(diagnostic);
        }

        public IReadOnlyList<ContentDiagnostic> All => _items.ToArray();

        public int ErrorCount => _items.Count(d => d.Severity == DiagnosticSeverity.Error);
        public int WarningCount => _items.Count(d => d.Severity == DiagnosticSeverity.Warning);
        public bool HasErrors => _items.Any(d => d.Severity == DiagnosticSeverity.Error);

        public IEnumerable<ContentDiagnostic> ForMod(string modId)
            => _items.Where(d => d.ModId == modId);

        public IEnumerable<ContentDiagnostic> ByCategory(DiagnosticCategory cat)
            => _items.Where(d => d.Category == cat);

        public IEnumerable<ContentDiagnostic> BySeverity(DiagnosticSeverity sev)
            => _items.Where(d => d.Severity == sev);

        public IEnumerable<ContentDiagnostic> ForDef(string defId)
            => _items.Where(d => d.DefId == defId);

        public void Clear()
        {
            while (_items.TryTake(out _)) { }
        }

        public string ToFormattedText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Diagnostic Report: {ErrorCount} error(s), {WarningCount} warning(s)");
            sb.AppendLine(new string('-', 60));

            foreach (var d in _items.OrderByDescending(x => x.Severity))
            {
                sb.Append($"[{d.Severity}] [{d.Category}]");
                if (!string.IsNullOrEmpty(d.ModId)) sb.Append($" Mod={d.ModId}");
                if (!string.IsNullOrEmpty(d.DefId)) sb.Append($" Def={d.DefId}");
                sb.AppendLine();
                sb.AppendLine($"  Message: {d.Message}");
                if (!string.IsNullOrEmpty(d.FilePath))
                    sb.AppendLine($"  File: {d.FilePath}" + (d.LineNumber >= 0 ? $":{d.LineNumber}" : ""));
                if (!string.IsNullOrEmpty(d.FieldPath))
                    sb.AppendLine($"  Field: {d.FieldPath}");
                if (!string.IsNullOrEmpty(d.ExpectedValue))
                    sb.AppendLine($"  Expected: {d.ExpectedValue}");
                if (!string.IsNullOrEmpty(d.ActualValue))
                    sb.AppendLine($"  Actual: {d.ActualValue}");
                if (!string.IsNullOrEmpty(d.Detail))
                    sb.AppendLine($"  Detail: {d.Detail}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");
            var items = _items.ToArray();
            for (int i = 0; i < items.Length; i++)
            {
                var d = items[i];
                sb.Append("  {");
                sb.Append($"\"severity\":\"{d.Severity}\",");
                sb.Append($"\"category\":\"{d.Category}\",");
                sb.Append($"\"modId\":\"{Escape(d.ModId)}\",");
                sb.Append($"\"filePath\":\"{Escape(d.FilePath)}\",");
                sb.Append($"\"defId\":\"{Escape(d.DefId)}\",");
                sb.Append($"\"fieldPath\":\"{Escape(d.FieldPath)}\",");
                sb.Append($"\"lineNumber\":{d.LineNumber},");
                sb.Append($"\"message\":\"{Escape(d.Message)}\",");
                sb.Append($"\"detail\":\"{Escape(d.Detail)}\",");
                sb.Append($"\"expectedValue\":\"{Escape(d.ExpectedValue)}\",");
                sb.Append($"\"actualValue\":\"{Escape(d.ActualValue)}\"");
                sb.Append("}");
                if (i < items.Length - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("]");
            return sb.ToString();
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
