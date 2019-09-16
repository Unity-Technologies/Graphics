using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class ShaderGraphContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var result = base.GetSerializableMembers(objectType);
            var fieldInfos = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var fieldInfo in fieldInfos)
            {
                if (!result.Contains(fieldInfo) && fieldInfo.GetCustomAttribute<SerializeField>() != null)
                {
                    Debug.Log($"adding {fieldInfo.Name}");
                    result.Add(fieldInfo);
                }
            }

            return result;
        }
    }
}
