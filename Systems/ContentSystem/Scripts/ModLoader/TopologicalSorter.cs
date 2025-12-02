using System.Collections.Generic;
using System.Linq;

namespace JG.Modding
{
    internal static class TopologicalSorter
    {
        public static bool TrySort(
            IList<LoadedMod> mods,
            out List<LoadedMod> sorted,
            out List<(string a, string b)> cycles)
        {
            sorted = new();
            cycles = new();

            var id2Mod = mods.ToDictionary(m => m.Manifest.id);
            var inDeg = mods.ToDictionary(m => m.Manifest.id, _ => 0);
            var graph = new Dictionary<string, List<string>>();

            // build graph edges: A → B means "A must load before B"
            void AddEdge(string a, string b)
            {
                if (!graph.TryGetValue(a, out var list))
                    graph[a] = list = new List<string>();
                list.Add(b);
                inDeg[b]++;
            }

            foreach (var m in mods)
            {
                foreach (var before in m.Manifest.loadBefore) AddEdge(m.Manifest.id, before);
                foreach (var after in m.Manifest.loadAfter) AddEdge(after, m.Manifest.id);
            }

            // Kahn
            var q = new Queue<string>(inDeg.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            while (q.Count > 0)
            {
                var id = q.Dequeue();
                sorted.Add(id2Mod[id]);

                if (!graph.TryGetValue(id, out var adj)) continue;
                foreach (var v in adj)
                    if (--inDeg[v] == 0) q.Enqueue(v);
            }

            bool ok = sorted.Count == mods.Count;
            if (!ok)
                cycles = inDeg.Where(kv => kv.Value > 0).Select(kv => kv.Key)
                              .SelectMany(a => graph[a].Select(b => (a, b)))
                              .ToList();
            return ok;
        }
    }
}
