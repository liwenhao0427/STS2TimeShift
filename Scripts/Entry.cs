using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace DevDeckTools.Scripts;

[ModInitializer(nameof(Init))]
public partial class Entry
{
    private static Harmony? _harmony;

    public static void Init()
    {
        _harmony = new Harmony("sts2.devdecktools");
        _harmony.PatchAll();
        Log.Info("[DevDeckTools] Mod initialized");
    }
}
