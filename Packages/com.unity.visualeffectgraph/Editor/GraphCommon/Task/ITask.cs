
using System;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Placeholder interface for tasks.
    /// </summary>
    /*public*/ interface ITask
    {
        /// <summary>
        /// Gets the expected data type for the specified data identifier.
        /// </summary>
        /// <param name="dataKey">The data identifier to check.</param>
        /// <returns>
        /// The <see cref="System.Type"/> representing the expected data type for <paramref name="dataKey"/>,
        /// or null if unsupported.
        /// </returns>
        public System.Type GetExpectedDataType(IDataKey dataKey) => null;

        /// <summary>
        /// Validates the data associated with a specific data identifier.
        /// </summary>
        /// <param name="dataKey">The data identifier to validate.</param>
        /// <param name="data">The data to validate.</param>
        /// <returns>
        /// True if the data identifier is supported by the task and the provided data is valid for that identifier, false otherwise
        /// </returns>
        public bool ValidateData(IDataKey dataKey, IDataDescription data) => false;

        /// <summary>
        /// Gets the binding usage information for a specific data identifier within the task.
        /// </summary>
        /// <param name="dataKey">The data identifier whose binding usage is being queried.</param>
        /// <param name="usage">
        /// When this method returns, contains the binding usage type for the specified data key,
        /// indicating how the task interacts with the binding.
        /// </param>
        /// <returns>
        /// True if the data identifier is supported by the task and binding usage is determined successfully, false otherwise.
        /// </returns>
        public bool GetBindingUsage(IDataKey dataKey, out BindingUsage usage)
        {
            usage = BindingUsage.Unknown;
            return true;
        }

        /// <summary>
        /// Gets the read and write usage for a specific data identifier within the task.
        /// </summary>
        /// <param name="dataKey">The data identifier whose usage is being queried.</param>
        /// <param name="readUsage">
        /// When this method returns, contains the set of data identifiers read by the task,
        /// or null if data is not read from.
        /// </param>
        /// <param name="writeUsage">
        /// When this method returns, contains the set of data identifiers written by the task,
        /// or null if data is not written to.
        /// </param>
        /// <returns>
        /// True if the data identifier is supported by the task
        /// </returns>
        public bool GetDataUsage(IDataKey dataKey, out DataPathSet readUsage, out DataPathSet writeUsage)
        {
            readUsage = null;
            writeUsage = null;
            return false;
        }
    }
}
