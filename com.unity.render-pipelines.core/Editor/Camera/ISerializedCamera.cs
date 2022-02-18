namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface to be implemented by each pipeline to hold the <see cref="SerializedObject"/> for a Camera Editor
    /// </summary>
    public interface ISerializedCamera
    {
        /// <summary>The camera serialized</summary>
        SerializedObject serializedObject { get; }
        /// <summary>The additional camera data serialized</summary>
        SerializedObject serializedAdditionalDataObject { get; }
        /// <summary>The base camera settings</summary>
        CameraEditor.Settings baseCameraSettings { get; }

        // This one is internal in UnityEditor for whatever reason...
        /// <summary>The projection matrix mode</summary>
        SerializedProperty projectionMatrixMode { get; }

        // Common properties
        /// <summary>Dithering property</summary>
        SerializedProperty dithering { get; }
        /// <summary>Stop NaNs property</summary>
        SerializedProperty stopNaNs { get; }
        /// <summary>Allow Dynamic resolution property</summary>
        SerializedProperty allowDynamicResolution { get; }
        /// <summary>Volume layer mask property</summary>
        SerializedProperty volumeLayerMask { get; }
        /// <summary>Clear Depth property property</summary>
        SerializedProperty clearDepth { get; }
        /// <summary>Anti aliasing property</summary>
        SerializedProperty antialiasing { get; }

        /// <summary>Method that updates the <see cref="SerializedObject"/> of the Camera and the Additional Camera Data</summary>
        void Update();

        /// <summary>Applies the modified properties to the <see cref="SerializedObject"/> of the Camera and the Additional Camera Data</summary>
        void Apply();

        /// <summary>Refreshes the <see cref="SerializedProperty"/> of the <see cref="SerializedObject"/> of the Camera and the Additional Camera Data</summary>
        void Refresh();
    }
}
