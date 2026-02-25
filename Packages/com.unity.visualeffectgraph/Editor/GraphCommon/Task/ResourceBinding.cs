namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A struct tying a resource's name to an index.
    /// </summary>
    /*public*/ readonly struct ResourceBinding
    {
        /// <summary>
        /// The name used to reference the resource in a task.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The index where this resource can be retrieved.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Constructs a ResourceBinding from a name and an index.
        /// </summary>
        /// <param name="name">The name used to reference the resource in a task.</param>
        /// <param name="index">The index where this resource can be retrieved.</param>
        public ResourceBinding(string name, int index)
        {
            Name = name;
            Index = index;
        }

        /// <summary>
        /// Generates a string with the name and index of the ResourceBinding.
        /// </summary>
        /// <returns>A string with the name and index of the ResourceBinding.</returns>
        public override string ToString()
        {
            return $"Name : {Name} - Index : {Index}";
        }
    }
}
