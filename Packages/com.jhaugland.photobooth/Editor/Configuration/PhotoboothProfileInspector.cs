using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Configuration
{
    [CustomEditor(typeof(PhotoboothProfile))]
    internal sealed class PhotoboothProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var profile = (PhotoboothProfile)target;
            DrawValidationMessages(profile);
        }

        static void DrawValidationMessages(PhotoboothProfile profile)
        {
            if (profile.SourceFolder != null &&
                !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(profile.SourceFolder)))
            {
                EditorGUILayout.HelpBox(
                    "Source Folder must reference a folder in the project.",
                    MessageType.Error);
            }

            string outputPath = profile.OutputPathMode == OutputPathMode.ProjectRelative
                ? profile.ProjectRelativeOutputPath
                : profile.AbsoluteOutputPath;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                EditorGUILayout.HelpBox(
                    "Configure an output path before running a capture.",
                    MessageType.Warning);
            }

            if (string.IsNullOrWhiteSpace(profile.FilenamePattern))
            {
                EditorGUILayout.HelpBox(
                    "Filename Pattern cannot be empty.",
                    MessageType.Error);
            }
            else if (!profile.FilenamePattern.Contains("{prefab}"))
            {
                EditorGUILayout.HelpBox(
                    "Filename Pattern should contain {prefab} to avoid collisions between assets.",
                    MessageType.Warning);
            }

            if (profile.CameraPresets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Add at least one camera preset.",
                    MessageType.Warning);
            }
        }
    }
}
