using UnityEngine;
using UnityEngine.EventSystems;

namespace NutriMind.Gameplay
{
    // Put this on the joystick BACKGROUND image (Raycast Target ON).
    // The handle must be a child of the background, anchored to the centre.
    // Works with mobile touch and mouse in the Editor.
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [Tooltip("How far the handle can move, in Canvas pixels.")]
        [SerializeField] float radius = 80f;

        public Vector2 Value { get; private set; }

        void Awake()
        {
            if (background == null) background = GetComponent<RectTransform>();
            if (handle == null && transform.childCount > 0)
            {
                handle = transform.GetChild(0) as RectTransform;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null) background = GetComponent<RectTransform>();
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out local);

            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            if (handle != null) handle.anchoredPosition = clamped;
            Value = clamped / radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }
    }
}
