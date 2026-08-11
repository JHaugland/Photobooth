using System;
using UnityEditor;

namespace Photobooth.Editor
{
    internal static class PhotoboothAssetPaths
    {
        const string DefaultProfileGuid = "3d15b3d160492174881d114df16a62d0";
        const string StagePrefabGuid = "f0d26abcb715a7142b36b334abcc28e3";
        const string StageSceneGuid = "ec114b4d9d1746a4dbba10206a4f009e";

        internal const string UserAssetDirectory = "Assets/Photobooth";
        internal const string UserProfilePath =
            UserAssetDirectory + "/DefaultPhotoboothProfile.asset";
        internal const string WritableStageDirectory =
            UserAssetDirectory + "/Internal";
        internal const string WritableStageScenePath =
            WritableStageDirectory + "/PhotoboothCaptureStage.unity";

        internal static string DefaultProfilePath =>
            Resolve(DefaultProfileGuid, "default profile");

        internal static string StagePrefabPath =>
            Resolve(StagePrefabGuid, "stage prefab");

        internal static string StageScenePath =>
            Resolve(StageSceneGuid, "staging scene");

        internal static string EnsureWritableStageScene()
        {
            string sourcePath = StageScenePath;
            if (!sourcePath.StartsWith("Packages/", StringComparison.Ordinal))
                return sourcePath;

            EnsureUserAssetDirectory();
            if (!AssetDatabase.IsValidFolder(WritableStageDirectory))
                AssetDatabase.CreateFolder(UserAssetDirectory, "Internal");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    WritableStageScenePath) != null)
            {
                return WritableStageScenePath;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, WritableStageScenePath))
            {
                throw new InvalidOperationException(
                    "Could not create Photobooth's writable staging scene at " +
                    $"'{WritableStageScenePath}'. Delete any conflicting asset " +
                    "at that path and try again.");
            }

            AssetDatabase.SaveAssets();
            return WritableStageScenePath;
        }

        internal static void EnsureUserAssetDirectory()
        {
            if (!AssetDatabase.IsValidFolder(UserAssetDirectory))
                AssetDatabase.CreateFolder("Assets", "Photobooth");
        }

        static string Resolve(string guid, string assetName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException(
                    $"The Photobooth {assetName} is missing. Reinstall the package.");
            }

            return path;
        }
    }
}
