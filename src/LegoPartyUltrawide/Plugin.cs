using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Koala.Core.Cameras;
using Koala.Minigames.UI;
using Koala.UI;
using UnityEngine;

namespace LegoPartyUltrawide;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "snaketech.legoparty.ultrawide";
    public const string Name = "LEGO Party Ultrawide";
    public const string Version = "1.0.0";

    internal static ManualLogSource Logger;

    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<float> MaxAspectRatio;
    internal static ConfigEntry<bool> HideLetterboxBorders;
    internal static ConfigEntry<int> ForceWidth;
    internal static ConfigEntry<int> ForceHeight;
    internal static ConfigEntry<FullScreenMode> ForceMode;
    internal static ConfigEntry<bool> VerboseLogging;

    public override void Load()
    {
        Logger = Log;

        Enabled = Config.Bind("General", "Enabled", true,
            "Master switch for the ultrawide fixes.");
        MaxAspectRatio = Config.Bind("General", "MaxAspectRatio", 0f,
            "Widest aspect ratio the game may render at before it adds black side bars. " +
            "0 = unlimited (use the full screen width). Example: 2.37 for 21:9, 3.56 for 32:9.");
        HideLetterboxBorders = Config.Bind("General", "HideLetterboxBorders", true,
            "Hide the black letterbox/pillarbox border overlay the UI draws.");
        ForceWidth = Config.Bind("Resolution", "ForceWidth", 0,
            "Force this render width on startup (0 = keep the game's own setting).");
        ForceHeight = Config.Bind("Resolution", "ForceHeight", 0,
            "Force this render height on startup (0 = keep the game's own setting).");
        ForceMode = Config.Bind("Resolution", "ForceMode", FullScreenMode.FullScreenWindow,
            "Window mode used together with ForceWidth/ForceHeight.");
        VerboseLogging = Config.Bind("Debug", "VerboseLogging", false,
            "Log camera rects and resolution lists to help diagnose problems.");

        if (!Enabled.Value)
        {
            Log.LogInfo("Disabled in config.");
            return;
        }

        new Harmony(Guid).PatchAll(typeof(Patches));
        ClassInjector.RegisterTypeInIl2Cpp<UltrawideBehaviour>();
        AddComponent<UltrawideBehaviour>();
        Log.LogInfo($"{Name} {Version} loaded.");
    }

    // Unlimited is expressed as a very wide ratio so the game's own clamp math still runs.
    internal static float EffectiveMaxAspect => MaxAspectRatio.Value <= 0f ? 100f : MaxAspectRatio.Value;
}

internal static class Patches
{
    // The game hard-codes a 16:9 clamp here (the min/max ratio constants are inlined into
    // native code, so changing the static fields has no effect). Widen the rect it returns.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraService), nameof(CameraService.ComputeCameraPixelRect))]
    private static void ComputeCameraPixelRectPostfix(Vector2 screen, ref Rect __result)
    {
        try
        {
            var original = __result;
            __result = WidenRect(screen, __result);
            if (Plugin.VerboseLogging.Value)
                Plugin.Logger.LogInfo($"ComputeCameraPixelRect(screen={screen}) {original} -> {__result}");
        }
        catch (Exception e) { Plugin.Logger.LogError(e); }
    }

    // Picture-in-picture minigame previews size their render texture from CameraService.PixelRect but
    // display it in a fixed 16:9 frame. Hand them the original 16:9 rect while the texture is created.
    private static readonly List<(CameraService service, Rect rect)> _swappedRects = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MinigamePIPGameView), nameof(MinigamePIPGameView.CreateAndAssignNewRenderTexture))]
    private static void PipCreatePrefix()
    {
        try
        {
            _swappedRects.Clear();
            foreach (var service in Resources.FindObjectsOfTypeAll<CameraService>())
            {
                var rect = service._pixelRect;
                if (rect.height <= 0f || rect.width / rect.height <= SixteenByNine + 0.001f) continue;
                _swappedRects.Add((service, rect));
                float width = rect.height * SixteenByNine;
                service._pixelRect = new Rect(rect.x + (rect.width - width) * 0.5f, rect.y, width, rect.height);
            }
        }
        catch (Exception e) { Plugin.Logger.LogError(e); }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MinigamePIPGameView), nameof(MinigamePIPGameView.CreateAndAssignNewRenderTexture))]
    private static void PipCreatePostfix(MinigamePIPGameView __instance)
    {
        foreach (var (service, rect) in _swappedRects)
            if (service != null) service._pixelRect = rect;
        _swappedRects.Clear();

        if (Plugin.VerboseLogging.Value && __instance._renderTexture != null)
            Plugin.Logger.LogInfo($"PIP render texture {__instance._renderTexture.width}x{__instance._renderTexture.height}");
    }

    private const float SixteenByNine = 16f / 9f;

    internal static Rect WidenRect(Vector2 screen, Rect rect)
    {
        // Only touch the pillarboxed case; narrower-than-16:9 letterboxing stays as the game wants.
        if (screen.y <= 0f || rect.height <= 0f || rect.width >= screen.x - 0.5f) return rect;
        if (screen.x / screen.y <= rect.width / rect.height + 0.001f) return rect;

        float width = Mathf.Min(screen.x, rect.height * Plugin.EffectiveMaxAspect);
        float x = Mathf.Round((screen.x - width) * 0.5f);
        return new Rect(x, rect.y, width, rect.height);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LetterboxOverlay), nameof(LetterboxOverlay.Start))]
    private static void LetterboxStartPostfix(LetterboxOverlay __instance) => FixLetterbox(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LetterboxOverlay), nameof(LetterboxOverlay.RefreshOverscanSize))]
    private static void LetterboxRefreshPostfix(LetterboxOverlay __instance) => FixLetterbox(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LetterboxOverlay), nameof(LetterboxOverlay.OnPixelRectChange))]
    private static void LetterboxPixelRectPostfix(LetterboxOverlay __instance) => FixLetterbox(__instance);

    internal static void FixLetterbox(LetterboxOverlay overlay)
    {
        if (overlay == null) return;
        var fitter = overlay._maxRatioFitter;
        if (fitter != null && fitter.aspectRatio < Plugin.EffectiveMaxAspect)
            fitter.aspectRatio = Plugin.EffectiveMaxAspect;

        if (!Plugin.HideLetterboxBorders.Value || overlay._borders == null) return;
        foreach (var border in overlay._borders)
            if (border != null) border.enabled = false;
    }
}
