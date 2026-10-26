using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace LegoPartyUltrawide;

// Applies a feature's Harmony patch classes only if everything they touch still exists in the game.
//
// BepInEx regenerates the game bindings after every game update. If a method or field the plugin uses
// was renamed or removed, calling into it would fail inside the game's own code. Compiling the patch
// methods up front surfaces those problems at startup, where they can be logged and the feature skipped.
internal static class SafePatcher
{
    private const BindingFlags AllDeclared =
        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public static bool TryApply(string feature, params Type[] patchClasses)
    {
        var harmony = new Harmony($"{Plugin.Guid}.{feature.Replace(' ', '-')}");
        try
        {
            // JIT-compile every method so missing game members throw here rather than in a game callback.
            foreach (var method in patchClasses.SelectMany(t => t.GetMethods(AllDeclared)))
                if (!method.IsAbstract && !method.ContainsGenericParameters)
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);

            foreach (var patchClass in patchClasses)
                harmony.PatchAll(patchClass);
            return true;
        }
        catch (Exception e)
        {
            harmony.UnpatchSelf();
            var cause = e is TargetInvocationException && e.InnerException != null ? e.InnerException : e;
            Plugin.Logger.LogWarning(
                $"The {feature} could not be applied, probably because a game update changed something it relies on. " +
                $"That part of the mod is switched off and the game behaves as without it. Details: {cause.GetType().Name}: {cause.Message}");
            return false;
        }
    }
}
