using System.Collections.Generic;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game.Scenes;

public class Level3OutroCutscene : BaseCutscene
{
    public const string SceneName = "Level3OutroCutscene";
    public const string CutsceneKey = "level3_outro";

    protected override IReadOnlyList<string> GetSections()
    {
        return
        [
            "DAMN THOSE MAMMALS! \n WE'VE BEEN PLAYED LIKE A GODDAMN SARDINE!",
            "They have already taken Dr.Lunix and escaped!",
            "We must have been too late.",
            "Get out, AgenT. Comeback and plan our next move.",
            "Next time, we'll get them for sure!"
        ];
    }

    protected override string GetBossPortraitTextureName() => "UI/PenguinBossPortrait";

    protected override string GetNextSceneName() => "levelcomplete";

    protected override void OnCutsceneCompleted()
    {
        ProgressionManager.Instance.MarkCutscenePlayed(CutsceneKey);
    }
}
