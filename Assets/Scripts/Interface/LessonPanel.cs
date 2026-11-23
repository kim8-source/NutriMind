using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NutriMind.Interface
{
    // Lesson card. Put bodyText inside a Scroll View if lessons can be long.
    public class LessonPanel : PanelBase
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button continueButton;

        void Awake()
        {
            continueButton.onClick.AddListener(Close);
        }

        public void Show(string title, string lesson)
        {
            Open();
            titleText.text = title;
            bodyText.text = lesson;
        }
    }
}
