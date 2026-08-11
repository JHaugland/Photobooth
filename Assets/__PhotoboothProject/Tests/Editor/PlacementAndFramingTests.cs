using NUnit.Framework;
using Photobooth.Editor.Configuration;
using Photobooth.Editor.Placement;
using UnityEngine;

namespace Photobooth.Editor.Tests
{
    internal sealed class PlacementAndFramingTests
    {
        GameObject subject;
        GameObject cameraObject;
        GameObject stageObject;
        GameObject targetObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(subject);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(stageObject);
            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void CalculateWorldBounds_CombinesNestedRenderers()
        {
            subject = new GameObject("Subject");
            GameObject left = CreateCube(subject.transform, new Vector3(-2f, 1f, 0f));
            GameObject right = CreateCube(subject.transform, new Vector3(2f, 1f, 0f));

            Bounds bounds = SubjectPlacementService.CalculateWorldBounds(subject);

            Assert.That(bounds.center, Is.EqualTo(new Vector3(0f, 1f, 0f)));
            Assert.That(bounds.size, Is.EqualTo(new Vector3(5f, 1f, 1f)));
            Object.DestroyImmediate(left);
            Object.DestroyImmediate(right);
        }

        [Test]
        public void CenterAndGround_AlignsBoundsToStagePosition()
        {
            subject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            subject.transform.localScale = new Vector3(2f, 4f, 2f);
            subject.transform.position = new Vector3(10f, 10f, -3f);
            Vector3 stagePosition = new(3f, 2f, 5f);

            Bounds bounds =
                SubjectPlacementService.CenterAndGround(subject, stagePosition);

            Assert.That(bounds.center.x, Is.EqualTo(stagePosition.x).Within(0.001f));
            Assert.That(bounds.min.y, Is.EqualTo(stagePosition.y).Within(0.001f));
            Assert.That(bounds.center.z, Is.EqualTo(stagePosition.z).Within(0.001f));
        }

        [Test]
        public void CalculateWorldBounds_WithoutEnabledRenderers_ThrowsClearError()
        {
            subject = new GameObject("EmptySubject");

            var exception = Assert.Throws<System.InvalidOperationException>(
                () => SubjectPlacementService.CalculateWorldBounds(subject));

            StringAssert.Contains("no enabled renderers", exception.Message);
        }

        [Test]
        public void AutoFramePerspective_KeepsEveryBoundsCornerInView()
        {
            Camera camera = CreateCamera();
            Bounds bounds = new(Vector3.up, new Vector3(4f, 2f, 6f));
            CameraPreset preset = CameraPreset.CreateAutoFrame(
                "Isometric",
                new Vector3(20f, 45f, 0f));

            CameraFramingService.Apply(
                camera,
                stageObject.transform,
                targetObject.transform,
                bounds,
                preset,
                1f);

            AssertBoundsInsideViewport(camera, bounds);
            Assert.That(camera.orthographic, Is.False);
        }

        [Test]
        public void AutoFrameOrthographic_SetsSizeAndKeepsBoundsInView()
        {
            Camera camera = CreateCamera();
            Bounds bounds = new(Vector3.up, new Vector3(8f, 2f, 3f));
            CameraPreset preset = CameraPreset.CreateAutoFrame(
                "Front",
                Vector3.zero,
                CameraProjection.Orthographic);

            CameraFramingService.Apply(
                camera,
                stageObject.transform,
                targetObject.transform,
                bounds,
                preset,
                1f);

            AssertBoundsInsideViewport(camera, bounds);
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.GreaterThan(0f));
        }

        [Test]
        public void FixedTransform_UsesStageRelativePositionAndRotation()
        {
            Camera camera = CreateCamera();
            stageObject.transform.SetPositionAndRotation(
                new Vector3(10f, 0f, 4f),
                Quaternion.Euler(0f, 90f, 0f));
            CameraPreset preset = CameraPreset.CreateFixed(
                "Fixed",
                new Vector3(0f, 2f, -5f),
                new Vector3(10f, 20f, 0f));

            CameraFramingService.Apply(
                camera,
                stageObject.transform,
                targetObject.transform,
                new Bounds(Vector3.zero, Vector3.one),
                preset,
                1f);

            Assert.That(
                camera.transform.position,
                Is.EqualTo(stageObject.transform.TransformPoint(new Vector3(0f, 2f, -5f))));
            Assert.That(
                Quaternion.Angle(
                    camera.transform.rotation,
                    stageObject.transform.rotation * Quaternion.Euler(10f, 20f, 0f)),
                Is.LessThan(0.001f));
        }

        [Test]
        public void Apply_WithInvalidAspectRatio_ThrowsArgumentOutOfRangeException()
        {
            Camera camera = CreateCamera();

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => CameraFramingService.Apply(
                    camera,
                    stageObject.transform,
                    targetObject.transform,
                    new Bounds(Vector3.zero, Vector3.one),
                    CameraPreset.CreateAutoFrame("Front", Vector3.zero),
                    0f));
        }

        Camera CreateCamera()
        {
            cameraObject = new GameObject("Camera");
            stageObject = new GameObject("Stage");
            targetObject = new GameObject("Target");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f;
            return camera;
        }

        static GameObject CreateCube(Transform parent, Vector3 position)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            return cube;
        }

        static void AssertBoundsInsideViewport(Camera camera, Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 corner = new(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 viewport = camera.WorldToViewportPoint(corner);
                        Assert.That(viewport.z, Is.GreaterThan(0f));
                        Assert.That(viewport.x, Is.InRange(0f, 1f));
                        Assert.That(viewport.y, Is.InRange(0f, 1f));
                    }
                }
            }
        }
    }
}
