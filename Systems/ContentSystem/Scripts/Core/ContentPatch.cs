using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace JG.GameContent
{
    public sealed class ContentPatch
    {
        public string TargetId;
        public string SourceMod;
        public string SourceFile;
        public List<PatchOperation> Ops = new();
    }

    public sealed class PatchOperation
    {
        public string Op;
        public string Path;
        public JToken Value;
        public JArray Values;
        public int? Index;
    }
}
