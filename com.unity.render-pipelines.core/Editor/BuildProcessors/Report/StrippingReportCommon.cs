using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Rendering
{
    interface IStrippedShader
    {
        string shaderName { get; }
        uint variantsIn { get; set; }
        uint variantsOut { get; set; }
        void AddVariant(IStrippedVariant variant);
        void Log(bool onlySRP);
    }

    interface IStrippedVariant
    {
        uint variantsIn { get; }
        uint variantsOut { get; }
        bool isSRPVariant { get; }
        void AppendLog(StringBuilder sb);
    }

    [Serializable]
    abstract class StrippedShaderBase
    {
        [SerializeField]
        protected string shader;
        public string shaderName => shader;

        [SerializeField] private uint inVariants;
        public uint variantsIn { get => inVariants; set => inVariants = value; }

        [SerializeField] private uint outVariants;
        public uint variantsOut { get => outVariants; set => outVariants = value; }

        protected List<IStrippedVariant> m_StrippedVariants = new List<IStrippedVariant>();

        public void AddVariant(IStrippedVariant variant)
        {
            variantsIn += variant.variantsIn;
            variantsOut += variant.variantsOut;
            m_StrippedVariants.Add(variant);
        }

        public void Log(bool onlySRP)
        {
            if (onlySRP && !m_StrippedVariants.Any(i => i.isSRPVariant))
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(
                $"STRIPPING {shaderName} - Total={variantsIn}/{variantsOut}({variantsOut / (float)variantsIn * 100f:0.00}%)");
            foreach (var strippedVariant in m_StrippedVariants)
            {

                strippedVariant.AppendLog(sb);
            }

            Debug.Log(sb);
        }
    }
}
