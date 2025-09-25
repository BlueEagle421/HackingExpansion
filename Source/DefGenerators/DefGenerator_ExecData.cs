using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USH_HE;

public class ThingDefGenerator_ExecData
{
    public static string ExecDataDefPrefix = "ExecData";

    public static IEnumerable<ThingDef> ImpliedThingDefs(bool hotReload = false)
    {
        foreach (var def in DefDatabase<VEF.Abilities.AbilityDef>.AllDefs)
        {
            AbilityExtension_Cyber ext = def.GetModExtension<AbilityExtension_Cyber>();

            if (ext == null)
                continue;

            yield return ExecData(def, hotReload);
        }
    }

    private static ThingDef ExecData(VEF.Abilities.AbilityDef def, bool hotReload = false)
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
                value = 80f
            },
            new() {
                stat = StatDefOf.Mass,
                value = 0.2f
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
        obj.descriptionHyperlinks = [new DefHyperlink(def)];

        obj.comps.Add(new CompProperties_Usable
        {
            compClass = typeof(CompUsableImplant),
            useJob = USH_DefOf.UseItem,
            useLabel = "USH_HE_UseExecData".Translate(def.label),
            showUseGizmo = true,
            userMustHaveHediff = USH_DefOf.USH_InstalledExecDataCase,
        });

        // thingDef.comps.Add(new CompProperties_UseEffectInstallImplant
        // {
        //     ability = def
        // });
        // thingDef.statBases.Add(new StatModifier
        // {
        //     stat = StatDefOf.MarketValue,
        //     value = Mathf.Round(Mathf.Lerp(100f, 1000f, def.level / 6f))
        // });

        return obj;
    }
}
