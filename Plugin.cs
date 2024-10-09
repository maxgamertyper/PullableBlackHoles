using BepInEx;
using HarmonyLib;
using System.Reflection;
using System;
using UnityEngine.Rendering;
using BoplFixedMath;
using UnityEngine;
using BepInEx.Configuration;

namespace HoleHook
{
    [BepInPlugin("com.maxgamertyper1.pullableblackholes", "Pullable Black Holes", "1.0.0")]
    public class HoleHook : BaseUnityPlugin
    {
        internal static ConfigFile config;

        internal static ConfigEntry<bool> WhiteHoleAttach;

        internal static ConfigEntry<bool> InvertWhiteHolePull;

        internal static ConfigEntry<bool> WhiteHolePull;

        internal static ConfigEntry<bool> BlackHolePull;
        private void Log(string message)
        {
            Logger.LogInfo(message);
        }

        private void Awake()
        {
            // Plugin startup logic
            Log($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            DoPatching();
        }

        private void DoPatching()
        {
            var harmony = new Harmony("com.maxgamertyper1.pullableblackholes");


            config = ((BaseUnityPlugin)this).Config;
            BlackHolePull = config.Bind<bool>("Black Hole Patches", "Black Hole Pull Patch", true, "pulls black holes when connected with a grappling hook");
            WhiteHoleAttach = config.Bind<bool>("White Hole Patches", "White Hole Attach Patch", true, "allows grappling hooks to attach to white holes");
            WhiteHolePull = config.Bind<bool>("White Hole Patches", "White Hole Pull Patch", true, "pulls white holes when connected with a grappling hook");
            InvertWhiteHolePull = config.Bind<bool>("White Hole Patches", "Invert White Hole Force Patch", false, "pulls white holes toward the player (unlike the beam)");

            Patch(harmony, typeof(RopeAttachment), "UpdateSim", "FullPatch", true);
        }

        private void OnDestroy()
        {
            Log($"Bye Bye From {PluginInfo.PLUGIN_GUID}");
        }

        private void Patch(Harmony harmony, Type OriginalClass , string OriginalMethod, string PatchMethod, bool prefix)
        {
            MethodInfo MethodToPatch = AccessTools.Method(OriginalClass, OriginalMethod); // the method to patch
            MethodInfo Patch = AccessTools.Method(typeof(Patches), PatchMethod);
            if (prefix)
            {
                harmony.Patch(MethodToPatch, new HarmonyMethod(Patch));
            }
            else
            {
                harmony.Patch(MethodToPatch, null, new HarmonyMethod(Patch));
            }
            Log($"Patched {OriginalMethod} in {OriginalClass.ToString()}");
        }
    }

    public class Patches
    {
        public static bool FullPatch(ref RopeAttachment __instance)
        {
            if (__instance.ropeBody != null && __instance.ropeBody.enabled && __instance.ropeBody.hookHasArrived && !__instance.ropeBody.hasBeenDettached && !GameTime.IsTimeStopped())
            {
                if (__instance.blackHole != null && !__instance.blackHole.IsDestroyed)
                {
                    if (__instance.blackHole.GetMass() < Fix.Zero && !HoleHook.WhiteHoleAttach.Value)
                    {
                        __instance.blackHole = null;
                        __instance.Deattach();
                    }
                    if (__instance.blackHole.GetMass() < Fix.Zero && !HoleHook.WhiteHolePull.Value)
                    {
                        return false;
                    }
                    if (__instance.blackHole.GetMass() > Fix.Zero && !HoleHook.BlackHolePull.Value)
                    {
                        return false;
                    }
                    int num = __instance.topAttachment ? 0 : (__instance.ropeBody.segmentCount - 1);
                    int num2 = __instance.topAttachment ? 1 : (__instance.ropeBody.segmentCount - 2);
                    Vec2 f = (__instance.ropeBody.segment[num] - __instance.ropeBody.segment[num2]) * (__instance.isPlatformAttachment ? __instance.platformPullStr : __instance.pullStr);
                    if (__instance.blackHole.GetMass() < Fix.Zero && HoleHook.InvertWhiteHolePull.Value)
                    {
                        __instance.blackHole.AddForce(-f);
                        return false;
                    }
                    __instance.blackHole.AddForce(f);
                    return false;
                }
            }
            return true;
        }
    }
}
