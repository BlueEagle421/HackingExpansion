using System.Linq;
using RimWorld;
using Verse;

namespace USH_HE;

public class IncidentWorker_BlackBoxDrop : IncidentWorker
{
    private const int BIG_INT = 999999;
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms))
            return false;

        Map map = (Map)parms.target;
        return TryFindBlackBoxDropCell(map.Center, map, BIG_INT, out _);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Map map = (Map)parms.target;

        if (!TryFindBlackBoxDropCell(map.Center, map, BIG_INT, out var pos))
            return false;

        SpawnBlackBoxes(pos, map);

        return true;
    }

    private void SpawnBlackBoxes(IntVec3 firstChunkPos, Map map)
    {
        if (!TryFindBlackBoxDropCell(firstChunkPos, map, 5, out var pos))
            return;

        SpawnChunk(pos, map);
    }

    private void SpawnChunk(IntVec3 pos, Map map)
    {
        Skyfaller skyfaller = SkyfallerMaker.SpawnSkyfaller(
            USH_DefOf.USH_BlackBoxIncoming,
            USH_DefOf.USH_BlackBox,
            pos,
            map);

        Faction owner = Find.FactionManager
            .RandomRaidableEnemyFaction(
                allowHidden: false,
                allowDefeated: false,
                allowNonHumanlike: false,
                TechLevel.Industrial);

        Faction compromised = Find.FactionManager.AllFactions
            .ToList()
            .Find(owner.HostileTo);

        foreach (var entry in skyfaller.innerContainer)
            if (entry is Thing t
                && t.TryGetComp(out CompBlackBox compBlackBox))
                compBlackBox.Initialize(owner, compromised);

        var label = "USH_HE_BlackBoxLetterLabel".Translate();
        var desc = "USH_HE_BlackBoxLetter".Translate(owner.Named("OWNER"), compromised.Named("COMPROMISED"));

        Find.LetterStack.ReceiveLetter(label, desc, LetterDefOf.NeutralEvent, new LookTargets(pos, map));
    }

    private bool TryFindBlackBoxDropCell(IntVec3 nearLoc, Map map, int maxDist, out IntVec3 pos)
    {
        return CellFinderLoose.TryFindSkyfallerCell(USH_DefOf.USH_BlackBoxIncoming, map, USH_DefOf.USH_BlackBox.terrainAffordanceNeeded, out pos, 10, nearLoc, maxDist);
    }
}
