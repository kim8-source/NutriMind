using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NutriMind.Interface
{
    /// <summary>
    /// Self-contained SweetAlert-style modal popup powered by LeanTween.
    /// Auto-spawns a high-sorting overlay Canvas on first use — no scene setup required.
    /// Uses Fredoka font, procedural vector-style sprite icons (no missing-glyph boxes),
    /// and dynamically sizes the modal & text to fit the message.
    /// </summary>
    public class SweetAlertPopup : MonoBehaviour
    {
        public enum AlertType
        {
            Success,
            Error,
            Info,
            Loading
        }

        static SweetAlertPopup s_Instance;

        Canvas rootCanvas;
        CanvasGroup backdropGroup;
        RectTransform cardRect;
        CanvasGroup cardGroup;
        RectTransform iconCircleRect;
        Image iconCircleImage;
        Image iconGlyphImage;
        TMP_Text titleText;
        TMP_Text messageText;
        Button okButton;
        TMP_Text okButtonText;

        Action onCloseCallback;

        static readonly Color SuccessColor = new Color32(56, 178, 108, 255);
        static readonly Color ErrorColor = new Color32(229, 77, 66, 255);
        static readonly Color InfoColor = new Color32(59, 130, 246, 255);
        static readonly Color LoadingColor = new Color32(245, 158, 11, 255);
        static readonly Color CardBgColor = new Color32(255, 255, 255, 255);
        static readonly Color TitleColor = new Color32(34, 41, 56, 255);
        static readonly Color BodyColor = new Color32(72, 82, 100, 255);

        static Sprite s_RoundedSprite;
        static Sprite s_CircleSprite;
        static Sprite s_CheckSprite;
        static Sprite s_CrossSprite;
        static Sprite s_InfoSprite;
        static Sprite s_DotsSprite;

        public static void ShowSuccess(string title, string message, Action onClose = null, string okLabel = "OK")
            => Show(title, message, AlertType.Success, true, onClose, okLabel);

        public static void ShowError(string title, string message, Action onClose = null, string okLabel = "OK")
            => Show(title, message, AlertType.Error, true, onClose, okLabel);

        public static void ShowInfo(string title, string message, bool showOkButton = true, Action onClose = null, string okLabel = "OK")
            => Show(title, message, AlertType.Info, showOkButton, onClose, okLabel);

        public static void ShowLoading(string title, string message)
            => Show(title, message, AlertType.Loading, false, null, "OK");

        public static void Show(string title, string message, AlertType type, bool showOkButton = true, Action onClose = null, string okLabel = "OK")
        {
            EnsureInstance();
            if (s_Instance == null) return;
            s_Instance.ShowInternal(title, message, type, showOkButton, onClose, okLabel);
        }

        public static void Hide()
        {
            if (s_Instance == null || !s_Instance.gameObject.activeSelf) return;
            s_Instance.HideInternal();
        }

        /// <summary>
        /// Animates an inline TMP_Text label with a SweetAlert-like LeanTween pop (and shake on error).
        /// </summary>
        public static void AnimateTextFeedback(TMP_Text label, bool isError)
        {
            if (label == null || !label.gameObject.activeInHierarchy) return;

            GameObject go = label.gameObject;
            LeanTween.cancel(go);

            Transform tr = go.transform;
            tr.localScale = Vector3.one * 0.85f;
            LeanTween.scale(go, Vector3.one, 0.25f)
                .setEaseOutBack()
                .setIgnoreTimeScale(true);

            if (isError)
            {
                RectTransform rt = label.rectTransform;
                float baseX = rt.anchoredPosition.x;
                LeanTween.moveLocalX(go, go.transform.localPosition.x + 12f, 0.32f)
                    .setEaseShake()
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        if (rt != null)
                        {
                            Vector2 p = rt.anchoredPosition;
                            p.x = baseX;
                            rt.anchoredPosition = p;
                        }
                    });
            }
        }

        static void EnsureInstance()
        {
            if (s_Instance != null) return;

            GameObject go = new GameObject("SweetAlertPopup");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<SweetAlertPopup>();
            s_Instance.BuildUI();
            go.SetActive(false);
        }

        void BuildUI()
        {
            TMP_FontAsset bodyFont = FindFredokaFont(false);
            TMP_FontAsset boldFont = FindFredokaFont(true) ?? bodyFont;
            Sprite rounded = GetRoundedSprite();
            Sprite circle = GetCircleSprite();

            rootCanvas = gameObject.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 500;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            // Dimmed backdrop
            GameObject backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            backdropGo.transform.SetParent(transform, false);
            RectTransform backdropRt = backdropGo.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            Image backdropImg = backdropGo.GetComponent<Image>();
            backdropImg.color = new Color(0.06f, 0.08f, 0.14f, 0.62f);
            backdropGroup = backdropGo.GetComponent<CanvasGroup>();

            // Modal card
            GameObject cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardGo.transform.SetParent(backdropGo.transform, false);
            cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(820f, 460f);

            Image cardImg = cardGo.GetComponent<Image>();
            cardImg.sprite = rounded;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = CardBgColor;
            cardGroup = cardGo.GetComponent<CanvasGroup>();

            // Icon circle badge
            GameObject iconGo = new GameObject("IconCircle", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cardGo.transform, false);
            iconCircleRect = iconGo.GetComponent<RectTransform>();
            iconCircleRect.anchorMin = new Vector2(0.5f, 1f);
            iconCircleRect.anchorMax = new Vector2(0.5f, 1f);
            iconCircleRect.pivot = new Vector2(0.5f, 0.5f);
            iconCircleRect.anchoredPosition = new Vector2(0f, -84f);
            iconCircleRect.sizeDelta = new Vector2(110f, 110f);
            iconCircleImage = iconGo.GetComponent<Image>();
            iconCircleImage.sprite = circle;

            // Crisp sprite icon inside circle (never shows missing-glyph boxes)
            GameObject glyphGo = new GameObject("IconGlyph", typeof(RectTransform), typeof(Image));
            glyphGo.transform.SetParent(iconGo.transform, false);
            RectTransform glyphRt = glyphGo.GetComponent<RectTransform>();
            glyphRt.anchorMin = new Vector2(0.2f, 0.2f);
            glyphRt.anchorMax = new Vector2(0.8f, 0.8f);
            glyphRt.offsetMin = Vector2.zero;
            glyphRt.offsetMax = Vector2.zero;
            iconGlyphImage = glyphGo.GetComponent<Image>();
            iconGlyphImage.color = Color.white;
            iconGlyphImage.preserveAspect = true;

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(cardGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.07f, 1f);
            titleRt.anchorMax = new Vector2(0.93f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -156f);
            titleRt.sizeDelta = new Vector2(0f, 64f);
            titleText = titleGo.GetComponent<TextMeshProUGUI>();
            if (boldFont != null) titleText.font = boldFont;
            titleText.fontSize = 44f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 28f;
            titleText.fontSizeMax = 48f;
            titleText.color = TitleColor;
            titleText.alignment = TextAlignmentOptions.Center;

            // Message body
            GameObject msgGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            msgGo.transform.SetParent(cardGo.transform, false);
            RectTransform msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.07f, 0f);
            msgRt.anchorMax = new Vector2(0.93f, 1f);
            msgRt.offsetMin = new Vector2(0f, 128f);
            msgRt.offsetMax = new Vector2(0f, -228f);
            messageText = msgGo.GetComponent<TextMeshProUGUI>();
            if (bodyFont != null) messageText.font = bodyFont;
            messageText.fontSize = 32f;
            messageText.enableWordWrapping = true;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 22f;
            messageText.fontSizeMax = 36f;
            messageText.color = BodyColor;
            messageText.alignment = TextAlignmentOptions.Center;

            // OK Button
            GameObject btnGo = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(cardGo.transform, false);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 32f);
            btnRt.sizeDelta = new Vector2(250f, 72f);
            Image btnImg = btnGo.GetComponent<Image>();
            btnImg.sprite = rounded;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = InfoColor;

            okButton = btnGo.GetComponent<Button>();
            okButton.targetGraphic = btnImg;
            okButton.onClick.AddListener(HideInternal);

            GameObject btnLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            RectTransform btnLabelRt = btnLabelGo.GetComponent<RectTransform>();
            btnLabelRt.anchorMin = Vector2.zero;
            btnLabelRt.anchorMax = Vector2.one;
            btnLabelRt.offsetMin = Vector2.zero;
            btnLabelRt.offsetMax = Vector2.zero;
            okButtonText = btnLabelGo.GetComponent<TextMeshProUGUI>();
            if (boldFont != null) okButtonText.font = boldFont;
            okButtonText.text = "OK";
            okButtonText.fontSize = 32f;
            okButtonText.fontStyle = FontStyles.Bold;
            okButtonText.color = Color.white;
            okButtonText.alignment = TextAlignmentOptions.Center;
        }

        void ShowInternal(string title, string message, AlertType type, bool showOkButton, Action onClose, string okLabel)
        {
            TMP_FontAsset bodyFont = FindFredokaFont(false);
            TMP_FontAsset boldFont = FindFredokaFont(true) ?? bodyFont;
            if (bodyFont != null) messageText.font = bodyFont;
            if (boldFont != null)
            {
                titleText.font = boldFont;
                okButtonText.font = boldFont;
            }

            onCloseCallback = onClose;
            string safeTitle = string.IsNullOrEmpty(title) ? "" : title;
            string safeMsg = string.IsNullOrEmpty(message) ? "" : message;
            titleText.text = safeTitle;
            messageText.text = safeMsg;

            Color accent;
            Sprite glyph;
            switch (type)
            {
                case AlertType.Success:
                    accent = SuccessColor;
                    glyph = GetCheckSprite();
                    break;
                case AlertType.Error:
                    accent = ErrorColor;
                    glyph = GetCrossSprite();
                    break;
                case AlertType.Loading:
                    accent = LoadingColor;
                    glyph = GetDotsSprite();
                    break;
                default:
                    accent = InfoColor;
                    glyph = GetInfoSprite();
                    break;
            }

            iconCircleImage.color = accent;
            iconGlyphImage.sprite = glyph;

            if (okButton != null)
            {
                okButton.gameObject.SetActive(showOkButton);
                if (okButton.image != null) okButton.image.color = accent;
                if (okButtonText != null) okButtonText.text = string.IsNullOrEmpty(okLabel) ? "OK" : okLabel;
            }

            // Dynamically resize modal & font sizes based on how much text is shown
            ApplyDynamicLayout(safeMsg, showOkButton);

            gameObject.SetActive(true);

            LeanTween.cancel(backdropGroup.gameObject);
            LeanTween.cancel(cardRect.gameObject);
            LeanTween.cancel(iconCircleRect.gameObject);

            backdropGroup.alpha = 0f;
            cardGroup.alpha = 0f;
            cardRect.localScale = Vector3.one * 0.68f;
            cardRect.anchoredPosition = Vector2.zero;
            iconCircleRect.localScale = Vector3.one * 0.25f;

            LeanTween.alphaCanvas(backdropGroup, 1f, 0.18f).setIgnoreTimeScale(true);
            LeanTween.alphaCanvas(cardGroup, 1f, 0.18f).setIgnoreTimeScale(true);
            LeanTween.scale(cardRect.gameObject, Vector3.one, 0.28f)
                .setEaseOutBack()
                .setIgnoreTimeScale(true);

            if (type == AlertType.Loading)
            {
                LeanTween.scale(iconCircleRect.gameObject, Vector3.one, 0.22f)
                    .setEaseOutBack()
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        if (iconCircleRect != null && gameObject.activeSelf)
                        {
                            LeanTween.scale(iconCircleRect.gameObject, Vector3.one * 1.12f, 0.45f)
                                .setLoopPingPong()
                                .setIgnoreTimeScale(true);
                        }
                    });
            }
            else
            {
                LeanTween.scale(iconCircleRect.gameObject, Vector3.one, 0.3f)
                    .setEaseOutBack()
                    .setDelay(0.06f)
                    .setIgnoreTimeScale(true);
            }

            if (type == AlertType.Error)
            {
                LeanTween.moveLocalX(cardRect.gameObject, 14f, 0.35f)
                    .setEaseShake()
                    .setDelay(0.12f)
                    .setIgnoreTimeScale(true)
                    .setOnComplete(() =>
                    {
                        if (cardRect != null) cardRect.anchoredPosition = Vector2.zero;
                    });
            }
        }

        void ApplyDynamicLayout(string safeMsg, bool showOkButton)
        {
            int len = safeMsg.Length;

            // Choose card width & ideal font size based on message length
            float cardWidth;
            float maxMsgFont;
            float titleFontMax;
            if (len <= 45)
            {
                cardWidth = 780f;
                maxMsgFont = 36f;
                titleFontMax = 46f;
            }
            else if (len <= 120)
            {
                cardWidth = 880f;
                maxMsgFont = 32f;
                titleFontMax = 44f;
            }
            else
            {
                cardWidth = 980f;
                maxMsgFont = 28f;
                titleFontMax = 42f;
            }

            float scale = NutriMind.Core.GameSettings.TextSizeScale;
            titleText.fontSizeMax = titleFontMax * scale;
            titleText.fontSizeMin = 28f * scale;

            messageText.fontSize = maxMsgFont * scale;
            messageText.fontSizeMax = maxMsgFont * scale;
            messageText.fontSizeMin = 22f * scale;

            if (okButtonText != null)
                okButtonText.fontSize = 32f * scale;

            // Adjust bottom margin if there's no OK button (loading modal)
            float bottomReserve = showOkButton ? 128f : 48f;
            float topReserve = 228f;
            RectTransform msgRt = messageText.rectTransform;
            msgRt.offsetMin = new Vector2(0f, bottomReserve);
            msgRt.offsetMax = new Vector2(0f, -topReserve);

            float textAreaWidth = cardWidth * 0.86f;
            Vector2 preferred = messageText.GetPreferredValues(safeMsg, textAreaWidth, 0f);
            float msgHeight = Mathf.Max(64f, preferred.y + 24f);
            float cardHeight = Mathf.Clamp(topReserve + msgHeight + bottomReserve, showOkButton ? 420f : 350f, 780f);

            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        }

        void HideInternal()
        {
            if (!gameObject.activeSelf) return;

            LeanTween.cancel(backdropGroup.gameObject);
            LeanTween.cancel(cardRect.gameObject);
            LeanTween.cancel(iconCircleRect.gameObject);

            Action cb = onCloseCallback;
            onCloseCallback = null;

            LeanTween.alphaCanvas(backdropGroup, 0f, 0.14f).setIgnoreTimeScale(true);
            LeanTween.alphaCanvas(cardGroup, 0f, 0.14f).setIgnoreTimeScale(true);
            LeanTween.scale(cardRect.gameObject, Vector3.one * 0.85f, 0.14f)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (gameObject != null) gameObject.SetActive(false);
                    cb?.Invoke();
                });
        }

        static TMP_FontAsset FindFredokaFont(bool preferBold)
        {
            // 1. Check all loaded TMP_FontAssets (scenes already use Fredoka)
            TMP_FontAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            TMP_FontAsset anyFredoka = null;
            for (int i = 0; i < loaded.Length; i++)
            {
                TMP_FontAsset f = loaded[i];
                if (f == null) continue;
                string n = f.name;
                if (n.IndexOf("Fredoka", StringComparison.OrdinalIgnoreCase) < 0) continue;

                if (anyFredoka == null) anyFredoka = f;
                bool isBold = n.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              n.IndexOf("SemiBold", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isRegular = n.IndexOf("Regular", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 n.IndexOf("Medium", StringComparison.OrdinalIgnoreCase) >= 0;

                if (preferBold && isBold) return f;
                if (!preferBold && isRegular) return f;
            }
            if (anyFredoka != null) return anyFredoka;

            // 2. Fallback to any active scene TMP_Text font or TMP default
            TMP_Text existing = FindAnyObjectByType<TMP_Text>();
            if (existing != null && existing.font != null) return existing.font;
            return TMP_Settings.defaultFontAsset;
        }

        public static Sprite RoundedSprite => GetRoundedSprite();

        static Sprite GetRoundedSprite()
        {
            if (s_RoundedSprite != null) return s_RoundedSprite;
            const int size = 64;
            const int radius = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - 1 - x, x - (size - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - 1 - y, y - (size - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            s_RoundedSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            s_RoundedSprite.hideFlags = HideFlags.DontSave;
            return s_RoundedSprite;
        }

        static Sprite GetCircleSprite()
        {
            if (s_CircleSprite != null) return s_CircleSprite;
            const int size = 64;
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            s_CircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            s_CircleSprite.hideFlags = HideFlags.DontSave;
            return s_CircleSprite;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static Sprite GetCheckSprite()
        {
            if (s_CheckSprite != null) return s_CheckSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            Vector2 p1 = new Vector2(14f, 32f);
            Vector2 p2 = new Vector2(27f, 19f);
            Vector2 p3 = new Vector2(50f, 44f);
            const float thickness = 4.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = Mathf.Min(DistToSegment(p, p1, p2), DistToSegment(p, p2, p3));
                    float a = Mathf.Clamp01(thickness - d + 0.75f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            s_CheckSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            s_CheckSprite.hideFlags = HideFlags.DontSave;
            return s_CheckSprite;
        }

        static Sprite GetCrossSprite()
        {
            if (s_CrossSprite != null) return s_CrossSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            Vector2 a1 = new Vector2(17f, 17f);
            Vector2 a2 = new Vector2(47f, 47f);
            Vector2 b1 = new Vector2(17f, 47f);
            Vector2 b2 = new Vector2(47f, 17f);
            const float thickness = 4.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = Mathf.Min(DistToSegment(p, a1, a2), DistToSegment(p, b1, b2));
                    float a = Mathf.Clamp01(thickness - d + 0.75f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            s_CrossSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            s_CrossSprite.hideFlags = HideFlags.DontSave;
            return s_CrossSprite;
        }

        static Sprite GetInfoSprite()
        {
            if (s_InfoSprite != null) return s_InfoSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            Vector2 dot = new Vector2(32f, 47f);
            Vector2 stemTop = new Vector2(32f, 35f);
            Vector2 stemBot = new Vector2(32f, 15f);
            const float thickness = 4.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float dDot = Vector2.Distance(p, dot);
                    float dStem = DistToSegment(p, stemTop, stemBot);
                    float d = Mathf.Min(dDot, dStem);
                    float a = Mathf.Clamp01(thickness - d + 0.75f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            s_InfoSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            s_InfoSprite.hideFlags = HideFlags.DontSave;
            return s_InfoSprite;
        }

        static Sprite GetDotsSprite()
        {
            if (s_DotsSprite != null) return s_DotsSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.DontSave;
            Vector2 c1 = new Vector2(15f, 32f);
            Vector2 c2 = new Vector2(32f, 32f);
            Vector2 c3 = new Vector2(49f, 32f);
            const float r = 5.2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = Mathf.Min(Vector2.Distance(p, c1), Mathf.Min(Vector2.Distance(p, c2), Vector2.Distance(p, c3)));
                    float a = Mathf.Clamp01(r - d + 0.75f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            s_DotsSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            s_DotsSprite.hideFlags = HideFlags.DontSave;
            return s_DotsSprite;
        }
    }
}
