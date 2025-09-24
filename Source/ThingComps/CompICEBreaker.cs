using Verse;

namespace USH_HE;

public class CompProperties_ICEBreaker : CompProperties
{
    public CompProperties_ICEBreaker()
        => compClass = typeof(CompICEBreaker);
}

public class CompICEBreaker : ThingComp { }