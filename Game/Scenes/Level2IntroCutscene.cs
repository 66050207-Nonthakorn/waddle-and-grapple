using System.Collections.Generic;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game.Scenes;

public class Level2IntroCutscene : BaseCutscene
{
    public const string SceneName = "Level2IntroCutscene";
    public const string CutsceneKey = "level2_intro";

    protected override IReadOnlyList<string> GetSections()
    {
        return
        [
            "Good job on infiltrating the site, Agen-T. \n But this is just the beginning.",
            "Dr.Lunix is being held in a heavily fortified area within the construction site. \n Expect heavy resistance from the P'an P'an Syndicate's forces.",
            "Looks for any suspicious buildings where they might be keeping him.",
            "If you find any enemy soldiers to tough to take down, \n try getting them of their feet by sliding into them.",
            "Use your agility to your advantage, Agen-T. \n Keep moving and use the environment to outmaneuver your foes.",
            "Stay vigilant, Agen-T.",
        ];
    }

    protected override string GetBossPortraitTextureName() => "UI/PenguinBossPortrait";

    protected override string GetNextSceneName() => "Level2";

    protected override void OnCutsceneCompleted()
    {
        ProgressionManager.Instance.MarkCutscenePlayed(CutsceneKey);
    }
}
