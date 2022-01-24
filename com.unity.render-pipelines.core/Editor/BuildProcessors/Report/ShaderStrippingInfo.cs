using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ShaderStrippingInfoOutput
    {
        public ShaderStrippingInfoOutput()
        {
            logs = new List<string>();
            exportAsJson = string.Empty;
        }

        public List<string> logs { get; }
        public string exportAsJson { get; set; }
    }

    interface IShaderStrippingOutput
    {
        ShaderStrippingInfoOutput GetOutput(ShaderVariantLogLevel shaderVariantLogLevel, bool exportStrippedVariants);
    }

    [Serializable]
    class ShaderStrippingInfo<TStrippedShader> : ISerializationCallbackReceiver, IVariantCounter, IShaderStrippingOutput
        where TStrippedShader : IStrippedShader
    {
        private readonly Dictionary<string, TStrippedShader> m_Dictionary = new();

        [SerializeField] private uint inVariants;
        public uint variantsIn
        {
            get => inVariants;
            set => inVariants = value;
        }

        [SerializeField] private uint outVariants;
        public uint variantsOut
        {
            get => outVariants;
            set => outVariants = value;
        }

        public bool TryGetStrippedShader(string shaderName, out IStrippedShader strippedShader)
        {
            bool found = m_Dictionary.TryGetValue(shaderName, out var result);
            strippedShader = result;
            return found;
        }

        public void Add(string shaderName, IStrippedShader strippedShader)
        {
            m_Dictionary.Add(shaderName, (TStrippedShader)strippedShader);
        }

        public ShaderStrippingInfoOutput GetOutput(ShaderVariantLogLevel shaderVariantLogLevel, bool exportStrippedVariants)
        {
            ShaderStrippingInfoOutput output = new ShaderStrippingInfoOutput();
            if (shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                foreach (var (_, value) in m_Dictionary)
                    output.logs.Add(value.Log(shaderVariantLogLevel));
            }

            if (exportStrippedVariants)
            {
                output.exportAsJson = JsonUtility.ToJson(this, true);
            }

            return output;
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
