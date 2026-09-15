using System.Collections.Generic;
using UnityEngine;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class VfxTemplateCodeGenerationPass : CompilationPass
    {
        readonly IncludeFileShaderWriter m_IncludeFileShaderWriter = new();
        readonly ComputeShaderWriter m_ComputeShaderWriter = new();
        readonly RenderingShaderWriter m_RenderingShaderWriter = new();

        readonly DataDescriptionWriterRegistry m_DataWriter;

        HashSet<DataBindingId> m_DataBindingsWithView = new();

        public VfxTemplateCodeGenerationPass(DataDescriptionWriterRegistry dataWriter)
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
                if (taskNode.Task is VfxTemplatedTask task)
                {
                    string sourceCode = task.IsCompute
                        ? GenerateTemplatedTaskSourceCode_Compute(taskNode, context)
                        : GenerateTemplatedTaskSourceCode_Rendering(taskNode, context);
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

        VFXSpace ResolveSpace(TaskNode taskNode)
        {
            foreach (var dataNode in taskNode.DataNodes)
            {
                var rootDataView = dataNode.DataContainer.RootDataView;
                if (rootDataView.DataDescription is ParticleData particleData)
                {
                    return particleData.Space;
                }
            }

            return VFXSpace.None;
        }

        string GenerateTemplatedTaskSourceCode_Compute(TaskNode taskNode, CompilationContext context)
        {
            VfxTemplatedTask templatedTask = taskNode.Task as VfxTemplatedTask;
            //TODO: Generate proper name
            string shaderName = taskNode.Name ?? templatedTask.TemplateName;

            m_ComputeShaderWriter.Begin(shaderName);

            IncludeCommonLibrary(m_ComputeShaderWriter, ResolveSpace(taskNode));

            m_ComputeShaderWriter.WriteLine("#include \"Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/ThreadData.hlsl\"");

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
            VfxTemplatedTask templatedTask = taskNode.Task as VfxTemplatedTask;
            //TODO: Generate proper name
            string shaderName = taskNode.Name ?? templatedTask.TemplateName;

            m_RenderingShaderWriter.Begin(shaderName);

            using (m_RenderingShaderWriter.CreateSubShaderScope())
            {
                using (m_RenderingShaderWriter.CreateHlslIncludeScope())
                {
                    m_RenderingShaderWriter.WriteLine("#pragma multi_compile_instancing");

                    IncludeCommonLibrary(m_RenderingShaderWriter, ResolveSpace(taskNode));

                    m_RenderingShaderWriter.WriteLine("#include \"Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl\"");
                    m_RenderingShaderWriter.WriteLine("#include \"Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/ThreadData.hlsl\"");

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
                    dataDescriptionWriter.DefineResourceUsage(shaderWriter, dataNode.UsedDataViewsRoot, dataNode);
                    shaderWriter.WriteLine(generatedCodeContainer.Find(dataNode.DataContainer.Id), ShaderWriter.WriteLineOptions.Multiline);
                    dataDescriptionWriter.UndefineResourceUsage(shaderWriter, dataNode.UsedDataViewsRoot, dataNode);
                    shaderWriter.WriteLine($"// End   {dataNode.DataContainer.Name}");
                }
                else
                {
                    Debug.Assert(false, "Data node not handled");
                }
            }
        }

        void GenerateAttributeSets(ShaderWriter shaderWriter, TaskNode taskNode, VfxTemplatedTask templatedTask,
            CompilationContext context)
        {
            var attributeSourceManager = context.data.GetOrCreate<AttributeSourceManager>();
            foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
            {
                var structType = attributeKeyMapping.Key.ToString();
                var (bindingKey, subDataPath) = attributeKeyMapping.Value;
                DataView dataView = new();
                foreach (var binding in taskNode.DataBindings)
                {
                    if (binding.BindingDataKey.Equals(bindingKey))
                    {
                        bool found = binding.DataView.FindSubData(subDataPath, out dataView);
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

        void GenerateDataViews(ShaderWriter shaderWriter, TaskNode taskNode, VfxTemplatedTask templatedTask,
            CompilationContext context)
        {
            void LocateDataView(DataView dataView, ref DataView usedDataView)
            {
                if (dataView.Parent.HasValue)
                {
                    LocateDataView(dataView.Parent.Value, ref usedDataView);
                    usedDataView.FindSubData(dataView.SubDataKey, out usedDataView);
                }
            }

            m_DataBindingsWithView.Clear();

            var dataBindings = taskNode.DataBindings;
            foreach(var dataBinding in dataBindings)
            {
                var dataView = dataBinding.DataView;
                var dataNode = dataBinding.DataNode;

                var usedDataView = dataNode.UsedDataViewsRoot;
                LocateDataView(dataView, ref usedDataView);

                var dataDescription = dataView.DataDescription;

                Debug.Assert(!usedDataView.Valid || usedDataView.DataDescription == dataDescription);

                shaderWriter.NewLine();

                bool hasView = false;
                string typeName = m_DataWriter.FindDataTypeName(dataView);
                string bindingName = dataBinding.BindingDataKey.ToString();
                if (IsCompositeData(dataDescription))
                {
                    if (m_DataWriter.TryGetDataDescriptionWriter(dataDescription, out var dataDescriptionWriter))
                    {
                        hasView = dataDescriptionWriter.WriteView(shaderWriter, usedDataView, dataNode, bindingName, typeName, context);
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

        void WriteProcessBlocksDeclaration(ShaderWriter shaderWriter, VfxTemplatedTask templatedTask)
        {
            shaderWriter.Indent();
            shaderWriter.Write("void VFXProcessBlocks(");
            bool first = true;
            foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
            {
                var structType = attributeKeyMapping.Key.ToString();
                var structName = $"{char.ToLowerInvariant(structType[0])}{structType.Substring(1)}";
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

        void IncludeCommonLibrary(ShaderWriter shaderWriter, VFXSpace space)
        {
            shaderWriter.NewLine();
            shaderWriter.Define(space == VFXSpace.Local ? "VFX_LOCAL_SPACE" : "VFX_WORLD_SPACE", "1");
            shaderWriter.IncludeFileWithPragmas($"{VFXLibrary.currentSRPBinder.runtimePath}/VFXDefines.hlsl");
            shaderWriter.IncludeFile($"{VFXLibrary.currentSRPBinder.runtimePath}/VFXCommon.hlsl");
            shaderWriter.IncludeFile("Packages/com.unity.visualeffectgraph/Shaders/VFXCommon.hlsl");
        }

        void ForwardDeclarations(ShaderWriter shaderWriter, VfxTemplatedTask templatedTask)
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
            computeShaderWriter.WriteMainFunction(VfxDispatch.ThreadsPerGroup, 1, 1);
            computeShaderWriter.OpenBlock();
            InitMainData(computeShaderWriter, taskNode, context);
            computeShaderWriter.NewLine();
            computeShaderWriter.WriteLine("// Template entry point");
            computeShaderWriter.WriteLine("main(threadData);");
            computeShaderWriter.CloseBlock();
        }

        void GenerateEntryPoints_Rendering(RenderingShaderWriter renderingShaderWriter, TaskNode taskNode, CompilationContext context)
        {
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteLine("static ThreadData threadData;");
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteVertexFunction();
            renderingShaderWriter.OpenBlock();
            InitMainData(renderingShaderWriter, taskNode, context);
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteLine("return vert(threadData, id, input);");
            renderingShaderWriter.CloseBlock();

            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteFragmentFunction();
            renderingShaderWriter.OpenBlock();
            InitFragmentBindings(renderingShaderWriter, taskNode);
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteLine("return frag(input);");
            renderingShaderWriter.CloseBlock();
        }

        void InitMainData(ShaderWriter shaderWriter, TaskNode taskNode, CompilationContext context)
        {
            InitInstancing(shaderWriter, taskNode, context);
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
                    shaderWriter.WriteLine($"{variableName}.Init(threadData);");
                }
            }
        }

        void InitInstancing(ShaderWriter shaderWriter, TaskNode taskNode, CompilationContext context)
        {
            VfxTemplatedTask templatedTask = taskNode.Task as VfxTemplatedTask;

            shaderWriter.WriteLine("// Init instancing");

            bool isOutput = templatedTask.TemplateName == "Output";
            if(isOutput)
                shaderWriter.WriteLine("uint linearThreadIndex = (id >> 2) + input.instanceID * 2048;");
            else
                shaderWriter.WriteLine($"uint linearThreadIndex = groupId.x * {VfxDispatch.ThreadsPerGroup}u + groupThreadId.x + groupId.y * dispatchWidth * {VfxDispatch.ThreadsPerGroup}u;");

            if (!IsFixedSizeDispatch(templatedTask, isOutput, taskNode, context))
            {
                shaderWriter.WriteLine("threadData.InitVariable(linearThreadIndex);");
                return;
            }

            uint capacity = 0;
            foreach (var dataNode in taskNode.DataNodes)
            {
                if (dataNode.DataContainer.RootDataView.DataDescription is ParticleData particleData)
                {
                    capacity = particleData.Capacity;
                    break;
                }
            }

            // A dispatch pads each instance's slice to a whole group (InstanceSplitData::AddInstance).
            uint instancingStride = isOutput ? capacity : VfxDispatch.AlignToThreadGroups(capacity);
            shaderWriter.WriteLine($"threadData.InitFixed(linearThreadIndex, {instancingStride}u);");
        }

        // Outputs draw a capacity worth of vertices per instance and inits are sized from the spawn
        // counts; for updates this has to agree with VFXParticleSystemAssetData::IsFixedSizeUpdate().
        static bool IsFixedSizeDispatch(VfxTemplatedTask templatedTask, bool isOutput, TaskNode taskNode, CompilationContext context)
        {
            if (isOutput)
                return true;

            if (templatedTask.TemplateName == "Init")
                return false;

            var particleSystem = FindOwningParticleSystem(context.data.Get<ParticleSystemContainer>(), taskNode.Id);
            if (particleSystem == null)
                return true;

            // TODO: strips also force a fixed size update once they are supported.
            const UnityEngine.VFX.VFXSystemFlag fixedSizeUpdateFlags =
                UnityEngine.VFX.VFXSystemFlag.SystemHasKill | UnityEngine.VFX.VFXSystemFlag.SystemReceivedEventGPU;
            return (particleSystem.SystemFlags & fixedSizeUpdateFlags) != 0;
        }

        static ParticleSystemContainer.ParticleSystem FindOwningParticleSystem(ParticleSystemContainer particleSystemContainer, TaskNodeId taskNodeId)
        {
            foreach (var particleSystem in particleSystemContainer)
            {
                foreach (var task in particleSystem.Tasks)
                {
                    if (task.Id.Equals(taskNodeId))
                        return particleSystem;
                }
            }

            return null;
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

        void InitFragmentBindings(ShaderWriter shaderWriter, TaskNode taskNode)
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
                if(dataDescription is not ValueData<Texture>)
                    continue;
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
            if (dataBinding.DataView.DataDescription is EventListData eventListData)
                shaderWriter.Write(", ContextData);");
            else
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
                Debug.Assert(parentDataView.Valid);
                if (parentDataView.Id.Index != fromDataView.Id.Index)
                {
                    WriteSubdataPath(shaderWriter, fromDataView, parentDataView);
                }
                shaderWriter.Write(m_DataWriter.GetSubdataName(parentDataView, toDataView.SubDataKey));
            }
        }

        void GenerateSnippetFunctions(ShaderWriter shaderWriter, VfxTemplatedTask templatedTask)
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
}
