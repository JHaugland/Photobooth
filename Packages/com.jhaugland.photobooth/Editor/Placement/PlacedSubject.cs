using UnityEngine;

namespace Photobooth.Editor.Placement
{
    internal readonly struct PlacedSubject
    {
        internal GameObject Instance { get; }
        internal Bounds Bounds { get; }

        internal PlacedSubject(GameObject instance, Bounds bounds)
        {
            Instance = instance;
            Bounds = bounds;
        }
    }
}
