using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Profiling;

namespace JG.GameContent.Diagnostics
{
    /// <summary>
    /// Accumulates per-phase timings across one <c>ModLoader.Reload()</c> run.
    /// Each phase shows up as a <see cref="ProfilerMarker"/> ("ModLoad.&lt;phase&gt;")
    /// in the Unity Profiler and as an accumulated total in <see cref="Summary"/>.
    /// </summary>
    public static class LoadProfiler
    {
        public const string Discover = "Discover";
        public const string ResolveOrder = "ResolveOrder";
        public const string Assemblies = "Assemblies";
        public const string Import = "Import";
        public const string JsonRead = "JsonRead";
        public const string Deserialize = "Deserialize";
        public const string AssetInject = "AssetInject";
        public const string ImageDecode = "ImageDecode";
        public const string AudioDecode = "AudioDecode";
        public const string Patches = "Patches";
        public const string Translations = "Translations";
        public const string EntryPoints = "EntryPoints";
        public const string Validation = "Validation";

        static readonly Dictionary<string, long> _ticks = new();
        static readonly Dictionary<string, ProfilerMarker> _markers = new();

        public static void Reset() => _ticks.Clear();

        public static PhaseScope Measure(string phase)
        {
            if (!_markers.TryGetValue(phase, out var marker))
                _markers[phase] = marker = new ProfilerMarker($"ModLoad.{phase}");
            marker.Begin();
            return new PhaseScope(phase, Stopwatch.GetTimestamp(), marker);
        }

        public static double Ms(string phase) =>
            _ticks.TryGetValue(phase, out var t) ? t * 1000.0 / Stopwatch.Frequency : 0.0;

        public static string Summary(params string[] phases)
        {
            var sb = new StringBuilder();
            foreach (var p in phases)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(p).Append(' ').Append(Ms(p).ToString("F0")).Append("ms");
            }
            return sb.ToString();
        }

        public readonly struct PhaseScope : IDisposable
        {
            readonly string _phase;
            readonly long _start;
            readonly ProfilerMarker _marker;

            internal PhaseScope(string phase, long start, ProfilerMarker marker)
            {
                _phase = phase;
                _start = start;
                _marker = marker;
            }

            public void Dispose()
            {
                _marker.End();
                long elapsed = Stopwatch.GetTimestamp() - _start;
                _ticks.TryGetValue(_phase, out var t);
                _ticks[_phase] = t + elapsed;
            }
        }
    }
}
