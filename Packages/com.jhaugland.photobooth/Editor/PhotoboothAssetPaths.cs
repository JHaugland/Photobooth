using System;
using UnityEditor;

namespace Photobooth.Editor
{
    internal static class PhotoboothAssetPaths
    {
        const string DefaultProfileGuid = "3d15b3d160492174881d114df16a62d0";
        const string StagePrefabGuid = "f0d26abcb715a7142b36b334abcc28e3";
        const string StageSceneGuid = "ec114b4d9d1746a4dbba10206a4f009e";

        internal static string DefaultProfilePath =>
            Resolve(DefaultProfileGuid, "default profile");

        internal static string StagePrefabPath =>
            Resolve(StagePrefabGuid, "stage prefab");

        internal static string StageScenePath =>
            Resolve(StageSceneGuid, "staging scene");

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
