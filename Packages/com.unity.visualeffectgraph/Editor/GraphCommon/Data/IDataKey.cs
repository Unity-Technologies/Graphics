namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a generic identifier for data in the Visual Effects Graph.
    /// </summary>
    /*public*/ interface IDataKey
    {
    }

    /// <summary>
    /// Represents a unique identifier for data.
    /// </summary>
    /*public*/ class UniqueDataKey : IDataKey
    {
        /// <summary>
        /// Returns the string representation of the unique data identifier.
        /// </summary>
        /// <returns>A string representing the unique ID ("UniqueID").</returns>
        public override string ToString()
        {
            return debugName;
        }

        private string debugName;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueDataKey"/> class with the specified debug name.
        /// </summary>
        /// <param name="debugName">An optional debug name to associate with this unique identifier.</param>
        public UniqueDataKey(string debugName = null)
        {
            this.debugName = debugName;
        }
    }

    /// <summary>
    /// Represents a data identifier with a textual name.
    /// </summary>
    /*public*/ record NameDataKey : IDataKey
    {
        /// <summary>
        /// Gets the name associated with this data identifier.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameDataKey"/> class with the specified name.
        /// </summary>
        /// <param name="name">The name to associate with this data identifier.</param>
        public NameDataKey(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns the string representation of this data identifier.
        /// </summary>
        /// <returns>The name associated with this data identifier.</returns>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Represents a data identifier with an index value.
    /// </summary>
    /*public*/ record IndexDataKey : IDataKey
    {
        /// <summary>
        /// Gets the index associated with this data identifier.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexDataKey"/> class with the specified index.
        /// </summary>
        /// <param name="index">The index to associate with this data identifier.</param>
        public IndexDataKey(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Returns the string representation of this data identifier.
        /// </summary>
        /// <returns>A string representation of the index.</returns>
        public override string ToString()
        {
            return Index.ToString();
        }
    }

    /// <summary>
    /// Represents a ranged data identifier, defined by a start and end value.
    /// </summary>
    /*public*/ record RangeDataKey : IDataKey
    {
        /// <summary>
        /// Gets the starting value of the range.
        /// </summary>
        public int From { get; }

        /// <summary>
        /// Gets the ending value of the range.
        /// </summary>
        public int To { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeDataKey"/> class with the specified range.
        /// </summary>
        /// <param name="from">The starting value of the range.</param>
        /// <param name="to">The ending value of the range.</param>
        public RangeDataKey(int from, int to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Returns the string representation of this data identifier.
        /// </summary>
        /// <returns>A string in the format "[From,To]".</returns>
        public override string ToString()
        {
            return $"[{From},{To}]";
        }
    }

    /// <summary>
    /// Represents a data identifier based on a type.
    /// </summary>
    /*public*/ record TypeDataKey : IDataKey
    {
        /// <summary>
        /// Gets the type associated with this data identifier.
        /// </summary>
        public System.Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDataKey"/> class with the specified type.
        /// </summary>
        /// <param name="type">The type to associate with this data identifier.</param>
        public TypeDataKey(System.Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Returns the string representation of this data identifier.
        /// </summary>
        /// <returns>A string representation of the type.</returns>
        public override string ToString()
        {
            return Type.ToString();
        }
    }

    /// <summary>
    /// Represents a data identifier based on an attribute.
    /// </summary>
    /*public*/ record AttributeKey : IDataKey
    {
        /// <summary>
        /// Gets the attribute associated with this data identifier.
        /// </summary>
        public Attribute Attribute { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeKey"/> class with the specified attribute.
        /// </summary>
        /// <param name="attribute">The attribute to associate with this data identifier.</param>
        public AttributeKey(Attribute attribute)
        {
            Attribute = attribute;
        }

        /// <summary>
        /// Returns the string representation of this data identifier.
        /// </summary>
        /// <returns>The name of the attribute.</returns>
        public override string ToString()
        {
            return Attribute.Name;
        }
    }
}
