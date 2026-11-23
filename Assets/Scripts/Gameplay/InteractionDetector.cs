using UnityEngine;

namespace NutriMind.Gameplay
{
    // Attach to the Player. Finds the nearest IInteractable (checkpoint station) within range.
    // Stations need ANY collider (a trigger sphere is easiest) on a layer included in the mask.
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] float radius = 3f;
        [SerializeField] LayerMask interactableMask = ~0;
        [SerializeField] float scanInterval = 0.15f;

        public IInteractable Current { get; private set; }

        readonly Collider[] hits = new Collider[16];
        float nextScan;

        void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + scanInterval;
            Current = FindNearest();
        }

        IInteractable FindNearest()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, radius, hits, interactableMask, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = hits[i].GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;

                Component component = candidate as Component;
                if (component == null) continue;

                float distance = (component.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        // Hook the HUD's Interact button to WorldController.OnInteractButton (which calls this).
        public void Interact()
        {
            if (Current != null && Current.CanInteract) Current.Interact();
        }
    }
}
