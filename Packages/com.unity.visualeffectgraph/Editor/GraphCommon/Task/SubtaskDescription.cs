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
        /// The list of expressions associated with the subtask, each paired with a data binding key.
        /// </summary>
        public List<(IDataKey, IExpression)> Expressions { get; set; }

        /// <summary>
        /// The actual task description.
        /// </summary>
        public ITask Task { get; set; }
    }
}
