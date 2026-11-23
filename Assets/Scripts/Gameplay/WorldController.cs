using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using NutriMind.Core;
using NutriMind.Data;
using NutriMind.Interface;

namespace NutriMind.Gameplay
{
    // One per world scene (World_LiteraQuest, World_Science, World_Wellness).
    // Loads the selected mission, feeds the 3 stations, runs checkpoints and the final challenge.
    public class WorldController : MonoBehaviour
    {
        public static WorldController Instance { get; private set; }

        [Tooltip("ENG, SCI or PEH. Must match the content file names (ENG_G5.json ...).")]
        [SerializeField] string worldId = "ENG";

        [Header("Scene references")]
        [SerializeField] PlayerController player;
        [SerializeField] InteractionDetector detector;
        [SerializeField] CheckpointStation[] stations;
        [SerializeField] CheckpointFlow flow;

        [Header("UI references")]
        [SerializeField] HUDController hud;
        [SerializeField] MissionIntroPanel introPanel;
        [SerializeField] ActivityPanel finalChallengePanel;
        [SerializeField] RewardPanel rewardPanel;

        MissionData mission;
        bool busy;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        IEnumerator Start()
        {
            GameSession session = GameSession.Instance;
            session.SelectedWorldId = worldId;

            mission = session.CurrentMission;
            if (mission == null)
            {
                hud.Toast("Mission content not found. Check Resources/Content.");
                yield break;
            }

            for (int i = 0; i < stations.Length; i++)
            {
                CheckpointData data = mission.checkpoints.Find(c => c.index == stations[i].checkpointIndex);
                if (data != null) stations[i].Setup(mission, data);
            }

            hud.SetMission(mission);
            hud.Refresh(mission);

            busy = true;
            player.InputEnabled = false;
            introPanel.Show(mission);
            yield return new WaitUntil(() => introPanel.IsDone);

            // Student quit after checkpoint 3 last time? Go straight to the final challenge.
            yield return TryRunFinalChallenge();

            busy = false;
            player.InputEnabled = true;
        }

        void Update()
        {
            if (hud == null || detector == null) return;

            bool canPrompt = !busy && detector.Current != null;
            hud.SetInteractPrompt(canPrompt ? detector.Current.PromptText : null);
        }

        // Hook the HUD Interact button's OnClick to this.
        public void OnInteractButton()
        {
            if (!busy) detector.Interact();
        }

        public void StartCheckpoint(CheckpointStation station)
        {
            if (busy || station.Data == null) return;

            if (!GameSession.Instance.IsCheckpointUnlocked(mission, station.checkpointIndex))
            {
                hud.Toast("Finish Checkpoint " + (station.checkpointIndex - 1) + " first.");
                return;
            }
            StartCoroutine(RunCheckpoint(station));
        }

        IEnumerator RunCheckpoint(CheckpointStation station)
        {
            busy = true;
            player.InputEnabled = false;

            yield return flow.Run(mission, station.Data);

            for (int i = 0; i < stations.Length; i++) stations[i].Refresh();
            hud.Refresh(mission);

            yield return TryRunFinalChallenge();

            busy = false;
            player.InputEnabled = true;
        }

        IEnumerator TryRunFinalChallenge()
        {
            GameSession session = GameSession.Instance;
            bool allDone = session.CompletedCheckpointCount(mission) >= mission.checkpoints.Count;
            if (!allDone || session.IsFinalChallengeComplete(mission)) yield break;

            FinalChallengeData challenge = mission.finalChallenge;

            // Placeholder panel until you build the real final challenges (Step 9 in the guide).
            finalChallengePanel.ShowFinal(challenge);
            yield return new WaitUntil(() => finalChallengePanel.IsDone);

            int points = session.CompleteFinalChallenge(mission);
            rewardPanel.Show("Mission Complete!", challenge.rewardName, points, "Back to Missions");
            yield return new WaitUntil(() => rewardPanel.IsDone);

            hud.Refresh(mission);
            ExitToMissionSelect();
        }

        // Hook your pause menu's "Back" button to this as well.
        public void ExitToMissionSelect()
        {
            if (Application.CanStreamedLevelBeLoaded(SceneNames.MissionSelect))
            {
                NutriMind.Interface.MenuNavigation.Cooldown(0.6f);
                SceneManager.LoadScene(SceneNames.MissionSelect);
            }
            else if (hud != null)
            {
                hud.Toast("The MissionSelect scene is not in Build Settings yet.");
            }
        }
    }
}
