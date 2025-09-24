using Verse;

namespace USH_HE;

public class CompProperties_ICEBreaker : CompProperties
{
    public float failChance = 0.05f;
    public float failExplosionRadius = 3.9f;
    public CompProperties_ICEBreaker()
        => compClass = typeof(CompICEBreaker);
}

public class CompICEBreaker : ThingComp
{
    public CompProperties_ICEBreaker Props => (CompProperties_ICEBreaker)props;
}