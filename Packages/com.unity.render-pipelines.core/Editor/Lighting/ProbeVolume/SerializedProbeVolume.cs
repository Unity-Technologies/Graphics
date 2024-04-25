namespace UnityEditor.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty mode;
        internal SerializedProperty size;
        internal SerializedProperty fillEmptySpaces;
        internal SerializedProperty overridesSubdivision;
        internal SerializedProperty objectLayerMask;
        internal SerializedProperty minRendererVolumeSize;
        internal SerializedProperty overrideRendererFilters;

        internal SerializedProperty minSubdivisionLevel;
        internal SerializedProperty maxSubdivisionLevel;

        internal SerializedObject serializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            serializedObject = obj;

            mode = serializedObject.FindProperty("mode");
            size = serializedObject.FindProperty("size");
            objectLayerMask = serializedObject.FindProperty("objectLayerMask");
            minRendererVolumeSize = serializedObject.FindProperty("minRendererVolumeSize");
            overrideRendererFilters = serializedObject.FindProperty("overrideRendererFilters");
            minSubdivisionLevel = serializedObject.FindProperty("lowestSubdivLevelOverride");
            maxSubdivisionLevel = serializedObject.FindProperty("highestSubdivLevelOverride");
            overridesSubdivision = serializedObject.FindProperty("overridesSubdivLevels");
            fillEmptySpaces = serializedObject.FindProperty("fillEmptySpaces");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
