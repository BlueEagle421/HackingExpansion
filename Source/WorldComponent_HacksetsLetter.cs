using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USH_HE;

public class WorldComponent_HacksetsLetter(World world) : WorldComponent(world)
{
    private bool _didLetter;

    public void TryToDoLetter(Pawn p, Thing building, HacksetDef hackset)
    {
        if (_didLetter)
            return;

        if (hackset == null)
            return;

        DoLetter(p, building, hackset);
    }


    private void DoLetter(Pawn p, Thing building, HacksetDef hackset)
    {
        string label = "USH_HE_CyberspaceLetterLabel".Translate();

        string content = "USH_HE_CyberspaceLetter"
            .Translate(p.Named("PAWN"), building.Named("BUILDING"), hackset.Named("HACKSET"));

        Find.LetterStack.ReceiveLetter(label, content, LetterDefOf.NeutralEvent);

        _didLetter = true;
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.Look(ref _didLetter, nameof(_didLetter));
    }
}