using System;
using NUnit.Framework;
using Photobooth.Editor.Configuration;
using Photobooth.Editor.Placement;
using Photobooth.Editor.Stage;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Photobooth.Editor.Tests
{
    internal sealed class StageSessionTests
    {
        const string FixtureFolder =
            "Assets/__PhotoboothProject/Tests/StageSessionFixtures";
        const string SubjectPath = FixtureFolder + "/Subject.prefab";
        const string InvalidStagePath = FixtureFolder + "/InvalidStage.prefab";

        GameObject stagePrefab;

        [SetUp]
        public void SetUp()
        {
            AssetDatabase.DeleteAsset(FixtureFolder);
            AssetDatabase.CreateFolder(
                "Assets/__PhotoboothProject/Tests",
                "StageSessionFixtures");
            CreateCubePrefab(SubjectPath);
            CreateEmptyPrefab(InvalidStagePath);
            stagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhotoboothAssetPaths.StagePrefabPath);
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(FixtureFolder);
        }

        [Test]
        public void Constructor_PreservesActiveSceneAndDisposeClosesStageScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            int initialSceneCount = SceneManager.sceneCount;
            Scene stageScene;

            using (var session = new PhotoboothStageSession(stagePrefab))
            {
                stageScene = session.StageScene;
                Assert.That(stageScene.IsValid(), Is.True);
                Assert.That(stageScene.isLoaded, Is.True);
                Assert.That(
                    stageScene.path,
                    Is.EqualTo(PhotoboothAssetPaths.WritableStageScenePath));
                Assert.That(
                    session.CaptureCamera.scene,
                    Is.EqualTo(stageScene));
                Assert.That(
                    session.CaptureCamera.cullingMask,
                    Is.EqualTo(1 << session.IsolatedLayer));
                Assert.That(SceneManager.sceneCount, Is.EqualTo(initialSceneCount + 1));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeScene));
            }

            Assert.That(stageScene.isLoaded, Is.False);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(initialSceneCount));
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(activeScene));
        }

        [Test]
        public void FrameSubject_AlignsLightingRigWithCameraYaw()
        {
            GameObject subjectPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SubjectPath);

            using var session = new PhotoboothStageSession(stagePrefab);
            PlacedSubject subject = session.PlaceSubject(subjectPrefab);
            try
            {
                session.FrameSubject(
                    subject,
                    CameraPreset.CreateAutoFrame(
                        "Right",
                        new Vector3(0f, 90f, 0f)),
                    1f);

                Vector3 cameraForward = session.CaptureCamera.transform.forward;
                cameraForward.y = 0f;
                Vector3 lightingForward = session.LightingRig.forward;
                lightingForward.y = 0f;
                Assert.That(
                    Vector3.Dot(
                        cameraForward.normalized,
                        lightingForward.normalized),
                    Is.GreaterThan(0.999f));
            }
            finally
            {
                session.UnloadSubject(subject);
            }
        }

        [Test]
        public void Constructor_InvalidStageClosesTemporaryScene()
        {
            int initialSceneCount = SceneManager.sceneCount;
            GameObject invalidStage =
                AssetDatabase.LoadAssetAtPath<GameObject>(InvalidStagePath);

            var exception = Assert.Throws<InvalidOperationException>(
                () => new PhotoboothStageSession(invalidStage));

            StringAssert.Contains("missing", exception.Message.ToLowerInvariant());
            Assert.That(SceneManager.sceneCount, Is.EqualTo(initialSceneCount));
        }

        static void CreateCubePrefab(string path)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(cube, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cube);
            }
        }

        static void CreateEmptyPrefab(string path)
        {
            var empty = new GameObject("InvalidStage");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(empty, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(empty);
            }
        }
    }
}
