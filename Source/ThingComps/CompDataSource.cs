using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_HE;

public class CompProperties_DataSource : CompProperties
{
    public CompProperties_DataSource()
        => compClass = typeof(CompDataSource);
}

[StaticConstructorOnStartup]
public class CompDataSource : ThingComp
{
    private List<OutputData> _outputThings = [];
    private int _currentOutputIndex;
    private float _progress;
    protected bool _designatedForRipping, _isBeingRipped;
    private static Texture2D IconRipData { get; } = ContentFinder<Texture2D>.Get("UI/Gizmos/RipData");
    private static Texture2D IconCancel { get; } = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
    private CompHackable _compHackable;
    protected CompHackable CompHackable
    {
        get
        {
            _compHackable ??= parent.TryGetComp<CompHackable>();
            return _compHackable;
        }
    }

    public bool CanOutput
        => _isBeingRipped
        && !CompHackable.IsHacked;

    private OutputData CurrentOutputData
    {
        get
        {
            try
            {
                return _outputThings[_currentOutputIndex];
            }
            catch
            {
                return null;
            }
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        if (_outputThings == null || !_outputThings.Any())
            foreach (var entry in CyberUtils.AllCyberDataDefs)
                _outputThings.Add(new(entry, CompHackable));
    }

    public virtual void ProcessHacked(Pawn hacker, bool suppressMessages)
    {
        HandleBrokenExecSpawn(hacker);
    }

    private void HandleBrokenExecSpawn(Pawn hacker)
    {
        if (!_isBeingRipped)
            return;

        if (this is not CompDataSourceProtected compProtected)
            return;

        ThingDef def = USH_DefOf.USH_BrokenExecData;

        bool matchingHackset = def
            .descriptionHyperlinks
            .Select(x => x.def)
            .Contains(compProtected.HacksetDef);

        if (!matchingHackset)
            return;

        var resExt = def.GetModExtension<ResearchPrerequisitesExtension>();

        if (!resExt.IsUnlocked())
            return;

        ProduceOutput(def, hacker);
    }

    public virtual void Hack(float amount, Pawn hacker = null)
    {
        HackForRipping();

        HackForLearning(amount, hacker);

        HackForOutput(amount, hacker);
    }

    private void HackForRipping()
    {
        if (!_designatedForRipping)
            return;

        if (this is CompDataSourceProtected compProtected)
            compProtected.IsHacksetActive = true;

        _isBeingRipped = true;
        _designatedForRipping = false;
        UpdateDesignation();

        CyberUtils.MakeHackingOutcomeEffect(parent, "USH_HE_RipperInstalled".Translate());
    }

    private void HackForLearning(float amount, Pawn hacker)
    {
        if (hacker == null)
            return;

        var hediffs = hacker.health.hediffSet.hediffs;

        for (int i = 0; i < hediffs.Count; i++)
            if (hediffs[i] is Hediff_LearningAbility hediffLearning)
                hediffLearning.Hack(amount);
    }

    private void HackForOutput(float amount, Pawn hacker)
    {
        if (!CanOutput)
            return;

        if (CurrentOutputData.AmountLeft <= 0)
            return;

        _progress += amount;

        if (_progress >= CurrentOutputData.HackCost)
            ProduceOutput(CurrentOutputData.Def, hacker);
    }

    private void ProduceOutput(ThingDef thingDef, Pawn hacker)
    {
        USH_DefOf.USH_DataRipped.PlayOneShot(parent);

        Thing newThing = ThingMaker.MakeThing(thingDef);
        GenPlace.TryPlaceThing(newThing, GetProductPosition(hacker), parent.Map, ThingPlaceMode.Near);

        CurrentOutputData.AmountLeft--;

        _progress = 0;
    }

    private IntVec3 GetProductPosition(Pawn hacker)
    {
        if (hacker == null)
            return parent.InteractionCell;

        var pod = hacker.GetHoldingCyberpod();

        if (pod != null)
            return pod.InteractionCell;

        IntVec3 pos = GenAdj.CellsAdjacent8Way(hacker)
            .Where(x => x.Walkable(hacker.Map))
            .RandomElement();

        return pos;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra())
            yield return gizmo;

        if (!_isBeingRipped
            && _outputThings.Exists(x => x.IsUnlocked())
            && parent.Faction != Faction.OfPlayer)
            yield return RipDataDesignationGizmo();

        if (CanOutput)
            yield return SelectOutputGizmo();
    }

    private Gizmo RipDataDesignationGizmo()
    {
        Command_Action command_Action = new()
        {

            defaultLabel = _designatedForRipping
                ? "USH_HE_CommandCancelRipDesignation".Translate()
                : "USH_HE_CommandRipDesignation".Translate(),

            defaultDesc = _designatedForRipping
                ? "USH_HE_CommandCancelRipDesignationDesc".Translate()
                : "USH_HE_CommandRipDesignationDesc".Translate(),

            icon = _designatedForRipping
                ? IconCancel
                : IconRipData,

            groupable = true,
            action = delegate
            {
                var toPlay = _designatedForRipping
                    ? SoundDefOf.Designate_Cancel
                    : SoundDefOf.Tick_Low;

                toPlay.PlayOneShotOnCamera();

                _designatedForRipping = !_designatedForRipping;
                UpdateDesignation();

            }
        };

        return command_Action;
    }

    protected virtual void UpdateDesignation()
    {
        Find.World.GetComponent<WorldComponent_HacksetsLetter>().TryToDoRippingMessage();

        Designation designation = parent.Map.designationManager.DesignationOn(parent, USH_DefOf.USH_RipData);

        if (designation == null)
            parent.Map.designationManager.AddDesignation(new Designation(parent, USH_DefOf.USH_RipData));
        else
            designation.Delete();
    }

    private Gizmo SelectOutputGizmo()
    {
        Command_Action command_Action = new()
        {
            defaultLabel = "USH_HE_CommandChooseDataOutput".Translate() + "...",
            defaultDesc = "USH_HE_CommandChooseDataOutputDesc".Translate(),
            icon = CurrentOutputData.Tex,
            groupable = false,
            action = delegate
            {
                List<FloatMenuOption> options = [];

                for (int i = 0; i < _outputThings.Count; i++)
                {
                    var data = _outputThings[i];

                    if (!data.IsUnlocked())
                        continue;

                    options.Add(CreateOutputOption(data, i));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        };

        return command_Action;
    }

    private FloatMenuOption CreateOutputOption(OutputData data, int index)
    {
        string text = data.Def.LabelCap;
        text += $" ({"DurationLeft".Translate(data.AmountLeft)})";

        text += $"\n{"USH_HE_HackPoints".Translate()}: {data.HackCost.ToStringWorkAmount()}"
            .Colorize(ColorLibrary.Grey);

        int capturedIndex = index;

        return new FloatMenuOption(text, delegate
        {
            if (_currentOutputIndex == capturedIndex)
                return;

            _currentOutputIndex = capturedIndex;
            _progress = 0;

        }, data.Def);
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new(base.CompInspectStringExtra());

        if (CanOutput)
        {
            string progressText = _progress.ToStringWorkAmount();
            string costText = CurrentOutputData.HackCost.ToStringWorkAmount();
            string progressInfo = $"{progressText} / {costText}";

            sb.AppendLine($"{"USH_HE_DataRippingProgress".Translate()}: {progressInfo}"
                .Colorize(ColorLibrary.RedReadable));

            sb.AppendLine(("DurationLeft".Translate(CurrentOutputData.Def.LabelCap) + ": "
                + CurrentOutputData.AmountLeft)
                .Colorize(ColorLibrary.RedReadable));
        }

        return sb.ToString().Trim();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Collections.Look(ref _outputThings, nameof(_outputThings), LookMode.Deep);

        _outputThings ??= [];

        Scribe_Values.Look(ref _currentOutputIndex, nameof(_currentOutputIndex));
        Scribe_Values.Look(ref _designatedForRipping, "_designatedForRipping");
        Scribe_Values.Look(ref _isBeingRipped, "_isBeingRipped");
    }

    private class OutputData : IExposable
    {
        private float _hackCost;
        public float HackCost => _hackCost;
        private Texture2D _tex;
        public Texture2D Tex
        {
            get
            {
                if (_def != null)
                    _tex ??= ContentFinder<Texture2D>.Get(_def.graphic.path);

                return _tex;
            }
        }

        private ThingDef _def;
        public ThingDef Def => _def;

        private ResearchPrerequisitesExtension _cachedExt;
        public ResearchPrerequisitesExtension ResearchExt
        {
            get
            {
                _cachedExt ??= Def.GetModExtension<ResearchPrerequisitesExtension>();
                return _cachedExt;
            }
        }

        private int _amountLeft;
        public int AmountLeft
        {
            get => _amountLeft;
            set => _amountLeft = Mathf.Max(value, 0);
        }

        public OutputData() { }

        public OutputData(ThingDef thingDef, CompHackable compHackable)
        {
            var ext = thingDef.GetModExtension<RippableExtension>()
                ?? throw new Exception($"{thingDef.defName} has no {nameof(RippableExtension)} def extension");

            _hackCost = ext.hackWorkAmount;
            _def = thingDef;

            int targetCount = ext.maxPerDataSourceRange.RandomInRange;
            int maxInThisComp = (int)(compHackable.Props.defence / ext.hackWorkAmount);

            _amountLeft = Mathf.Min(maxInThisComp, targetCount);
        }

        public bool IsUnlocked()
        {
            if (ResearchExt == null)
                return true;

            return ResearchExt.IsUnlocked();
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref _def, nameof(_def));

            Scribe_Values.Look(ref _hackCost, nameof(_hackCost));
            Scribe_Values.Look(ref _amountLeft, nameof(_amountLeft));

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                _tex = null;
        }
    }
}