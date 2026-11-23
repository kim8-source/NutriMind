using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NutriMind.Interface
{
    // Used for BOTH "Checkpoint Complete!" and "Mission Complete!" (same prefab, different text and icon).
    public class RewardPanel : PanelBase
    {
        [Header("UI References")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text rewardText;
        [SerializeField] TMP_Text pointsText;
        [SerializeField] TMP_Text buttonLabel;
        [SerializeField] Button continueButton;
        [SerializeField] Image rewardIcon;
        [Tooltip("Points icon image (e.g. Card/Image (4)). Hidden automatically when points <= 0 so it won't look empty.")]
        [SerializeField] Image pointsIcon;

        [Header("Reward Sprites (Inspector Configurable)")]
        [Tooltip("Sprite for checkpoint rewards (e.g., Leaf). Set in Inspector to prevent unexpected UI images.")]
        [SerializeField] Sprite checkpointRewardSprite;
        [Tooltip("Sprite for mission completion rewards (e.g., Badge/Medal). Set in Inspector.")]
        [SerializeField] Sprite missionRewardSprite;

        [Header("Reward Text (Inspector Configurable)")]
        [Tooltip("Default text for checkpoint rewards (e.g. '1 Leaf'). Can be modified directly in the Inspector.")]
        [SerializeField] string defaultCheckpointRewardText = "1 Leaf";

        [Header("Confetti Celebration Effect")]
        [Tooltip("Optional ParticleSystem for confetti celebration. If assigned, will Play() on checkpoint completion.")]
        [SerializeField] ParticleSystem confettiParticleSystem;
        [Tooltip("Optional confetti prefab to instantiate on celebration.")]
        [SerializeField] GameObject confettiPrefab;
        [Tooltip("Whether to trigger confetti pop when completing a checkpoint.")]
        [SerializeField] bool confettiOnCheckpoint = true;
        [Tooltip("Whether to trigger confetti pop when completing a full mission.")]
        [SerializeField] bool confettiOnMission = true;
        [Tooltip("Number of procedural UI confetti particles to burst.")]
        [SerializeField] int confettiParticleCount = 50;

        Coroutine m_ConfettiCoroutine;
        GameObject m_ConfettiRoot;

        struct ConfettiParticle
        {
            public RectTransform rect;
            public Image image;
            public Vector2 velocity;
            public float rotationSpeed;
            public float currentRot;
        }

        static readonly Color[] ConfettiColors = new Color[]
        {
            new Color(1f, 0.84f, 0f, 1f),       // Gold
            new Color(0f, 0.95f, 0.45f, 1f),    // Green
            new Color(0.1f, 0.8f, 1f, 1f),      // Cyan
            new Color(1f, 0.25f, 0.55f, 1f),    // Pink
            new Color(1f, 0.55f, 0.1f, 1f),     // Orange
            new Color(0.75f, 0.35f, 1f, 1f),    // Purple
            new Color(1f, 0.95f, 0.25f, 1f),    // Bright Yellow
        };

        void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(Close);
            }

            if (rewardIcon == null)
            {
                rewardIcon = transform.Find("Card/Image")?.GetComponent<Image>()
                          ?? transform.Find("Card")?.GetComponentInChildren<Image>();
            }

            if (pointsIcon == null)
            {
                pointsIcon = transform.Find("Card/Image (4)")?.GetComponent<Image>()
                          ?? transform.Find("Card/PointsIcon")?.GetComponent<Image>()
                          ?? transform.Find("Card/PointsImage")?.GetComponent<Image>();
            }

            if (checkpointRewardSprite == null)
            {
                checkpointRewardSprite = Resources.Load<Sprite>("Badges/leaf");
            }
            if (missionRewardSprite == null)
            {
                missionRewardSprite = Resources.Load<Sprite>("Badges/medal");
            }
        }

        public void Show(string title, string rewardName, int points, string buttonText)
        {
            Open();
            if (titleText != null) titleText.text = title;

            bool isCheckpoint = !string.IsNullOrEmpty(title) && title.IndexOf("checkpoint", StringComparison.OrdinalIgnoreCase) >= 0;

            // Load and apply corresponding reward icon (Leaf for checkpoints, Medal for missions)
            if (rewardIcon != null)
            {
                Sprite icon = isCheckpoint ? checkpointRewardSprite : missionRewardSprite;
                if (icon == null)
                {
                    icon = isCheckpoint
                        ? (Resources.Load<Sprite>("Badges/leaf") ?? FindFallbackSprite("checklisticon", "leaf"))
                        : (Resources.Load<Sprite>("Badges/medal") ?? FindFallbackSprite("medal", "star"));
                }

                if (icon != null)
                {
                    rewardIcon.sprite = icon;
                    rewardIcon.color = Color.white;
                    rewardIcon.preserveAspect = true;
                    rewardIcon.gameObject.SetActive(true);
                }
            }

            // Reward text: removed "Reward:" prefix and any parentheses
            if (rewardText != null)
            {
                if (isCheckpoint)
                {
                    rewardText.text = defaultCheckpointRewardText;
                }
                else
                {
                    rewardText.text = rewardName;
                }
            }

            // Points display: hide both pointsText and pointsIcon when points <= 0
            bool hasPoints = points > 0;
            if (pointsIcon != null)
            {
                pointsIcon.gameObject.SetActive(hasPoints);
            }

            if (pointsText != null)
            {
                pointsText.text = hasPoints ? "+" + points + " Points" : "";
                pointsText.gameObject.SetActive(hasPoints);
            }

            if (buttonLabel != null) buttonLabel.text = buttonText;

            // Trigger celebration confetti pop
            if ((isCheckpoint && confettiOnCheckpoint) || (!isCheckpoint && confettiOnMission))
            {
                TriggerConfettiPop();
            }
        }

        public void TriggerConfettiPop()
        {
            if (confettiParticleSystem != null)
            {
                confettiParticleSystem.Play();
            }

            if (confettiPrefab != null)
            {
                var inst = Instantiate(confettiPrefab, transform);
                Destroy(inst, 3.5f);
            }

            StopConfetti();
            m_ConfettiCoroutine = StartCoroutine(ConfettiBurstRoutine());
        }

        void StopConfetti()
        {
            if (m_ConfettiCoroutine != null)
            {
                StopCoroutine(m_ConfettiCoroutine);
                m_ConfettiCoroutine = null;
            }
            if (m_ConfettiRoot != null)
            {
                Destroy(m_ConfettiRoot);
                m_ConfettiRoot = null;
            }
        }

        IEnumerator ConfettiBurstRoutine()
        {
            if (m_ConfettiRoot != null) Destroy(m_ConfettiRoot);

            m_ConfettiRoot = new GameObject("ConfettiRoot");
            m_ConfettiRoot.transform.SetParent(transform, false);
            m_ConfettiRoot.transform.SetAsLastSibling();

            var rootRt = m_ConfettiRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Vector2 spawnCenter = Vector2.zero;
            Transform card = transform.Find("Card");
            if (card != null && card.TryGetComponent<RectTransform>(out var cardRt))
            {
                spawnCenter = cardRt.anchoredPosition;
            }

            var particles = new ConfettiParticle[confettiParticleCount];

            for (int i = 0; i < confettiParticleCount; i++)
            {
                var pGo = new GameObject("Confetti_" + i);
                pGo.transform.SetParent(m_ConfettiRoot.transform, false);

                var pRt = pGo.AddComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0.5f, 0.5f);
                pRt.anchorMax = new Vector2(0.5f, 0.5f);
                pRt.pivot = new Vector2(0.5f, 0.5f);

                float w = UnityEngine.Random.Range(10f, 18f);
                float h = UnityEngine.Random.Range(16f, 28f);
                pRt.sizeDelta = new Vector2(w, h);
                pRt.anchoredPosition = spawnCenter + UnityEngine.Random.insideUnitCircle * 50f;

                var img = pGo.AddComponent<Image>();
                Color col = ConfettiColors[UnityEngine.Random.Range(0, ConfettiColors.Length)];
                img.color = col;
                img.raycastTarget = false;

                // Burst angle: upward cone with spread
                float angle = UnityEngine.Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = UnityEngine.Random.Range(450f, 1050f);
                Vector2 vel = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);

                particles[i] = new ConfettiParticle
                {
                    rect = pRt,
                    image = img,
                    velocity = vel,
                    rotationSpeed = UnityEngine.Random.Range(-400f, 400f),
                    currentRot = UnityEngine.Random.Range(0f, 360f)
                };
            }

            float duration = 2.5f;
            float elapsed = 0f;
            float gravity = 1200f;

            while (elapsed < duration)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                float norm = Mathf.Clamp01(elapsed / duration);

                float alpha = norm > 0.65f ? Mathf.Lerp(1f, 0f, (norm - 0.65f) / 0.35f) : 1f;

                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i].rect == null) continue;

                    var p = particles[i];
                    p.velocity.y -= gravity * dt;
                    p.velocity.x *= (1f - 0.35f * dt);

                    p.rect.anchoredPosition += p.velocity * dt;
                    p.currentRot += p.rotationSpeed * dt;
                    p.rect.localEulerAngles = new Vector3(0f, 0f, p.currentRot);

                    if (p.image != null)
                    {
                        Color c = p.image.color;
                        c.a = alpha;
                        p.image.color = c;
                    }

                    particles[i] = p;
                }

                yield return null;
            }

            StopConfetti();
        }

        new public void Close()
        {
            StopConfetti();
            base.Close();
        }

        void OnDisable()
        {
            StopConfetti();
        }

        static Sprite FindFallbackSprite(params string[] names)
        {
            var all = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < all.Length; i++)
            {
                for (int c = 0; c < names.Length; c++)
                {
                    if (all[i].name.Equals(names[c], StringComparison.OrdinalIgnoreCase))
                        return all[i];
                }
            }
            return null;
        }
    }
}
