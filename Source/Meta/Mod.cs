using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace USH_HE;

public class HE_Mod : Mod
{
    public static HE_Settings Settings { get; private set; }
    public HE_Mod(ModContentPack content) : base(content)
    {
        InitHarmony();

        Settings = GetSettings<HE_Settings>();
    }

    private void InitHarmony()
    {
        Harmony harmony = new("HackingExpansion");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);

    public override string SettingsCategory() => "Hacking Expansion";
}