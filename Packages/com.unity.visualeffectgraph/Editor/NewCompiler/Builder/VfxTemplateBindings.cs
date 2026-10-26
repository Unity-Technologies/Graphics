using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// The names the templated tasks refer to their bindings by, and the paths into them the tasks
    /// declare. Shared vocabulary: a template's generated code, the data description writers and every
    /// builder that puts a task together all have to agree on them, so they belong to none of the three.
    /// </summary>
    static class VfxTemplateBindings
    {
        public static DataPath AttributeDataPath { get; } = DataPath.Root + ParticleData.AttributeDataKey;
        public static IDataKey ParticleDataBindingKey { get; } = new NameDataKey("ParticleDataBinding");
        public static IDataKey GraphValuesKey { get; } = new NameDataKey("GraphValues");
        public static IDataKey EventListDataBindingKey { get; } = new NameDataKey("EventListDataBinding");
        public static IDataKey SourceAttributesBindingKey { get; } = new NameDataKey("SourceAttributeDataBinding");
        public static IDataKey RenderingDataBindingKey { get; } = new NameDataKey("RenderingDataBinding");
        public static IDataKey ContextDataKey { get; } = new NameDataKey("ContextData");
        public static IDataKey MaxParticleCountKey { get; } = new NameDataKey("maxParticleCount");
        public static DataPath MaxParticleCountPath { get; } = DataPath.Root + MaxParticleCountKey;
        public static IDataKey SystemSeedKey { get; } = new NameDataKey("systemSeed");
        public static DataPath SystemSeedPath { get; } = DataPath.Root + SystemSeedKey;
        public static IDataKey InitSpawnIndexKey { get; } = new NameDataKey("initSpawnIndex");
        public static DataPath InitSpawnIndexPath { get; } = DataPath.Root + InitSpawnIndexKey;
    }
}
