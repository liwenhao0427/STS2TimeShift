using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace TimeShift.Scripts;

[ModInitializer(nameof(Init))]
public partial class Entry
{
    private static Harmony? _harmony;

    public static void Init()
    {
        _harmony = new Harmony("sts2.timeshift");
        _harmony.PatchAll();
        Log.Debug("TimeShift Mod initialized!");
    }
}
