using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class ResearchPrerequisitesResolver(World world) : WorldComponent(world)
{
    private static List<Def> _toCheck = [];

    public static void AddDefToCheck(Def def)
        => _toCheck.Add(def);

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);

        AddUnlockedDefs();
    }

    private void AddUnlockedDefs()
    {
        foreach (Def def in _toCheck)
        {
            var ext = def.GetModExtension<ResearchPrerequisitesExtension>();

            foreach (var res in ext.researchPrerequisites)
            {
                FieldInfo cachedField = AccessTools.Field(typeof(ResearchProjectDef), "cachedUnlockedDefs");

                var list = cachedField.GetValue(res) as List<Def>;

                if (list == null)
                {
                    var prop = res.GetType().GetProperty("UnlockedDefs", BindingFlags.Public | BindingFlags.Instance);

                    if (prop != null)
                        list = prop.GetValue(res) as List<Def>;

                    if (list == null)
                    {
                        list = [];
                        cachedField.SetValue(res, list);
                    }
                }

                if (!list.Contains(def))
                {
                    list.Add(def);

                    cachedField.SetValue(res, list);
                }
            }
        }
    }
}