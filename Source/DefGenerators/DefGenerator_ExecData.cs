using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_HE;

public class ThingDefGenerator_ExecData
{
    public static string ExecDataDefPrefix = "ExecData";
    public static string ExecDataDefLearningPrefix = ExecDataDefPrefix + "Learning";

    private static Dictionary<VEF.Abilities.AbilityDef, HediffDef> _abilityHediffDict = [];

    public static IEnumerable<HediffDef> ImpliedHediffDefs(bool hotReload = false)
    {
        if (hotReload) _abilityHediffDict.Clear();

        foreach (var def in DefDatabase<VEF.Abilities.AbilityDef>.AllDefs)
        {
            AbilityExtension_Cyber ext = def.GetModExtension<AbilityExtension_Cyber>();

            if (ext == null)
                continue;

            var hediffLearningDef = ExecDataHediffLearning(def, ext, hotReload);
            _abilityHediffDict.Add(def, hediffLearningDef);

            yield return hediffLearningDef;
        }
    }

    public static IEnumerable<ThingDef> ImpliedThingDefs(bool hotReload = false)
    {
        foreach (var def in DefDatabase<VEF.Abilities.AbilityDef>.AllDefs)
        {
            AbilityExtension_Cyber ext = def.GetModExtension<AbilityExtension_Cyber>();

            if (ext == null)
                continue;

            var learningHediffDef = _abilityHediffDict.TryGetValue(def);

            yield return ExecData(def, learningHediffDef, ext, hotReload);
        }
    }

    private static HediffDef ExecDataHediffLearning(VEF.Abilities.AbilityDef def, AbilityExtension_Cyber ext, bool hotReload = false)
    {
        string defName = ExecDataDefLearningPrefix + "_" + def.defName;
        HediffDef obj = hotReload ? (DefDatabase<HediffDef>.GetNamed(defName, errorOnFail: false) ?? new HediffDef()) : new HediffDef();

        obj.defName = defName;
        obj.label = "USH_HE_ExecDataHediffLearningLabel".Translate(def.LabelCap);
        obj.description = "USH_HE_ExecDataHediffLearningDescription".Translate(def.Named("EXEC"));

        obj.hediffClass = typeof(Hediff_LearningAbility);
        obj.descriptionHyperlinks = [new(def)];
        obj.defaultLabelColor = new(1, 1, 1);
        obj.isBad = false;
        obj.priceImpact = true;
        obj.keepOnBodyPartRestoration = true;
        obj.duplicationAllowed = false;

        obj.modExtensions = [new LearningAbilityExtension()
        {
            abilityDef = def,
            abilityExt = ext,
        }];

        return obj;
    }

    private static ThingDef ExecData(VEF.Abilities.AbilityDef def, HediffDef learningHediffDef, AbilityExtension_Cyber ext, bool hotReload = false)
    {
        string defName = ExecDataDefPrefix + "_" + def.defName;
        ThingDef obj = hotReload ? (DefDatabase<ThingDef>.GetNamed(defName, errorOnFail: false) ?? new ThingDef()) : new ThingDef();

        obj.defName = defName;
        obj.label = "USH_HE_ExecDataLabel".Translate(def.label);
        obj.description = "USH_HE_ExecDataDescription".Translate(def.LabelCap, def.description);

        obj.category = ThingCategory.Item;
        obj.selectable = true;
        obj.thingClass = typeof(ThingWithComps);

        obj.comps =
        [
            new CompProperties_UseEffectPlaySound
            {
                soundOnUsed = USH_DefOf.USH_CyberlinkInstalled
            },
            new CompProperties_UseEffectDestroySelf
            {
                compClass = typeof(CompUseEffect_DestroySelf)
            },
            new CompProperties_Forbiddable()
        ];

        obj.graphicData = new GraphicData
        {
            texPath = "Things/Item/ExecData",
            graphicClass = typeof(Graphic_Single)
        };

        obj.drawGUIOverlay = false;

        obj.statBases =
        [
            new() {
                stat = StatDefOf.MaxHitPoints,
                value = 120f
            },
            new() {
                stat = StatDefOf.Mass,
                value = 0.8f
            },
            new() {
                stat = StatDefOf.DeteriorationRate,
                value = 2f
            },
            new() {
                stat = StatDefOf.Flammability,
                value = 0.2f
            }
        ];

        obj.techLevel = TechLevel.Spacer;
        obj.altitudeLayer = AltitudeLayer.Item;
        obj.alwaysHaulable = true;
        obj.rotatable = false;
        obj.pathCost = 14;
        obj.tradeTags = ["ExoticMisc"];
        obj.stackLimit = 1;
        obj.tradeNeverStack = true;
        obj.forceDebugSpawnable = true;
        obj.drawerType = DrawerType.MapMeshOnly;

        obj.thingCategories = [USH_DefOf.USH_ExecDatas];
        obj.modContentPack = def.modContentPack;
        obj.descriptionHyperlinks = [new(def), new(USH_DefOf.USH_ExecDataCase)];

        obj.comps.Add(new CompProperties_Usable
        {
            compClass = typeof(CompUsableImplant),
            useJob = USH_DefOf.UseItem,
            useLabel = "USH_HE_UseExecData".Translate(def.label),
            showUseGizmo = true,
            userMustHaveHediff = USH_DefOf.USH_InstalledExecDataCase,
        });

        obj.comps.Add(new CompProperties_UseEffectInstallImplant
        {
            hediffDef = learningHediffDef,
            bodyPart = USH_DefOf.Brain
        });

        obj.statBases.Add(new StatModifier
        {
            stat = StatDefOf.MarketValue,
            value = ext.marketValue
        });

        return obj;
    }
}
