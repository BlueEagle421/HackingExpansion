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

public class CompDataSource : ThingComp
{
    private List<OutputData> _installedOutputThings = [];
    private int _currentOutputIndex;
    private float _progress;
    private CompHackable _compHackable;
    protected CompHackable CompHackable
    {
        get
        {
            _compHackable ??= parent.TryGetComp<CompHackable>();
            return _compHackable;
        }
    }

    public bool IsReadyToOutput
        => _installedOutputThings.Count > 0
        && !CompHackable.IsHacked;

    private OutputData CurrentOutputData
    {
        get
        {
            try
            {
                return _installedOutputThings[_currentOutputIndex];
            }
            catch
            {
                return null;
            }
        }
    }

    public AcceptanceReport CanAcceptDataRipper(IDataRipper dataRipper)
        => CanAcceptDataRipper(dataRipper.RippableThings);

    public AcceptanceReport CanAcceptDataRipper(IEnumerable<ThingDef> rippableThings)
    {
        var installedDefs = new HashSet<ThingDef>(
            _installedOutputThings.Select(x => x.Def)
        );

        bool hasNew = rippableThings
            .Any(td => td != null && !installedDefs.Contains(td));

        if (!hasNew)
            return "USH_HE_NoNewData".Translate();

        return true;
    }

    public void AddDataRipper(IDataRipper dataRipper)
        => AddDataRipper(dataRipper.RippableThings);

    public void AddDataRipper(IEnumerable<ThingDef> rippableThings)
    {
        CyberUtils.MakeHackingOutcomeEffect(parent, "USH_HE_RipperInstalled".Translate());

        foreach (var thingDef in rippableThings)
            if (!_installedOutputThings.Select(x => x.Def).Contains(thingDef))
                _installedOutputThings.Add(new(thingDef, CompHackable));
    }

    public virtual void ProcessHacked(Pawn hacker, bool suppressMessages)
    {
        HandleBrokenExecSpawn(hacker);
    }

    private void HandleBrokenExecSpawn(Pawn hacker)
    {
        if (!_installedOutputThings.Any())
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
        HackForLearning(amount, hacker);

        HackForOutput(amount, hacker);
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
        if (!IsReadyToOutput)
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

        if (IsReadyToOutput)
            yield return SelectOutputGizmo();
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

                for (int i = 0; i < _installedOutputThings.Count; i++)
                {
                    var data = _installedOutputThings[i];
                    var resExt = data.Def.GetModExtension<ResearchPrerequisitesExtension>();

                    if (!resExt.IsUnlocked())
                        continue;

                    string text = data.Def.LabelCap;
                    text += $" ({data.HackCost.ToStringWorkAmount()} {"USH_HE_HackPoints".Translate()})".Colorize(CyberUtils.HackColor);

                    int capturedIndex = i;
                    options.Add(new FloatMenuOption(text, delegate
                    {
                        if (_currentOutputIndex == capturedIndex)
                            return;

                        _currentOutputIndex = capturedIndex;
                        _progress = 0;

                    }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        };

        return command_Action;
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new(base.CompInspectStringExtra());

        if (IsReadyToOutput)
        {
            string progressText = _progress.ToStringWorkAmount();
            string costText = CurrentOutputData.HackCost.ToStringWorkAmount();

            sb.AppendLine($"{"USH_HE_DataRippingProgress".Translate()}: {progressText} / {costText}");
        }

        if (CurrentOutputData != null && !CompHackable.IsHacked)
            sb.AppendLine("Data amount left: " + CurrentOutputData.AmountLeft);

        return sb.ToString().Trim();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Collections.Look(ref _installedOutputThings, nameof(_installedOutputThings), LookMode.Deep);

        _installedOutputThings ??= [];

        Scribe_Values.Look(ref _currentOutputIndex, nameof(_currentOutputIndex));
    }

    private class OutputData : IExposable
    {
        private float _hackCost;
        public float HackCost => _hackCost;
        private Texture2D _tex;
        public Texture2D Tex => _tex;
        private ThingDef _def;
        public ThingDef Def => _def;
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
            _tex = ContentFinder<Texture2D>.Get(thingDef.graphic.path);
            _def = thingDef;

            int targetCount = ext.maxPerDataSourceRange.RandomInRange;
            int maxInThisComp = (int)(compHackable.Props.defence / ext.hackWorkAmount);

            _amountLeft = Mathf.Min(maxInThisComp, targetCount);
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref _def, nameof(_def));

            Scribe_Values.Look(ref _hackCost, nameof(_hackCost));
            Scribe_Values.Look(ref _amountLeft, nameof(_amountLeft));

            if (_def != null)
            {
                var ext = _def.GetModExtension<RippableExtension>();
                if (ext != null)
                    _hackCost = ext.hackWorkAmount;

                _tex = ContentFinder<Texture2D>.Get(_def.graphic.path);
            }
        }
    }
}