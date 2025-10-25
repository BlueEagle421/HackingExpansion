using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace USH_HE;

public static class DelayedRaidUtility
{
    public static IncidentParms RaidParms(Faction faction, Map targetMap)
    {
        IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, targetMap);

        parms.faction = faction;
        parms.forced = true;

        parms.raidArrivalMode = PawnsArrivalModeDefOf.RandomDrop;
        parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

        parms.points = Mathf.Max(parms.points, faction.def.MinPointsToGeneratePawnGroup(PawnGroupKindDefOf.Combat));

        return parms;
    }

    public static void TriggerDelayedRaid(Faction faction, Map map, int delay)
    {
        var obj = (SignalAction_Incident)ThingMaker.MakeThing(ThingDefOf.SignalAction_Incident);
        obj.incident = IncidentDefOf.RaidEnemy;
        obj.incidentParms = RaidParms(faction, map);

        string tag = "TriggerRaid_" + Find.UniqueIDsManager.GetNextThingID();
        obj.signalTag = tag;

        GenSpawn.Spawn(obj, IntVec3.Zero, map);

        var comp = map.GetComponent<SignalSchedulerMapComponent>();
        comp.ScheduleSignal(tag, delay);
    }

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

}

