using TMPro;
using UnityEngine;
using NutriMind.Core;
using NutriMind.Data;

namespace NutriMind.Gameplay
{
    // One of the 3 learning stations in a world scene (CP1 garden, CP2 hall, CP3 library).
    // The SAME station prefab is reused for every mission; WorldController feeds it the content.
    public class CheckpointStation : MonoBehaviour, IInteractable
    {
        [Range(1, 3)] public int checkpointIndex = 1;

        [Header("Optional world-space visuals")]
        [SerializeField] TMP_Text signText;        // wooden sign: "Checkpoint 1 / Story Garden..."
        [SerializeField] TMP_Text npcNameText;     // name above the NPC
        [SerializeField] GameObject lockedMarker;
        [SerializeField] GameObject availableMarker;   // glowing book / "!" marker
        [SerializeField] GameObject completedMarker;

        public MissionData Mission { get; private set; }
        public CheckpointData Data { get; private set; }

        public string PromptText
        {
            get { return "Talk"; }
        }

        public bool CanInteract
        {
            get { return Data != null; }
        }

        public void Setup(MissionData mission, CheckpointData data)
        {
            Mission = mission;
            Data = data;

            if (signText != null) signText.text = "Checkpoint " + data.index + "\n" + data.title;
            if (npcNameText != null) npcNameText.text = data.npcName;

            Refresh();
        }

        public void Refresh()
        {
            if (Data == null || GameSession.Instance == null) return;

            GameSession session = GameSession.Instance;
            bool done = session.IsCheckpointCompleted(Mission, checkpointIndex);
            bool unlocked = session.IsCheckpointUnlocked(Mission, checkpointIndex);

            if (lockedMarker != null) lockedMarker.SetActive(!unlocked);
            if (availableMarker != null) availableMarker.SetActive(unlocked && !done);
            if (completedMarker != null) completedMarker.SetActive(done);
        }

        public void Interact()
        {
            if (WorldController.Instance != null) WorldController.Instance.StartCheckpoint(this);
        }
    }
}
