using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace USH_HE;

public class HE_Mod : Mod
{

    public HE_Mod(ModContentPack content) : base(content)
    {
        InitHarmony();
    }

    private void InitHarmony()
    {
        Harmony harmony = new("HackingExpansion");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}