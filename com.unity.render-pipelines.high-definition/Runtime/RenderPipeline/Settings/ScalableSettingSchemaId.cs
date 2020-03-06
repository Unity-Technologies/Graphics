using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Identifies uniquely a <see cref="ScalableSettingSchema"/>.
    ///
    /// Use <see cref="ScalableSettingSchema.GetSchemaOrNull(ScalableSettingSchemaId)"/> to get a schema.
    /// </summary>
    [Serializable]
    public struct ScalableSettingSchemaId: IEquatable<ScalableSettingSchemaId>
    {
        /// <summary>A scalable setting with 3 levels.</summary>
        public static readonly ScalableSettingSchemaId With3Levels = new ScalableSettingSchemaId("With3Levels");
        /// <summary>A scalable setting with 4 levels.</summary>
        public static readonly ScalableSettingSchemaId With4Levels = new ScalableSettingSchemaId("With4Levels");

        [SerializeField]
        string m_Id;

        internal ScalableSettingSchemaId(string id) => m_Id = id;

        /// <summary>
        /// Checks equality
        /// </summary>
        /// <param name="other">The other to check.</param>
        /// <returns>True when they are equals</returns>
        public bool Equals(ScalableSettingSchemaId other) => m_Id == other.m_Id;

        /// <summary>
        /// Checks equality
        /// </summary>
        /// <param name="obj">The other to check.</param>
        /// <returns>True when they are equals</returns>
        public override bool Equals(object obj)
            => (obj is ScalableSettingSchemaId id) && id.m_Id == m_Id;

        /// <summary>
        /// Compute the hash code
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode() => m_Id?.GetHashCode() ?? 0;

        /// <summary>
        /// Compute a human friendly string representation
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString() => m_Id;

    }
}
