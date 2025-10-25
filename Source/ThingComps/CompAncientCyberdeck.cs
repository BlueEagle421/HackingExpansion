using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace USH_HE;

public class CompProperties_AncientCyberdeckHackable : CompProperties_Hackable
{
    public CompProperties_AncientCyberdeckHackable()
        => compClass = typeof(CompAncientCyberdeck);
}

public class CompAncientCyberdeck : CompHackable
{
    private IntRange _raidDelayTicksRange = new(2500, 2500 * 3);

    protected override void OnHacked(Pawn hacker = null, bool suppressMessages = false)
    {
        base.OnHacked(hacker, suppressMessages);

        Thing t = ThingMaker.MakeThing(USH_DefOf.USH_Cyberlink);
        GenPlace.TryPlaceThing(t, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);

        SoundDefOf.CryptosleepCasket_Eject.PlayOneShot(parent);

        if (!TryFindRandomEnemyFaction(out var faction))
            return;

        SendLetter(faction);
        DelayedRaidUtility.TriggerDelayedRaid(faction, parent.Map, _raidDelayTicksRange.RandomInRange);
    }

    private void SendLetter(Faction faction)
    {
        string label = "USH_HE_RaidLetterLabel".Translate();

        string content = "USH_HE_RaidLetter".Translate(faction.Named("FACTION"));

        Find.LetterStack.ReceiveLetter(label, content, LetterDefOf.ThreatBig);
    }

    private bool TryFindRandomEnemyFaction(out Faction faction)
    {
        faction = Find.FactionManager
            .RandomRaidableEnemyFaction(
                allowHidden: false,
                allowDefeated: false,
                allowNonHumanlike: false,
                TechLevel.Industrial);

        return faction != null;
    }
}
