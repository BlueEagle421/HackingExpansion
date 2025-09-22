using RimWorld;
using UnityEngine;
using Verse;

namespace USH_HE;

[StaticConstructorOnStartup]
public static class CyberLibrary
{
    public static TargetingParameters HackTargetingParams;
    public static TargetingParameters RipperTargetingParams;

    private static Texture2D _cachedHackTex;
    public static Texture2D TargetHackTex
    {
        get
        {
            _cachedHackTex ??= ContentFinder<Texture2D>.Get("UI/Gizmos/HackTarget");
            return _cachedHackTex;
        }
    }

    static CyberLibrary()
    {
        HackTargetingParams = new()
        {
            canTargetPawns = true,
            canTargetSelf = false,
            canTargetBuildings = true,

            validator = HackTargetValidator
        };

        RipperTargetingParams = new()
        {
            canTargetPawns = false,
            canTargetItems = false,
            canTargetBuildings = true,
            validator = RipperTargetValidator
        };
    }

    private static bool HackTargetValidator(TargetInfo targetInfo)
    {
        if (targetInfo.Thing is null)
            return false;

        if (!targetInfo.Thing.TryGetComp(out CompHackable _))
            return false;

        return true;
    }

    private static bool RipperTargetValidator(TargetInfo target)
    {
        if (!target.Thing.TryGetComp(out CompDataSource _))
            return false;

        return true;
    }
}