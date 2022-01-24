using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering
{
    [Serializable]
    sealed class StrippedComputeShader : StrippedShaderBase, IStrippedShader, ISerializationCallbackReceiver
    {
        public StrippedComputeShader(ComputeShader shader)
        {
            this.shader = shader.name;
            variantsIn = 0;
            variantsOut = 0;
        }

        [SerializeField] private KernelVariant[] kernels;

        public void OnBeforeSerialize()
        {
            kernels = m_StrippedVariants.Cast<KernelVariant>().ToArray();
        }

        public void OnAfterDeserialize()
        {
        }
    }

    [Serializable]
    class KernelVariant : IStrippedVariant
    {
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

        [SerializeField] private string kernel;
        public double stripTime { get; }

        public bool isSRPVariant => true; // TODO : Once Compute shaders implement a tag system for SRP

        public KernelVariant(string kernel, uint variantsIn, uint variantsOut, double stripTime)
        {
            this.kernel = kernel;
            inVariants = variantsIn;
            outVariants = variantsOut;
            this.stripTime = stripTime;
        }

        public void AppendLog(StringBuilder sb)
        {
            sb.AppendLine($" kernel ({kernel}) - Total={variantsIn}/{variantsOut}({variantsOut / (float)variantsIn * 100f:0.00}% - Time={stripTime}Ms");
        }
    }
}
