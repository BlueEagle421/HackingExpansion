using RimWorld;
using Verse;

namespace USH_HE;

public class StatWorker_Hacker : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        if (!base.ShouldShowFor(req))
            return false;

        if (req.Thing != null && req.Thing is Pawn pawn)
            return pawn.IsHacker();

        return false;
    }
}
