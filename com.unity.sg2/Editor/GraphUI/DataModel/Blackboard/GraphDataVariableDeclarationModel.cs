using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class GraphDataVariableDeclarationModel : VariableDeclarationModel
    {
        [SerializeField]
        [HideInInspector]
        string m_ContextNodeName;

        /// <summary>
        /// Name of the context node that owns the entry for this Variable
        /// </summary>
        public string contextNodeName
        {
            get => m_ContextNodeName;
            set => m_ContextNodeName = value;
        }

        [SerializeField]
        [HideInInspector]
        string m_GraphDataName;

        /// <summary>
        /// Name of the port on the Context Node that owns the entry for this Variable
        /// </summary>
        public string graphDataName
        {
            get => m_GraphDataName;
            set => m_GraphDataName = value;
        }

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        internal PortHandler ContextEntry => shaderGraphModel.GraphHandler
            .GetNode(contextNodeName)
            .GetPort(graphDataName);

        /// <summary>
        /// Returns true if this variable declaration's data type is exposable according to the stencil,
        /// false otherwise.
        /// </summary>
        public bool IsExposable => ((ShaderGraphStencil)shaderGraphModel?.Stencil)?.IsExposable(DataType) ?? false;

        public override bool IsExposed
        {
            get
            {
                if (!IsExposable)
                {
                    return false;
                }

                return ContextEntry
                    .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                    .GetData() == ContextEntryEnumTags.PropertyBlockUsage.Included;
            }
            set
            {
                if (!IsExposable)
                {
                    value = false;
                }

                ContextEntry
                    .GetField<ContextEntryEnumTags.PropertyBlockUsage>(ContextEntryEnumTags.kPropertyBlockUsage)
                    .SetData(value ? ContextEntryEnumTags.PropertyBlockUsage.Included : ContextEntryEnumTags.PropertyBlockUsage.Excluded);
            }
        }

        public bool HasEditableInitialization => DataType != ShaderGraphExampleTypes.SamplerStateTypeHandle;

        public override void Rename(string newName)
        {
            base.Rename(newName); // Result is assigned to Title, can be different from newName (i.e. numbers at end)
            ContextEntry.GetField<string>(ContextEntryEnumTags.kDisplayName).SetData(Title);
        }

        public override void CreateInitializationValue()
        {
            if (string.IsNullOrEmpty(contextNodeName) || string.IsNullOrEmpty(graphDataName))
            {
                return;
            }

            if (GraphModel?.Stencil?.GetConstantType(DataType) != null)
            {
                InitializationModel = GraphModel.Stencil.CreateConstantValue(DataType);
                if (InitializationModel is BaseShaderGraphConstant cldsConstant)
                {
                    cldsConstant.Initialize(shaderGraphModel, contextNodeName, graphDataName);
                }

                if (DataType == ShaderGraphExampleTypes.Matrix2 ||
                    DataType == ShaderGraphExampleTypes.Matrix3 ||
                    DataType == ShaderGraphExampleTypes.Matrix4)
                {
                    InitializationModel.ObjectValue = Matrix4x4.identity;
                }

                if (DataType == ShaderGraphExampleTypes.Color)
                {
                    ContextEntry.AddField(ContextEntryEnumTags.kIsColor, true);
                }
            }
        }

        /// <summary>
        /// Gets the settings applicable to this variable declaration.
        /// </summary>
        internal IEnumerable<VariableSetting> GetSettings()
        {
            // TODO: Ultimately the type itself should determine what its available settings are,
            // eliminating the need for matching here.

            // TODO (Joe): Enable slider mode when Range(min, max) display type can be generated.
            // if (DataType == TypeHandle.Float)
            // {
            //     yield return VariableSettings.floatMode;
            //
            //     if (VariableSettings.floatMode.GetTyped(this) is ContextEntryEnumTags.FloatDisplayType.Slider)
            //     {
            //         yield return VariableSettings.rangeMin;
            //         yield return VariableSettings.rangeMax;
            //     }
            // }

            if (DataType == ShaderGraphExampleTypes.Color)
            {
                yield return VariableSettings.colorMode;
            }

            if (DataType == ShaderGraphExampleTypes.SamplerStateTypeHandle)
            {
                yield return VariableSettings.samplerStateFilter;
                yield return VariableSettings.samplerStateWrap;
            }

            if (DataType == ShaderGraphExampleTypes.Texture2DTypeHandle)
            {
                yield return VariableSettings.textureMode;
                yield return VariableSettings.textureUseTilingOffset;
            }

            if (IsExposable)
            {
                yield return VariableSettings.shaderDeclaration;
            }
        }
    }
}
