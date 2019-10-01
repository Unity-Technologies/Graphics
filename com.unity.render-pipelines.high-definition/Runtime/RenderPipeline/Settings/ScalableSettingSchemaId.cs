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

        public bool Equals(ScalableSettingSchemaId other) => m_Id == other.m_Id;

        public override bool Equals(object obj)
            => (obj is ScalableSettingSchemaId id) && id.m_Id == m_Id;

        public override int GetHashCode() => m_Id?.GetHashCode() ?? 0;

        public override string ToString() => m_Id;

    }
}
