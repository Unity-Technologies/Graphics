using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [Serializable]
    class ShaderStrippingInfo<TStrippedShader> : ISerializationCallbackReceiver
        where TStrippedShader : IStrippedShader
    {
        private readonly Dictionary<string, TStrippedShader> m_Dictionary = new();

        private uint totalInVariants = 0;
        public uint totalVariantsIn { get => totalInVariants; set => totalInVariants = value; }

        private uint totalOutVariants = 0;
        public uint totalVariantsOut { get => totalOutVariants; set => totalOutVariants = value; }

        public bool TryGetStrippedShader(string shaderName, out IStrippedShader strippedShader)
        {
            bool found = m_Dictionary.TryGetValue(shaderName, out var result);
            strippedShader = result as IStrippedShader;
            return found;
        }

        public void Add(string shaderName, IStrippedShader strippedShader)
        {
            m_Dictionary.Add(shaderName, (TStrippedShader)strippedShader);
        }

        public void Log(string shaderType, bool onlySRP)
        {
            Debug.Log($"STRIPPING {shaderType} Total={totalVariantsIn}/{totalVariantsOut}({totalVariantsOut / (float)totalVariantsIn * 100f:0.00}%)");
            foreach (var (_, value) in m_Dictionary)
                value.Log(onlySRP);
        }

        public void Export(string fileName)
        {
            try
            {
                File.WriteAllText(fileName, JsonUtility.ToJson(this, true));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        [SerializeField]
        private TStrippedShader[] strippedShaders;

        public void OnBeforeSerialize()
        {
            strippedShaders = m_Dictionary.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
