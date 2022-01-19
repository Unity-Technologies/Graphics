using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

namespace UnityEditor.Rendering
{
    [Serializable]
    internal class StrippingReport<TShader, TShaderVariant, TVariantStrippingInfo> : VariantCounter,
        IStrippingReport<TShader, TShaderVariant>, ISerializationCallbackReceiver
        where TShader : UnityEngine.Object
        where TVariantStrippingInfo : IVariantStrippingInfo<TShader, TShaderVariant>, new()
    {
        readonly Dictionary<string, TVariantStrippingInfo> m_StrippingInfos = new();
        ShaderVariantLogLevel logStrippedVariants { get; }
        bool exportStrippedVariants { get; }

        public StrippingReport()
        {
            // Obtain logging and export information if the Global settings are configured as IShaderVariantSettings
            if (RenderPipelineManager.currentPipeline != null &&
                RenderPipelineManager.currentPipeline.defaultSettings is IShaderVariantSettings shaderVariantSettings)
            {
                logStrippedVariants = shaderVariantSettings.shaderVariantLogLevel;
                exportStrippedVariants = shaderVariantSettings.exportShaderVariants;
            }
        }

        public void OnShaderProcessed([NotNull] TShader shader, TShaderVariant shaderVariant, int variantsIn,
            int variantsOut, double stripTimeMs)
        {
            RecordVariants(variantsIn, variantsOut);

            if (!m_StrippingInfos.TryGetValue(shader.name, out var shaderVariantInfo))
            {
                shaderVariantInfo = new TVariantStrippingInfo();
                shaderVariantInfo.SetShader(shader);
                m_StrippingInfos.Add(shader.name, shaderVariantInfo);
            }

            shaderVariantInfo.Add(shaderVariant, variantsIn, variantsOut, stripTimeMs);
        }

        public void DumpReport(string path)
        {
            Log();
            Export(path);
        }

        #region Logging

        void Log()
        {
            if (logStrippedVariants == ShaderVariantLogLevel.Disabled)
                return;

            bool onlySrp = logStrippedVariants == ShaderVariantLogLevel.OnlySRPShaders;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("---------------------------STRIPPING---------------------------");
            sb.AppendLine($"{typeof(TShader)} - {strippedVariantsInfo})");

            foreach (var shaderStrippingInfo in m_StrippingInfos)
            {
                var shaderStrippingIterations = shaderStrippingInfo.Value;
                shaderStrippingInfo.Value.AppendLog(sb, onlySrp);
            }

            Debug.Log(sb.ToString());
        }

        #endregion

        #region Export

        private void Export(string path)
        {
            if (!exportStrippedVariants)
                return;

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(this, true));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        #region ISerializationCallbackReceiver

        [SerializeField] private TVariantStrippingInfo[] shaders;

        public void OnBeforeSerialize()
        {
            shaders = m_StrippingInfos.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
        }

        #endregion

        #endregion
    }
}
