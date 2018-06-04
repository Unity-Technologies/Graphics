using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.VFX;
using UnityEngine.Experimental.VFX;
using UnityEngine.Profiling;
using System.Reflection;

using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [Serializable]
    struct VFXParameterInfo
    {
        public VFXParameterInfo(string exposedName, string realType)
        {
            name = exposedName;
            this.realType = realType;
            path = null;
            min = Mathf.NegativeInfinity;
            max = Mathf.Infinity;
            descendantCount = 0;
            sheetType = null;
        }

        public string name;
        public string path;

        public string sheetType;

        public string realType;

        public float min;
        public float max;

        public int descendantCount;


        public static VFXParameterInfo[] BuildParameterInfo(VFXGraph graph)
        {
            var categories = graph.UIInfos.categories;
            if (categories == null)
                categories = new List<string>();


            var parameters = graph.children.OfType<VFXParameter>().Where(t => t.exposed && (string.IsNullOrEmpty(t.category) || !categories.Contains(t.category))).OrderBy(t => t.order).ToArray();

            var infos = new List<VFXParameterInfo>();
            BuildCategoryParameterInfo(parameters, infos);

            foreach (var cat in categories)
            {
                parameters = graph.children.OfType<VFXParameter>().Where(t => t.exposed && t.category == cat).OrderBy(t => t.order).ToArray();
                if (parameters.Length > 0)
                {
                    VFXParameterInfo paramInfo = new VFXParameterInfo(cat, "");

                    paramInfo.descendantCount = 0;//parameters.Length;
                    infos.Add(paramInfo);
                    BuildCategoryParameterInfo(parameters, infos);
                }
            }


            return infos.ToArray();
        }

        static void BuildCategoryParameterInfo(VFXParameter[] parameters, List<VFXParameterInfo> infos)
        {
            var subList = new List<VFXParameterInfo>();
            foreach (var parameter in parameters)
            {
                string rootFieldName = VisualEffectUtility.GetTypeField(parameter.type);

                VFXParameterInfo paramInfo = new VFXParameterInfo(parameter.exposedName, parameter.type.Name);
                if (rootFieldName != null)
                {
                    paramInfo.sheetType = rootFieldName;
                    paramInfo.path = paramInfo.name;
                    if (parameter.hasRange)
                    {
                        float min = (float)System.Convert.ChangeType(parameter.m_Min.Get(), typeof(float));
                        float max = (float)System.Convert.ChangeType(parameter.m_Max.Get(), typeof(float));
                        paramInfo.min = min;
                        paramInfo.max = max;
                    }

                    paramInfo.descendantCount = 0;
                }
                else
                {
                    paramInfo.descendantCount = RecurseBuildParameterInfo(subList, parameter.type, parameter.exposedName);
                }


                infos.Add(paramInfo);
                infos.AddRange(subList);
                subList.Clear();
            }
        }

        static int RecurseBuildParameterInfo(List<VFXParameterInfo> infos, System.Type type, string path)
        {
            if (!type.IsValueType) return 0;

            int count = 0;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

            var subList = new List<VFXParameterInfo>();
            foreach (var field in fields)
            {
                var info = new VFXParameterInfo(field.Name, field.FieldType.Name);

                info.path = path + "_" + field.Name;

                var fieldName = VisualEffectUtility.GetTypeField(field.FieldType);

                if (fieldName != null)
                {
                    info.sheetType = fieldName;
                    RangeAttribute attr = field.GetCustomAttributes(true).OfType<RangeAttribute>().FirstOrDefault();
                    if (attr != null)
                    {
                        info.min = attr.min;
                        info.max = attr.max;
                    }
                    info.descendantCount = 0;
                }
                else
                {
                    if (field.FieldType == typeof(CoordinateSpace)) // For space
                        continue;
                    info.descendantCount = RecurseBuildParameterInfo(subList, field.FieldType, info.path);
                }
                infos.Add(info);
                infos.AddRange(subList);
                subList.Clear();
                count++;
            }
            return count;
        }
    }
}
