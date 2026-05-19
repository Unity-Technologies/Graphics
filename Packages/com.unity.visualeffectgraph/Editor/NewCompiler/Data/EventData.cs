using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class EventData : IDataDescription
    {
        public static readonly UniqueDataKey AttributeDataKey = new UniqueDataKey("AttributeData");

        public string Name { get; }

        public EventData(string name)
        {
            Name = name;
        }

        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey.Equals(AttributeDataKey))
                return new AttributeData(1);

            return null;
        }
    }

    class EventListData : IDataDescription
    {
        public string Name { get; }

        public bool IsCpu { get; }
        public uint Capacity { get; }

        public uint BufferSize
        {
            get
            {
                // TODO: Until we find a better way to know the size of a buffer, this helps avoiding redundant buffer size calculations.
                if (IsCpu)
                {
                    return 1 + Capacity; // event count (1, prefix sum with instancing) + event spawn count prefix sum (1 per spawner)
                }
                else
                {
                    return 3 + Capacity; // event count + total event count + prefix sum + attribute count (stored in capacity). All this * instance count
                }
            }
        }

        public EventListData(string name, bool isCpu, uint capacity)
        {
            Name = name;
            IsCpu = isCpu;
            Capacity = capacity;
        }

        public IDataDescription GetSubdata(IDataKey dataKey)
        {
            if (dataKey.Equals(EventData.AttributeDataKey) && IsCpu)
                return new AttributeData(Capacity);

            return null;
        }
    }
}
