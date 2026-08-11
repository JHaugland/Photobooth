using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Configuration
{
    [CustomEditor(typeof(PhotoboothProfile))]
    internal sealed class PhotoboothProfileInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("sourceFolder"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("includeSubfolders"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            SerializedProperty outputMode =
                serializedObject.FindProperty("outputPathMode");
            EditorGUILayout.PropertyField(outputMode);
            string outputPropertyName =
                outputMode.enumValueIndex == (int)OutputPathMode.Absolute
                    ? "absoluteOutputPath"
                    : "projectRelativeOutputPath";
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(outputPropertyName));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("existingFilePolicy"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("filenamePattern"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("captureWidth"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("captureHeight"));
            SerializedProperty transparentBackground =
                serializedObject.FindProperty("transparentBackground");
            EditorGUILayout.PropertyField(transparentBackground);
            if (!transparentBackground.boolValue)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("backgroundColor"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("stagePrefab"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Camera Presets", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("cameraPresets"),
                true);

            serializedObject.ApplyModifiedProperties();

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
