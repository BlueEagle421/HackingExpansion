using System.Text;
using RimWorld;
using Verse;

namespace USH_HE;


public class CompProperties_BlackBoxHackable : CompProperties_Hackable
{
    public IntRange goodwillGainRange;
    public float raidChance;
    public CompProperties_BlackBoxHackable()
        => compClass = typeof(CompBlackBox);
}

public class CompBlackBox : CompHackable
{
    private IntRange _raidDelayTicksRange = new(2500, 2500 * 3);
    public new CompProperties_BlackBoxHackable Props => (CompProperties_BlackBoxHackable)props;
    private Faction _ownerFaction, _compromisedFaction;
    private int _goodwillGain;

    public void Initialize(Faction ownerFaction, Faction compromisedFaction)
    {
        _ownerFaction = ownerFaction;
        _compromisedFaction = compromisedFaction;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _goodwillGain = Props.goodwillGainRange.RandomInRange;
    }

    protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
    {
        base.OnHacked(hacker, suppressMessages);

        _compromisedFaction.TryAffectGoodwillWith(hacker.Faction, _goodwillGain);

        if (Rand.Chance(Props.raidChance))
            DelayedRaidUtility.TriggerDelayedRaid(_ownerFaction, parent.Map, _raidDelayTicksRange.RandomInRange);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Values.Look(ref _goodwillGain, nameof(_goodwillGain));
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.CompInspectStringExtra());

        if (!IsHacked)
        {
            sb.AppendLine($"Hack to gain {_goodwillGain} goodwill with {_compromisedFaction}");
            sb.AppendLine($"Hacking might be detected by {_ownerFaction}".Colorize(ColorLibrary.RedReadable));
        }

        return sb.ToString().Trim();
    }
}
