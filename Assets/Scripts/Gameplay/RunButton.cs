using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NutriMind.Gameplay
{
    // Attach to the RunButton UI GameObject in the HUD.
    // Works reliably anytime: whether idle, walking, running, or transitioning between them.
    // 1. Single Tap: Toggles Run mode on/off.
    // 2. Press and Hold: Sprints while held down, returns to walk upon release.
    public class RunButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Reference to the player controller. Auto-detected if null.")]
        [SerializeField] PlayerController player;
        [Tooltip("Button image for tint feedback.")]
        [SerializeField] Image buttonImage;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color activeRunningColor = new Color(0.7f, 1f, 0.7f, 1f);

        [Tooltip("Time in seconds to differentiate between a tap and a press-and-hold.")]
        [SerializeField] float holdThreshold = 0.18f;

        float pointerDownTime;
        bool isPointerDown;
        bool wasRunningOnDown;
        int activePointerId = int.MinValue;
        int activeTouchId = -1;
        bool isMouseHold;

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

        void ResolvePlayer()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isPointerDown) return;

            ResolvePlayer();
            isPointerDown = true;
            activePointerId = eventData != null ? eventData.pointerId : int.MinValue;
            pointerDownTime = Time.unscaledTime;
            CaptureActiveInputSource(eventData);

            if (player != null)
            {
                wasRunningOnDown = player.IsRunning;
                // Instantly engage run on touch down for zero latency
                player.SetRunning(true);
            }
            UpdateVisual(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPointerDown) return;
            if (eventData != null && activePointerId != int.MinValue && eventData.pointerId != activePointerId) return;

            HandleRelease();
        }

        void HandleRelease()
        {
            ResolvePlayer();
            float duration = Time.unscaledTime - pointerDownTime;
            isPointerDown = false;
            activePointerId = int.MinValue;
            activeTouchId = -1;
            isMouseHold = false;

            if (player != null)
            {
                if (duration < holdThreshold)
                {
                    // TAP: Toggle run mode anytime (idle, walking, running)
                    bool targetState = !wasRunningOnDown;
                    player.SetRunning(targetState);
                    UpdateVisual(targetState);
                }
                else
                {
                    // PRESS AND HOLD: Releasing returns to walking
                    player.SetRunning(false);
                    UpdateVisual(false);
                }
            }
        }

        void CaptureActiveInputSource(PointerEventData eventData)
        {
            activeTouchId = -1;
            isMouseHold = false;

#if ENABLE_INPUT_SYSTEM
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && eventData != null)
            {
                float bestDist = float.MaxValue;
                foreach (var t in ts.touches)
                {
                    if (!t.press.isPressed) continue;
                    float d = Vector2.SqrMagnitude(t.position.ReadValue() - eventData.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        activeTouchId = t.touchId.ReadValue();
                    }
                }
            }

            if (activeTouchId < 0)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    isMouseHold = true;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0 && eventData != null)
            {
                float bestDist = float.MaxValue;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                    float d = Vector2.SqrMagnitude(t.position - eventData.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        activeTouchId = t.fingerId;
                    }
                }
            }
            if (activeTouchId < 0 && Input.GetMouseButton(0))
            {
                isMouseHold = true;
            }
#endif
        }

        void Update()
        {
            if (player == null) ResolvePlayer();

            // Mobile safety net: if the touch was released/cancelled without OnPointerUp firing,
            // release immediately so hold-to-run never stays stuck ON.
            if (isPointerDown && !IsActivePointerStillPressed())
            {
                HandleRelease();
                return;
            }

            if (player != null && !isPointerDown)
            {
                UpdateVisual(player.IsRunning);
            }
        }

        bool IsActivePointerStillPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (activeTouchId >= 0)
            {
                var ts = UnityEngine.InputSystem.Touchscreen.current;
                if (ts == null) return false;
                foreach (var t in ts.touches)
                {
                    if (t.touchId.ReadValue() == activeTouchId)
                    {
                        return t.press.isPressed;
                    }
                }
                return false;
            }
            if (isMouseHold)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                return mouse != null && mouse.leftButton.isPressed;
            }
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (activeTouchId >= 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.fingerId == activeTouchId)
                    {
                        return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                    }
                }
                return false;
            }
            if (isMouseHold)
            {
                return Input.GetMouseButton(0);
            }
            return true;
#else
            return true;
#endif
        }

        void OnDisable()
        {
            if (isPointerDown)
            {
                HandleRelease();
            }
        }

        void UpdateVisual(bool running)
        {
            if (buttonImage != null)
            {
                buttonImage.color = running ? activeRunningColor : normalColor;
            }
        }
    }
}
