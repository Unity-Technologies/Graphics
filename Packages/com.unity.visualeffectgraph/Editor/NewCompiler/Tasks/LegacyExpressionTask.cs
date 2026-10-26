using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class LegacyExpressionTask : ITask
    {
        public static UniqueDataKey Value { get; } = new($"{nameof(Value)}");

        public VFXExpression Expression { get; private set; }

        public LegacyExpressionTask(VFXExpression expression)
        {
            Expression = expression;
        }

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            if (dataKey is IndexDataKey indexDataKey)
            {
                if (indexDataKey.Index < Expression.parents.Length)
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
