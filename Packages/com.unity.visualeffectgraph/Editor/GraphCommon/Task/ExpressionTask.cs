
namespace Unity.GraphCommon.LowLevel.Editor
{
    class ExpressionTask : ITask
    {
        public IExpression Expression { get; }

        public static UniqueDataKey Value { get; } = new();

        public ExpressionTask(IExpression expression)
        {
            Expression = expression;
        }

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            if (dataKey is IndexDataKey indexDataKey)
            {
                if (indexDataKey.Index < Expression.Parents.Count)
                {
                    readUsage?.Add(DataPath.Root);
                    return BindingUsage.Read;
                }
            }
            else if (dataKey == Value)
            {
                writeUsage?.Add(DataPath.Root);
                return BindingUsage.Write;
            }

            return BindingUsage.Unknown;
        }
    }
}
