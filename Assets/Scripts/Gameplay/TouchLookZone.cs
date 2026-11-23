using UnityEngine;
using UnityEngine.EventSystems;

namespace NutriMind.Gameplay
{
    // A transparent, full-height Image on the RIGHT half of the screen (Raycast Target ON).
    // Dragging a finger on it turns the camera. Put it BEHIND the Run and Interact buttons
    // (higher in the Hierarchy) so the buttons still get their taps.
    public class TouchLookZone : MonoBehaviour, IDragHandler
    {
        [SerializeField] ThirdPersonCamera cameraRig;

        public void OnDrag(PointerEventData eventData)
        {
            if (cameraRig != null) cameraRig.AddLook(eventData.delta);
        }
    }
}
