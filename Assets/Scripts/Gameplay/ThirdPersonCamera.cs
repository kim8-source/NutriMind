using UnityEngine;

namespace NutriMind.Gameplay
{
    // Attach to the Main Camera. Follows the player from behind; drag on the screen to orbit.
    // Includes smooth damping to eliminate camera shake/jitter when looking around on touch screens.
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float distance = 5f;
        [SerializeField] float heightOffset = 1.6f;
        [SerializeField] float minPitch = -5f;
        [SerializeField] float maxPitch = 55f;
        [SerializeField] float startPitch = 18f;
        [Tooltip("Degrees of rotation per pixel dragged.")]
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] float followSharpness = 14f;
        [Tooltip("Smoothing sharpness for looking around. Eliminates touch jitter/shake.")]
        [SerializeField] float lookSharpness = 22f;
        [Tooltip("Layers the camera should not clip through. Exclude the Player layer.")]
        [SerializeField] LayerMask collisionMask = ~0;

        float targetYaw;
        float targetPitch;
        float currentYaw;
        float currentPitch;
        float currentDistance;

        void Start()
        {
            targetPitch = currentPitch = startPitch;
            if (target != null) targetYaw = currentYaw = target.eulerAngles.y;
            currentDistance = distance;
        }

        public void AddLook(Vector2 delta)
        {
            float sensitivityFactor = NutriMind.Core.GameSettings.LookSensitivity;
            targetYaw += delta.x * lookSensitivity * sensitivityFactor;
            targetPitch = Mathf.Clamp(targetPitch - delta.y * lookSensitivity * sensitivityFactor, minPitch, maxPitch);
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Exponential smoothing for rotation angles to remove touch screen jitter
            float rotT = 1f - Mathf.Exp(-lookSharpness * Time.deltaTime);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotT);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotT);

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 focus = target.position + Vector3.up * heightOffset;
            Vector3 direction = rotation * Vector3.back;   // from the player toward the camera
            float desiredDist = distance;

            RaycastHit hit;
            if (Physics.SphereCast(focus, 0.25f, direction, out hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredDist = Mathf.Max(0.6f, hit.distance);
            }

            // Smooth distance to prevent harsh clipping snaps
            currentDistance = Mathf.Lerp(currentDistance, desiredDist, rotT);
            Vector3 desired = focus + direction * currentDistance;

            float posT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, posT);
            transform.rotation = rotation;
        }
    }
}
