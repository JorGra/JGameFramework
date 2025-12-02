using System.Collections.Generic;
using System.IO;
using System;

namespace JG.Modding
{
    public interface IModSource { IEnumerable<IModHandle> Discover(); }
    public interface IModHandle { string Path { get; } Stream OpenFile(string rel); }
    public interface IManifestReader { ModManifest ReadManifest(IModHandle handle); }
    public interface IStateStore { ModStateTable Load(); void Save(ModStateTable table); }
    public interface IContentImporter { void Import(IModHandle handle); }   // game-specific
}
