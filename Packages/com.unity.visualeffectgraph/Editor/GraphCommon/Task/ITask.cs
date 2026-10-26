
namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Placeholder interface for tasks.
    /// </summary>
    /*public*/ interface ITask
    {
        /// <summary>
        /// Gets the binding usage information for a specific data identifier within the task.
        /// </summary>
        /// <param name="dataKey">The data identifier whose binding usage is being queried.</param>
        /// <param name="readUsage">
        /// Optional set to populate with read data paths. If null, read paths are not collected.
        /// </param>
        /// <param name="writeUsage">
        /// Optional set to populate with write data paths. If null, write paths are not collected.
        /// </param>
        /// <returns>
        /// The binding usage type for the specified data key, indicating how the task interacts with the binding.
        /// Returns <see cref="BindingUsage.Unknown"/> if the data key is not supported.
        /// </returns>
        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null) => BindingUsage.Unknown;

        /// <summary>
        /// Gets the expected data type for the specified data identifier.
        /// </summary>
        /// <param name="dataKey">The data identifier to check.</param>
        /// <returns>
        /// The <see cref="System.Type"/> representing the expected data type for <paramref name="dataKey"/>,
        /// or null if unsupported.
        /// </returns>
        public System.Type GetBindingExpectedDataType(IDataKey dataKey) => null;

        /// <summary>
        /// Validates the data associated with a specific data identifier.
        /// </summary>
        /// <param name="dataKey">The data identifier to validate.</param>
        /// <param name="data">The data to validate.</param>
        /// <returns>
        /// True if the data identifier is supported by the task and the provided data is valid for that identifier, false otherwise
        /// </returns>
        public bool ValidateBindingData(IDataKey dataKey, IDataDescription data) => true;
    }
}
