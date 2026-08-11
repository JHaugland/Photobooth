using System;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Discovery
{
    public readonly struct PrefabDiscoveryEntry : IEquatable<PrefabDiscoveryEntry>
    {
        public string Guid { get; }
        public string AssetPath { get; }
        public string Name { get; }

        internal PrefabDiscoveryEntry(string guid, string assetPath)
        {
            Guid = guid;
            AssetPath = assetPath;
            Name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        }

        public GameObject LoadPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Prefab could not be loaded at '{AssetPath}'.");
            }

            return prefab;
        }

        public bool Equals(PrefabDiscoveryEntry other) =>
            string.Equals(Guid, other.Guid, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is PrefabDiscoveryEntry other && Equals(other);

        public override int GetHashCode() =>
            Guid != null ? StringComparer.Ordinal.GetHashCode(Guid) : 0;

        public override string ToString() => AssetPath;
    }
}
