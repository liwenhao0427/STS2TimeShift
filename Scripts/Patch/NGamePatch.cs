using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes;

namespace DevDeckTools.Scripts.Patch;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
internal static class NGamePatch
{
    [HarmonyPostfix]
    private static void PostfixOnReady(NGame __instance)
    {
        if (__instance.GetNodeOrNull<DevMenuController>("%DevDeckToolsController") != null)
            return;

        DevMenuController controller = new DevMenuController
        {
            Name = "%DevDeckToolsController"
        };

        __instance.AddChild(controller);
        Log.Info("[DevDeckTools] Controller injected into NGame");
    }
}
