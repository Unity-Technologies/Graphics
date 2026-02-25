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

        public bool GetDataUsage(IDataKey dataKey, out DataPathSet readUsage, out DataPathSet writeUsage)
        {
            if (AttributeSets.TryGetValue(dataKey, out var attributeSet))
            {
                DataPath dataPath = new(dataKey);
                readUsage = new DataPathSet();
                foreach (var attribute in attributeSet.ReadAttributes)
                {
                    readUsage.Add(new DataPath(dataPath, new AttributeKey(attribute)));
                }
                writeUsage = new DataPathSet();
                foreach (var attribute in attributeSet.WriteAttributes)
                {
                    writeUsage.Add(new DataPath(dataPath, new AttributeKey(attribute)));
                }
                return true;
            }
            readUsage = null;
            writeUsage = null;
            return false;
        }
    }
}
