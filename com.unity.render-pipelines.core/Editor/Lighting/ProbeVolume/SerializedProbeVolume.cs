namespace UnityEditor.Experimental.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty globalVolume;
        internal SerializedProperty size;
        internal SerializedProperty overridesSubdivision;
        internal SerializedProperty highestSubdivisionLevelOverride;
        internal SerializedProperty lowestSubdivisionLevelOverride;
        internal SerializedProperty objectLayerMask;
        internal SerializedProperty geometryDistanceOffset;

        internal SerializedObject serializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            serializedObject = obj;

            globalVolume = serializedObject.FindProperty("globalVolume");
            size = serializedObject.FindProperty("size");
            objectLayerMask = serializedObject.FindProperty("objectLayerMask");
            geometryDistanceOffset = serializedObject.FindProperty("geometryDistanceOffset");
            highestSubdivisionLevelOverride = serializedObject.FindProperty("highestSubdivLevelOverride");
            lowestSubdivisionLevelOverride = serializedObject.FindProperty("lowestSubdivLevelOverride");
            overridesSubdivision = serializedObject.FindProperty("overridesSubdivLevels");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
