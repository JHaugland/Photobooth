using System;
using Photobooth.Editor.Configuration;
using UnityEngine;

namespace Photobooth.Editor.Placement
{
    internal static class CameraFramingService
    {
        const float MinimumAspectRatio = 0.01f;
        const float MinimumClearance = 0.01f;

        internal static void Apply(
            Camera camera,
            Transform stageOrigin,
            Transform cameraTarget,
            Bounds subjectBounds,
            CameraPreset preset,
            float aspectRatio)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            if (stageOrigin == null)
                throw new ArgumentNullException(nameof(stageOrigin));
            if (cameraTarget == null)
                throw new ArgumentNullException(nameof(cameraTarget));
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            if (aspectRatio < MinimumAspectRatio)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(aspectRatio),
                    "Camera aspect ratio must be greater than zero.");
            }

            camera.aspect = aspectRatio;
            cameraTarget.position = subjectBounds.center;
            ConfigureProjection(camera, preset);

            if (preset.PlacementMode == CameraPlacementMode.FixedTransform)
            {
                camera.transform.SetPositionAndRotation(
                    stageOrigin.TransformPoint(preset.FixedPosition),
                    stageOrigin.rotation * Quaternion.Euler(preset.FixedRotation));
                return;
            }

            Quaternion rotation =
                stageOrigin.rotation * Quaternion.Euler(preset.ViewingAngles);
            Vector3[] corners = GetCorners(subjectBounds);
            float distance = preset.Projection == CameraProjection.Orthographic
                ? ConfigureOrthographic(camera, rotation, subjectBounds.center, corners, preset)
                : CalculatePerspectiveDistance(camera, rotation, subjectBounds.center, corners, preset);

            camera.transform.SetPositionAndRotation(
                subjectBounds.center - rotation * Vector3.forward * distance,
                rotation);
        }

        static void ConfigureProjection(Camera camera, CameraPreset preset)
        {
            camera.orthographic = preset.Projection == CameraProjection.Orthographic;
            camera.fieldOfView = preset.FieldOfView;
            if (preset.PlacementMode == CameraPlacementMode.FixedTransform &&
                camera.orthographic)
            {
                camera.orthographicSize = preset.OrthographicSize;
            }
        }

        static float ConfigureOrthographic(
            Camera camera,
            Quaternion rotation,
            Vector3 center,
            Vector3[] corners,
            CameraPreset preset)
        {
            float verticalExtent = 0f;
            float horizontalExtent = 0f;
            float nearestDepth = 0f;
            Quaternion inverseRotation = Quaternion.Inverse(rotation);

            foreach (Vector3 corner in corners)
            {
                Vector3 local = inverseRotation * (corner - center);
                verticalExtent = Mathf.Max(verticalExtent, Mathf.Abs(local.y));
                horizontalExtent = Mathf.Max(horizontalExtent, Mathf.Abs(local.x));
                nearestDepth = Mathf.Min(nearestDepth, local.z);
            }

            camera.orthographicSize = Mathf.Max(
                verticalExtent,
                horizontalExtent / camera.aspect) * preset.FramingPadding;
            return -nearestDepth + camera.nearClipPlane + MinimumClearance;
        }

        static float CalculatePerspectiveDistance(
            Camera camera,
            Quaternion rotation,
            Vector3 center,
            Vector3[] corners,
            CameraPreset preset)
        {
            float verticalTangent =
                Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float horizontalTangent = verticalTangent * camera.aspect;
            float distance = 0f;
            Quaternion inverseRotation = Quaternion.Inverse(rotation);

            foreach (Vector3 corner in corners)
            {
                Vector3 local = inverseRotation * (corner - center);
                distance = Mathf.Max(
                    distance,
                    Mathf.Abs(local.x) * preset.FramingPadding / horizontalTangent - local.z,
                    Mathf.Abs(local.y) * preset.FramingPadding / verticalTangent - local.z,
                    -local.z + camera.nearClipPlane + MinimumClearance);
            }

            return distance;
        }

        static Vector3[] GetCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            return new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };
        }
    }
}
