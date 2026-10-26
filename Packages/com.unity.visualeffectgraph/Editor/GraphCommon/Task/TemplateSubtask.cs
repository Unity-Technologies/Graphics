using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class TemplateSubtask : ITask
    {
        public string Name { get; }

        public string Code { get; }

        public Dictionary<IDataKey, AttributeSet> AttributeSets { get; }

        public TemplateSubtask(string name, string code, Dictionary<IDataKey, AttributeSet> attributeSets)
        {
            Name = name;
            Code = code;
            AttributeSets = attributeSets;
        }

        public BindingUsage GetBindingUsage(IDataKey dataKey, DataPathSet readUsage = null, DataPathSet writeUsage = null)
        {
            BindingUsage usage = BindingUsage.Unknown;

            if (AttributeSets?.TryGetValue(dataKey, out var attributeSet) ?? false)
            {
                DataPath dataPath = DataPath.Root + dataKey;

                if (attributeSet.ReadAttributes.Count > 0)
                {
                    usage |= BindingUsage.Read;
                    if (readUsage != null)
                    {
                        foreach (var attribute in attributeSet.ReadAttributes)
                            readUsage.Add(dataPath + new AttributeKey(attribute));
                    }
                }
                if (attributeSet.WriteAttributes.Count > 0)
                {
                    usage |= BindingUsage.Write;
                    if (writeUsage != null)
                    {
                        foreach (var attribute in attributeSet.WriteAttributes)
                            writeUsage.Add(dataPath + new AttributeKey(attribute));
                    }
                }
            }

            return usage;
        }
    }
}
