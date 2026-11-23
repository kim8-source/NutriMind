using TMPro;
using UnityEngine;
using NutriMind.Core;
using NutriMind.Data;

namespace NutriMind.Interface
{
    // On-screen HUD for the world scenes: points, mission checklist, Interact button, toast message.
    public class HUDController : MonoBehaviour
    {
        [Header("Quest Card")]
        [Tooltip("Header title of the Quest Card (e.g. 'LiteraQuest', 'SciVenture', 'HealthQuest')")]
        [SerializeField] TMP_Text questCardHeaderTitleText;
        [SerializeField] TMP_Text missionTitleText;
        [SerializeField] TMP_Text[] checklistLabels;    // 3 labels: "Checkpoint 1" ...
        [SerializeField] GameObject[] checklistTicks;   // 3 green check icons (hidden until done)

        [Header("Points")]
        [SerializeField] TMP_Text pointsText;

        [Header("Interact button")]
        [SerializeField] GameObject interactButton;
        [SerializeField] TMP_Text interactLabel;

        [Header("Toast")]
        [SerializeField] TMP_Text toastText;
        [SerializeField] float toastSeconds = 2.5f;

        float toastUntil;

        void Start()
        {
            UpdateHeaderTitle(null);
        }

        public void SetMission(MissionData mission)
        {
            UpdateHeaderTitle(mission);
            if (missionTitleText != null) missionTitleText.text = "Mission " + mission.number + ":\n" + mission.title;
            for (int i = 0; i < checklistLabels.Length; i++)
            {
                checklistLabels[i].text = i < mission.checkpoints.Count
                    ? "Checkpoint " + mission.checkpoints[i].index
                    : "";
            }
        }

        void UpdateHeaderTitle(MissionData mission)
        {
            if (questCardHeaderTitleText == null)
            {
                Transform found = transform.Find("QuestCard/QuestCardHeader") ?? transform.Find("QuestCardHeader");
                if (found != null) questCardHeaderTitleText = found.GetComponent<TMP_Text>();
                if (questCardHeaderTitleText == null)
                {
                    TMP_Text[] all = GetComponentsInChildren<TMP_Text>(true);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (all[i].gameObject.name == "QuestCardHeader")
                        {
                            questCardHeaderTitleText = all[i];
                            break;
                        }
                    }
                }
            }

            if (questCardHeaderTitleText == null) return;

            string worldId = GameSession.Instance != null ? GameSession.Instance.SelectedWorldId : null;
            if (string.IsNullOrEmpty(worldId) && mission != null && !string.IsNullOrEmpty(mission.id))
            {
                if (mission.id.Contains("SCI")) worldId = WorldCatalog.Sci;
                else if (mission.id.Contains("PEH")) worldId = WorldCatalog.Peh;
                else if (mission.id.Contains("ENG")) worldId = WorldCatalog.Eng;
            }

            if (worldId == WorldCatalog.Sci || worldId == "SCI")
            {
                questCardHeaderTitleText.text = "SciVenture";
            }
            else if (worldId == WorldCatalog.Peh || worldId == "PEH")
            {
                questCardHeaderTitleText.text = "HealthQuest";
            }
            else
            {
                questCardHeaderTitleText.text = "LiteraQuest";
            }
        }

        public void Refresh(MissionData mission)
        {
            GameSession session = GameSession.Instance;
            pointsText.text = session.Save.totalPoints.ToString();

            for (int i = 0; i < checklistTicks.Length; i++)
            {
                bool done = i < mission.checkpoints.Count
                    && session.IsCheckpointCompleted(mission, mission.checkpoints[i].index);
                checklistTicks[i].SetActive(done);
            }
        }

        // Pass null to hide the button.
        public void SetInteractPrompt(string label)
        {
            bool show = !string.IsNullOrEmpty(label);
            if (interactButton.activeSelf != show) interactButton.SetActive(show);
            if (show && interactLabel != null) interactLabel.text = label;
        }

        public void Toast(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + toastSeconds;
        }

        void Update()
        {
            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime > toastUntil)
            {
                toastText.gameObject.SetActive(false);
            }
        }
    }
}
