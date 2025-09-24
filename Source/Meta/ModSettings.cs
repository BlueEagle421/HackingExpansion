using UnityEngine;
using Verse;

namespace USH_HE;

public class HE_Settings : ModSettings
{
    public Setting<bool> EnableMechHacking = new(true);
    public Setting<bool> EnableTurretsHacking = new(true);

    private static Vector2 _scrollPosition = new(0f, 0f);
    private static float _totalContentHeight = 1000f;
    private const float SCROLL_BAR_WIDTH_MARGIN = 18f;

    public void ResetAll()
    {
        EnableMechHacking.ToDefault();
        EnableTurretsHacking.ToDefault();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        EnableMechHacking.ExposeData(nameof(EnableMechHacking));
        EnableTurretsHacking.ExposeData(nameof(EnableTurretsHacking));
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Rect outerRect = inRect.ContractedBy(10f);

        bool scrollBarVisible = _totalContentHeight > outerRect.height;
        var scrollViewTotal = new Rect(0f, 0f, outerRect.width - (scrollBarVisible ? SCROLL_BAR_WIDTH_MARGIN : 0), _totalContentHeight);
        Widgets.BeginScrollView(outerRect, ref _scrollPosition, scrollViewTotal);

        Listing_Standard listingStandard = new();
        listingStandard.Begin(new Rect(0f, 0f, scrollViewTotal.width, 9999f));


        //EnableMechHacking
        listingStandard.Label("\n");
        listingStandard.CheckboxLabeled("USH_GE_EnableMechSetting".Translate().Colorize(Color.cyan), ref EnableMechHacking.Value);
        listingStandard.Label("USH_GE_EnableMechSettingDesc".Translate());

        //EnableTurretsHacking
        listingStandard.Label("\n");
        listingStandard.CheckboxLabeled("USH_GE_EnableTurretSetting".Translate().Colorize(Color.cyan), ref EnableTurretsHacking.Value);
        listingStandard.Label("USH_GE_EnableTurretSettingDesc".Translate());

        //Reset button
        listingStandard.Label("\n");
        bool shouldReset = listingStandard.ButtonText("USH_GE_ResetSettings".Translate());
        if (shouldReset) ResetAll();
        listingStandard.Label("\n");

        //End
        listingStandard.End();
        _totalContentHeight = listingStandard.CurHeight + 10f;
        Widgets.EndScrollView();
    }

    public class Setting<T>(T defaultValue)
    {
        public T Value = defaultValue;
        public T DefaultValue { get; private set; } = defaultValue;

        public void ToDefault() => Value = DefaultValue;
        public void ExposeData(string key) => Scribe_Values.Look(ref Value, $"USH_{key}", DefaultValue);
    }
}