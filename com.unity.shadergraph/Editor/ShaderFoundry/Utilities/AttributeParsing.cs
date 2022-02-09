using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class AttributeParsing
    {
        internal class ParamData
        {
            public string ParamName;
            public delegate void ParseDelegate(ShaderAttributeParam param, ParamData paramData);
            public ParseDelegate parseDelegate;
        }

        static internal void ReadPositional(ShaderAttributeParam attributeParam, int paramIndex, List<ParamData> paramDataList)
        {
            if (paramIndex >= paramDataList.Count)
                throw new Exception("Too many parameters");

            var paramData = paramDataList[paramIndex];
            paramData.parseDelegate(attributeParam, paramData);
        }

        static internal void ReadNamed(ShaderAttributeParam attributeParam, int paramIndex, List<ParamData> paramDataList)
        {
            if (string.IsNullOrEmpty(attributeParam.Name))
                throw new Exception("Cannot use positional argument after specifying named arguments");
            var paramData = paramDataList.Find((p) => (p.ParamName == attributeParam.Name));
            if (paramData == null)
                throw new Exception("Invalid parameter");

            paramData.parseDelegate(attributeParam, paramData);
        }

        static internal void Parse(ShaderAttribute attribute, List<ParamData> paramDataList)
        {
            var index = 0;
            bool isPositional = true;
            foreach(var param in attribute.Parameters)
            {
                if(isPositional)
                {
                    if (!string.IsNullOrEmpty(param.Name))
                    {
                        isPositional = false;
                        ReadNamed(param, index, paramDataList);
                    }
                    else
                        ReadPositional(param, index, paramDataList);
                }
                else
                {
                    ReadNamed(param, index, paramDataList);
                }
                ++index;
            }
        }
    }
}
