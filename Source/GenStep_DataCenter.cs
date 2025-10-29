using RimWorld;
using Verse;

namespace USH_HE;

public class GenStep_DataCenter : GenStep_Scatterer
{
    private const float STEP_CHANCE = 0.35f;
    private const float CRATE_SPAWN_CHANCE = 0.6f;
    private const int NEARBY_RADIUS = 3;
    private const int TERRAIN_SIZE = 30;
    private const int CLEAR_RADIUS = 5;
    private const int MIN_CENTER_DISTANCE = 2;
    private static readonly IntRange _steelSlagRange = new(2, 4);
    private static readonly IntRange _ancientCrateRange = new(1, 3);
    public override int SeedPart => 345173948;

    protected override bool ShouldSkipMap(Map map)
    {
        if (map.IsPlayerHome)
            return true;

        if (!Rand.Chance(STEP_CHANCE))
            return true;

        return base.ShouldSkipMap(map);
    }

    protected override bool CanScatterAt(IntVec3 loc, Map map)
    {
        if (!base.CanScatterAt(loc, map))
            return false;

        if (loc.Fogged(map))
            return false;

        CellRect cellRect = CellRect.CenteredOn(loc, CLEAR_RADIUS);

        int newZ = cellRect.minZ - 1;
        for (int i = cellRect.minX; i <= cellRect.maxX; i++)
        {
            IntVec3 c = new(i, 0, newZ);
            if (!c.InBounds(map) || !c.Walkable(map))
                return false;
        }

        return true;
    }

    protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
    {
        GenDataCenter(loc, map);

        if (Rand.Chance(CRATE_SPAWN_CHANCE))
        {
            GenNearby(loc, map, USH_DefOf.USH_EncryptedCrate);

            for (int i = 0; i < _ancientCrateRange.RandomInRange; i++)
                GenNearby(loc, map, ThingDefOf.AncientSpacerCrate);
        }

        for (int i = 0; i < _steelSlagRange.RandomInRange; i++)
            GenNearby(loc, map, ThingDefOf.ChunkSlagSteel);

        GenTerrain(loc, map);
    }

    private void GenTerrain(IntVec3 loc, Map map)
    {
        foreach (IntVec3 item in GridShapeMaker.IrregularLump(loc, map, TERRAIN_SIZE))
            map.terrainGrid.SetTerrain(item, TerrainDefOf.AncientConcrete);
    }

    public static void GenDataCenter(IntVec3 loc, Map map)
    {
        Thing deck = ThingMaker.MakeThing(USH_DefOf.USH_DataCenter);
        GenSpawn.Spawn(deck, loc, map);
    }

    private void GenNearby(IntVec3 loc, Map map, ThingDef def)
    {
        if (!CellFinder.TryFindRandomCellNear(loc, map, NEARBY_RADIUS, c => CellValidator(map, c, loc), out var pos))
            return;

        Thing t = ThingMaker.MakeThing(def);
        GenSpawn.Spawn(t, pos, map);
    }

    private bool CellValidator(Map map, IntVec3 c, IntVec3 centerPos)
    {
        if (!c.Standable(map))
            return false;

        if (c.DistanceTo(centerPos) <= MIN_CENTER_DISTANCE)
            return false;

        return true;
    }
}