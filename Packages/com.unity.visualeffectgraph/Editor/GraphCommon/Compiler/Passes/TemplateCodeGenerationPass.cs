using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.VFX;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class TemplateCodeGenerationPass : CompilationPass
    {
        readonly IncludeFileShaderWriter m_IncludeFileShaderWriter = new();
        readonly ComputeShaderWriter m_ComputeShaderWriter = new();
        readonly RenderingShaderWriter m_RenderingShaderWriter = new();

        readonly DataDescriptionWriterRegistry m_DataWriter;

        HashSet<DataBindingId> m_DataBindingsWithView = new();

        public TemplateCodeGenerationPass(DataDescriptionWriterRegistry dataWriter)
        {
            m_DataWriter = dataWriter;
        }

        public bool Execute(ref CompilationContext context)
        {
            var generatedCodeContainer = context.data.GetOrCreate<GeneratedCodeContainer>();

            foreach (var dataContainer in context.graph.DataContainers)
            {
                if (IsCompositeData(dataContainer.RootDataView.DataDescription))
                {
                    string sourceCode = GenerateContainerSourceCode(dataContainer, context);
                    generatedCodeContainer.Add(dataContainer.Id, sourceCode);
                }
            }

            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is TemplatedTask templatedTask)
                {
                    //TODO: The ImportContext, when present, must be accounted for during compilation (see usage of CreateShaderAsset & CreateComputeShaderAsset)
                    string sourceCode;
                    if (templatedTask.IsCompute)
                    {
                        sourceCode = GenerateTemplatedTaskSourceCode_Compute(taskNode, context);

                        var computeShader = ShaderUtil.CreateComputeShaderAsset(sourceCode);
                        computeShader.name = taskNode.Name ?? templatedTask.TemplateName;
                        var task = new GpuKernelTask(computeShader, 0);
                        context.graph.SetTask(taskNode.Id, task);
                    }
                    else
                    {
                        sourceCode = GenerateTemplatedTaskSourceCode_Rendering(taskNode, context);
                        Shader shader = ShaderUtil.CreateShaderAsset(sourceCode);
                        shader.name = taskNode.Name ?? templatedTask.TemplateName;
                        Material material = new Material(shader);
                        var task = new RenderingTask(material);
                        context.graph.SetTask(taskNode.Id, task);
                    }
                    generatedCodeContainer.Add(taskNode.Id, sourceCode);
                }
            }
            return true;
        }

        string GenerateContainerSourceCode(DataContainer dataContainer, CompilationContext context)
        {
            var containerName = dataContainer.IdentifierName;
            var rootDataView = dataContainer.RootDataView;
            m_IncludeFileShaderWriter.Begin(containerName);
            if (m_DataWriter.TryGetDataDescriptionWriter(rootDataView.DataDescription, out var dataDescriptionWriter))
            {
                foreach (var resource in dataDescriptionWriter.GetUsedResources(containerName, rootDataView))
                {
                    if (resource.Item2 != null)
                    {
                        WriteVariableDeclaration(m_IncludeFileShaderWriter, resource.Item1, resource.Item2);
                        m_IncludeFileShaderWriter.NewLine();
                    }
                }

                dataDescriptionWriter.WriteDescription(m_IncludeFileShaderWriter, rootDataView, containerName, context);
            }
            return m_IncludeFileShaderWriter.End();
        }

        void WriteVariableDeclaration(ShaderWriter shaderWriter, string type, string name, bool isStatic = false) // TODO: maybe add an enum of modifiers and move to shader writer
        {
            shaderWriter.WriteLine($"{(isStatic ? "static " : "")}{type} {name};");
        }

        string GenerateTemplatedTaskSourceCode_Compute(TaskNode taskNode, CompilationContext context)
        {
            TemplatedTask templatedTask = taskNode.Task as TemplatedTask;
            //TODO: Generate proper name
            string shaderName = taskNode.Name ?? templatedTask.TemplateName;

            m_ComputeShaderWriter.Begin(shaderName);

            m_ComputeShaderWriter.WriteLine("#include \"Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/ThreadData.hlsl\"");

            IncludeCommonLibrary(m_ComputeShaderWriter);

            IncludeData(m_ComputeShaderWriter, taskNode, context);

            GenerateAttributeSets(m_ComputeShaderWriter, taskNode, templatedTask, context);

            GenerateDataViews(m_ComputeShaderWriter, taskNode, templatedTask, context);

            ForwardDeclarations(m_ComputeShaderWriter, templatedTask);

            IncludeTemplateFile(m_ComputeShaderWriter, templatedTask.TemplateName);

            GenerateEntryPoint_Compute(m_ComputeShaderWriter, taskNode, context);

            GenerateSnippetFunctions(m_ComputeShaderWriter, templatedTask);

            return m_ComputeShaderWriter.End();
        }

        string GenerateTemplatedTaskSourceCode_Rendering(TaskNode taskNode, CompilationContext context)
        {
            TemplatedTask templatedTask = taskNode.Task as TemplatedTask;
            //TODO: Generate proper name
            string shaderName = taskNode.Name ?? templatedTask.TemplateName;

            m_RenderingShaderWriter.Begin(shaderName);

            using (m_RenderingShaderWriter.CreateSubShaderScope())
            {
                using (m_RenderingShaderWriter.CreateHlslIncludeScope())
                {
                    IncludeCommonLibrary(m_RenderingShaderWriter);

                    IncludeData(m_RenderingShaderWriter, taskNode, context);

                    GenerateAttributeSets(m_RenderingShaderWriter, taskNode, templatedTask, context);

                    GenerateDataViews(m_RenderingShaderWriter, taskNode, templatedTask, context);

                    ForwardDeclarations(m_RenderingShaderWriter, templatedTask);

                    IncludeTemplateFile(m_RenderingShaderWriter, templatedTask.TemplateName);

                    GenerateSnippetFunctions(m_RenderingShaderWriter, templatedTask);
                }

                using (m_RenderingShaderWriter.CreatePassScope(""))
                {
                    GenerateEntryPoints_Rendering(m_RenderingShaderWriter, taskNode, context);
                }
            }
            return m_RenderingShaderWriter.End();
        }

        void IncludeData(ShaderWriter shaderWriter, TaskNode taskNode, CompilationContext context)
        {
            var generatedCodeContainer = context.data.Get<GeneratedCodeContainer>();

            foreach (var dataNode in taskNode.DataNodes)
            {
                var rootDataView = dataNode.DataContainer.RootDataView;
                var dataDescription = rootDataView.DataDescription;
                if (!IsCompositeData(dataDescription))
                {
                    shaderWriter.NewLine();
                    var dataTypename = m_DataWriter.FindDataTypeName(rootDataView);
                    WriteVariableDeclaration(shaderWriter, dataTypename, dataNode.DataContainer.Name);
                }
                else if (m_DataWriter.TryGetDataDescriptionWriter(dataDescription, out var dataDescriptionWriter))
                {
                    shaderWriter.NewLine();
                    //shaderWriter.IncludeFile($"{dataNode.DataContainer.Name}.hlsl");
                    shaderWriter.WriteLine($"// Begin {dataNode.DataContainer.Name}");
                    dataDescriptionWriter.DefineResourceUsage(shaderWriter, dataNode.UsedDataViewsRoot, dataNode.ReadDataViewsRoot, dataNode.WrittenDataViewsRoot);
                    shaderWriter.WriteLine(generatedCodeContainer.Find(dataNode.DataContainer.Id), ShaderWriter.WriteLineOptions.Multiline);
                    dataDescriptionWriter.UndefineResourceUsage(shaderWriter, dataNode.UsedDataViewsRoot, dataNode.ReadDataViewsRoot, dataNode.WrittenDataViewsRoot);
                    shaderWriter.WriteLine($"// End   {dataNode.DataContainer.Name}");
                }
                else
                {
                    Debug.Assert(false, "Data node not handled");
                }
            }
        }

        void GenerateAttributeSets(ShaderWriter shaderWriter, TaskNode taskNode, TemplatedTask templatedTask,
            CompilationContext context)
        {
            var attributeSourceManager = context.data.GetOrCreate<AttributeSourceManager>();
            foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
            {
                var structType = attributeKeyMapping.Key.ToString();
                var bindingRelativePath = attributeKeyMapping.Value;
                DataView dataView = new();
                foreach (var binding in taskNode.DataBindings) // TODO: refactor linear search method?
                {
                    if (binding.BindingDataKey == bindingRelativePath.BindingKey)
                    {
                        bool found = binding.DataView.FindSubData(bindingRelativePath.SubDataPath, out dataView);
                        Debug.Assert(found);
                        break;
                    }
                }

                attributeSourceManager.Add(dataView.Id, attributeKeyMapping.Key);

                shaderWriter.NewLine();
                shaderWriter.WriteLine($"struct {structType}");
                shaderWriter.OpenBlock();
                foreach (var attributeDataView in dataView.Children)
                {
                    var attribute = (attributeDataView.SubDataKey as AttributeKey).Attribute;
                    shaderWriter.WriteLine($"{HlslCodeHelper.GetTypeName(attribute.Type)} {attribute.Name};");
                }
                shaderWriter.NewLine();
                shaderWriter.WriteLine("void Init()");
                shaderWriter.OpenBlock();
                foreach (var attributeDataView in dataView.Children)
                {
                    var attribute = (attributeDataView.SubDataKey as AttributeKey).Attribute;
                    shaderWriter.WriteLine($"{attribute.Name} = {HlslCodeHelper.GetValueString(attribute.DefaultValue)};");
                }
                shaderWriter.CloseBlock();
                shaderWriter.CloseBlock(false);
                shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            }
        }

        void GenerateDataViews(ShaderWriter shaderWriter, TaskNode taskNode, TemplatedTask templatedTask,
            CompilationContext context)
        {
            void LocateDataView(DataView dataView, ref DataView usedDataView, ref DataView readDataView, ref DataView writtenDataView)
            {
                if (dataView.Parent.HasValue)
                {
                    LocateDataView(dataView.Parent.Value, ref usedDataView, ref readDataView, ref writtenDataView);
                    var subDataKey = dataView.SubDataKey;
                    usedDataView.FindSubData(subDataKey, out usedDataView);
                    readDataView.FindSubData(subDataKey, out readDataView);
                    writtenDataView.FindSubData(subDataKey, out writtenDataView);
                }
            }

            m_DataBindingsWithView.Clear();

            var dataBindings = taskNode.DataBindings;
            foreach(var dataBinding in dataBindings)
            {
                var dataView = dataBinding.DataView;
                var dataNode = dataBinding.DataNode;

                var usedDataView = dataNode.UsedDataViewsRoot;
                var readDataView = dataNode.ReadDataViewsRoot;
                var writtenDataView = dataNode.WrittenDataViewsRoot;
                LocateDataView(dataView, ref usedDataView, ref readDataView, ref writtenDataView);

                var dataDescription = dataView.DataDescription;

                Debug.Assert(!usedDataView.Id.IsValid || usedDataView.DataDescription == dataDescription);
                Debug.Assert(!readDataView.Id.IsValid || readDataView.DataDescription == dataDescription);
                Debug.Assert(!writtenDataView.Id.IsValid || writtenDataView.DataDescription == dataDescription);

                shaderWriter.NewLine();

                bool hasView = false;
                string typeName = m_DataWriter.FindDataTypeName(dataView);
                string bindingName = dataBinding.BindingDataKey.ToString();
                if (IsCompositeData(dataDescription))
                {
                    if (m_DataWriter.TryGetDataDescriptionWriter(dataDescription, out var dataDescriptionWriter))
                    {
                        hasView = dataDescriptionWriter.WriteView(shaderWriter, usedDataView, readDataView, writtenDataView, bindingName, typeName, context);
                    }
                }

                if (hasView)
                {
                    m_DataBindingsWithView.Add(dataBinding.Id);
                }

                if (BindingNeedsRemapping(dataBinding))
                {
                    bool isStatic = !IsCompositeData(dataDescription);
                    WriteVariableDeclaration(shaderWriter, hasView ? $"{bindingName}View": typeName, bindingName, isStatic);
                }

            }
            var attributeSourceManager = context.data.GetOrCreate<AttributeSourceManager>();
            attributeSourceManager.Clear();
        }

        bool BindingNeedsRemapping(DataBinding dataBinding)
        {
            var dataView = dataBinding.DataView;
            var containerName = dataView.DataContainer.Name;
            var bindingName = dataBinding.BindingDataKey.ToString();
            return !dataView.IsRoot || containerName != bindingName;
        }

        void WriteProcessBlocksDeclaration(ShaderWriter shaderWriter, TemplatedTask templatedTask)
        {
            shaderWriter.Indent();
            shaderWriter.Write("void VFXProcessBlocks(");
            bool first = true;
            foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
            {
                var structType = attributeKeyMapping.Key.ToString();
                var structName = attributeKeyMapping.Key == templatedTask.DefaultAttributeKey ? "attributes" : $"{char.ToLowerInvariant(structType[0])}{structType.Substring(1)}";
                if (!first)
                {
                    shaderWriter.Write(", ");
                }
                shaderWriter.Write("inout ");
                shaderWriter.Write(structType);
                shaderWriter.Write(" ");
                shaderWriter.Write(structName);
                first = false;
            }
            shaderWriter.Write(")");
        }

        void IncludeCommonLibrary(ShaderWriter shaderWriter)
        {
            shaderWriter.NewLine();
            shaderWriter.Define("VFX_LOCAL_SPACE", "1");
            shaderWriter.IncludeFile($"{VFXLibrary.currentSRPBinder.runtimePath}/VFXCommon.hlsl");
            shaderWriter.IncludeFile("Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl");
        }

        void ForwardDeclarations(ShaderWriter shaderWriter, TemplatedTask templatedTask)
        {
            shaderWriter.NewLine();
            WriteProcessBlocksDeclaration(shaderWriter, templatedTask);
            shaderWriter.Write(";");
            shaderWriter.NewLine();
        }

        void IncludeTemplateFile(ShaderWriter shaderWriter, string templateName)
        {
            shaderWriter.NewLine();
            shaderWriter.IncludeFile($"Packages/com.unity.visualeffectgraph/Shaders/Temp/Templates/{templateName}.hlsl"); // TODO: Template should have full path
        }

        void GenerateEntryPoint_Compute(ComputeShaderWriter computeShaderWriter, TaskNode taskNode,
            CompilationContext context)
        {
            computeShaderWriter.NewLine();
            computeShaderWriter.WriteLine("static ThreadData threadData;");
            computeShaderWriter.NewLine();
            uint threadCount = 64u;
            computeShaderWriter.WriteMainFunction(threadCount, 1, 1);
            computeShaderWriter.OpenBlock();
            InitMainData(computeShaderWriter, taskNode);
            computeShaderWriter.NewLine();
            computeShaderWriter.WriteLine("// Template entry point");
            computeShaderWriter.WriteLine($"threadData.Init(groupId.x * {threadCount} + groupThreadId.x);");
            computeShaderWriter.WriteLine("main(threadData);");
            computeShaderWriter.CloseBlock();
        }

        void GenerateEntryPoints_Rendering(RenderingShaderWriter renderingShaderWriter, TaskNode taskNode, CompilationContext context)
        {
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteVertexFunction();
            renderingShaderWriter.OpenBlock();
            InitMainData(renderingShaderWriter, taskNode);
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteLine("return vert(id, input);");
            renderingShaderWriter.CloseBlock();

            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteFragmentFunction();
            renderingShaderWriter.OpenBlock();
            InitMainData(renderingShaderWriter, taskNode);
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteLine("return frag(input);");
            renderingShaderWriter.CloseBlock();
        }

        void InitMainData(ShaderWriter shaderWriter, TaskNode taskNode)
        {
            InitContainers(shaderWriter, taskNode);
            InitBindings(shaderWriter, taskNode);
        }

        void InitContainers(ShaderWriter shaderWriter, TaskNode taskNode)
        {
            bool initContainers = false;
            foreach (var dataNode in taskNode.DataNodes)
            {
                var dataContainer = dataNode.DataContainer;
                if (IsCompositeData(dataContainer.RootDataView.DataDescription))
                {
                    shaderWriter.NewLine();
                    if (!initContainers)
                    {
                        shaderWriter.WriteLine("// Init containers");
                        initContainers = true;
                    }

                    var typeName = dataContainer.IdentifierName;
                    var variableName = char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
                    shaderWriter.WriteLine($"{typeName} {variableName};");
                    shaderWriter.WriteLine($"{variableName}.Init();");
                }
            }
        }

        void InitBindings(ShaderWriter shaderWriter, TaskNode taskNode)
        {
            bool initBindings = false;
            foreach (var dataBinding in taskNode.DataBindings)
            {
                if (!initBindings)
                {
                    shaderWriter.WriteLine("// Init bindings");
                    initBindings = true;
                }

                var dataDescription = dataBinding.DataView.DataDescription;
                bool hasView = m_DataBindingsWithView.Contains(dataBinding.Id);
                if (hasView)
                {
                    WriteBindingInit(shaderWriter, dataBinding);
                    shaderWriter.NewLine();
                }
                else if (BindingNeedsRemapping(dataBinding))
                {
                    WriteBindingRemap(shaderWriter, dataBinding);
                    shaderWriter.NewLine();
                }
            }
        }

        void WriteBindingInit(ShaderWriter shaderWriter, DataBinding dataBinding)
        {
            shaderWriter.Indent();
            shaderWriter.Write(dataBinding.BindingDataKey.ToString());
            shaderWriter.Write(".Init(");
            WriteSubdataPath(shaderWriter, dataBinding.DataNode.DataContainer, dataBinding.DataView);
            shaderWriter.WriteLine(");", ShaderWriter.WriteLineOptions.NoIndent);
        }

        void WriteBindingRemap(ShaderWriter shaderWriter, DataBinding dataBinding)
        {
            shaderWriter.Indent();
            shaderWriter.Write(dataBinding.BindingDataKey.ToString());
            shaderWriter.Write(" = ");
            WriteSubdataPath(shaderWriter, dataBinding.DataNode.DataContainer, dataBinding.DataView);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        void WriteSubdataPath(ShaderWriter shaderWriter, DataContainer container, DataView toDataView)
        {
            if (toDataView.DataDescription is ConstantValueData constantValueData)
            {
                shaderWriter.Write(HlslCodeHelper.GetValueString(constantValueData.ObjectValue));
            }
            else
            {
                var rootDataView = container.RootDataView;
                var containerName = container.IdentifierName;
                var variableName = containerName;
                if (IsCompositeData(rootDataView.DataDescription))
                {
                    variableName = char.ToLowerInvariant(variableName[0]) + variableName.Substring(1);
                }
                shaderWriter.Write(variableName);
                WriteSubdataPath(shaderWriter, rootDataView, toDataView);
            }
        }

        void WriteSubdataPath(ShaderWriter shaderWriter, DataView fromDataView, DataView toDataView)
        {
            if (fromDataView.Id.Index != toDataView.Id.Index)
            {
                var parentDataView = toDataView.Parent.GetValueOrDefault();
                Debug.Assert(parentDataView.Id.IsValid);
                if (parentDataView.Id.Index != fromDataView.Id.Index)
                {
                    WriteSubdataPath(shaderWriter, fromDataView, parentDataView);
                }
                shaderWriter.Write(m_DataWriter.GetSubdataName(parentDataView, toDataView.SubDataKey));
            }
        }

        void GenerateSnippetFunctions(ShaderWriter shaderWriter, TemplatedTask templatedTask)
        {
            shaderWriter.NewLine();
            WriteProcessBlocksDeclaration(shaderWriter, templatedTask);
            shaderWriter.NewLine();
            shaderWriter.OpenBlock();
            foreach (var block in templatedTask.Subtasks)
            {
                if (block.Task is TemplateSubtask templateSubtask)
                {
                    shaderWriter.WriteTemplateSubtask(templateSubtask);
                }
            }
            shaderWriter.CloseBlock();
        }

        bool IsCompositeData(IDataDescription dataDescription) => dataDescription is not ValueData; // TODO: Temp
    }

    class GeneratedCodeContainer
    {
        readonly Dictionary<DataContainerId, string> m_DataContainerSourceCodes = new();
        readonly Dictionary<TaskNodeId, string> m_TaskNodeSourceCodes = new();

        public void Add(DataContainerId dataContainerId, string sourceCode) => m_DataContainerSourceCodes.Add(dataContainerId, sourceCode);
        public void Add(TaskNodeId taskNodeId, string sourceCode) => m_TaskNodeSourceCodes.Add(taskNodeId, sourceCode);

        public string Find(DataContainerId dataContainerId) => m_DataContainerSourceCodes.TryGetValue(dataContainerId, out string sourceCode) ? sourceCode : null;
        public string Find(TaskNodeId taskNodeId) => m_TaskNodeSourceCodes.TryGetValue(taskNodeId, out string sourceCode) ? sourceCode : null;
    }

    class AttributeSourceManager
    {
        readonly Dictionary<DataViewId, IDataKey> m_AttributeSources = new();

        public void Add(DataViewId dataViewId, IDataKey attributeSourceKey) => m_AttributeSources.Add(dataViewId, attributeSourceKey);

        public bool TryGetAttributeSource(DataViewId dataViewId, out IDataKey attributeSourceKey) => m_AttributeSources.TryGetValue(dataViewId, out attributeSourceKey);

        public void Clear() => m_AttributeSources.Clear();
    }
}
