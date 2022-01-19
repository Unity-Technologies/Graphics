using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [Serializable]
    internal class KernelVariantStrippingInfo : VariantCounter, IVariantStrippingInfo<ComputeShader, string>, ISerializationCallbackReceiver
    {
        [Serializable]
        class Kernel : VariantCounter
        {
            public string kernel;
            [NonSerialized]
            public double stripTimeMs;

            public Kernel(string shaderVariant, int variantsIn, int variantsOut, double stripTimeMs)
            {
                RecordVariants(variantsIn, variantsOut);
                kernel = shaderVariant;
                this.stripTimeMs = stripTimeMs;
            }
        }

        [SerializeField]
        private string shaderName;
        private ComputeShader m_Shader;
        public void SetShader(ComputeShader shader)
        {
            m_Shader = shader;
            shaderName = shader.name;
        }

        private List<Kernel> kernelsList { get; } = new List<Kernel>();

        public void Add(string shaderVariant, int variantsIn, int variantsOut, double stripTimeMs)
        {
            RecordVariants(variantsIn, variantsOut);
            kernelsList.Add(new Kernel(shaderVariant, variantsIn, variantsOut, stripTimeMs));
        }

        public void AppendLog(StringBuilder sb, bool _)
        {
            sb.AppendLine($"{shaderName} - {strippedVariantsInfo}");
            foreach (var kernel in kernelsList)
            {
                sb.AppendLine($"- Kernel {kernel.kernel} - {kernel.strippedVariantsInfo} - Time={kernel.stripTimeMs}Ms");
            }
        }

        #region ISerializationCallbackReceiver
        [SerializeField]
        private Kernel[] kernels;

        public void OnBeforeSerialize()
        {
            kernels = kernelsList.ToArray();
        }

        public void OnAfterDeserialize()
        {
        }
        #endregion
    }
}
