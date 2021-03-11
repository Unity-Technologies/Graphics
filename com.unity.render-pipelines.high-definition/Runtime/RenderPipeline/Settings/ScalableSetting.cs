using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Define the level's value for a <see cref="ScalableSettingValue{T}"/>.
    ///
    /// Use this setting in an asset that defines the quality settings for a specific platform.
    /// Then those settings can be used with the <see cref="ScalableSettingValue{T}"/> to get the actual value to use.
    ///
    /// If you intend to serialize this type, use specialized version instead. (<see cref="IntScalableSetting"/>).
    /// </summary>
    /// <typeparam name="T">The type of the scalable setting.</typeparam>
    [Serializable]
    public class ScalableSetting<T>: ISerializationCallbackReceiver
    {
        [SerializeField] private T[] m_Values;
        [SerializeField] private ScalableSettingSchemaId m_SchemaId;

        /// <summary>Build a new scalable setting.</summary>
        /// <param name="values">The values of the scalable setting. Must not be <c>null</c>.</param>
        /// <param name="schemaId">The of the schema for this scalable setting.</param>
        public ScalableSetting(T[] values, ScalableSettingSchemaId schemaId)
        {
            Assert.IsNotNull(values, $"{nameof(values)} must not be null.");

            m_Values = values;
            m_SchemaId = schemaId;
        }

        /// <summary>The schema id of this scalable setting.</summary>
        public ScalableSettingSchemaId schemaId
        {
            get => m_SchemaId;
            set => m_SchemaId = value;
        }

        /// <summary>Get the value for a specific level.</summary>
        /// <param name="index">The index of the value to get.</param>
        /// <returns>
        /// If the <paramref name="index"/> is in the range of the contained values, the associated value will be returned.
        /// Otherwise, <c>default</c> is returned.
        /// </returns>
        public T this[int index] => m_Values != null && index >= 0 && index < m_Values.Length ? m_Values[index] : default;

        /// <summary>
        /// Get the value of the level <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the level to get.</param>
        /// <param name="value">
        /// Contains the value of the level when the level is found.
        ///
        /// <c>default</c> when:
        /// - The index is out of range
        /// </param>
        /// <returns><c>true</c> when the value was evaluated, <c>false</c> when the value could not be evaluated.</returns>
        public bool TryGet(int index, out T value)
        {
            Assert.IsNotNull(m_Values, "Values must not be null, it was checked in the constructor and in serialization callbacks");

            if (index >= 0 && index < m_Values.Length)
            {
                value = m_Values[index];
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>Serialization callback</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Enforce the number of stored value after serialization
            if (ScalableSettingSchema.Schemas.TryGetValue(m_SchemaId, out var schema))
                Array.Resize(ref m_Values, schema.levelCount);
            else if (m_Values == null)
                m_Values = new T[0];
        }

        /// <summary>Serialization callback</summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Enforce the number of stored value before serialization
            if (ScalableSettingSchema.Schemas.TryGetValue(m_SchemaId, out var schema))
                Array.Resize(ref m_Values, schema.levelCount);
            else if (m_Values == null)
                m_Values = new T[0];
        }
    }

    #region Specialized Scalable Settings

    // We define explicitly specialized version of the ScalableSetting so it can be serialized with
    // Unity's serialization API.

    /// <summary><see cref="ScalableSetting{T}"/>.</summary>
    [Serializable]
    public class IntScalableSetting : ScalableSetting<int>
    {
        /// <summary>
        /// Instantiate a new int scalable setting.
        /// </summary>
        /// <param name="values">The values of the settings</param>
        /// <param name="schemaId">The schema of the setting.</param>
        public IntScalableSetting(int[] values, ScalableSettingSchemaId schemaId) : base(values, schemaId) {}
    }
    /// <summary><see cref="ScalableSetting{T}"/>.</summary>
    [Serializable]
    public class UintScalableSetting : ScalableSetting<uint>
    {
        /// <summary>
        /// Instantiate a new uint scalable setting.
        /// </summary>
        /// <param name="values">The values of the settings</param>
        /// <param name="schemaId">The schema of the setting.</param>
        public UintScalableSetting(uint[] values, ScalableSettingSchemaId schemaId) : base(values, schemaId) {}
    }
    /// <summary><see cref="ScalableSetting{T}"/>.</summary>
    [Serializable]
    public class FloatScalableSetting : ScalableSetting<float>
    {
        /// <summary>
        /// Instantiate a new float scalable setting.
        /// </summary>
        /// <param name="values">The values of the settings</param>
        /// <param name="schemaId">The schema of the setting.</param>
        public FloatScalableSetting(float[] values, ScalableSettingSchemaId schemaId) : base(values, schemaId) {}
    }
    /// <summary><see cref="ScalableSetting{T}"/>.</summary>
    [Serializable]
    public class BoolScalableSetting : ScalableSetting<bool>
    {
        /// <summary>
        /// Instantiate a new bool scalable setting.
        /// </summary>
        /// <param name="values">The values of the settings</param>
        /// <param name="schemaId">The schema of the setting.</param>
        public BoolScalableSetting(bool[] values, ScalableSettingSchemaId schemaId) : base(values, schemaId) {}
    }
    #endregion
}
