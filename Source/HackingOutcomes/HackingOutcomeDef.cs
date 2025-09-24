using System;
using Verse;

namespace USH_HE;

public class HackingOutcomeDef : Def
{
    public float weight;
    public HediffDef giveHediff;
    public Type workerClass;
    public EffecterDef effecterDef;
    public bool doMessage;
    public bool worksWithPod;
    public bool disabledByBreaker;

    private HackingOutcomeWorker worker;
    public HackingOutcomeWorker Worker
        => worker ??= (HackingOutcomeWorker)Activator.CreateInstance(workerClass, this);
}