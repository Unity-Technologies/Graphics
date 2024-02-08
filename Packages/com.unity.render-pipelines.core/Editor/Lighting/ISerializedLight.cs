using System;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface to be implemented by each pipeline to hold the <see cref="SerializedObject"/> for a Light Editor
    /// </summary>
    public interface ISerializedLight
    {
        /// <summary>The base settings of the light</summary>
        LightEditor.Settings settings { get; }
        /// <summary>The light serialized</summary>
        SerializedObject serializedObject { get; }
        /// <summary>The additinal light data serialized</summary>
        SerializedObject serializedAdditionalDataObject { get; }

        /// <summary>Light Intensity Property</summary>
        [Obsolete("This property has been deprecated. Use ISerializedLight.settings.intensity instead.")]
        SerializedProperty intensity { get; }

        /// <summary>Method that updates the <see cref="SerializedObject"/> of the Light and the Additional Light Data</summary>
        void Update();

        /// <summary>Method that applies the modified properties the <see cref="SerializedObject"/> of the Light and the Light Camera Data</summary>
        void Apply();
    }
}
