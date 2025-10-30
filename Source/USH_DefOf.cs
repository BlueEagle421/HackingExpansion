using RimWorld;
using Verse;

namespace USH_HE;

[DefOf]
public static class USH_DefOf
{
    static USH_DefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(USH_DefOf));

    public static StatCategoryDef USH_Hacker;
    public static StatDef USH_RemoteHackingDistance;
    public static JobDef USH_InstallICEBreaker;
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
    public static ThingDef USH_Cyberlink;
    public static ThingDef USH_AncientCyberdeck;
    public static ThingDef USH_AncientJammer;
    public static ThingDef USH_EncryptedCrate;
    public static HacksetDef USH_BlackICE;
    public static ThingDef USH_BrokenExecData;
    public static HediffDef USH_Disabled;
    public static HediffDef USH_ForkBomb;
    public static JobDef UseItem;
    public static HediffDef USH_InstalledExecDataCase;
    public static ThingCategoryDef USH_ExecDatas;
    public static SoundDef USH_CyberlinkInstalled;
    public static ThingDef USH_ExecDataCase;
    public static BodyPartDef Brain;
    public static EffecterDef HackingTerminal;
    public static ThingDef USH_BlackBox;
    public static ThingDef USH_BlackBoxIncoming;
    public static ThingDef USH_DataCenter;
    public static DesignationDef USH_RipData;
    public static JobDef USH_ApplyResearchGiver;
}
