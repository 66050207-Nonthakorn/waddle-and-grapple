using System.Collections.Generic;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game.Scenes;

public class Level3IntroCutscene : BaseCutscene
{
    public const string SceneName = "Level3IntroCutscene";
    public const string CutsceneKey = "level3_intro";

    protected override IReadOnlyList<string> GetSections()
    {
        return
        [
            "Good work on spotting the factory, Agen-T. \n According to the enemy's structure, Dr.Lunix should be somewhere inside.",
            "Expect the unexpected, Agen-T. \n There might be even more hidden forces we have not detected yet.",
            "Stay on your flipper, Agen-T. \n No need to rush at every thing you see."
        ];
    }

    protected override string GetBossPortraitTextureName() => "UI/PenguinBossPortrait";

    protected override string GetNextSceneName() => "Level3";

    protected override void OnCutsceneCompleted()
    {
        ProgressionManager.Instance.MarkCutscenePlayed(CutsceneKey);
    }
}
