using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using EntityId = UnityEngine.EntityId;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A lowered task, bound directly to concrete VFX buffers rather than to the logical data the
    /// templated tasks were written against. Usage is declared per binding key as the task is built,
    /// since a lowered task has no fixed set of bindings to switch on.
    /// </summary>
    class VfxTask : ITask
    {
        public static NameDataKey InstancingBufferKey = new("instancingIndirectAndActiveIndirect");
        public static NameDataKey InstancingPrefixSumBufferKey = new("instancingPrefixSum");

        readonly HashSet<IDataKey> m_ReadBindings = new();
        readonly HashSet<IDataKey> m_WriteBindings = new();
        readonly HashSet<IDataKey> m_ParameterBindings = new();
        readonly List<(IDataKey key, int value)> m_ParameterValues = new();

        /// <summary>
        /// The kind of task the runtime is to run, which for an output also selects its topology.
        /// Set by the lowering it came from, which is the one that knows.
        /// </summary>
        public UnityEngine.VFX.VFXTaskType TaskType { get; set; }

        /// <summary>
        /// The model (a <see cref="VFXContext"/>) the task was authored from, which the runtime needs
        /// for the material setup callbacks.
        /// </summary>
        public EntityId SourceModelId { get; set; }

        /// <summary>
        /// Whether the task runs generated code. A task the runtime implements itself - a sort - has
        /// none, and is emitted with no shader source at all rather than an empty one.
        /// </summary>
        public bool HasShaderSource { get; set; } = true;

        public void AddReadBinding(IDataKey bindingKey) => m_ReadBindings.Add(bindingKey);
        public void AddWriteBinding(IDataKey bindingKey) => m_WriteBindings.Add(bindingKey);

        /// <summary>
        /// Declares a task parameter the runtime resolves to a constant rather than to a graph value,
        /// such as a sort's sorting mode. Unlike a <see cref="AddParameterBinding"/> parameter it carries no
        /// data binding at all, so it never reaches the graph and is emitted straight from here, in the
        /// order it was declared.
        /// </summary>
        public void AddParameterValue(IDataKey bindingKey, int value) => m_ParameterValues.Add((bindingKey, value));

        /// <inheritdoc cref="AddParameterValue"/>
        public IReadOnlyList<(IDataKey key, int value)> ParameterValues => m_ParameterValues;

        /// <inheritdoc cref="AddParameterValue"/>
        public bool HasParameterValue(IDataKey bindingKey)
        {
            foreach (var (key, _) in m_ParameterValues)
            {
                if (key.Equals(bindingKey))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Marks a binding as one the runtime resolves as a task parameter rather than as a value the
        /// generated code reads. Carried over from the <see cref="VfxTaskParameterBinding"/> the task was
        /// lowered from, since a lowered task no longer knows what kind each of its bindings was.
        /// </summary>
        public void AddParameterBinding(IDataKey bindingKey) => m_ParameterBindings.Add(bindingKey);

        /// <inheritdoc cref="AddParameterBinding"/>
        public bool IsParameterBinding(IDataKey bindingKey) => m_ParameterBindings.Contains(bindingKey);

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            var usage = BindingUsage.Unknown;

            // A concrete buffer is bound whole, so the declared path is always the root.
            if (m_ReadBindings.Contains(dataKey))
            {
                readUsage?.Add(DataPath.Root);
                usage |= BindingUsage.Read;
            }

            if (m_WriteBindings.Contains(dataKey))
            {
                writeUsage?.Add(DataPath.Root);
                usage |= BindingUsage.Write;
            }

            if(dataKey.Equals(InstancingBufferKey) || dataKey.Equals(InstancingPrefixSumBufferKey))
            {
                readUsage?.Add(DataPath.Root);
                usage |= BindingUsage.Read;
            }

            return usage;
        }
    }
}
