namespace UnityEditor.Rendering
{
    public interface ISerializedCamera
    {
        SerializedObject serializedObject { get; }
        SerializedObject serializedAdditionalDataObject { get; }
        CameraEditor.Settings baseCameraSettings { get; }

        // This one is internal in UnityEditor for whatever reason...
        SerializedProperty projectionMatrixMode { get; }

        // Common properties
        SerializedProperty dithering { get; }
        SerializedProperty stopNaNs { get; }
        SerializedProperty allowDynamicResolution { get; }
        SerializedProperty volumeLayerMask { get; }
        SerializedProperty clearDepth { get; }
        SerializedProperty antialiasing { get; }

        void Update();
        void Apply();
        void Refresh();
    }
}
