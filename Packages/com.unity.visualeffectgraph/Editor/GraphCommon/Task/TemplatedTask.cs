using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a set of attributes located at a specific relative path within a data structure.
    /// </summary>
    /*public*/ struct LocatedAttributeSet
    {
        /// <summary>
        /// The relative path that binds the attribute set to a specific location.
        /// </summary>
        public BindingRelativePath BindingRelativePath;

        /// <summary>
        /// The set of attributes associated with the specified binding path.
        /// </summary>
        public AttributeSet AttributeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocatedAttributeSet"/> struct.
        /// </summary>
        /// <param name="bindingKey">The key used to bind the attribute set.</param>
        /// <param name="attributePath">The relative path within the data structure where the attributes are located.</param>
        /// <param name="attributeSet">The set of attributes to associate with the binding.</param>
        public LocatedAttributeSet(IDataKey bindingKey, DataPath attributePath, AttributeSet attributeSet)
        {
            BindingRelativePath = new BindingRelativePath(bindingKey, attributePath);
            AttributeSet = attributeSet;
        }
    }

    /// <summary>
    /// Represents a relative path used to bind data within a data structure.
    /// </summary>
    /*public*/ struct BindingRelativePath
    {
        /// <summary>
        /// The key used to identify the binding within the data structure.
        /// </summary>
        public IDataKey BindingKey;

        /// <summary>
        /// The relative path within the data structure where the binding applies.
        /// </summary>
        public DataPath SubDataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingRelativePath"/> struct.
        /// </summary>
        /// <param name="bindingKey">The key used to identify the binding.</param>
        /// <param name="subDataPath">The relative path within the data structure where the binding applies.</param>
        public BindingRelativePath(IDataKey bindingKey, DataPath subDataPath)
        {
            BindingKey = bindingKey;
            SubDataPath = subDataPath;
        }
    }

    /// <summary>
    /// Represents the paths used for reading and writing data bindings within a system.
    /// </summary>
    /*public*/ class BindingUsagePaths
    {
        /// <summary>
        /// The set of paths used for reading data bindings.
        /// </summary>
        public DataPathSet Read { get; } = new();

        /// <summary>
        /// The set of paths used for writing data bindings.
        /// </summary>
        public DataPathSet Write { get; } = new();

        /// <summary>
        /// Adds the read and write paths from another <see cref="BindingUsagePaths"/> instance
        /// to this instance.
        /// </summary>
        /// <param name="other">
        /// The <see cref="BindingUsagePaths"/> instance whose paths will be added.
        /// </param>
        public void Add(BindingUsagePaths other)
        {
            Read.Add(other.Read);
            Write.Add(other.Write);
        }

        /// <summary>
        /// Adds attribute read and write paths for the specified attribute set, using the provided base data path.
        /// </summary>
        /// <param name="attributesPath">
        /// The base <see cref="DataPath"/> under which attribute keys will be added.
        /// </param>
        /// <param name="attributeSet">
        /// The <see cref="AttributeSet"/> containing the attributes to be read and written.
        /// </param>
        public void Add(DataPath attributesPath, AttributeSet attributeSet)
        {
            foreach (var attribute in attributeSet.ReadAttributes)
            {
                Read.Add(new DataPath(attributesPath, new AttributeKey(attribute)));
            }
            foreach (var attribute in attributeSet.WriteAttributes)
            {
                Write.Add(new DataPath(attributesPath, new AttributeKey(attribute)));
            }
        }
    }

    /// <summary>
    /// Represents a binding for a templated task, associating a data type with its usage paths.
    /// </summary>
    /*public*/ class TemplatedTaskBinding
    {
        /// <summary>
        /// Gets the type of data associated with this binding.
        /// Must implement <see cref="IDataDescription"/>.
        /// </summary>
        public System.Type DataType { get; }

        /// <summary>
        /// Gets the <see cref="BindingUsagePaths"/> describing how the data is read and written.
        /// </summary>
        public BindingUsagePaths UsagePaths { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedTaskBinding"/> class
        /// with the specified data type and usage paths.
        /// </summary>
        /// <param name="dataType">
        /// The type of data to bind, which must implement <see cref="IDataDescription"/>.
        /// </param>
        /// <param name="usagePaths">
        /// The usage paths indicating how the data is read and written. If not null, its paths are added to <see cref="UsagePaths"/>.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <paramref name="dataType"/> does not implement <see cref="IDataDescription"/>.
        /// </exception>
        public TemplatedTaskBinding(System.Type dataType, BindingUsagePaths usagePaths)
        {
            Debug.Assert(typeof(IDataDescription).IsAssignableFrom(dataType));
            DataType = dataType;

            if (usagePaths != null)
            {
                UsagePaths.Add(usagePaths);
            }
        }
    }

    /// <summary>
    /// Arguments used to initialize a <see cref="TemplatedTask"/>.
    /// </summary>
    /*public*/ struct TemplatedTaskArgs
    {
        /// <summary>
        /// The collection of subtasks to compose the task.
        /// </summary>
        public IEnumerable<SubtaskDescription> Subtasks;

        /// <summary>
        /// The data key used for the default attribute set.
        /// </summary>
        public IDataKey DefaultAttributeKey;

        /// <summary>
        /// Mappings from generic attribute keys to their corresponding data paths.
        /// </summary>
        public Dictionary<IDataKey, BindingRelativePath> AttributeKeyMappings;

        /// <summary>
        /// List of all the task bindings and their subdata path.
        /// </summary>
        public Dictionary<IDataKey, TemplatedTaskBinding> Bindings;

        /// <summary>
        /// List of all the expressions used by the task.
        /// </summary>
        public List<(IDataKey bindingKey, IExpression expression)> Expressions;

    }

    /// <summary>
    /// Represents a task generated using a template and a list of subtask.
    /// </summary>
    /*public*/ class TemplatedTask : ITask
    {
        readonly Dictionary<IDataKey, BindingUsagePaths> m_BindingToUsage = new();
        readonly Dictionary<IDataKey, BindingRelativePath> m_AttributeKeyMappings;

        readonly Dictionary<IDataKey, IExpression> m_Expressions = new();

        /// <summary>
        /// Gets the name of the template associated with the task.
        /// </summary>
        public string TemplateName { get; }

        /// <summary>
        /// Gets the collection of subtasks that compose the task.
        /// </summary>
        public List<SubtaskDescription> Subtasks { get; } = new();

        /// <summary>
        /// Gets a value indicating whether the task is a compute task.
        /// </summary>
        public bool IsCompute { get; } = true;

        /// <summary>
        /// The data key used for the default attribute set.
        /// </summary>
        public IDataKey DefaultAttributeKey { get; }

        /// <summary>
        /// Gets the mappings from attribute keys to their corresponding binding relative paths.
        /// </summary>
        public IEnumerable<KeyValuePair<IDataKey, BindingRelativePath>> AttributeKeyMappings => m_AttributeKeyMappings;

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<IDataKey, IExpression>> Expressions => m_Expressions;

        //TODO: These two keys are legacy and should be moved somewhere else.
        /// <summary>
        /// Unique data key that represents the graph values data used by this task.
        /// </summary>
        public static UniqueDataKey GraphValuesKey { get; } = new UniqueDataKey("GraphValues");
        /// <summary>
        /// Unique data key that represents the (legacy) context data used by this task.
        /// </summary>
        public static UniqueDataKey ContextDataKey { get; } = new UniqueDataKey("ContextData");

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedTask"/> class using the specified template name and arguments.
        /// </summary>
        /// <param name="templateName">The name of the template associated with the task.</param>
        /// <param name="args">The parameters to setup the templated task <see cref="TemplatedTaskArgs"/>.</param>
        public TemplatedTask(string templateName, TemplatedTaskArgs args) : this(templateName, args, true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedTask"/> class using the specified template name, arguments, and compute flag.
        /// </summary>
        /// <param name="templateName">The name of the template associated with the task.</param>
        /// <param name="args">The parameters to setup the templated task <see cref="TemplatedTaskArgs"/>.</param>
        /// <param name="isCompute">Indicates if the task is a compute task.</param>
        public TemplatedTask(string templateName, TemplatedTaskArgs args, bool isCompute)
        {
            TemplateName = templateName;
            Subtasks.AddRange(args.Subtasks);

            IsCompute = isCompute;

            m_AttributeKeyMappings = args.AttributeKeyMappings != null ? new(args.AttributeKeyMappings) : new();

            if (args.Expressions != null)
            {
                AddExpressions(args.Expressions);
            }

            if (args.Bindings != null)
            {
                foreach (var binding in args.Bindings)
                {
                    if (!m_BindingToUsage.TryGetValue(binding.Key, out var bindingToUsage))
                    {
                        bindingToUsage = new();
                        m_BindingToUsage.Add(binding.Key, bindingToUsage);
                    }
                    bindingToUsage.Add(binding.Value.UsagePaths);
                }
            }

            DefaultAttributeKey = args.DefaultAttributeKey;

            foreach (var attributeKeyMapping in args.AttributeKeyMappings)
            {
                Debug.Assert(m_BindingToUsage.ContainsKey(attributeKeyMapping.Value.BindingKey));
            }

            foreach (var block in args.Subtasks)
            {
                if (block.Task is TemplateSubtask subtask)
                {
                    foreach (var kvp in subtask.AttributeSets)
                    {
                        var attributeKey = kvp.Key;
                        if (attributeKey == AttributeData.DefaultKey)
                        {
                            //attributeKey = default attribute mapping
                        }
                        if (m_AttributeKeyMappings.TryGetValue(attributeKey, out var mappedKeyPath))
                        {
                            IDataKey bindingKey = mappedKeyPath.BindingKey;
                            DataPath attributesPath = mappedKeyPath.SubDataPath;

                            var snippetAttributeSet = kvp.Value;
                            if (!m_BindingToUsage.ContainsKey(bindingKey))
                            {
                                m_BindingToUsage.Add(bindingKey, new());
                            }
                            //TODO MAYBE ?: Consider attributes that are written and THEN read as write only in the scope of the task.
                            foreach (var attribute in snippetAttributeSet.ReadAttributes)
                            {
                                m_BindingToUsage[bindingKey].Read
                                    .Add(new DataPath(attributesPath, new AttributeKey(attribute)));
                            }

                            foreach (var attribute in snippetAttributeSet.WriteAttributes)
                            {
                                m_BindingToUsage[bindingKey].Write
                                    .Add(new DataPath(attributesPath, new AttributeKey(attribute)));
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Attribute set \"{kvp.Key}\" not found in task {this}");
                        }
                    }
                }
                AddExpressions(block.Expressions);
            }

            foreach (var (bindingKey, _) in Expressions)
            {
                var bindingUsagePath = new BindingUsagePaths();
                bindingUsagePath.Read.Add(DataPath.Empty);
                m_BindingToUsage.TryAdd(bindingKey, bindingUsagePath);
            }
        }

        /// <inheritdoc />
        public bool GetDataUsage(IDataKey dataKey, out DataPathSet readUsage, out DataPathSet writeUsage)
        {
            if (m_BindingToUsage.ContainsKey(dataKey))
            {
                readUsage = m_BindingToUsage[dataKey].Read;
                writeUsage = m_BindingToUsage[dataKey].Write;
                return true;
            }

            readUsage = null;
            writeUsage = null;
            return false;
        }

        /// <inheritdoc />
        public bool GetBindingUsage(IDataKey dataKey, out BindingUsage usage)
        {
            if(GetDataUsage(dataKey, out var readUsage, out var writeUsage))
            {
                usage = BindingUsage.Unknown;
                if (readUsage != null && !readUsage.Empty)
                {
                    usage |= BindingUsage.Read;
                }
                if (writeUsage != null && !writeUsage.Empty)
                {
                    usage |= BindingUsage.Write;
                }
                return true;
            }
            usage = BindingUsage.Unknown;
            return true;
        }

        void AddExpressions(List<(IDataKey, IExpression)> expressions)
        {
            foreach (var (bindingKey, expression) in expressions)
            {
                m_Expressions.TryAdd(bindingKey, expression); // TODO: What to do on name collision
            }
        }
    }
}
