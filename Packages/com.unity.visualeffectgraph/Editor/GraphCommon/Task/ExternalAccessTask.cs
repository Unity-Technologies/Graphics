namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A task with exactly one binding, on a key meaningful outside the graph, read or written by
    /// something the graph itself does not model - the boundary between a graph value and whatever
    /// sets or observes it externally.
    /// </summary>
    /*public*/ abstract class ExternalAccessTask : ITask
    {
        public IDataKey Key { get; }
        public BindingUsage Usage { get; }

        protected ExternalAccessTask(IDataKey key, BindingUsage usage)
        {
            Key = key;
            Usage = usage;
        }

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            if (!dataKey.Equals(Key))
                return BindingUsage.Unknown;

            if (Usage == BindingUsage.Read)
                readUsage?.Add(DataPath.Root);
            else if (Usage == BindingUsage.Write)
                writeUsage?.Add(DataPath.Root);

            return Usage;
        }
    }
}
