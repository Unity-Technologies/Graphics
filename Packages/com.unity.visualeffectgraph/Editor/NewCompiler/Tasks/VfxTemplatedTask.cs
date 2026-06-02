using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine;
using EntityId = UnityEngine.EntityId;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Extension methods for adding attribute paths to templated task bindings.
    /// </summary>
    static class VfxTemplatedTaskBindingExtensions
    {
        /// <summary>
        /// Adds attribute read and write paths for the specified attribute set, using the provided base data path.
        /// </summary>
        /// <param name="binding">The binding to add paths to.</param>
        /// <param name="attributesPath">The base <see cref="DataPath"/> under which attribute keys will be added.</param>
        /// <param name="attributeSet">The <see cref="AttributeSet"/> containing the attributes to be read and written.</param>
        public static void Add(this TemplatedTaskBinding binding, DataPath attributesPath, AttributeSet attributeSet)
        {
            foreach (var attribute in attributeSet.ReadAttributes)
            {
                binding.ReadPathSet.Add(attributesPath + new AttributeKey(attribute));
            }
            foreach (var attribute in attributeSet.WriteAttributes)
            {
                binding.WritePathSet.Add(attributesPath + new AttributeKey(attribute));
            }
        }

        /// <summary>
        /// Adds a VFX attribute to the read and/or write path sets of the binding.
        /// </summary>
        /// <param name="binding">The binding to add paths to.</param>
        /// <param name="attributesPath">The base <see cref="DataPath"/> under which the attribute key will be added.</param>
        /// <param name="vfxAttribute">The <see cref="VFXAttribute"/> to add.</param>
        /// <param name="usage">The <see cref="AttributeUsage"/> flags indicating read and/or write access.</param>
        public static void Add(this TemplatedTaskBinding binding, DataPath attributesPath, VFXAttribute vfxAttribute, AttributeUsage usage)
        {
            var attribute = VFXAttributesManager.ConvertToNewCompiler(vfxAttribute);
            var attributePath = attributesPath + new AttributeKey(attribute);
            if (usage.HasFlag(AttributeUsage.Read))
                binding.ReadPathSet.Add(attributePath);
            if (usage.HasFlag(AttributeUsage.Write))
                binding.WritePathSet.Add(attributePath);
        }
    }

    /// <summary>
    /// Represents a VFX task generated using a template and a list of subtasks, with attribute support.
    /// </summary>
    class VfxTemplatedTask : TemplatedTask
    {
        /// <summary>
        /// Arguments used to initialize a <see cref="VfxTemplatedTask"/>.
        /// </summary>
        public new struct Args
        {
            /// <summary>
            /// Base arguments for the templated task.
            /// </summary>
            public TemplatedTask.Args BaseArgs;

            /// <summary>
            /// Mappings from generic attribute keys to their corresponding data paths.
            /// </summary>
            public Dictionary<IDataKey, (IDataKey, DataPath)> AttributeKeyMappings;
        }

        readonly Dictionary<IDataKey, (IDataKey, DataPath)> m_AttributeKeyMappings;

        /// <summary>
        /// Gets a value indicating whether the task is a compute task.
        /// </summary>
        public bool IsCompute { get; }

        /// <summary>
        /// Gets the mappings from attribute keys to their corresponding binding relative paths.
        /// </summary>
        public IEnumerable<KeyValuePair<IDataKey, (IDataKey, DataPath)>> AttributeKeyMappings => m_AttributeKeyMappings;

        /// <summary>
        /// Gets the entity ID of the source model (e.g., VFXContext) that this task was created from.
        /// Used for material setup callbacks during compilation.
        /// </summary>
        public EntityId SourceModelId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VfxTemplatedTask"/> class using the specified template name, arguments, and compute flag.
        /// </summary>
        /// <param name="templateName">The name of the template associated with the task.</param>
        /// <param name="args">The parameters to setup the templated task <see cref="Args"/>.</param>
        /// <param name="isCompute">Indicates if the task is a compute task.</param>
        /// <param name="sourceModelId">Optional entity ID of the source model (e.g., VFXContext) that this task was created from.</param>
        public VfxTemplatedTask(string templateName, Args args, bool isCompute, EntityId sourceModelId = default)
            : base(templateName, args.BaseArgs)
        {
            IsCompute = isCompute;
            m_AttributeKeyMappings = args.AttributeKeyMappings != null ? new(args.AttributeKeyMappings) : new();
            SourceModelId = sourceModelId;

            // Validate attribute key mappings reference valid bindings
            foreach (var attributeKeyMapping in m_AttributeKeyMappings)
            {
                Debug.Assert(HasBinding(attributeKeyMapping.Value.Item1));
            }

            // Process attribute sets from TemplateSubtasks
            foreach (var subtaskDescription in args.BaseArgs.Subtasks)
            {
                if (subtaskDescription.Task is TemplateSubtask templateSubtask)
                {
                    foreach (var kvp in templateSubtask.AttributeSets)
                    {
                        var attributeKey = kvp.Key;
                        if (m_AttributeKeyMappings.TryGetValue(attributeKey, out var mappedKeyPath))
                        {
                            IDataKey bindingKey = mappedKeyPath.Item1;
                            DataPath attributesPath = mappedKeyPath.Item2;

                            var snippetAttributeSet = kvp.Value;
                            Debug.Assert(HasBinding(bindingKey), $"Binding key '{bindingKey}' not found in args.Bindings. All bindings should be declared upfront with their types.");

                            foreach (var attribute in snippetAttributeSet.ReadAttributes)
                            {
                                AddReadPath(bindingKey, attributesPath + new AttributeKey(attribute));
                            }

                            foreach (var attribute in snippetAttributeSet.WriteAttributes)
                            {
                                AddWritePath(bindingKey, attributesPath + new AttributeKey(attribute));
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Attribute set \"{kvp.Key}\" not found in task {this}");
                        }
                    }
                }
            }
        }
    }
}
