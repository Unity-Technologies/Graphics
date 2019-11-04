using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class InfluenceVolume
    {
        //editor value that need to be saved for easy passing from simplified to advanced and vice et versa
        // /!\ must not be used outside editor code
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendDistancePositive")]
        Vector3 m_EditorAdvancedModeBlendDistancePositive;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendDistanceNegative")]
        Vector3 m_EditorAdvancedModeBlendDistanceNegative;
        [SerializeField, FormerlySerializedAs("editorSimplifiedModeBlendDistance")]
        float m_EditorSimplifiedModeBlendDistance;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendNormalDistancePositive")]
        Vector3 m_EditorAdvancedModeBlendNormalDistancePositive;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendNormalDistanceNegative")]
        Vector3 m_EditorAdvancedModeBlendNormalDistanceNegative;
        [SerializeField, FormerlySerializedAs("editorSimplifiedModeBlendNormalDistance")]
        float m_EditorSimplifiedModeBlendNormalDistance;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeEnabled")]
        bool m_EditorAdvancedModeEnabled;
        [SerializeField]
        Vector3 m_EditorAdvancedModeFaceFadePositive = Vector3.one;
        [SerializeField]
        Vector3 m_EditorAdvancedModeFaceFadeNegative = Vector3.one;
    }
}
