namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Interface that represents any data used by the graph.
    /// </summary>
    /*public*/ interface IDataDescription
    {
        /// <summary>
        /// Retrieves a subdata element corresponding to the specified <see cref="IDataKey"/>.
        /// </summary>
        /// <param name="dataKey">The identifier of the subdata to retrieve.</param>
        /// <returns>
        /// The subdata object associated with the given <see cref="IDataKey"/>, or <see langword="null"/> if not found.
        /// </returns>
        IDataDescription GetSubdata(IDataKey dataKey) => dataKey == null ? this : null;

        /// <summary>
        /// Retrieves a subdata element by traversing the data hierarchy using the specified <see cref="DataPath"/>.
        /// </summary>
        /// <param name="dataPath">The path that specifies the sequence of <see cref="IDataKey"/> objects to navigate.</param>
        /// <returns>
        /// The resulting subdata object at the end of the specified <paramref name="dataPath"/>, or <see langword="null"/>
        /// if no matching subdata is found during traversal.
        /// </returns>
        IDataDescription GetSubdata(DataPath dataPath)
        {
            IDataDescription data = this;
            foreach (var dataKey in dataPath.PathSequence)
            {
                data = data.GetSubdata(dataKey);

                if (data == null)
                    return null;
            }
            return data;
        }

        /// <summary>
        /// Gets the type of the subdata corresponding to the specified <see cref="IDataKey"/>.
        /// </summary>
        /// <param name="dataKey">The identifier of the subdata whose type is requested.</param>
        /// <returns>
        /// A <see cref="System.Type"/> representing the type of the subdata, or <see langword="null"/> if the subdata does not exist.
        /// </returns>
        System.Type GetSubdataType(IDataKey dataKey) => GetSubdata(dataKey)?.GetType();

        /// <summary>
        /// Gets the type of the subdata at the end of the specified <see cref="DataPath"/>.
        /// </summary>
        /// <param name="dataPath">The data path used to traverse the hierarchy to retrieve the subdata type.</param>
        /// <returns>
        /// A <see cref="System.Type"/> representing the type of the subdata, or <see langword="null"/> if the subdata does not exist.
        /// </returns>
        System.Type GetSubdataType(DataPath dataPath) => GetSubdata(dataPath)?.GetType();

        /// <summary>
        /// Determines if other data description can be used in places where this data description is expected.
        /// </summary>
        /// <param name="other">The data description to be used instead of this.</param>
        /// <returns>
        /// True if the other data is compatible with this, false otherwise.
        /// </returns>
        bool IsCompatible(IDataDescription other) => Equals(other);

        /// <summary>
        /// Gets the name of this data object.
        /// </summary>
        string Name => "";
    }
}
