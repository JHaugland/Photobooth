using System;
using UnityEngine;

namespace Photobooth.Editor.Placement
{
    internal static class SubjectPlacementService
    {
        internal static Bounds CenterAndGround(
            GameObject subject,
            Vector3 stagePosition)
        {
            if (subject == null)
                throw new ArgumentNullException(nameof(subject));

            Bounds bounds = CalculateWorldBounds(subject);
            Vector3 offset = new(
                stagePosition.x - bounds.center.x,
                stagePosition.y - bounds.min.y,
                stagePosition.z - bounds.center.z);
            subject.transform.position += offset;
            return CalculateWorldBounds(subject);
        }

        internal static Bounds CalculateWorldBounds(GameObject subject)
        {
            if (subject == null)
                throw new ArgumentNullException(nameof(subject));

            Renderer[] renderers = subject.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds combinedBounds = default;

            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException(
                    $"Prefab '{subject.name}' has no enabled renderers to capture.");
            }

            return combinedBounds;
        }
    }
}
