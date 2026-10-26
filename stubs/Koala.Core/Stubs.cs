using System;
using UnityEngine;

namespace Koala.Core.Cameras
{
    // Real base class is Koala.Core.MonoBehaviourService.
    public class CameraService : MonoBehaviour
    {
        public CameraService(IntPtr pointer) : base(pointer) { }
        public static float MinHorizontalAspectRatio { get => throw null; set => throw null; }
        public static float MaxHorizontalAspectRatio { get => throw null; set => throw null; }
        public Rect _pixelRect { get => throw null; set => throw null; }
        public Rect PixelRect => throw null;
        public Rect ComputeCameraPixelRect(Vector2 screen) => throw null;
        public void RecomputePixelRect() => throw null;
    }
}
