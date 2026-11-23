using System;
using TMPro;
using UnityEngine;

namespace NutriMind.Interface
{
    /// <summary>
    /// Attaches to any TextMeshProUGUI (Canvas UI) or TMP_Text to bend/arc text along a circular curve.
    /// Works safely in both Edit Mode (Scene view) and Play Mode without recursion or crashes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("UI/NutriMind/TMP Arc Text")]
    public class TMP_ArcText : MonoBehaviour
    {
        [Header("Arc Settings")]
        [Tooltip("Arc angle in degrees.\n• Positive (e.g. 60° to 120°): Arch curve over a circle/badge (∩)\n• Negative (e.g. -60° to -120°): Smile curve underneath (∪)\n• 0: Flat normal text")]
        [Range(-360f, 360f)]
        [SerializeField] float arcAngle = 90f;

        [Tooltip("Automatically calculates arc radius based on text width and Arc Angle.")]
        [SerializeField] bool autoRadius = true;

        [Tooltip("Manual radius in canvas units (only used when Auto Radius is unchecked).")]
        [Min(5f)]
        [SerializeField] float manualRadius = 150f;

        [Tooltip("Rotates individual letters to follow the tangent of the curve.")]
        [SerializeField] bool rotateCharacters = true;

        [Header("Alignment & Offset")]
        [Tooltip("Horizontal center of the arc: 0.5 = center, 0.0 = left, 1.0 = right.")]
        [Range(0f, 1f)]
        [SerializeField] float arcCenterPivot = 0.5f;

        [Tooltip("Vertical offset applied to the arched text in canvas units.")]
        [SerializeField] float yOffset = 0f;

        TMP_Text m_Text;
        bool m_IsUpdating = false;
        bool m_IsDirty = true;

        // Cached property values to avoid redundant mesh updates
        float m_LastAngle;
        bool m_LastAutoRadius;
        float m_LastManualRadius;
        bool m_LastRotate;
        float m_LastPivot;
        float m_LastYOffset;
        string m_LastTextContent;
        float m_LastFontSize;

        public float ArcAngle
        {
            get { return arcAngle; }
            set { arcAngle = value; m_IsDirty = true; }
        }

        public bool AutoRadius
        {
            get { return autoRadius; }
            set { autoRadius = value; m_IsDirty = true; }
        }

        public float ManualRadius
        {
            get { return manualRadius; }
            set { manualRadius = Mathf.Max(5f, value); m_IsDirty = true; }
        }

        public bool RotateCharacters
        {
            get { return rotateCharacters; }
            set { rotateCharacters = value; m_IsDirty = true; }
        }

        public float YOffset
        {
            get { return yOffset; }
            set { yOffset = value; m_IsDirty = true; }
        }

        void Awake()
        {
            m_Text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            if (m_Text == null) m_Text = GetComponent<TMP_Text>();
            m_IsDirty = true;
        }

        void OnDisable()
        {
            // Restore normal mesh when disabled
            if (m_Text != null && !m_IsUpdating)
            {
                m_IsUpdating = true;
                try
                {
                    m_Text.ForceMeshUpdate();
                }
                catch { }
                finally
                {
                    m_IsUpdating = false;
                }
            }
        }

        // OnValidate runs inside Unity's property inspector.
        // We ONLY mark as dirty here; NEVER call ForceMeshUpdate inside OnValidate to avoid editor crashes.
        void OnValidate()
        {
            m_IsDirty = true;
        }

        void LateUpdate()
        {
            if (m_IsUpdating || m_Text == null) return;

            if (m_IsDirty || m_Text.havePropertiesChanged || HasPropertiesChanged())
            {
                ApplyArc();
            }
        }

        public void ForceUpdate()
        {
            m_IsDirty = true;
            ApplyArc();
        }

        bool HasPropertiesChanged()
        {
            if (!Mathf.Approximately(arcAngle, m_LastAngle)) return true;
            if (autoRadius != m_LastAutoRadius) return true;
            if (!Mathf.Approximately(manualRadius, m_LastManualRadius)) return true;
            if (rotateCharacters != m_LastRotate) return true;
            if (!Mathf.Approximately(arcCenterPivot, m_LastPivot)) return true;
            if (!Mathf.Approximately(yOffset, m_LastYOffset)) return true;
            if (m_Text.text != m_LastTextContent) return true;
            if (!Mathf.Approximately(m_Text.fontSize, m_LastFontSize)) return true;
            return false;
        }

        void CacheProperties()
        {
            m_LastAngle = arcAngle;
            m_LastAutoRadius = autoRadius;
            m_LastManualRadius = manualRadius;
            m_LastRotate = rotateCharacters;
            m_LastPivot = arcCenterPivot;
            m_LastYOffset = yOffset;
            m_LastTextContent = m_Text.text;
            m_LastFontSize = m_Text.fontSize;
        }

        void ApplyArc()
        {
            if (m_IsUpdating || m_Text == null) return;
            m_IsUpdating = true;

            try
            {
                // Generate base text mesh first
                m_Text.ForceMeshUpdate();
                TMP_TextInfo textInfo = m_Text.textInfo;

                if (textInfo == null || textInfo.characterCount == 0 || textInfo.meshInfo == null || textInfo.meshInfo.Length == 0)
                {
                    m_IsDirty = false;
                    return;
                }

                CacheProperties();
                m_IsDirty = false;

                // If flat (0 angle), base mesh is already correct
                if (Mathf.Abs(arcAngle) < 0.05f)
                {
                    return;
                }

                float boundsMinX = m_Text.bounds.min.x;
                float boundsMaxX = m_Text.bounds.max.x;
                float boundsWidth = boundsMaxX - boundsMinX;
                if (boundsWidth <= 0.001f) return;

                float arcRad = Mathf.Abs(arcAngle) * Mathf.Deg2Rad;
                float sign = Mathf.Sign(arcAngle); // +1 = ∩ arch, -1 = ∪ smile

                float effectiveRadius = autoRadius
                    ? (boundsWidth / Mathf.Max(0.01f, arcRad))
                    : Mathf.Max(5f, manualRadius);

                float centerX = boundsMinX + boundsWidth * arcCenterPivot;

                int characterCount = textInfo.characterCount;
                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    int vertexIndex = charInfo.vertexIndex;
                    int materialIndex = charInfo.materialReferenceIndex;

                    if (materialIndex >= textInfo.meshInfo.Length) continue;
                    Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                    if (vertices == null || vertexIndex + 3 >= vertices.Length) continue;

                    // Character midpoint along baseline
                    float charMidX = (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) * 0.5f;
                    float charBaseY = charInfo.baseLine;
                    Vector3 charPivot = new Vector3(charMidX, charBaseY, 0f);

                    // Angular position on circle
                    float arcDist = charMidX - centerX;
                    float theta = arcDist / effectiveRadius;

                    // Position on circular arc
                    float newX = centerX + effectiveRadius * Mathf.Sin(theta);
                    float newY = sign > 0
                        ? (effectiveRadius * (Mathf.Cos(theta) - 1f))
                        : (effectiveRadius * (1f - Mathf.Cos(theta)));

                    newY += yOffset;

                    // Tangent rotation
                    float rotAngle = rotateCharacters
                        ? (-theta * Mathf.Rad2Deg * sign)
                        : 0f;

                    // Transform vertices: translate to origin, rotate, translate to arc position
                    Matrix4x4 matrix = Matrix4x4.TRS(
                        new Vector3(newX, newY + charBaseY, 0f),
                        Quaternion.Euler(0f, 0f, rotAngle),
                        Vector3.one
                    );

                    for (int v = 0; v < 4; v++)
                    {
                        Vector3 localPoint = vertices[vertexIndex + v] - charPivot;
                        vertices[vertexIndex + v] = matrix.MultiplyPoint3x4(localPoint);
                    }
                }

                // Push vertex data back to mesh
                m_Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TMP_ArcText] Mesh update skipped: " + ex.Message);
            }
            finally
            {
                m_IsUpdating = false;
            }
        }
    }
}
