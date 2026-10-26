using System;
using System.Text;
using Koala.Core.Cameras;
using Koala.UI;
using UnityEngine;

namespace LegoPartyUltrawide;

public class UltrawideBehaviour : MonoBehaviour
{
    public UltrawideBehaviour(IntPtr ptr) : base(ptr) { }

    private bool _resolutionForced;
    private int _lastWidth, _lastHeight;
    private float _nextDiagnostics = 10f;

    private void Update()
    {
        if (!_resolutionForced && Time.realtimeSinceStartup > 5f)
        {
            _resolutionForced = true;
            ForceResolution();
        }

        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            Plugin.Logger.LogInfo($"Screen is now {_lastWidth}x{_lastHeight} ({(float)_lastWidth / _lastHeight:0.###}:1)");
            try { RefreshAll(); } catch (Exception e) { Plugin.Logger.LogError(e); }
        }

        if (Plugin.VerboseLogging.Value && Time.realtimeSinceStartup > _nextDiagnostics)
        {
            _nextDiagnostics = Time.realtimeSinceStartup + 30f;
            try { LogDiagnostics(); } catch (Exception e) { Plugin.Logger.LogError(e); }
        }
    }

    private static void ForceResolution()
    {
        int w = Plugin.ForceWidth.Value, h = Plugin.ForceHeight.Value;
        if (w <= 0 || h <= 0) return;
        Plugin.Logger.LogInfo($"Forcing resolution {w}x{h} {Plugin.ForceMode.Value}");
        Screen.SetResolution(w, h, Plugin.ForceMode.Value);
    }

    // Makes every live camera service and letterbox overlay pick up the unlocked aspect ratio.
    // Each part only runs if its feature was applied, so it never touches game code that failed the startup check.
    internal static void RefreshAll()
    {
        if (Plugin.CameraFixActive) RecomputeCameras();
        if (Plugin.LetterboxFixActive) RefreshLetterboxes();
    }

    private static void RecomputeCameras()
    {
        foreach (var service in Resources.FindObjectsOfTypeAll<CameraService>())
            service.RecomputePixelRect();
    }

    private static void RefreshLetterboxes()
    {
        foreach (var overlay in Resources.FindObjectsOfTypeAll<LetterboxOverlay>())
            LetterboxPatches.FixLetterbox(overlay);
    }

    private static void LogDiagnostics()
    {
        var sb = new StringBuilder("Diagnostics:\n");
        sb.AppendLine($"  Screen {Screen.width}x{Screen.height}, mode {Screen.fullScreenMode}");
        sb.AppendLine($"  Camera fix active={Plugin.CameraFixActive}, letterbox fix active={Plugin.LetterboxFixActive}");
        try { AppendCameraServices(sb); } catch (Exception e) { sb.AppendLine($"  CameraService unavailable: {e.Message}"); }
        foreach (var cam in Camera.allCameras)
            sb.AppendLine($"  Camera '{cam.name}' rect={cam.rect} pixelRect={cam.pixelRect} fov={cam.fieldOfView:0.#} aspect={cam.aspect:0.###}");
        Plugin.Logger.LogInfo(sb.ToString());
    }

    private static void AppendCameraServices(StringBuilder sb)
    {
        sb.AppendLine($"  Aspect clamp min={CameraService.MinHorizontalAspectRatio} max={CameraService.MaxHorizontalAspectRatio}");
        foreach (var service in Resources.FindObjectsOfTypeAll<CameraService>())
            sb.AppendLine($"  CameraService pixelRect={service.PixelRect}");
    }
}
