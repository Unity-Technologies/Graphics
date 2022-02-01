namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information on a parsed variable.
    /// </summary>
    public readonly struct Variable : IValue
    {
        public readonly string Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="id">The name of the variable.</param>
        public Variable(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Returns a string that represents the parsed variable.
        /// </summary>
        /// <returns>A string that represents the parsed variable.</returns>
        public override string ToString() => $"${Id}";
    }
}
