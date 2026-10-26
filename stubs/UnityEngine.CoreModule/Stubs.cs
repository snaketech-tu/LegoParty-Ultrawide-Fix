using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace UnityEngine
{
    public class Object : Il2CppSystem.Object
    {
        public Object(IntPtr pointer) : base(pointer) { }
        public string name { get => throw null; set => throw null; }
    }

    public class Component : Object
    {
        public Component(IntPtr pointer) : base(pointer) { }
    }

    public class Behaviour : Component
    {
        public Behaviour(IntPtr pointer) : base(pointer) { }
        public bool enabled { get => throw null; set => throw null; }
    }

    public class MonoBehaviour : Behaviour
    {
        public MonoBehaviour(IntPtr pointer) : base(pointer) { }
    }

    public class Texture : Object
    {
        public Texture(IntPtr pointer) : base(pointer) { }
        public virtual int width { get => throw null; set => throw null; }
        public virtual int height { get => throw null; set => throw null; }
    }

    public class RenderTexture : Texture
    {
        public RenderTexture(IntPtr pointer) : base(pointer) { }
    }

    public sealed class Camera : Behaviour
    {
        public Camera(IntPtr pointer) : base(pointer) { }
        public float fieldOfView { get => throw null; set => throw null; }
        public float aspect { get => throw null; set => throw null; }
        public Rect rect { get => throw null; set => throw null; }
        public Rect pixelRect { get => throw null; set => throw null; }
        public static Il2CppReferenceArray<Camera> allCameras => throw null;
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) => throw null;
        public float x { get => throw null; set => throw null; }
        public float y { get => throw null; set => throw null; }
        public float width { get => throw null; set => throw null; }
        public float height { get => throw null; set => throw null; }
    }

    public struct Vector2
    {
        public float x;
        public float y;
    }

    public enum FullScreenMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3,
    }

    public sealed class Screen : Il2CppSystem.Object
    {
        public Screen(IntPtr pointer) : base(pointer) { }
        public static int width => throw null;
        public static int height => throw null;
        public static FullScreenMode fullScreenMode { get => throw null; set => throw null; }
        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode) => throw null;
    }

    public class Time : Il2CppSystem.Object
    {
        public Time(IntPtr pointer) : base(pointer) { }
        public static float realtimeSinceStartup => throw null;
    }

    public struct Mathf
    {
        public static float Min(float a, float b) => throw null;
        public static float Round(float f) => throw null;
    }

    public sealed class Resources : Il2CppSystem.Object
    {
        public Resources(IntPtr pointer) : base(pointer) { }
        public static Il2CppArrayBase<T> FindObjectsOfTypeAll<T>() where T : Object => throw null;
    }
}
