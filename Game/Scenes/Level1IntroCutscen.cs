using System.Collections.Generic;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game.Scenes;

public class Level1IntroCutscene : BaseCutscene
{
    public const string SceneName = "Level1IntroCutscene";
    public const string CutsceneKey = "level1_intro";

    protected override IReadOnlyList<string> GetSections()
    {
        return
        [
            "Do you copy, AgenT? This is \"Big Boss\". \n Do you read me?",
            "We have received reports about the whereabouts of the missing \"Dr.Lunix\".",
            "Intelligence suggests that he is being held captive by the \"P'an P'an Syndicate\". \n They operate inside a construction site on the outskirts of town.",
            "The exact location of the site is unknown, but we have reason \nto believe that it is near the old factory district. ",
            "Your mission is to infiltrate the construction site,\n locate Dr.Lunix, and extract him safely.",
            "Use your newly installed \"Grappling Pickaxe\" modeule on  your backpack \n to navigate the environment and take down your foes.",
            "He should have left traces of holographic fish \n that only your augmented vision can see. \n Follow the trail to find him.",
            "Do you understand the mission, AgenT? \n This is a high-priority operation, and we are counting on you to succeed. \n Good luck, AgenT."
        ];
    }

    protected override string GetBossPortraitTextureName() => "UI/PenguinBossPortrait";

    protected override string GetNextSceneName() => "Level1";

    protected override void OnCutsceneCompleted()
    {
        ProgressionManager.Instance.MarkCutscenePlayed(CutsceneKey);
    }
}
