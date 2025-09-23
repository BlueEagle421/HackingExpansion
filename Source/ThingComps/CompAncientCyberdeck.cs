using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_HE;

public class ScheduledSignal : IExposable
{
    public string tag;
    public int ticksLeft;

    public ScheduledSignal() { }
    public ScheduledSignal(string tag, int ticksLeft)
    {
        this.tag = tag;
        this.ticksLeft = ticksLeft;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref tag, nameof(tag));
        Scribe_Values.Look(ref ticksLeft, nameof(ticksLeft));
    }
}

public class SignalSchedulerMapComponent(Map map) : MapComponent(map)
{
    private List<ScheduledSignal> _scheduled = [];

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        if (_scheduled.Count == 0) return;

        for (int i = _scheduled.Count - 1; i >= 0; i--)
        {
            var s = _scheduled[i];
            s.ticksLeft--;
            if (s.ticksLeft <= 0)
            {
                Find.SignalManager.SendSignal(new Signal(s.tag));
                _scheduled.RemoveAt(i);
            }
        }
    }

    public void ScheduleSignal(string tag, int delayTicks)
    {
        if (string.IsNullOrEmpty(tag) || delayTicks <= 0) return;
        _scheduled.Add(new ScheduledSignal(tag, delayTicks));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _scheduled, nameof(_scheduled), LookMode.Deep);
        _scheduled ??= [];
    }
}

public class CompProperties_AncientCyberdeckHackable : CompProperties_Hackable
{
    public CompProperties_AncientCyberdeckHackable()
        => compClass = typeof(CompAncientCyberdeck);
}


public class CompAncientCyberdeck : CompHackable
{
    private IntRange _raidDelayTicksRange = new(2500, 2500 * 3);

    protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
    {
        base.OnHacked(hacker, suppressMessages);

        Thing t = ThingMaker.MakeThing(USH_DefOf.USH_Cyberlink);
        GenPlace.TryPlaceThing(t, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);

        SoundDefOf.CryptosleepCasket_Eject.PlayOneShot(parent);

        if (!TryFindRandomEnemyFaction(out var faction))
            return;

        SendLetter(faction);
        TriggerDelayedRaid(faction);
    }

    private void SendLetter(Faction faction)
    {
        string label = "USH_HE_RaidLetterLabel".Translate();

        string content = "USH_HE_RaidLetter".Translate(faction.Named("FACTION"));

        Find.LetterStack.ReceiveLetter(label, content, LetterDefOf.ThreatBig);
    }

    private void TriggerDelayedRaid(Faction faction)
    {
        var obj = (SignalAction_Incident)ThingMaker.MakeThing(ThingDefOf.SignalAction_Incident);
        obj.incident = IncidentDefOf.RaidEnemy;
        obj.incidentParms = RaidParms(faction);

        string tag = "TriggerRaid_" + Find.UniqueIDsManager.GetNextThingID();
        obj.signalTag = tag;

        GenSpawn.Spawn(obj, parent.InteractionCell, parent.Map);

        var comp = parent.Map.GetComponent<SignalSchedulerMapComponent>();
        comp.ScheduleSignal(tag, _raidDelayTicksRange.RandomInRange);
    }

    private IncidentParms RaidParms(Faction faction)
    {
        IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, parent.Map);

        parms.faction = faction;
        parms.forced = true;

        parms.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
        parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

        parms.points = Mathf.Max(parms.points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));

        return parms;
    }

    private bool TryFindRandomEnemyFaction(out Faction faction)
    {
        faction = Find.FactionManager
            .RandomRaidableEnemyFaction(
                allowHidden: false,
                allowDefeated: false,
                allowNonHumanlike: false,
                TechLevel.Industrial);

        return faction != null;
    }
}
