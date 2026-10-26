using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Describes a subtask within a templated task, including its name, associated expressions, and the task instance.
    /// </summary>
    /*public*/ struct SubtaskDescription
    {
        /// <summary>
        /// The name of the subtask.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The list of expression bindings associated with the subtask.
        /// </summary>
        public List<TemplatedTaskBinding> Bindings { get; set; }

        /// <summary>
        /// The actual task description.
        /// </summary>
        public ITask Task { get; set; }
    }
}
