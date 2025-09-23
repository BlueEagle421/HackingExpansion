using RimWorld;
using Verse;

namespace USH_HE;

[DefOf]
public static class USH_DefOf
{
    static USH_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(USH_DefOf));

    public static StatCategoryDef USH_Hacker;
    public static StatDef USH_RemoteHackingDistance;
    public static JobDef USH_InstallDataRipper;
    public static JobDef USH_InstallVirtualDataRipper;
    public static EffecterDef USH_RippingData;
    public static EffecterDef USH_HackingOutcome;
    public static SoundDef USH_DataRipped;
    public static HediffDef USH_CyberspaceNausea;
    public static HediffDef USH_CyberspaceLockOut;
    public static HediffDef USH_CyberspaceComa;
    public static DamageDef USH_CyberNecrosis;
    public static JobDef USH_EnterCyberpod;
    public static StatDef USH_HackingSpeedMultiplier;
    public static StatDef USH_HackingStealthMultiplier;
    public static ThingDef USH_VitalityCoprocessor;
    public static ThingDef USH_DataRipper;
    public static ThingDef USH_Cyberlink;
    public static ThingDef USH_AncientCyberdeck;
    public static ThingDef USH_AncientJammer;
    public static ThingDef USH_EncryptedCrate;
}
