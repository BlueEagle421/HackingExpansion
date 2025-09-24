using RimWorld;

namespace USH_HE;

public class CompProperties_CyberTarget : CompProperties_Hackable
{
    public CompProperties_CyberTarget()
    {
        compClass = typeof(CompCyberTarget);
    }
}

public class CompCyberTarget : CompHackable
{
    private new CompProperties_CyberTarget Props
        => (CompProperties_CyberTarget)props;
}