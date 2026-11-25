using UnityEngine;

namespace NutriMind.Interface
{
    // Base class for every full-screen panel (dialogue, lesson, quiz, reward ...).
    // Usage from a flow:   panel.Show(...);  yield return new WaitUntil(() => panel.IsDone);
    public abstract class PanelBase : MonoBehaviour
    {
        public bool IsDone { get; private set; } = true;

        // Call Open() FIRST inside Show(): activating the panel runs Awake, which wires the buttons.
        protected void Open()
        {
            IsDone = false;
            gameObject.SetActive(true);
            NutriMind.Core.TextSizeManager.ApplyToHierarchy(transform);
        }

        protected void Close()
        {
            gameObject.SetActive(false);
            IsDone = true;
        }
    }
}
