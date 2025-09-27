using RimWorld;
using Verse;

namespace USH_HE;


public class LearningAbilityExtension : DefModExtension
{
    public VEF.Abilities.AbilityDef abilityDef;
    public float learningPoints;
}


public class Hediff_LearningAbility : HediffWithComps
{
    private float _progress;
    private LearningAbilityExtension _ext;
    private LearningAbilityExtension Ext
    {
        get
        {
            _ext ??= def.GetModExtension<LearningAbilityExtension>();
            return _ext;
        }
    }
    public override string Label => $"{base.Label} ({"USH_HE_Learning".Translate()} {_progress / 60f:0}/{Ext.learningPoints})";

    public virtual void Hack(float amount, Pawn hacker = null)
    {
        _progress += amount;

        if (_progress / 60f >= Ext.learningPoints)
        {
            pawn.health.RemoveHediff(this);
            pawn.GetComp<VEF.Abilities.CompAbilities>()?.GiveAbility(Ext.abilityDef);

            string label = "USH_HE_AbilityLearnedLetterLabel".Translate(Ext.abilityDef.LabelCap);

            string content = "USH_HE_AbilityLearnedLetter"
                .Translate(Ext.abilityDef.label, pawn.Named("PAWN"));

            Find.LetterStack.ReceiveLetter(label, content, LetterDefOf.PositiveEvent);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _progress, nameof(_progress));
    }
}
