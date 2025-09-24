using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_HE;

public class CompProperties_DataSourceProtected : CompProperties
{
    public CompProperties_DataSourceProtected()
        => compClass = typeof(CompDataSourceProtected);
}

public class CompDataSourceProtected : CompDataSource
{
    private const float MIN_TICKS_BETWEEN_OUTCOMES = 1000f;
    private float _progressLastOutcome;
    private HashSet<HackingOutcomeDef> _appliedOutcomes = [];
    private HacksetDef _hacksetDef;
    public HacksetDef HacksetDef => _hacksetDef;

    private bool _installedICEBreaker;

    private WorldComponent_HacksetsLetter _worldComp;
    private WorldComponent_HacksetsLetter WorldComp
    {
        get
        {
            _worldComp ??= Find.World.GetComponent<WorldComponent_HacksetsLetter>();
            return _worldComp;
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        _hacksetDef ??= CompHackable.GetHacksetDef();
    }

    public override void Hack(float amount, Pawn hacker = null)
    {
        if (ShouldApplyOutcomeNow(amount, hacker))
        {
            ApplyOutcome(hacker);
            return;
        }

        base.Hack(amount, hacker);
    }

    private void ApplyOutcome(Pawn hacker)
    {
        var outcome = GetHackingOutcome(hacker);

        if (outcome == null)
        {
            _appliedOutcomes = [];
            return;
        }

        outcome.ApplyOutcome(hacker, parent);

        _progressLastOutcome = CompHackable.GetProgress();
    }

    private bool ShouldApplyOutcomeNow(float progressAmount, Pawn hacker = null)
    {
        if (_hacksetDef == null)
            return false;

        if (hacker == null || !hacker.IsHacker())
            return false;

        WorldComp.TryToDoLetter(hacker, parent, _hacksetDef);

        float mtb = hacker.GetStatValue(StatDefOf.HackingStealth);
        float mtbUnit = progressAmount * 60f;
        float ticksSinceCheck = 1f;

        if (CompHackable.GetProgress() - _progressLastOutcome <= MIN_TICKS_BETWEEN_OUTCOMES)
            return false;

        if (!Rand.MTBEventOccurs(mtb, mtbUnit, ticksSinceCheck))
            return false;

        return true;
    }

    private HackingOutcomeWorker GetHackingOutcome(Pawn hacker)
    {
        List<HackingOutcomeDef> allDefs = _hacksetDef.hackingOutcomes;

        if (_installedICEBreaker)
            allDefs = [.. allDefs.Where(x => !x.disabledByBreaker)];

        var outcome = allDefs
            .Where(x => x.Worker.CanApplyOnPawn(hacker)
            && !_appliedOutcomes.Contains(x))
            .RandomElementByWeight(def => def.weight).Worker;

        return outcome;
    }

    public AcceptanceReport CanAcceptICEBreaker(CompICEBreaker compICEBreaker)
    {
        if (_installedICEBreaker)
            return "USH_HE_AlreadyBroken".Translate();

        if (_hacksetDef != USH_DefOf.USH_BlackICE)
            return "USH_HE_NeedsBlackICE".Translate();

        return true;
    }

    public void AddICEBreaker(CompICEBreaker compICEBreaker)
    {
        if (_installedICEBreaker)
            return;

        if (Rand.Chance(compICEBreaker.Props.failChance))
        {
            ICEBreakerFail(compICEBreaker);
            return;
        }

        CyberUtils.MakeHackingOutcomeEffect(parent, "USH_HE_ICEBreakerInstalled".Translate());

        _installedICEBreaker = true;
    }

    private void ICEBreakerFail(CompICEBreaker compICEBreaker)
    {
        GenExplosion.DoExplosion(
            parent.Position,
            parent.Map,
            compICEBreaker.Props.failExplosionRadius,
            DamageDefOf.Bomb,
            null);

        string label = "USH_HE_BreakerFailLetterLabel".Translate();
        string content = "USH_HE_BreakerFailLetter".Translate();

        Find.LetterStack.ReceiveLetter(label, content, LetterDefOf.NegativeEvent, new LookTargets(parent));
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (parent.Faction == Faction.OfPlayer)
            yield break;

        string statLabel = "USH_HE_SecurityHackset".Translate();
        string statValue = _hacksetDef == null ? "None".Translate() : _hacksetDef.LabelCap;
        string statContent = "USH_HE_SecurityHacksetDesc".Translate();

        if (_hacksetDef != null)
        {
            statContent += "\n\n";
            statContent += _hacksetDef.LabelCap + ": ";
            statContent += _hacksetDef.description.UncapitalizeFirst();
            statContent += "\n\n";
            statContent += "USH_HE_HackActionsProbabilities".Translate() + ": ";
            statContent += "\n\n";
            statContent += _hacksetDef.GetOutcomesDescription();
        }

        StatDrawEntry hacksetEntry = new(USH_DefOf.USH_Hacker, statLabel, statValue, statContent, 0);

        yield return hacksetEntry;
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new();

        sb.AppendLine(base.CompInspectStringExtra());

        if (parent.Faction == Faction.OfPlayer)
            return sb.ToString().Trim();

        if (CompHackable.IsHacked)
            return sb.ToString().Trim();

        string hacksetText = _hacksetDef == null ? "None".Translate() : _hacksetDef.LabelCap.Colorize(Color.red);
        sb.AppendLine("USH_HE_SecurityHackset".Translate() + ": " + hacksetText);

        if (_installedICEBreaker)
        {
            var disabledOutcomes = _hacksetDef.hackingOutcomes.Where(x => x.disabledByBreaker);

            if (disabledOutcomes.Count() > 0)
            {
                var disabledText = string.Join(", ", disabledOutcomes.Select(x => x.LabelCap));
                var toAppend = "USH_HE_DisabledOutcomes".Translate() + ": " + disabledText;

                sb.AppendLine(toAppend.Colorize(ColorLibrary.LimeGreen));
            }
        }

        return sb.ToString().Trim();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Defs.Look(ref _hacksetDef, nameof(_hacksetDef));
        Scribe_Values.Look(ref _installedICEBreaker, nameof(_installedICEBreaker));
    }
}