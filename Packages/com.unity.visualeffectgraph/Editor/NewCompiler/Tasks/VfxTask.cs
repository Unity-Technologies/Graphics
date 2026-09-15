using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

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

        public void AddReadBinding(IDataKey bindingKey) => m_ReadBindings.Add(bindingKey);
        public void AddWriteBinding(IDataKey bindingKey) => m_WriteBindings.Add(bindingKey);

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
