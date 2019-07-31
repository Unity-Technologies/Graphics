using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    [HasDependencies(typeof(MinimalCustomFunctionNode))]
    [Title("Utility", "Custom Function")]
    class CustomFunctionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IHasSettings
    {
        [Serializable]
        public class MinimalCustomFunctionNode : IHasDependencies
        {
            [SerializeField]
            HlslSourceType m_SourceType = HlslSourceType.File;

            [SerializeField]
            string m_FunctionName = k_DefaultFunctionName;

            [SerializeField]
            string m_FunctionSource = null;

            public void GetSourceAssetDependencies(List<string> paths)
            {
                if (m_SourceType == HlslSourceType.File)
                {
                    m_FunctionSource = UpgradeFunctionSource(m_FunctionSource);
                    if (IsValidFunction(m_SourceType, m_FunctionName, m_FunctionSource, null))
                    {
                        paths.Add(AssetDatabase.GUIDToAssetPath(m_FunctionSource));
                    }
                }
            }
        }
        
        public static string[] s_ValidExtensions = { ".hlsl", ".cginc" };
        const string k_InvalidFileType = "Source file is not a valid file type. Valid file extensions are .hlsl and .cginc";
        const string k_MissingOutputSlot = "A Custom Function Node must have at least one output slot";

        public CustomFunctionNode()
        {
            name = "Custom Function";
        }

        public override bool hasPreview => true;

        [SerializeField]
        HlslSourceType m_SourceType = HlslSourceType.File;

        public HlslSourceType sourceType
        {
            get => m_SourceType;
            set => m_SourceType = value;
        }

        [SerializeField]
        string m_FunctionName = k_DefaultFunctionName;

        const string k_DefaultFunctionName = "Enter function name here...";

        public string functionName
        {
            get => m_FunctionName;
            set => m_FunctionName = value;
        }

        public static string defaultFunctionName => k_DefaultFunctionName;

        [SerializeField]
        string m_FunctionSource;

        const string k_DefaultFunctionSource = "Enter function source file path here...";

        public string functionSource
        {
            get => m_FunctionSource;
            set => m_FunctionSource = value;
        }

        [SerializeField]
        string m_FunctionBody = k_DefaultFunctionBody;

        const string k_DefaultFunctionBody = "Enter function body here...";

        public string functionBody
        {
            get => m_FunctionBody;
            set => m_FunctionBody = value;
        }

        public static string defaultFunctionBody => k_DefaultFunctionBody;

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetOutputSlots<MaterialSlot>(slots);

            if(!IsValidFunction())
            {
                if(generationMode == GenerationMode.Preview && slots.Count != 0)
                {
                    slots.OrderBy(s => s.id);
                    sb.AppendLine("{0} {1};",
                        slots[0].concreteValueType.ToShaderString(),
                        GetVariableNameForSlot(slots[0].id));
                }
                return;
            }

            foreach (var argument in slots)
                sb.AppendLine("{0} {1};",
                    argument.concreteValueType.ToShaderString(),
                    GetVariableNameForSlot(argument.id));

            string call = $"{functionName}_$precision(";
            bool first = true;

            slots.Clear();
            GetInputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    call += ", ";
                first = false;
                call += SlotInputValue(argument, generationMode);
            }

            slots.Clear();
            GetOutputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    call += ", ";
                first = false;
                call += GetVariableNameForSlot(argument.id);
            }
            call += ");";
            sb.AppendLine(call);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            if(!IsValidFunction())
                return;

            switch (sourceType)
            {
                case HlslSourceType.File:
                    registry.ProvideFunction(functionSource, builder =>
                    {
                        string path = AssetDatabase.GUIDToAssetPath(functionSource);

                        // This is required for upgrading without console errors
                        if(string.IsNullOrEmpty(path))
                            path = functionSource;

                        string hash;
                        try
                        {
                            hash = AssetDatabase.GetAssetDependencyHash(path).ToString();
                        }
                        catch
                        {
                            hash = "Failed to compute hash for include";
                        }

                        builder.AppendLine($"// {hash}");
                        builder.AppendLine($"#include \"{path}\"");
                    });
                    break;
                case HlslSourceType.String:
                    registry.ProvideFunction(functionName, builder =>
                    {
                        builder.AppendLine(GetFunctionHeader());
                        using (builder.BlockScope())
                        {
                            builder.AppendLines(functionBody);
                        }
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        string GetFunctionHeader()
        {
            string header = $"void {functionName}_$precision(";
            var first = true;
            List<MaterialSlot> slots = new List<MaterialSlot>();

            GetInputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    header += ", ";
                first = false;
                header += $"{argument.concreteValueType.ToShaderString()} {argument.shaderOutputName}";
            }

            slots.Clear();
            GetOutputSlots<MaterialSlot>(slots);
            foreach (var argument in slots)
            {
                if (!first)
                    header += ", ";
                first = false;
                header += $"out {argument.concreteValueType.ToShaderString()} {argument.shaderOutputName}";
            }
            header += ")";
            return header;
        }

        string SlotInputValue(MaterialSlot port, GenerationMode generationMode)
        {
            IEdge[] edges = port.owner.owner.GetEdges(port.slotReference).ToArray();
            if (edges.Any())
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(fromSocketRef.nodeGuid);
                if (fromNode == null)
                    return string.Empty;

                var slot = fromNode.FindOutputSlot<MaterialSlot>(fromSocketRef.slotId);
                if (slot == null)
                    return string.Empty;

                return ShaderGenerator.AdaptNodeOutput(fromNode, slot.id, port.concreteValueType);
            }

            return port.GetDefaultValue(generationMode);
        }
        
        bool IsValidFunction()
        {
            return IsValidFunction(sourceType, functionName, functionSource, functionBody);
        }

        static bool IsValidFunction(HlslSourceType sourceType, string functionName, string functionSource, string functionBody)
        {
            bool validFunctionName = !string.IsNullOrEmpty(functionName) && functionName != k_DefaultFunctionName;

            if(sourceType == HlslSourceType.String)
            {
                bool validFunctionBody = !string.IsNullOrEmpty(functionBody) && functionBody != k_DefaultFunctionBody;
                return validFunctionName & validFunctionBody;
            }
            else
            {
                if(!validFunctionName || string.IsNullOrEmpty(functionSource) || functionSource == k_DefaultFunctionSource)
                    return false;

                string path = AssetDatabase.GUIDToAssetPath(functionSource);
                if(string.IsNullOrEmpty(path))
                    path = functionSource;

                string extension = Path.GetExtension(path);
                return s_ValidExtensions.Contains(extension);
            }
        }

        void ValidateSlotName()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetSlots(slots);

            foreach (var slot in slots)
            {
                var error = NodeUtils.ValidateSlotName(slot.RawDisplayName(), out string errorMessage);
                if (error)
                {
                    owner.AddValidationError(tempId, errorMessage);
                    break;
                }
            }
        }

        public override void ValidateNode()
        {
            if (!this.GetOutputSlots<MaterialSlot>().Any())
            {
                owner.AddValidationError(tempId, k_MissingOutputSlot, ShaderCompilerMessageSeverity.Warning);
            }
            if(sourceType == HlslSourceType.File)
            {
                if(!string.IsNullOrEmpty(functionSource))
                {
                    string path = AssetDatabase.GUIDToAssetPath(functionSource);
                    if(!string.IsNullOrEmpty(path))
                    {
                        string extension = path.Substring(path.LastIndexOf('.'));
                        if(!s_ValidExtensions.Contains(extension))
                        {
                            owner.AddValidationError(tempId, k_InvalidFileType, ShaderCompilerMessageSeverity.Error);
                        }
                    }
                }
            }
            ValidateSlotName();

            base.ValidateNode();
        }

        public void Reload(HashSet<string> changedFileDependencies)
        {
            if (changedFileDependencies.Contains(m_FunctionSource))
            {
                owner.ClearErrorsForNode(this);
                ValidateNode();
                Dirty(ModificationScope.Graph);
            }
        }

        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();
            ps.Add(new ReorderableSlotListView(this, SlotType.Input));
            ps.Add(new ReorderableSlotListView(this, SlotType.Output));
            ps.Add(new HlslFunctionView(this));
            return ps;
        }

        public static string UpgradeFunctionSource(string functionSource)
        {
            // Handle upgrade from legacy asset path version
            // If functionSource is not empty or a guid then assume it is legacy version
            // If asset can be loaded from path then get its guid
            // Otherwise it was the default string so set to empty
            Guid guid;
            if(!string.IsNullOrEmpty(functionSource) && !Guid.TryParse(functionSource, out guid))
            {
                string guidString = string.Empty;
                TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(functionSource);
                if(textAsset != null)
                {
                    long localId;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(textAsset, out guidString, out localId);
                }
                functionSource = guidString;
            }

            return functionSource;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            functionSource = UpgradeFunctionSource(functionSource);
        }
    }
}
