
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

        public bool GetDataUsage(IDataKey dataKey, out DataPathSet readUsage, out DataPathSet writeUsage)
        {
            if (dataKey is IndexDataKey indexDataKey)
            {
                if (indexDataKey.Index < Expression.Parents.Count)
                {
                    readUsage = new DataPathSet();
                    writeUsage = new DataPathSet();
                    readUsage.Add(DataPath.Empty);
                    return true;
                }
            }

            if (dataKey == Value)
            {
                readUsage = new DataPathSet();
                writeUsage = new DataPathSet();
                writeUsage.Add(DataPath.Empty);
                return true;
            }

            readUsage = null;
            writeUsage = null;
            return false;
        }
    }
}
