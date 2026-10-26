namespace UnityEditor.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty m_Mode;
        internal SerializedProperty m_Size;
        internal SerializedProperty m_FillEmptySpaces;
        internal SerializedProperty m_OverridesSubdivision;
        internal SerializedProperty m_ObjectLayerMask;
        internal SerializedProperty m_MinRendererVolumeSize;
        internal SerializedProperty m_OverrideRendererFilters;

        internal SerializedProperty m_MinSubdivisionLevel;
        internal SerializedProperty m_MaxSubdivisionLevel;

        internal SerializedObject m_SerializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            m_SerializedObject = obj;

            m_Mode = m_SerializedObject.FindProperty("mode");
            m_Size = m_SerializedObject.FindProperty("size");
            m_ObjectLayerMask = m_SerializedObject.FindProperty("objectLayerMask");
            m_MinRendererVolumeSize = m_SerializedObject.FindProperty("minRendererVolumeSize");
            m_OverrideRendererFilters = m_SerializedObject.FindProperty("overrideRendererFilters");
            m_MinSubdivisionLevel = m_SerializedObject.FindProperty("lowestSubdivLevelOverride");
            m_MaxSubdivisionLevel = m_SerializedObject.FindProperty("highestSubdivLevelOverride");
            m_OverridesSubdivision = m_SerializedObject.FindProperty("overridesSubdivLevels");
            m_FillEmptySpaces = m_SerializedObject.FindProperty("fillEmptySpaces");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
