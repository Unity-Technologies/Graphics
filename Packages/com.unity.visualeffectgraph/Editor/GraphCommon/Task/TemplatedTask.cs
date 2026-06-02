using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Base class for templated task bindings, tracking read and write paths.
    /// </summary>
    /*public*/ abstract class TemplatedTaskBinding
    {
        /// <summary>
        /// The binding key associated with this binding, if any.
        /// </summary>
        public IDataKey Key { get; }

        /// <summary>
        /// The set of paths used for reading data bindings.
        /// </summary>
        public DataPathSet ReadPathSet { get; } = new();

        /// <summary>
        /// The set of paths used for writing data bindings.
        /// </summary>
        public DataPathSet WritePathSet { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedTaskBinding"/> class with the specified binding key.
        /// </summary>
        /// <param name="key">The data key associated with this binding.</param>
        protected TemplatedTaskBinding(IDataKey key)
        {
            Key = key;
        }

        /// <summary>
        /// Adds a path to both the read and write path sets.
        /// </summary>
        /// <param name="path">The path to add.</param>
        public void AddReadWrite(DataPath path)
        {
            ReadPathSet.Add(path);
            WritePathSet.Add(path);
        }
    }

    /// <summary>
    /// Represents a binding for a templated task with a specific data description type.
    /// </summary>
    /// <typeparam name="T">The data description type, must implement <see cref="IDataDescription"/>.</typeparam>
    /*public*/ class TemplatedTaskBinding<T> : TemplatedTaskBinding where T : IDataDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedTaskBinding{T}"/> class with the specified binding key.
        /// </summary>
        /// <param name="key">The data key associated with this binding.</param>
        public TemplatedTaskBinding(IDataKey key) : base(key)
        {
        }

        /// <summary>
        /// Creates a new <see cref="TemplatedTaskBinding"/> with the specified data description type and binding key.
        /// </summary>
        /// <param name="dataDescriptionType">The type of the data description, must implement <see cref="IDataDescription"/>.</param>
        /// <param name="key">The data key associated with this binding.</param>
        /// <returns>A new instance of <see cref="TemplatedTaskBinding"/> with the specified type and key.</returns>
        public static TemplatedTaskBinding Create(System.Type dataDescriptionType, IDataKey key)
        {
            var genericType = typeof(TemplatedTaskBinding<>).MakeGenericType(dataDescriptionType);
            return (TemplatedTaskBinding)System.Activator.CreateInstance(genericType, key);
        }
    }

    /// <summary>
    /// Represents a task generated using a template and a list of subtask.
    /// </summary>
    /*public*/ class TemplatedTask : ITask
    {
        /// <summary>
        /// Arguments used to initialize a <see cref="TemplatedTask"/>.
        /// </summary>
        public struct Args
        {
            /// <summary>
            /// The collection of subtasks to compose the task.
            /// </summary>
            public List<SubtaskDescription> Subtasks;

            /// <summary>
            /// List of all the task bindings.
            /// </summary>
            public List<TemplatedTaskBinding> Bindings;
        }

        readonly Dictionary<IDataKey, TemplatedTaskBinding> m_Bindings = new();

        /// <summary>
        /// Gets the name of the template associated with the task.
        /// </summary>
        public string TemplateName { get; }

        /// <summary>
        /// Gets the collection of subtasks that compose the task.
        /// </summary>
        public List<SubtaskDescription> Subtasks { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedTask"/> class using the specified template name and arguments.
        /// </summary>
        /// <param name="templateName">The name of the template associated with the task.</param>
        /// <param name="args">The parameters to setup the templated task <see cref="Args"/>.</param>
        public TemplatedTask(string templateName, Args args)
        {
            TemplateName = templateName;

            // Build bindings dictionary from the list using each binding's key
            if (args.Bindings != null)
            {
                foreach (var binding in args.Bindings)
                {
                    m_Bindings.Add(binding.Key, binding);
                }
            }

            Subtasks.AddRange(args.Subtasks);
            foreach (var subtaskDescription in args.Subtasks)
            {
                if (subtaskDescription.Bindings != null)
                {
                    foreach (var binding in subtaskDescription.Bindings)
                    {
                        binding.ReadPathSet.Add(DataPath.Root);
                        m_Bindings.TryAdd(binding.Key, binding);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a binding with the specified key exists.
        /// </summary>
        /// <param name="key">The data key to check.</param>
        /// <returns>True if a binding with the key exists, false otherwise.</returns>
        protected bool HasBinding(IDataKey key) => m_Bindings.ContainsKey(key);

        /// <summary>
        /// Adds a read path to the binding with the specified key.
        /// </summary>
        /// <param name="bindingKey">The binding key.</param>
        /// <param name="path">The path to add.</param>
        protected void AddReadPath(IDataKey bindingKey, DataPath path)
        {
            m_Bindings[bindingKey].ReadPathSet.Add(path);
        }

        /// <summary>
        /// Adds a write path to the binding with the specified key.
        /// </summary>
        /// <param name="bindingKey">The binding key.</param>
        /// <param name="path">The path to add.</param>
        protected void AddWritePath(IDataKey bindingKey, DataPath path)
        {
            m_Bindings[bindingKey].WritePathSet.Add(path);
        }

        /// <inheritdoc />
        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            BindingUsage usage = BindingUsage.Unknown;

            if (m_Bindings.TryGetValue(dataKey, out var binding))
            {
                if (!binding.ReadPathSet.Empty)
                {
                    usage |= BindingUsage.Read;
                    if (readUsage != null)
                    {
                        foreach (var path in binding.ReadPathSet)
                            readUsage.Add(path);
                    }
                }
                if (!binding.WritePathSet.Empty)
                {
                    usage |= BindingUsage.Write;
                    if (writeUsage != null)
                    {
                        foreach (var path in binding.WritePathSet)
                            writeUsage.Add(path);
                    }
                }
            }

            return usage;
        }
    }
}
