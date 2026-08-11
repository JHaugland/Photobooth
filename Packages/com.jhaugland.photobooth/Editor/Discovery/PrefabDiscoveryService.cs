using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Photobooth.Editor.Discovery
{
    public static class PrefabDiscoveryService
    {
        public static PrefabDiscoveryResult Discover(
            DefaultAsset sourceFolder,
            bool includeSubfolders)
        {
            if (sourceFolder == null)
                throw new ArgumentNullException(nameof(sourceFolder));

            string folderPath = AssetDatabase.GetAssetPath(sourceFolder);
            return Discover(folderPath, includeSubfolders);
        }

        public static PrefabDiscoveryResult Discover(
            string sourceFolderPath,
            bool includeSubfolders)
        {
            string folderPath = NormalizeAndValidateFolder(sourceFolderPath);
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { folderPath });

            var entries = new List<PrefabDiscoveryEntry>(guids.Length);
            var discoveredPaths = new HashSet<string>(StringComparer.Ordinal);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    throw new InvalidOperationException(
                        $"Asset Database returned no path for prefab GUID '{guid}'.");
                }

                assetPath = NormalizePath(assetPath);
                if (!includeSubfolders &&
                    !string.Equals(
                        GetDirectoryPath(assetPath),
                        folderPath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (discoveredPaths.Add(assetPath))
                    entries.Add(new PrefabDiscoveryEntry(guid, assetPath));
            }

            entries.Sort(CompareEntries);
            return new PrefabDiscoveryResult(
                folderPath,
                includeSubfolders,
                entries.AsReadOnly());
        }

        static string NormalizeAndValidateFolder(string sourceFolderPath)
        {
            if (string.IsNullOrWhiteSpace(sourceFolderPath))
            {
                throw new ArgumentException(
                    "A prefab source folder is required.",
                    nameof(sourceFolderPath));
            }

            string folderPath = NormalizePath(sourceFolderPath).TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                throw new ArgumentException(
                    $"Prefab source path is not a valid project folder: '{folderPath}'.",
                    nameof(sourceFolderPath));
            }

            return folderPath;
        }

        static int CompareEntries(
            PrefabDiscoveryEntry left,
            PrefabDiscoveryEntry right)
        {
            int comparison = StringComparer.OrdinalIgnoreCase.Compare(
                left.AssetPath,
                right.AssetPath);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.AssetPath, right.AssetPath);
        }

        static string GetDirectoryPath(string assetPath) =>
            NormalizePath(Path.GetDirectoryName(assetPath) ?? string.Empty);

        static string NormalizePath(string path) =>
            path.Replace('\\', '/');
    }
}
