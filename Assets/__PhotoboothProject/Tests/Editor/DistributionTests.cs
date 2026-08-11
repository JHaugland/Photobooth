using System.IO;
using System.Linq;
using NUnit.Framework;
using Photobooth.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Tests
{
    internal sealed class DistributionTests
    {
        const string PackageRoot = "Packages/com.jhaugland.photobooth";

        [Test]
        public void PackageAssets_ResolveByGuidAfterRelocation()
        {
            Assert.That(
                PhotoboothAssetPaths.DefaultProfilePath,
                Does.StartWith(PackageRoot));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<PhotoboothProfile>(
                    PhotoboothAssetPaths.DefaultProfilePath),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PhotoboothAssetPaths.StagePrefabPath),
                Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    PhotoboothAssetPaths.StageScenePath),
                Is.Not.Null);
        }

        [Test]
        public void DefaultProfile_UsesSafeProjectAgnosticSettings()
        {
            PhotoboothProfile profile =
                AssetDatabase.LoadAssetAtPath<PhotoboothProfile>(
                    PhotoboothAssetPaths.DefaultProfilePath);

            Assert.That(profile.SourceFolder, Is.Null);
            Assert.That(profile.OutputPathMode, Is.EqualTo(OutputPathMode.ProjectRelative));
            Assert.That(profile.ProjectRelativeOutputPath, Is.EqualTo("PhotoboothCaptures"));
            Assert.That(profile.AbsoluteOutputPath, Is.Empty);
            Assert.That(profile.ExistingFilePolicy, Is.EqualTo(ExistingFilePolicy.Skip));
        }

        [Test]
        public void ProductionAssembly_IsEditorOnly()
        {
            string[] assemblyDefinitions = Directory.GetFiles(
                Path.GetFullPath(PackageRoot),
                "*.asmdef",
                SearchOption.AllDirectories);

            Assert.That(assemblyDefinitions, Has.Length.EqualTo(1));
            string assemblyDefinition = File.ReadAllText(assemblyDefinitions[0]);
            StringAssert.Contains("\"includePlatforms\"", assemblyDefinition);
            StringAssert.Contains("\"Editor\"", assemblyDefinition);
        }

        [Test]
        public void Package_ContainsNoTestsThirdPartyAssetsOrBinaries()
        {
            string[] paths = Directory.GetFiles(
                    Path.GetFullPath(PackageRoot),
                    "*",
                    SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .ToArray();

            Assert.That(paths, Has.None.Matches<string>(
                path => path.Contains("/Tests/") ||
                        path.Contains("/Test/") ||
                        path.Contains("/Samples/") ||
                        path.Contains("/Synty/")));
            Assert.That(paths, Has.None.Matches<string>(
                path => path.EndsWith(".dll") ||
                        path.EndsWith(".so") ||
                        path.EndsWith(".dylib")));
        }

        [Test]
        public void StagingScene_IsNotIncludedInBuildSettings()
        {
            string writableStageScene =
                PhotoboothAssetPaths.EnsureWritableStageScene();

            Assert.That(
                EditorBuildSettings.scenes.Select(scene => scene.path),
                Does.Not.Contain(PhotoboothAssetPaths.StageScenePath));
            Assert.That(
                EditorBuildSettings.scenes.Select(scene => scene.path),
                Does.Not.Contain(writableStageScene));
        }

        [Test]
        public void StagingScene_FromPackageIsCopiedToWritableProjectFolder()
        {
            AssetDatabase.DeleteAsset(PhotoboothAssetPaths.WritableStageDirectory);

            string firstPath = PhotoboothAssetPaths.EnsureWritableStageScene();
            string firstGuid = AssetDatabase.AssetPathToGUID(firstPath);
            string secondPath = PhotoboothAssetPaths.EnsureWritableStageScene();

            Assert.That(
                PhotoboothAssetPaths.StageScenePath,
                Does.StartWith("Packages/"));
            Assert.That(
                firstPath,
                Is.EqualTo(PhotoboothAssetPaths.WritableStageScenePath));
            Assert.That(secondPath, Is.EqualTo(firstPath));
            Assert.That(AssetDatabase.AssetPathToGUID(secondPath), Is.EqualTo(firstGuid));
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(secondPath),
                Is.Not.Null);
        }
    }
}
