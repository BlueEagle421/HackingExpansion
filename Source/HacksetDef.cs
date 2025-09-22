using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace USH_HE;

public class HacksetDef : Def
{
    public float minDefense;
    public float weight;
    public float stealthMultiplier;
    public List<HackingOutcomeDef> hackingOutcomes;

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (var entry in base.SpecialDisplayStats(req))
            yield return entry;

        string statLabel = "USH_HE_HackActions".Translate();
        string statValue = "USH_HE_HackActionsHacksetDesc".Translate();
        string statContent = string.Join(", ", hackingOutcomes.Select(x => x.LabelCap));

        IEnumerable<Dialog_InfoCard.Hyperlink> hyperlinks = hackingOutcomes
            .Select(x => new Dialog_InfoCard.Hyperlink(x));

        StatDrawEntry hackActionsEntry = new(USH_DefOf.USH_Hacker, statLabel, statContent, statValue, 0, null, hyperlinks);

        yield return hackActionsEntry;
    }

    public string GetOutcomesDescription()
    {
        StringBuilder sb = new();

        var chances = hackingOutcomes.PercentChancesByWeight(x => x.weight);

        for (int i = 0; i < hackingOutcomes.Count(); i++)
            sb.AppendLine($"   - {hackingOutcomes[i].LabelCap} ({chances[i].ToStringPercent()})");

        return sb.ToString();
    }
}