using System;
using Photobooth.Editor.Capture;
using Photobooth.Editor.Configuration;
using Photobooth.Editor.Discovery;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Window
{
    internal sealed class PhotoboothWindow : EditorWindow
    {
        [SerializeField]
        PhotoboothProfile profile;

        UnityEditor.Editor profileEditor;
        PhotoboothCaptureSession captureSession;
        CaptureBatchResult lastBatchResult;
        PrefabDiscoveryResult queue;
        Vector2 settingsScroll;
        Vector2 queueScroll;
        Vector2 resultsScroll;
        bool showSettings = true;
        bool showQueue = true;
        bool showFailures = true;
        string statusMessage = "Ready.";
        MessageType statusType = MessageType.Info;

        [MenuItem("Tools/Photobooth")]
        static void Open()
        {
            var window = GetWindow<PhotoboothWindow>();
            window.titleContent = new GUIContent("Photobooth");
            window.minSize = new Vector2(430f, 520f);
            window.Show();
        }

        void OnEnable()
        {
            if (profile == null || IsPackageAsset(profile))
                profile = LoadDefaultProfile();

            RebuildProfileEditor();
            RefreshQueue();
        }

        void OnDisable()
        {
            EditorApplication.update -= ProcessCaptureStep;
            captureSession?.Dispose();
            captureSession = null;
            DestroyImmediate(profileEditor);
            profileEditor = null;
        }

        void OnGUI()
        {
            DrawProfileSelection();
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawQueue();
            GUILayout.FlexibleSpace();
            DrawCaptureStatus();
            DrawFailures();
            DrawCaptureControls();
        }

        void DrawProfileSelection()
        {
            EditorGUI.BeginDisabledGroup(captureSession != null);
            EditorGUI.BeginChangeCheck();
            var selectedProfile = (PhotoboothProfile)EditorGUILayout.ObjectField(
                "Capture Profile",
                profile,
                typeof(PhotoboothProfile),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                profile = EnsureEditableProfile(selectedProfile);
                RebuildProfileEditor();
                RefreshQueue();
            }
            EditorGUI.EndDisabledGroup();
        }

        void DrawSettings()
        {
            showSettings = EditorGUILayout.Foldout(
                showSettings,
                "Profile Settings",
                true,
                EditorStyles.foldoutHeader);
            if (showSettings)
            {
                if (profileEditor == null)
                {
                    EditorGUILayout.HelpBox(
                        "Select a capture profile to configure the batch.",
                        MessageType.Info);
                }
                else
                {
                    using var scroll = new EditorGUILayout.ScrollViewScope(
                        settingsScroll,
                        GUILayout.MaxHeight(300f));
                    settingsScroll = scroll.scrollPosition;
                    EditorGUI.BeginDisabledGroup(captureSession != null);
                    profileEditor.OnInspectorGUI();
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        void DrawQueue()
        {
            showQueue = EditorGUILayout.BeginFoldoutHeaderGroup(
                showQueue,
                queue == null
                    ? "Prefab Queue"
                    : $"Prefab Queue ({queue.Count})");
            if (showQueue)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(profile == null || captureSession != null);
                    if (GUILayout.Button("Refresh Queue", GUILayout.Width(110f)))
                        RefreshQueue();
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                }

                using var scroll = new EditorGUILayout.ScrollViewScope(
                    queueScroll,
                    EditorStyles.helpBox,
                    GUILayout.MinHeight(100f),
                    GUILayout.MaxHeight(220f));
                queueScroll = scroll.scrollPosition;
                if (queue == null)
                {
                    EditorGUILayout.LabelField(
                        "Configure a valid prefab source folder.",
                        EditorStyles.wordWrappedLabel);
                }
                else if (queue.Count == 0)
                {
                    EditorGUILayout.LabelField(
                        "No prefabs found.",
                        EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    foreach (PrefabDiscoveryEntry entry in queue.Entries)
                        EditorGUILayout.LabelField(entry.AssetPath);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawCaptureStatus()
        {
            if (captureSession != null)
            {
                Rect progressRect = GUILayoutUtility.GetRect(
                    18f,
                    18f,
                    GUILayout.ExpandWidth(true));
                string progressText =
                    $"{captureSession.CompletedOperations}/{captureSession.TotalOperations}";
                EditorGUI.ProgressBar(
                    progressRect,
                    captureSession.Progress,
                    progressText);

                if (!string.IsNullOrEmpty(captureSession.CurrentPrefabPath))
                {
                    EditorGUILayout.LabelField(
                        "Current Prefab",
                        captureSession.CurrentPrefabPath);
                    EditorGUILayout.LabelField(
                        "Camera Preset",
                        captureSession.CurrentPresetName);
                }
            }

            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        void DrawCaptureControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(
                    captureSession != null ||
                    profile == null ||
                    queue == null ||
                    queue.Count == 0);
                if (GUILayout.Button("Start Batch", GUILayout.Height(30f)))
                    StartCapture();
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(captureSession == null);
                if (GUILayout.Button("Cancel", GUILayout.Height(30f)))
                {
                    captureSession.Cancel();
                    statusMessage = "Cancellation requested...";
                    statusType = MessageType.Warning;
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        void StartCapture()
        {
            try
            {
                lastBatchResult = null;
                RefreshQueue();
                captureSession = new PhotoboothCaptureSession(profile);
                statusMessage = "Capture batch running...";
                statusType = MessageType.Info;
                EditorApplication.update += ProcessCaptureStep;
            }
            catch (Exception exception)
            {
                SetError(exception);
            }
        }

        void ProcessCaptureStep()
        {
            if (captureSession == null)
                return;

            try
            {
                bool hasMoreWork = captureSession.Step();
                Repaint();
                if (hasMoreWork)
                    return;

                CaptureBatchResult result = captureSession.Result;
                lastBatchResult = result;
                statusMessage = captureSession.IsCancelled
                    ? BuildSummary("Cancelled", result)
                    : BuildSummary("Complete", result);
                statusType = captureSession.IsCancelled
                    ? MessageType.Warning
                    : result.FailedCount > 0
                        ? MessageType.Warning
                        : MessageType.Info;
                StopCapture();
            }
            catch (Exception exception)
            {
                SetError(exception);
                StopCapture();
            }
        }

        void DrawFailures()
        {
            if (lastBatchResult == null || lastBatchResult.FailedCount == 0)
                return;

            showFailures = EditorGUILayout.BeginFoldoutHeaderGroup(
                showFailures,
                $"Failures ({lastBatchResult.FailedCount})");
            if (showFailures)
            {
                using var scroll = new EditorGUILayout.ScrollViewScope(
                    resultsScroll,
                    EditorStyles.helpBox,
                    GUILayout.MaxHeight(160f));
                resultsScroll = scroll.scrollPosition;
                foreach (CaptureFileResult file in lastBatchResult.Files)
                {
                    if (file.Status != CaptureFileStatus.Failed)
                        continue;

                    EditorGUILayout.LabelField(
                        $"{file.Prefab.AssetPath} [{file.PresetName}]",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        file.ErrorMessage,
                        EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.Space(2f);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        static string BuildSummary(string state, CaptureBatchResult result) =>
            $"{state}. Captured {result.CapturedCount}; " +
            $"skipped {result.SkippedCount}; failed {result.FailedCount}.";

        void StopCapture()
        {
            EditorApplication.update -= ProcessCaptureStep;
            captureSession?.Dispose();
            captureSession = null;
            Repaint();
        }

        void RefreshQueue()
        {
            queue = null;
            if (profile == null || profile.SourceFolder == null)
                return;

            try
            {
                queue = PrefabDiscoveryService.Discover(
                    profile.SourceFolder,
                    profile.IncludeSubfolders);
                statusMessage = $"Queue ready: {queue.Count} prefab(s).";
                statusType = MessageType.Info;
            }
            catch (Exception exception)
            {
                statusMessage = exception.Message;
                statusType = MessageType.Error;
            }
        }

        void RebuildProfileEditor()
        {
            DestroyImmediate(profileEditor);
            profileEditor = profile == null
                ? null
                : UnityEditor.Editor.CreateEditor(profile);
        }

        static PhotoboothProfile LoadDefaultProfile()
        {
            string templatePath = PhotoboothAssetPaths.DefaultProfilePath;
            var template =
                AssetDatabase.LoadAssetAtPath<PhotoboothProfile>(templatePath);
            if (template == null || !templatePath.StartsWith(
                    "Packages/",
                    StringComparison.Ordinal))
            {
                return template;
            }

            var userProfile =
                AssetDatabase.LoadAssetAtPath<PhotoboothProfile>(
                    PhotoboothAssetPaths.UserProfilePath);
            if (userProfile != null)
                return userProfile;

            PhotoboothAssetPaths.EnsureUserAssetDirectory();
            if (!AssetDatabase.CopyAsset(
                    templatePath,
                    PhotoboothAssetPaths.UserProfilePath))
            {
                throw new InvalidOperationException(
                    "Could not create an editable profile at " +
                    $"'{PhotoboothAssetPaths.UserProfilePath}'.");
            }

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<PhotoboothProfile>(
                PhotoboothAssetPaths.UserProfilePath);
        }

        internal static PhotoboothProfile EnsureEditableProfile(
            PhotoboothProfile selectedProfile)
        {
            return selectedProfile != null && IsPackageAsset(selectedProfile)
                ? LoadDefaultProfile()
                : selectedProfile;
        }

        static bool IsPackageAsset(PhotoboothProfile selectedProfile) =>
            AssetDatabase.GetAssetPath(selectedProfile).StartsWith(
                "Packages/",
                StringComparison.Ordinal);

        void SetError(Exception exception)
        {
            statusMessage = exception.Message;
            statusType = MessageType.Error;
            Debug.LogException(exception);
        }
    }
}
