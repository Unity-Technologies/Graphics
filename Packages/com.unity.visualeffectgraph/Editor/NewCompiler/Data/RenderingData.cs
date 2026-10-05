using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /*public*/ class RenderingData : IDataDescription
    {
        public static readonly UniqueDataKey IndirectBuffer = new UniqueDataKey("IndirectBuffer");
        public static readonly UniqueDataKey SortingData = new UniqueDataKey("SortingData");
        public uint Capacity { get; }
        public string Name { get; }
        public RenderingData(string name, uint capacity)
        {
            Name = name;
            Capacity = capacity;
        }

        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if(dataKey == SortingData)
            {
                return new SortingData(Capacity);
            }

            if (dataKey == IndirectBuffer)
            {
                return new DrawIndirectionData(Capacity);
            }
            return null;
        }
    }

    /// <summary>
    /// The sorting keys an output update registers, one per particle it draws, ahead of a counter.
    /// Read by <see cref="SortTask"/>, which sorts them into a <see cref="DrawIndirectionData"/>.
    /// </summary>
    /*public*/ class SortingData : IDataDescription
    {
        /// <summary>The space the sort merges these keys through, when it needs one.</summary>
        public static readonly UniqueDataKey ScratchBufferKey = new UniqueDataKey("ScratchBuffer");

        /// <summary>How many keys can be registered, on top of the counter the first element holds.</summary>
        public uint Capacity { get; }

        public SortingData(uint capacity)
        {
            Capacity = capacity;
        }

        public IDataDescription GetSubdata(IDataKey dataKey)
            => dataKey == ScratchBufferKey ? new SortScratchData(Capacity) : null;
    }

    /// <summary>
    /// The space the runtime merges the sorting keys through. Only needed above the capacity where it
    /// stops sorting in a single pass, so this is a view that a buffer does not always back - see
    /// <see cref="MinCapacity"/> and BufferConversionPass.
    /// </summary>
    /*public*/ class SortScratchData : IDataDescription
    {
        /// <summary>
        /// The capacity above which the sort runs merge passes, which need somewhere to merge into.
        /// Mirrors k_MinSortMergePassCapacityThreshold in <see cref="VFXDataParticle"/>.
        /// </summary>
        public const uint MinCapacity = 2048;

        /// <summary>The capacity of the keys being sorted, which this has to hold as many of.</summary>
        public uint Capacity { get; }

        public SortScratchData(uint capacity)
        {
            Capacity = capacity;
        }
    }

    /// <summary>
    /// The particle indices an output draws through, in the order they are to be drawn, ahead of a
    /// counter. Written by <see cref="SortTask"/> and read by the output.
    /// </summary>
    /*public*/ class DrawIndirectionData : IDataDescription
    {
        /// <summary>How many indices can be drawn, on top of the counter the first element holds.</summary>
        public uint Capacity { get; }

        public DrawIndirectionData(uint capacity)
        {
            Capacity = capacity;
        }
    }
}
