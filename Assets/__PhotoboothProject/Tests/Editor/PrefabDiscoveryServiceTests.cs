using System;
using System.Linq;
using NUnit.Framework;
using Photobooth.Editor.Discovery;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Tests
{
    internal sealed class PrefabDiscoveryServiceTests
    {
        const string TestRoot = "Assets/__PhotoboothProject/Tests/DiscoveryFixtures";
        const string NestedFolder = TestRoot + "/Nested";

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(TestRoot);
            AssetDatabase.CreateFolder(
                "Assets/__PhotoboothProject/Tests",
                "DiscoveryFixtures");
            AssetDatabase.CreateFolder(TestRoot, "Nested");

            CreatePrefab(TestRoot + "/Zulu.prefab");
            CreatePrefab(TestRoot + "/Alpha.prefab");
            CreatePrefab(NestedFolder + "/Nested.prefab");
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TestRoot);
        }

        [Test]
        public void Discover_WithoutSubfolders_ReturnsOnlyRootPrefabsInPathOrder()
        {
            PrefabDiscoveryResult result =
                PrefabDiscoveryService.Discover(TestRoot, false);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(
                result.Entries.Select(entry => entry.Name),
                Is.EqualTo(new[] { "Alpha", "Zulu" }));
            Assert.That(result.IncludedSubfolders, Is.False);
        }

        [Test]
        public void Discover_WithSubfolders_ReturnsNestedPrefabsInPathOrder()
        {
            PrefabDiscoveryResult result =
                PrefabDiscoveryService.Discover(TestRoot, true);
            string[] paths = result.Entries
                .Select(entry => entry.AssetPath)
                .ToArray();
            string[] sortedPaths = paths
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(paths, Is.EqualTo(sortedPaths));
            Assert.That(
                result.Entries.Any(entry => entry.Name == "Nested"),
                Is.True);
        }

        [Test]
        public void Discover_WithInvalidFolder_ThrowsClearError()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => PrefabDiscoveryService.Discover(
                    TestRoot + "/Missing",
                    true));

            StringAssert.Contains("not a valid project folder", exception.Message);
        }

        [Test]
        public void Discover_WithNullFolderAsset_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => PrefabDiscoveryService.Discover(
                    (DefaultAsset)null,
                    true));
        }

        [Test]
        public void Entry_LoadPrefab_LoadsDiscoveredAsset()
        {
            PrefabDiscoveryEntry entry =
                PrefabDiscoveryService.Discover(TestRoot, false).Entries[0];

            GameObject prefab = entry.LoadPrefab();

            Assert.That(prefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(entry.AssetPath));
        }

        static void CreatePrefab(string path)
        {
            var instance = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            try
            {
                PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
