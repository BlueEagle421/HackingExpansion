using System.Reflection;
using HarmonyLib;
using Verse;

namespace USH_HE;

[StaticConstructorOnStartup]
public static class PlayerEjectablePodHolderPatch
{
    static PlayerEjectablePodHolderPatch()
    {
        var harmony = new Harmony("HackingExpansion.PlayerEjectablePodHolderPatch");

        var method = typeof(MapPawns).GetMethod(
            "PlayerEjectablePodHolder",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(PlayerEjectablePodHolderPatch).GetMethod(nameof(Postfix)))
        );
    }

    public static void Postfix(Thing thing, ref IThingHolder __result)
    {
        if (__result != null)
            return;

        if (thing is Building_Cyberpod pod)
            __result = pod;
    }
}