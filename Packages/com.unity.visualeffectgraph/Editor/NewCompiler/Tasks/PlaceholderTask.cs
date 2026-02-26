using System.Collections.Generic;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    class PlaceholderSystemTask : ITask
    {
        Dictionary<IDataKey, BindingUsagePaths> m_BindingToUsage = new();

        private List<KeyValuePair<IDataKey, IExpression>> m_Expressions = new();

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<IDataKey, IExpression>> Expressions => m_Expressions;

        public PlaceholderSystemTask(IEnumerable<(IDataKey bindingKey, IExpression expression)> inputExpressions,
            IEnumerable<BindingRelativePath> bindingUsages)
        {
            foreach (var (bindingKey, expression) in inputExpressions)
            {
                m_Expressions.Add(new KeyValuePair<IDataKey, IExpression>(bindingKey, expression));
                if (!m_BindingToUsage.ContainsKey(bindingKey))
                {
                    m_BindingToUsage.Add(bindingKey, new());
                }

                m_BindingToUsage[bindingKey].Read.Add(DataPath.Empty);
            }

            foreach (var bindingUsage in bindingUsages)
            {
                if (!m_BindingToUsage.ContainsKey(bindingUsage.BindingKey))
                {
                    m_BindingToUsage.Add(bindingUsage.BindingKey, new());
                }

                m_BindingToUsage[bindingUsage.BindingKey].Write.Add(bindingUsage.SubDataPath);
            }
        }
        public bool GetDataUsage(IDataKey dataKey, out DataPathSet readUsage, out DataPathSet writeUsage)
        {
            if (m_BindingToUsage.ContainsKey(dataKey))
            {
                readUsage = m_BindingToUsage[dataKey].Read;
                writeUsage = m_BindingToUsage[dataKey].Write;
                return true;
            }

            readUsage = null;
            writeUsage = null;
            return false;
        }

        /// <inheritdoc />
        public bool GetBindingUsage(IDataKey dataKey, out BindingUsage usage)
        {
            if(GetDataUsage(dataKey, out var readUsage, out var writeUsage))
            {
                usage = BindingUsage.Unknown;
                if (readUsage != null && !readUsage.Empty)
                {
                    usage |= BindingUsage.Read;
                }
                if (writeUsage != null && !writeUsage.Empty)
                {
                    usage |= BindingUsage.Write;
                }
                return true;
            }
            usage = BindingUsage.Unknown;
            return true;
        }
    }
}
