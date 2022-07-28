using System;
using System.Collections.Generic;
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

        public PortHandler ContextEntry => shaderGraphModel.GraphHandler
            .GetNode(contextNodeName)
            .GetPort(graphDataName);

        public ContextEntryEnumTags.DataSource ShaderDeclaration
        {
            get =>
                ContextEntry
                    .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                    .GetData();
            set =>
                ContextEntry
                    .GetField<ContextEntryEnumTags.DataSource>(ContextEntryEnumTags.kDataSource)
                    .SetData(value);
        }

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
            }
        }

        static void SetSubFieldSafe<T>(FieldHandler field, string key, T value)
        {
            var subField = field.GetSubField(key);
            if (subField != null)
            {
                subField.SetData(value);
            }
            else
            {
                field.AddSubField(key, value);
            }
        }

        static T GetSubFieldOrDefault<T>(FieldHandler field, string key, T defaultValue)
        {
            var subField = field.GetSubField(key);
            return subField != null ? subField.GetData<T>() : defaultValue;
        }

        // TODO: naming - also this seems like it exposes way too much detail
        internal void SetPropSubField<T>(string key, T value) =>
            SetSubFieldSafe(ContextEntry.GetPropertyDescription(), key, value);

        internal void SetTypeSubField<T>(string key, T value) =>
            SetSubFieldSafe(ContextEntry.GetTypeField(), key, value);

        internal T GetPropSubFieldOrDefault<T>(string key, T defaultValue = default) =>
            GetSubFieldOrDefault(ContextEntry.GetPropertyDescription(), key, defaultValue);

        internal T GetTypeSubFieldOrDefault<T>(string key, T defaultValue = default) =>
            GetSubFieldOrDefault(ContextEntry.GetTypeField(), key, defaultValue);
    }
}
