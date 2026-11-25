using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NutriMind.Gameplay
{
    // Attach to the JumpButton UI GameObject in the HUD.
    // Supports both touch/click and UI Button events to trigger player jump.
    public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Reference to the player controller. Auto-detected if null.")]
        [SerializeField] PlayerController player;

        [Tooltip("Button image for click tint feedback.")]
        [SerializeField] Image buttonImage;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color pressedColor = new Color(0.75f, 0.9f, 1f, 1f);

        void Awake()
        {
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            if (buttonImage != null) normalColor = buttonImage.color;
            ResolvePlayer();

            // Do NOT add TriggerJump to onClick — OnPointerDown fires immediately on press
            // and handles the jump. onClick fires AFTER pointer up (or after AudioManager
            // hooks it), which would cause a second jump call.
            // We clear any serialized persistent listeners so nothing fires via onClick.
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                // Leave onClick empty — AudioManager may add PlayButtonClick here, that's fine
            }
        }

        void ResolvePlayer()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TriggerJump();
            if (buttonImage != null)
            {
                buttonImage.color = pressedColor;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }

        public void TriggerJump()
        {
            ResolvePlayer();
            if (player != null)
            {
                player.Jump();
            }
        }
    }
}
