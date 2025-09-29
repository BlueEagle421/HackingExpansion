using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace USH_HE;

public class MapComponent_CyberpodManager(Map map) : MapComponent(map)
{
    private readonly List<Building_Cyberpod> _pods = [];
    public List<Building_Cyberpod> Pods => _pods;
    private const int CHECK_TICK_INTERVAL = 250;
    private int _ticksPassed;

    public override void MapComponentTick()
    {
        base.MapComponentTick();

        _ticksPassed++;

        if (_ticksPassed >= CHECK_TICK_INTERVAL)
        {
            _pods.ForEach(x => x.TryToStartHacking());
            _ticksPassed = 0;
        }
    }

    public void Register(Building_Cyberpod pod)
    {
        if (pod == null) return;
        if (!_pods.Contains(pod)) _pods.Add(pod);
    }

    public void Unregister(Building_Cyberpod pod)
    {
        if (pod == null) return;
        _pods.Remove(pod);
    }

    public List<CompHackable> GetAllHackables
    {
        get
        {
            var thingsToCheck = new List<Thing>();
            thingsToCheck.AddRange(map.mapPawns.AllPawnsSpawned.Cast<Thing>());
            thingsToCheck.AddRange(map.listerBuildings.allBuildingsNonColonist.Cast<Thing>());
            thingsToCheck.AddRange(map.listerBuildings.allBuildingsColonist.Cast<Thing>());

            var comps = thingsToCheck
                .Select(t => t.TryGetComp<CompHackable>())
                .Where(c => c != null)
                .ToList();

            return comps;
        }
    }
}


[StaticConstructorOnStartup]
public class Building_Cyberpod : Building_Casket, ISuspendableThingHolder
{
    private const int WAKING_UP_TICK_DURATION = 5000; //2 hours
    private MapComponent_CyberpodManager Manager => Map?.GetComponent<MapComponent_CyberpodManager>();

    private CompPowerTrader _compPower;
    private CompBreakdownable _compBreakdownable;
    private CompStunnable _compStunnable;
    private CompAffectedByFacilities _compFacilities;

    private Pawn Hacker => ContainedThing as Pawn;
    private CompHackable _currentlyHacking;
    private readonly TargetingParameters HackTargetingParameters;

    private static readonly Material LineMatGreen = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, Color.green);
    private static Texture2D IconIgnoreAutohack { get; } = ContentFinder<Texture2D>.Get("UI/Gizmos/IgnoreAutoHack");
    private static Texture2D IconHack { get; } = ContentFinder<Texture2D>.Get("UI/Gizmos/HackTarget");
    private static Texture2D IconStartWaking { get; } = ContentFinder<Texture2D>.Get("UI/Gizmos/StartWakingUp");
    private static Texture2D IconStopWaking { get; } = ContentFinder<Texture2D>.Get("UI/Gizmos/StopWakingUp");

    [Unsaved(false)] private Effecter _barEffecter;
    [Unsaved(false)] private Effecter _hackEffecter;
    [Unsaved(false)] private Effecter _propsEffecter;
    private Sustainer _soundSustainer;
    private bool _ignoreAutoHack;
    private bool _isAutohacking = true;
    private bool _isWakingUp;
    private int _wakeUpTicksPassed;

    public override bool CanOpen => false;

    public bool IsContentsSuspended
    {
        get
        {
            var coprocessor = _compFacilities.LinkedFacilitiesListForReading
                .Find(x => x.def == USH_DefOf.USH_VitalityCoprocessor);

            if (coprocessor == null)
                return false;

            if (!_compFacilities.IsFacilityActive(coprocessor))
                return false;

            return true;
        }
    }

    public Building_Cyberpod()
    {
        HackTargetingParameters = new()
        {
            canTargetPawns = true,
            canTargetSelf = false,
            canTargetBuildings = true,

            validator = CanTargetThing
        };
    }

    private bool CanTargetThing(TargetInfo targetInfo)
    {
        if (targetInfo.Thing is null)
            return false;

        if (!targetInfo.Thing.TryGetComp(out CompHackable _))
            return false;

        return true;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _compPower = GetComp<CompPowerTrader>();
        _compBreakdownable = GetComp<CompBreakdownable>();
        _compStunnable = GetComp<CompStunnable>();
        _compFacilities = GetComp<CompAffectedByFacilities>();

        Manager?.Register(this);
        TryToStartHacking();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        Manager?.Unregister(this);
        base.Destroy(mode);
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();

        if (_currentlyHacking == null)
            return;

        GenDraw.DrawLineBetween(DrawPos, _currentlyHacking.parent.DrawPos, LineMatGreen);
    }

    protected override void Tick()
    {
        base.Tick();

        WakingUpTick();
        HackTick();
    }

    private void WakingUpTick()
    {
        if (!_isWakingUp)
            return;

        _wakeUpTicksPassed++;

        if (_wakeUpTicksPassed >= WAKING_UP_TICK_DURATION)
            EjectContents();
    }

    private void HackTick()
    {
        if (!CanHackNow() || !CanBeHacked(_currentlyHacking, _isAutohacking))
        {
            StopHacking();
            return;
        }

        SustainHackEffect();
        SustainPropsEffect();
        SustainProgressBar();
        SustainSound();

        Hack();
    }

    private void Hack()
    {
        float amount = Hacker.GetStatValue(StatDefOf.HackingSpeed);
        amount *= this.GetStatValue(USH_DefOf.USH_HackingSpeedMultiplier);

        _currentlyHacking.Hack(amount, Hacker);
        Hacker.skills.Learn(SkillDefOf.Intellectual, 0.1f);

        if (_currentlyHacking.IsHacked)
            HackingCompleted();
    }

    private void SustainHackEffect()
    {
        _hackEffecter ??= EffecterDefOf.Hacking.Spawn();
        _hackEffecter?.EffectTick(this, _currentlyHacking.parent);
    }

    private void SustainPropsEffect()
    {
        if (_currentlyHacking.Props.effectHacking == null)
            return;

        _propsEffecter ??= _currentlyHacking.Props.effectHacking.Spawn();
        var pos = _currentlyHacking.parent
            .OccupiedRect()
            .ClosestCellTo(Hacker.Position);

        _propsEffecter?.EffectTick(this, new TargetInfo(pos, Map));
    }

    private void SustainProgressBar()
    {
        _barEffecter ??= EffecterDefOf.ProgressBar.Spawn();
        _barEffecter?.EffectTick(this, TargetInfo.Invalid);

        MoteProgressBar mote = ((SubEffecter_ProgressBar)_barEffecter.children[0]).mote;
        mote.progress = _currentlyHacking.ProgressPercent;
        mote.offsetZ = -0.5f;
    }

    private void SustainSound()
    {
        if (_soundSustainer == null || _soundSustainer.Ended)
            _soundSustainer = SoundDefOf.Hacking_InProgress
                .TrySpawnSustainer(SoundInfo.InMap(this));

        _soundSustainer?.Maintain();
    }

    private bool CanHackNow()
    {
        if (!Spawned)
            return false;

        if (_isWakingUp)
            return false;

        if (!_compPower.PowerOn)
            return false;

        if (_compBreakdownable.BrokenDown)
            return false;

        if (_compStunnable.StunHandler.Stunned)
            return false;

        return true;
    }

    public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        bool accepted = base.TryAcceptThing(thing, allowSpecialEffects);

        if (!accepted)
            return false;

        TryToStartHacking();

        return true;
    }

    public override void EjectContents()
    {
        ThingDef filthSlime = ThingDefOf.Filth_Slime;
        FilthMaker.TryMakeFilth(InteractionCell, Map, filthSlime, Rand.Range(8, 12));

        if (!Destroyed)
            SoundDefOf.CryptosleepCasket_Eject
                .PlayOneShot(SoundInfo.InMap(new TargetInfo(Position, Map)));

        _isWakingUp = false;

        base.EjectContents();
    }

    private CompHackable GetAnyMapHackable()
    {
        if (Manager == null)
            return null;

        var hackables = Manager.GetAllHackables;
        if (hackables == null || hackables.Count == 0)
            return null;

        var hackable = hackables.FirstOrDefault(comp => CanBeHacked(comp, true));
        if (hackable == null)
            return null;

        return hackable;
    }

    public void TryToStartHacking()
        => TryToStartHacking(GetAnyMapHackable());

    private void TryToStartHacking(CompHackable compHackable, bool chosenAutomatically = true, bool force = false)
    {
        if (!force && _currentlyHacking != null)
            return;

        if (!CanBeHacked(compHackable, chosenAutomatically))
            return;

        if (!CanHackNow())
            return;

        _isAutohacking = chosenAutomatically;

        SoundDefOf.Hacking_Started.PlayOneShot(this);

        _currentlyHacking = compHackable;
    }

    private bool CanBeHacked(CompHackable compHackable, bool autohackCheck = true)
    {
        if (Hacker == null)
            return false;

        if (!Hacker.CanHack(compHackable))
            return false;

        if (autohackCheck && !compHackable.Autohack && !_ignoreAutoHack)
            return false;

        if (compHackable.parent.TryGetComp(out CompDataSourceProtected compProtected)
            && compProtected.HacksetDef == USH_DefOf.USH_BlackICE)
            return false;

        return true;
    }

    private void HackingCompleted()
    {
        SoundDefOf.Hacking_Completed.PlayOneShot(_currentlyHacking.parent);
        _currentlyHacking.Props.hackingCompletedSound?.PlayOneShot(_currentlyHacking.parent);

        StopHacking();

        TryToStartHacking();
    }

    private void StopHacking()
    {
        if (_currentlyHacking == null)
            return;

        _hackEffecter?.Cleanup();
        _hackEffecter = null;

        _propsEffecter?.Cleanup();
        _propsEffecter = null;

        _barEffecter?.Cleanup();
        _barEffecter = null;

        _soundSustainer?.End();
        _soundSustainer = null;

        _currentlyHacking = null;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var entry in base.GetGizmos())
            yield return entry;

        if (Faction != Faction.OfPlayer)
            yield break;

        if (Hacker != null)
        {
            yield return _isWakingUp ? StopWakingGizmo() : StartWakingGizmo();

            yield return HackGizmo();
        }

        yield return IgnoreAutoHackGizmo();
    }

    private Gizmo IgnoreAutoHackGizmo()
        => new Command_Toggle()
        {
            icon = IconIgnoreAutohack,
            isActive = () => _ignoreAutoHack,
            defaultLabel = "USH_HE_CommandIgnoreAutoHack".Translate(),
            defaultDesc = "USH_HE_CommandIgnoreAutoHackDesc".Translate(),
            toggleAction = () => _ignoreAutoHack = !_ignoreAutoHack,
        };

    private Gizmo StartWakingGizmo()
        => new Command_Action()
        {
            defaultLabel = "USH_HE_CommandStartWaking".Translate(),
            defaultDesc = "USH_HE_CommandStartWakingDesc".Translate(),
            icon = IconStartWaking,
            groupable = false,
            shrinkable = true,
            action = () =>
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                _isWakingUp = true;
                _wakeUpTicksPassed = 0;
            },
        };

    private Gizmo StopWakingGizmo()
        => new Command_Action()
        {
            defaultLabel = "USH_HE_CommandStopWaking".Translate(),
            defaultDesc = "USH_HE_CommandStopWakingDesc".Translate(),
            icon = IconStopWaking,
            groupable = false,
            shrinkable = true,
            action = () =>
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                _isWakingUp = false;
                _wakeUpTicksPassed = 0;
            },
        };

    private Gizmo HackGizmo()
        => new Command_Action()
        {
            defaultLabel = "USH_HE_OrderHackTarget".Translate() + "...",
            defaultDesc = "USH_HE_OrderHackTargetDesc".Translate(),
            icon = IconHack,
            groupable = false,
            shrinkable = true,
            action = () =>
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

                Find.Targeter.BeginTargeting(
                    HackTargetingParameters,
                    (target) =>
                    {
                        StopHacking();
                        TryToStartHacking(target.Thing.TryGetComp<CompHackable>(), false, true);
                    },
                    (target) => GenDraw.DrawTargetHighlight(target),
                    (target) => Hacker.CanHack(target).Accepted,
                    null, null, IconHack, playSoundOnAction: true,
                    (target) =>
                    {
                        string mouseCommand = "USH_HE_CommandChooseToHack".Translate();

                        if (target.Thing is null || !target.Thing.TryGetComp(out CompHackable compHackable))
                        {
                            Widgets.MouseAttachedLabel(mouseCommand);
                            return;
                        }

                        AcceptanceReport hackReport = Hacker.CanHack(target.Thing);
                        if (!hackReport.Accepted)
                        {
                            var reportMsg = hackReport.Reason.CapitalizeFirst().Colorize(ColorLibrary.RedReadable);
                            mouseCommand = "CannotChooseHacker".Translate() + ": " + reportMsg;
                            Widgets.MouseAttachedLabel(mouseCommand);
                            return;
                        }

                        Widgets.MouseAttachedLabel(mouseCommand, 0, 0, CyberUtils.HackColor);
                    });
            },
        };

    public override string GetInspectString()
    {
        StringBuilder sb = new();
        sb.AppendLine(base.GetInspectString());

        if (!Spawned)
            return sb.ToString();

        StatDef speedStat = USH_DefOf.USH_HackingSpeedMultiplier;
        sb.AppendLine(GetStatReportText(speedStat));

        StatDef stealthStat = USH_DefOf.USH_HackingStealthMultiplier;
        sb.AppendLine(GetStatReportText(stealthStat));

        if (_isWakingUp)
            sb.AppendLine(GetWakingUpText());
        else if (_currentlyHacking != null)
            sb.AppendLine(GetHackingProgressText());

        return sb.ToString().Trim();
    }

    private string GetStatReportText(StatDef statDef)
    {
        string statDefText = this.GetStatValue(statDef).ToStringPercent();
        return statDef.LabelCap + ": " + statDefText;
    }

    private string GetWakingUpText()
    {
        int ticksLeft = WAKING_UP_TICK_DURATION - _wakeUpTicksPassed;
        var timeText = ticksLeft.ToStringTicksToPeriod()
            .Colorize(ColorLibrary.Cyan);

        var timeArg = ("DurationLeft".Translate(timeText) + ".").Named("TIME");

        return "USH_HE_WakingUpProgress".Translate(timeArg);
    }

    private string GetHackingProgressText()
    {
        var comp = _currentlyHacking;

        var thingArg = comp.parent.Named("THING");

        string progressText = comp.GetProgress().ToStringWorkAmount();
        string defenseText = comp.defence.ToStringWorkAmount();

        var progressArg = $"{progressText} / {defenseText}"
            .Named("PROGRESS");

        return "USH_HE_Hacking".Translate(thingArg, progressArg);
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
    {
        if (!myPawn.IsHacker())
        {
            yield return new("USH_HE_CannotUseNotHacker".Translate(), null);
            yield break;
        }

        if (myPawn.IsQuestLodger())
        {
            yield return new("CannotUseReason".Translate("USH_HE_CyberpodGuestsNotAllowed".Translate()), null);
            yield break;
        }

        foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(myPawn))
            yield return floatMenuOption;

        if (innerContainer.Count != 0)
            yield break;

        if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            yield break;
        }

        JobDef jobDef = USH_DefOf.USH_EnterCyberpod;
        string label = "USH_HE_EnterCyberpod".Translate();

        void Action()
        {
            myPawn.jobs.StopAll(true);
            myPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(jobDef, this));
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, Action), myPawn, this);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _ignoreAutoHack, nameof(_ignoreAutoHack));

        Scribe_Values.Look(ref _isWakingUp, nameof(_isWakingUp));
        Scribe_Values.Look(ref _wakeUpTicksPassed, nameof(_wakeUpTicksPassed));
    }
}
