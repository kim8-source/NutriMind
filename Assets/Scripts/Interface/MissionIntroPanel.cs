using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NutriMind.Data;

namespace NutriMind.Interface
{
    // "Mission Introduction" step of the game loop: title, story, focus, Play Mission button.
    public class MissionIntroPanel : PanelBase
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text storyText;
        [SerializeField] TMP_Text focusText;
        [SerializeField] Button playButton;

        void Awake()
        {
            playButton.onClick.AddListener(Close);
        }

        public void Show(MissionData mission)
        {
            Open();
            titleText.text = "Mission " + mission.number + ":\n" + mission.title;
            storyText.text = mission.story;
            if (focusText != null) focusText.text = mission.focus;
        }
    }
}
