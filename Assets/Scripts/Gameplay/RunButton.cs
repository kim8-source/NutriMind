using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NutriMind.Gameplay
{
    // Attach to the RunButton UI GameObject.
    // Supports BOTH tap/click (toggle run on/off) AND hold/press (run while held, walk when released).
    // Specifically tuned for mobile touch controls.
    public class RunButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Reference to the player controller. Auto-detected if null.")]
        [SerializeField] PlayerController player;
        [Tooltip("Button image for tint feedback.")]
        [SerializeField] Image buttonImage;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color activeRunningColor = new Color(0.7f, 1f, 0.7f, 1f);

        [Tooltip("Time in seconds to differentiate between a tap and a press-and-hold.")]
        [SerializeField] float holdThreshold = 0.35f;

        float pointerDownTime;
        bool isPointerDown;
        bool wasRunningBeforePress;
        int activePointerId = -999;

        void Awake()
        {
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            ResolvePlayer();

            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ResolvePlayer();
            activePointerId = eventData.pointerId;
            pointerDownTime = Time.unscaledTime;
            isPointerDown = true;

            if (player != null)
            {
                wasRunningBeforePress = player.IsRunning;
                // Instantly engage running on press down so there is 0 latency
                player.SetRunning(true);
            }
            UpdateVisual(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (activePointerId != -999 && eventData.pointerId != activePointerId) return;

            ResolvePlayer();
            float duration = Time.unscaledTime - pointerDownTime;
            isPointerDown = false;
            activePointerId = -999;

            if (player != null)
            {
                if (duration < holdThreshold)
                {
                    // Quick Tap:
                    // If was already running before the tap -> stop running (walk)
                    // If was walking before the tap -> keep running (run)
                    bool targetState = !wasRunningBeforePress;
                    player.SetRunning(targetState);
                    UpdateVisual(targetState);
                }
                else
                {
                    // Press and Hold:
                    // Releasing after hold ALWAYS drops back to walk
                    player.SetRunning(false);
                    UpdateVisual(false);
                }
            }
        }

        void Update()
        {
            if (player == null) ResolvePlayer();
            if (player != null && !isPointerDown)
            {
                UpdateVisual(player.IsRunning);
            }
        }

        void UpdateVisual(bool running)
        {
            if (buttonImage != null)
            {
                buttonImage.color = running ? activeRunningColor : normalColor;
            }
        }

        void ResolvePlayer()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }
        }
    }
}
