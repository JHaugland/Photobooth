using System.Collections.Generic;

namespace Photobooth.Editor.Discovery
{
    public sealed class PrefabDiscoveryResult
    {
        readonly IReadOnlyList<PrefabDiscoveryEntry> entries;

        public string SourceFolderPath { get; }
        public bool IncludedSubfolders { get; }
        public IReadOnlyList<PrefabDiscoveryEntry> Entries => entries;
        public int Count => entries.Count;

        internal PrefabDiscoveryResult(
            string sourceFolderPath,
            bool includedSubfolders,
            IReadOnlyList<PrefabDiscoveryEntry> discoveredEntries)
        {
            SourceFolderPath = sourceFolderPath;
            IncludedSubfolders = includedSubfolders;
            entries = discoveredEntries;
        }
    }
}
