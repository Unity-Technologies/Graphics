namespace UnityEditor.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty globalVolume;
        internal SerializedProperty size;
        internal SerializedProperty fillEmptySpaces;
        internal SerializedProperty overridesSubdivision;
        internal SerializedProperty highestSubdivisionLevelOverride;
        internal SerializedProperty lowestSubdivisionLevelOverride;
        internal SerializedProperty objectLayerMask;
        internal SerializedProperty minRendererVolumeSize;
        internal SerializedProperty overrideRendererFilters;

        internal SerializedObject serializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            serializedObject = obj;

            globalVolume = serializedObject.FindProperty("globalVolume");
            size = serializedObject.FindProperty("size");
            objectLayerMask = serializedObject.FindProperty("objectLayerMask");
            minRendererVolumeSize = serializedObject.FindProperty("minRendererVolumeSize");
            overrideRendererFilters = serializedObject.FindProperty("overrideRendererFilters");
            highestSubdivisionLevelOverride = serializedObject.FindProperty("highestSubdivLevelOverride");
            lowestSubdivisionLevelOverride = serializedObject.FindProperty("lowestSubdivLevelOverride");
            overridesSubdivision = serializedObject.FindProperty("overridesSubdivLevels");
            fillEmptySpaces = serializedObject.FindProperty("fillEmptySpaces");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
