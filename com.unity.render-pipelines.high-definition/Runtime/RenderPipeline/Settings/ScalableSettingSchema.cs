using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Defines the number of levels and the level names for a scalable setting.
    ///
    /// This class is intended to be immutable. As it is a reference type, a schema should be instantiated only once and used
    /// as reference everywhere.
    /// </summary>
    public class ScalableSettingSchema
    {
        /// <summary>
        /// Available scalable setting schemas.
        ///
        /// In the future this array will probably be dynamic.
        /// For now it is immutable to avoid to handle cases where a schema is missing.
        /// </summary>
        internal static readonly Dictionary<ScalableSettingSchemaId, ScalableSettingSchema> Schemas = new Dictionary<ScalableSettingSchemaId, ScalableSettingSchema>
        {
            { ScalableSettingSchemaId.With3Levels, new ScalableSettingSchema(new[] {
                new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High")
            }) },
            { ScalableSettingSchemaId.With4Levels, new ScalableSettingSchema(new[] {
                new GUIContent("Low"), new GUIContent("Medium"), new GUIContent("High"), new GUIContent("Ultra")
            }) },
        };

        /// <summary>Get the <see cref="ScalableSettingSchema"/> for the provided <paramref name="id"/>.</summary>
        /// <param name="id">Id to search for.</param>
        /// <returns>The schema if it exists, otherwise <c>null</c>.</returns>
        internal static ScalableSettingSchema GetSchemaOrNull(ScalableSettingSchemaId id)
            => Schemas.TryGetValue(id, out var value) ? value : null;

        /// <summary>Get the <see cref="ScalableSettingSchema"/> for the provided <paramref name="id"/>.</summary>
        /// <param name="id">Id to search for.</param>
        /// <returns>The schema if it exists, otherwise <c>null</c>.</returns>
        internal static ScalableSettingSchema GetSchemaOrNull(ScalableSettingSchemaId? id)
            => id.HasValue && Schemas.TryGetValue(id.Value, out var value) ? value : null;

        /// <summary>The names of the levels.</summary>
        public readonly GUIContent[] levelNames;

        /// <summary>The number of levels.</summary>
        public int levelCount => levelNames.Length;

        /// <summary>
        /// Instantiate a new schema.
        /// </summary>
        /// <param name="levelNames">The names of each level.</param>
        public ScalableSettingSchema(GUIContent[] levelNames) => this.levelNames = levelNames;
    }
}
