using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using System.Text;

namespace UnityEditor.ShaderGraph
{
    [HasDependencies(typeof(MinimalCustomFunctionNode))]
    [Title("Utility", "Custom Function")]
    class CustomFunctionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        // 0 original version
        // 1 differentiate between struct-based UnityTexture2D and bare Texture2D resources (for all texture and samplerstate resources)
        public override int latestVersion => 1;

        public override IEnumerable<int> allowedNodeVersions => new int[] { 1 };

        [Serializable]
        public class MinimalCustomFunctionNode : IHasDependencies
        {
            [SerializeField]
            HlslSourceType m_SourceType = HlslSourceType.File;

            [SerializeField]
            string m_FunctionName = k_DefaultFunctionName;

            [SerializeField]
            string m_FunctionSource = null;

            public void GetSourceAssetDependencies(AssetCollection assetCollection)
            {
                if (m_SourceType == HlslSourceType.File)
                {
                    m_FunctionSource = UpgradeFunctionSource(m_FunctionSource);
                    if (IsValidFunction(m_SourceType, m_FunctionName, m_FunctionSource, null))
                    {
                        if (GUID.TryParse(m_FunctionSource, out GUID guid))
                        {
                            // as this is just #included into the generated .shader file
                            // it doesn't actually need to be a dependency, other than for export package
                            assetCollection.AddAssetDependency(guid, AssetCollection.Flags.IncludeInExportPackage);
                        }
                    }
                }
            }
        }

        public static string[] s_ValidExtensions = { ".hlsl", ".cginc" };
        const string k_InvalidFileType = "Source file is not a valid file type. Valid file extensions are .hlsl and .cginc";
        const string k_MissingOutputSlot = "A Custom Function Node must have at least one output slot";

        public CustomFunctionNode()
        {
            UpdateNodeName();
        }

        void UpdateNodeName()
        {
            if ((functionName == defaultFunctionName) || (functionName == null))
                name = "Custom Function";
            else
                name = functionName + " (Custom Function)";
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
            set
            {
                m_FunctionName = value;
                UpdateNodeName();
            }
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

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            using (var inputSlots = PooledList<MaterialSlot>.Get())
            using (var outputSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots<MaterialSlot>(inputSlots);
                GetOutputSlots<MaterialSlot>(outputSlots);

                if (!IsValidFunction())
                {
                    // invalid functions generate special preview code..  (why?)
                    if (generationMode == GenerationMode.Preview && outputSlots.Count != 0)
                    {
                        outputSlots.OrderBy(s => s.id);
                        var hlslVariableType = outputSlots[0].concreteValueType.ToShaderString();
                        sb.AppendLine("{0} {1};",
                            hlslVariableType,
                            GetVariableNameForSlot(outputSlots[0].id));
                    }
                    return;
                }

                // declare output variables
                foreach (var output in outputSlots)
                {
                    sb.AppendLine("{0} {1};",
                        output.concreteValueType.ToShaderString(),
                        GetVariableNameForSlot(output.id));

                    if (output.bareResource)
                        AssignDefaultBareResource(output, sb);
                }

                // call function
                sb.AppendIndentation();
                sb.Append(functionName);
                sb.Append("_$precision(");
                bool first = true;

                foreach (var input in inputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;

                    sb.Append(SlotInputValue(input, generationMode));

                    // fixup input for Bare types
                    if (input.bareResource)
                    {
                        if (input is SamplerStateMaterialSlot)
                            sb.Append(".samplerstate");
                        else
                            sb.Append(".tex");
                    }
                }

                foreach (var output in outputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    sb.Append(GetVariableNameForSlot(output.id));

                    // fixup output for Bare types
                    if (output.bareResource)
                    {
                        if (output is SamplerStateMaterialSlot)
                            sb.Append(".samplerstate");
                        else
                            sb.Append(".tex");
                    }
                }
                sb.Append(");");
                sb.AppendNewLine();
            }
        }

        void AssignDefaultBareResource(MaterialSlot slot, ShaderStringBuilder sb)
        {
            switch (slot.concreteValueType)
            {
                case ConcreteSlotValueType.Texture2D:
                    {
                        var slotVariable = GetVariableNameForSlot(slot.id);
                        sb.AppendIndentation();
                        sb.Append(slotVariable);
                        sb.Append(".samplerstate = default_sampler_Linear_Repeat;");
                        sb.AppendNewLine();
                        sb.AppendIndentation();
                        sb.Append(slotVariable);
                        sb.Append(".texelSize = float4(1.0f/128.0f, 1.0f/128.0f, 128.0f, 128.0f);");
                        sb.AppendNewLine();
                        sb.AppendIndentation();
                        sb.Append(slotVariable);
                        sb.Append(".scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f);");
                        sb.AppendNewLine();
                    }
                    break;
                case ConcreteSlotValueType.Texture3D:
                case ConcreteSlotValueType.Texture2DArray:
                case ConcreteSlotValueType.Cubemap:
                    {
                        var slotVariable = GetVariableNameForSlot(slot.id);
                        sb.AppendIndentation();
                        sb.Append(slotVariable);
                        sb.Append(".samplerstate = default_sampler_Linear_Repeat;");
                        sb.AppendNewLine();
                    }
                    break;
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
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
                        GetFunctionHeader(builder);
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

        void GetFunctionHeader(ShaderStringBuilder sb)
        {
            using (var inputSlots = PooledList<MaterialSlot>.Get())
            using (var outputSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(inputSlots);
                GetOutputSlots(outputSlots);

                sb.Append("void ");
                sb.Append(functionName);
                sb.Append("_$precision(");

                var first = true;

                foreach (var argument in inputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    argument.AppendHLSLParameterDeclaration(sb, argument.shaderOutputName);
                }

                foreach (var argument in outputSlots)
                {
                    if (!first)
                        sb.Append(", ");
                    first = false;
                    sb.Append("out ");
                    argument.AppendHLSLParameterDeclaration(sb, argument.shaderOutputName);
                }

                sb.Append(")");
            }
        }

        string SlotInputValue(MaterialSlot port, GenerationMode generationMode)
        {
            IEdge[] edges = port.owner.owner.GetEdges(port.slotReference).ToArray();
            if (edges.Any())
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = fromSocketRef.node;
                if (fromNode == null)
                    return string.Empty;

                return fromNode.GetOutputForSlot(fromSocketRef, port.concreteValueType, generationMode);
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
            using (var slots = PooledList<MaterialSlot>.Get())
            {
                GetSlots(slots);
                foreach (var slot in slots)
                {
                    // check for bad slot names
                    var error = NodeUtils.ValidateSlotName(slot.RawDisplayName(), out string errorMessage);
                    if (error)
                    {
                        owner.AddValidationError(objectId, errorMessage);
                        break;
                    }
                }
            }
        }

        void ValidateBareTextureSlots()
        {
            using (var outputSlots = PooledList<MaterialSlot>.Get())
            {
                GetOutputSlots(outputSlots);
                foreach (var slot in outputSlots)
                {
                    if (slot.bareResource)
                    {
                        owner.AddValidationError(objectId, "This node uses Bare Texture or SamplerState outputs, which may produce unexpected results when fed to other nodes. Please convert the node to use the non-Bare struct-based outputs (see the structs defined in com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl)", ShaderCompilerMessageSeverity.Warning);
                        break;
                    }
                }
            }
        }

        public override void ValidateNode()
        {
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
                            owner.AddValidationError(objectId, k_InvalidFileType, ShaderCompilerMessageSeverity.Error);
                        }
                        else
                        {
                            owner.ClearErrorsForNode(this);
                        }
                    }
                }
            }
            if (!this.GetOutputSlots<MaterialSlot>().Any())
            {
                owner.AddValidationError(objectId, k_MissingOutputSlot, ShaderCompilerMessageSeverity.Warning);
            }
            ValidateSlotName();
            ValidateBareTextureSlots();

            base.ValidateNode();
        }

        public bool Reload(HashSet<string> changedFileDependencies)
        {
            if (changedFileDependencies.Contains(m_FunctionSource))
            {
                owner.ClearErrorsForNode(this);
                ValidateNode();
                Dirty(ModificationScope.Graph);
                return true;
            }
            return false;
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
            UpdateNodeName();
        }

        public override void OnAfterMultiDeserialize(string json)
        {
            if (sgVersion < 1)
            {
                // any Texture2D slots used prior to version 1 should be flagged as "bare" so we can
                // generate backwards compatible code
                var slots = new List<MaterialSlot>();
                GetSlots(slots);
                foreach (var slot in slots)
                {
                    slot.bareResource = true;
                }
                ChangeVersion(1);
            }
        }
    }
}
