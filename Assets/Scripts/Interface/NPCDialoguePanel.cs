using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NutriMind.Interface
{
    // Speech-bubble style panel: NPC name + one line at a time, Next / Start button.
    public class NPCDialoguePanel : PanelBase
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text nextLabel;
        [SerializeField] Button nextButton;

        IList<string> lines;
        int index;

        void Awake()
        {
            nextButton.onClick.AddListener(Next);
        }

        public void Show(string npcName, IList<string> dialogueLines)
        {
            Open();
            lines = dialogueLines;
            index = 0;
            nameText.text = npcName;
            Render();
        }

        void Render()
        {
            bodyText.text = lines[index];
            bool last = index >= lines.Count - 1;
            if (nextLabel != null) nextLabel.text = last ? "Start" : "Next";
        }

        void Next()
        {
            if (index < lines.Count - 1)
            {
                index++;
                Render();
            }
            else
            {
                Close();
            }
        }
    }
}
