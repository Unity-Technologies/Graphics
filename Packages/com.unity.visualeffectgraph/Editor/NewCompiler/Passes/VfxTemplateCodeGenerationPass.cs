using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.GraphCommon.LowLevel.Editor;
using UnityEngine.VFX;
using Attribute = Unity.GraphCommon.LowLevel.Editor.Attribute;

namespace UnityEditor.VFX
{
    class VfxTemplateCodeGenerationPass : CompilationPass
    {
        // Must match kNbParticlePerInstance in VFXParticleSystem::RenderQuadCommand.
        // Only correct for quads: the octagon and hexahedron draws split by 1024 instead, so this
        // has to become per output topology when those are generated.
        const uint k_QuadParticlesPerDrawInstance = 2048u;

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

            CollectResourceTypes(ref context);

            foreach (var dataContainer in context.graph.DataContainers)
            {
                if (IsCompositeData(dataContainer.RootDataView.DataDescription))
                {
                    // Null when the writer had nothing to declare for the container, in which case there is
                    // no include file and nothing for IncludeData to pull in.
                    string sourceCode = GenerateContainerSourceCode(dataContainer, context);
                    if (sourceCode != null)
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

        // The element type a task declared for a buffer it reads, published so the resource writer can reach
        // it: the writer is handed a data view and the compilation data, never the task that bound it.
        static void CollectResourceTypes(ref CompilationContext context)
        {
            var resourceTypes = context.data.GetOrCreate<ResourceTypeContainer>();
            foreach (var taskNode in context.graph.TaskNodes)
            {
                if (taskNode.Task is not VfxTemplatedTask templatedTask)
                    continue;

                foreach (var dataBinding in taskNode.DataBindings)
                {
                    if (templatedTask.TryGetResourceType(dataBinding.BindingDataKey, out var bufferType))
                        resourceTypes.Add(dataBinding.DataView.Id, bufferType);
                }
            }
        }

        string GenerateContainerSourceCode(DataContainer dataContainer, CompilationContext context)
        {
            var containerName = dataContainer.IdentifierName;
            var rootDataView = dataContainer.RootDataView;
            m_IncludeFileShaderWriter.Begin(containerName);
            if (m_DataWriter.TryGetDataDescriptionWriter(rootDataView.DataDescription, out var dataDescriptionWriter))
            {
                dataDescriptionWriter.WriteDescription(m_IncludeFileShaderWriter, rootDataView, containerName, context);
            }
            return m_IncludeFileShaderWriter.End();
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

            GenerateThreadDataDeclaration(m_ComputeShaderWriter);

            IncludeData(m_ComputeShaderWriter, taskNode, context);

            GenerateAttributeSets(m_ComputeShaderWriter, taskNode, templatedTask, context);

            GenerateContextParametersStruct(m_ComputeShaderWriter, templatedTask, declareInstance: false);

            GenerateDataViews(m_ComputeShaderWriter, taskNode, templatedTask, context, declareBindingGlobals: false);

            GenerateTaskStruct(m_ComputeShaderWriter, taskNode, templatedTask);

            GenerateEntryPoint_Compute(m_ComputeShaderWriter, taskNode, context);

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

                    GenerateThreadDataDeclaration(m_RenderingShaderWriter);

                    IncludeData(m_RenderingShaderWriter, taskNode, context);

                    GenerateAttributeSets(m_RenderingShaderWriter, taskNode, templatedTask, context);

                    GenerateDataViews(m_RenderingShaderWriter, taskNode, templatedTask, context, declareBindingGlobals: true);

                    GenerateContextParametersStruct(m_RenderingShaderWriter, templatedTask, declareInstance: true);

                    ForwardDeclarations(m_RenderingShaderWriter, templatedTask);

                    IncludeTemplateFile(m_RenderingShaderWriter, templatedTask.TemplateName);

                    GenerateSnippetFunctions(m_RenderingShaderWriter, taskNode, templatedTask);
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
            var templatedTask = taskNode.Task as VfxTemplatedTask;

            foreach (var dataNode in taskNode.DataNodes)
            {
                var rootDataView = dataNode.DataContainer.RootDataView;
                var dataDescription = rootDataView.DataDescription;
                // The task only hands this one to the runtime, so the generated code has no use for it, and
                // its type is not necessarily one that could even be declared in a shader.
                if (IsBoundOnlyAsTaskParameter(templatedTask, dataNode))
                {
                    continue;
                }

                if (!m_DataWriter.TryGetDataDescriptionWriter(dataDescription, out var dataDescriptionWriter))
                {
                    // A plain value is declared here, inline, under the container name. Anything else with
                    // no writer is a description the generated code has no way to reach.
                    Debug.Assert(dataDescription is ValueData, "Data node not handled");

                    shaderWriter.NewLine();
                    var dataTypename = m_DataWriter.FindDataTypeName(rootDataView);
                    shaderWriter.WriteVariableDeclaration(dataTypename, dataNode.DataContainer.Name);
                    continue;
                }

                // Anything with a writer is reached through the source generated for its container. Null
                // when that writer had nothing to declare there, which is where a GPU resource lands unless
                // a task declared an element type for it: the resource the shader reads is declared per
                // binding, by GenerateDataViews.
                var containerSourceCode = generatedCodeContainer.Find(dataNode.DataContainer.Id);
                if (containerSourceCode == null)
                {
                    continue;
                }

                shaderWriter.NewLine();
                //shaderWriter.IncludeFile($"{dataNode.DataContainer.IdentifierName}.hlsl");
                shaderWriter.WriteLine($"// Begin {dataNode.DataContainer.IdentifierName}");
                dataDescriptionWriter.DefineResourceUsage(shaderWriter, rootDataView, dataNode);
                shaderWriter.WriteLine(containerSourceCode, ShaderWriter.WriteLineOptions.Multiline);
                dataDescriptionWriter.UndefineResourceUsage(shaderWriter, rootDataView, dataNode);
                shaderWriter.WriteLine($"// End   {dataNode.DataContainer.IdentifierName}");
            }
        }

        // The view holding the attributes an attribute struct is built from, i.e. the subdata the task's
        // binding points at for that attribute key.
        static bool TryGetAttributeDataView(TaskNode taskNode, IDataKey bindingKey, DataPath subDataPath, out DataView dataView)
        {
            foreach (var binding in taskNode.DataBindings)
            {
                if (binding.BindingDataKey.Equals(bindingKey))
                {
                    bool found = binding.DataView.FindSubData(subDataPath, out dataView);
                    Debug.Assert(found);
                    return found;
                }
            }

            dataView = new();
            return false;
        }

        // The name the attribute struct is passed under to VFXProcessSubtasks. Must match the name
        // VFXAttribute.GetNameInCode builds for the matching attribute location.
        static string GetAttributeStructName(IDataKey attributeKey)
        {
            var structType = attributeKey.ToString();
            return $"{char.ToLowerInvariant(structType[0])}{structType.Substring(1)}";
        }

        static Attribute GetAttribute(DataView attributeDataView) => (attributeDataView.SubDataKey as AttributeKey).Attribute;

        void GenerateAttributeSets(ShaderWriter shaderWriter, TaskNode taskNode, VfxTemplatedTask templatedTask,
            CompilationContext context)
        {
            var attributeSourceManager = context.data.GetOrCreate<AttributeSourceManager>();
            foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
            {
                var structType = attributeKeyMapping.Key.ToString();
                var (bindingKey, subDataPath) = attributeKeyMapping.Value;
                TryGetAttributeDataView(taskNode, bindingKey, subDataPath, out var dataView);

                attributeSourceManager.Add(dataView.Id, attributeKeyMapping.Key);

                shaderWriter.NewLine();
                shaderWriter.WriteLine($"struct {structType}");
                shaderWriter.OpenBlock();
                foreach (var attributeDataView in dataView.Children)
                {
                    var attribute = GetAttribute(attributeDataView);
                    shaderWriter.WriteLine($"{HlslCodeHelper.GetTypeName(attribute.Type)} {attribute.Name};");
                }
                shaderWriter.NewLine();
                shaderWriter.WriteLine("void Init()");
                shaderWriter.OpenBlock();
                foreach (var attributeDataView in dataView.Children)
                {
                    var attribute = GetAttribute(attributeDataView);
                    shaderWriter.WriteLine($"{attribute.Name} = {HlslCodeHelper.GetValueString(attribute.DefaultValue)};");
                }
                shaderWriter.CloseBlock();
                shaderWriter.CloseBlock(false);
                shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
            }
        }

        void GenerateDataViews(ShaderWriter shaderWriter, TaskNode taskNode, VfxTemplatedTask templatedTask,
            CompilationContext context, bool declareBindingGlobals)
        {
            m_DataBindingsWithView.Clear();

            var dataBindings = taskNode.DataBindings;
            foreach(var dataBinding in dataBindings)
            {
                // The runtime resolves this one for the task, the generated code never names it.
                if (IsTaskParameter(templatedTask, dataBinding))
                    continue;

                var dataView = dataBinding.DataView;
                var dataNode = dataBinding.DataNode;
                var dataDescription = dataView.DataDescription;

                shaderWriter.NewLine();

                bool hasView = false;
                string typeName = m_DataWriter.FindDataTypeName(dataView);
                string bindingName = dataBinding.BindingDataKey.ToString();

                if (m_DataWriter.TryGetDataDescriptionWriter(dataDescription, out var dataDescriptionWriter))
                {
                    hasView = dataDescriptionWriter.WriteView(shaderWriter, dataView, dataNode, bindingName, typeName, context);
                }

                if (hasView)
                {
                    m_DataBindingsWithView.Add(dataBinding.Id);
                }

                if (declareBindingGlobals)
                {
                    var kind = GetBindingVariableKind(templatedTask, dataBinding);
                    if (kind != BindingVariableKind.None)
                    {
                        shaderWriter.WriteVariableDeclaration(GetBindingVariableType(kind, dataBinding), bindingName,
                            isStatic: kind == BindingVariableKind.Value);
                    }
                }
            }
            var attributeSourceManager = context.data.GetOrCreate<AttributeSourceManager>();
            attributeSourceManager.Clear();
        }

        enum BindingVariableKind
        {
            None,
            Value,
            View,
            ContainerAlias
        }

        BindingVariableKind GetBindingVariableKind(VfxTemplatedTask templatedTask, DataBinding dataBinding)
        {
            if (IsTaskParameter(templatedTask, dataBinding))
                return BindingVariableKind.None;

            if (m_DataBindingsWithView.Contains(dataBinding.Id))
                return BindingVariableKind.View;

            if (!BindingNeedsRemapping(dataBinding))
                return BindingVariableKind.None;

            return IsCompositeData(dataBinding.DataView.DataDescription)
                ? BindingVariableKind.ContainerAlias
                : BindingVariableKind.Value;
        }

        string GetBindingVariableType(BindingVariableKind kind, DataBinding dataBinding)
            => kind == BindingVariableKind.View
                ? $"{dataBinding.BindingDataKey}View"
                : m_DataWriter.FindDataTypeName(dataBinding.DataView);

        static string GetTaskStructName(VfxTemplatedTask templatedTask) => $"{templatedTask.TemplateName}_Task";

        void GenerateTaskStruct(ShaderWriter shaderWriter, TaskNode taskNode, VfxTemplatedTask templatedTask)
        {
            shaderWriter.NewLine();
            shaderWriter.WriteLine("// Build task");
            shaderWriter.WriteLine($"struct {GetTaskStructName(templatedTask)}");
            shaderWriter.OpenBlock();
            foreach (var dataBinding in taskNode.DataBindings)
            {
                var kind = GetBindingVariableKind(templatedTask, dataBinding);
                if (kind != BindingVariableKind.None)
                    shaderWriter.WriteVariableDeclaration(GetBindingVariableType(kind, dataBinding), dataBinding.BindingDataKey.ToString());
            }
            if (templatedTask.ContextParameters.Count > 0)
                shaderWriter.WriteVariableDeclaration(VfxContextParametersHlsl.TypeName, VfxContextParametersHlsl.VariableName);

            GenerateSnippetFunctions(shaderWriter, taskNode, templatedTask);

            IncludeTemplateFile(shaderWriter, templatedTask.TemplateName);
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);
        }

        static bool IsTaskParameter(VfxTemplatedTask templatedTask, DataBinding dataBinding)
            => templatedTask != null && templatedTask.IsTaskParameter(dataBinding.BindingDataKey);

        // Whether the task reaches this data only to hand it to the runtime. The same container can be a
        // task parameter here and something the code reads elsewhere, so this is asked per task.
        static bool IsBoundOnlyAsTaskParameter(VfxTemplatedTask templatedTask, DataNode dataNode)
        {
            bool any = false;
            foreach (var dataBinding in dataNode.DataBindings)
            {
                if (!IsTaskParameter(templatedTask, dataBinding))
                    return false;
                any = true;
            }
            return any;
        }

        static bool HasContainerVariable(IDataDescription dataDescription) => dataDescription is not ResourceValueData;

        bool BindingNeedsRemapping(DataBinding dataBinding)
        {
            var dataView = dataBinding.DataView;

            if (!HasContainerVariable(dataView.DataDescription))
                return false;

            var containerName = dataView.DataContainer.Name;
            var bindingName = dataBinding.BindingDataKey.ToString();
            return !dataView.IsRoot || containerName != bindingName;
        }

        void WriteProcessSubtasksDeclaration(ShaderWriter shaderWriter, VfxTemplatedTask templatedTask)
        {
            shaderWriter.Indent();
            shaderWriter.Write("void VFXProcessSubtasks(");
            bool first = true;
            foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
            {
                if (!first)
                    shaderWriter.Write(", ");
                shaderWriter.Write($"inout {attributeKeyMapping.Key} {GetAttributeStructName(attributeKeyMapping.Key)}");
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
            WriteProcessSubtasksDeclaration(shaderWriter, templatedTask);
            shaderWriter.Write(";");
            shaderWriter.NewLine();
        }

        void GenerateContextParametersStruct(ShaderWriter shaderWriter, VfxTemplatedTask templatedTask, bool declareInstance)
        {
            if (templatedTask.ContextParameters.Count == 0)
                return;

            shaderWriter.NewLine();
            shaderWriter.WriteLine($"struct {VfxContextParametersHlsl.TypeName}");
            shaderWriter.OpenBlock();
            foreach (var parameter in templatedTask.ContextParameters)
            {
                shaderWriter.WriteVariableDeclaration(parameter.HlslTypeName, parameter.Name);
            }
            shaderWriter.CloseBlock(false);
            shaderWriter.WriteLine(";", ShaderWriter.WriteLineOptions.NoIndent);

            if (declareInstance)
            {
                shaderWriter.WriteVariableDeclaration(VfxContextParametersHlsl.TypeName,
                    VfxContextParametersHlsl.VariableName, isStatic: true);
            }
        }

        void GenerateThreadDataDeclaration(ShaderWriter shaderWriter)
        {
            shaderWriter.NewLine();
            shaderWriter.WriteVariableDeclaration("ThreadData", "threadData", isStatic: true);
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
            computeShaderWriter.WriteMainFunction(VfxDispatch.ThreadsPerGroup, 1, 1);
            computeShaderWriter.OpenBlock();
            InitMainData(computeShaderWriter, taskNode, context, declareBindingLocals: true);
            InitTask(computeShaderWriter, taskNode);
            computeShaderWriter.NewLine();
            computeShaderWriter.WriteLine("// Template entry point");
            computeShaderWriter.WriteLine("task.main(threadData);");
            computeShaderWriter.CloseBlock();
        }

        void GenerateEntryPoints_Rendering(RenderingShaderWriter renderingShaderWriter, TaskNode taskNode, CompilationContext context)
        {
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteVertexFunction();
            renderingShaderWriter.OpenBlock();
            InitMainData(renderingShaderWriter, taskNode, context, declareBindingLocals: false);
            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteLine("return vert(threadData, id, input);");
            renderingShaderWriter.CloseBlock();

            renderingShaderWriter.NewLine();
            renderingShaderWriter.WriteFragmentFunction();
            renderingShaderWriter.OpenBlock();
            renderingShaderWriter.WriteLine("return frag(input);");
            renderingShaderWriter.CloseBlock();
        }

        void InitMainData(ShaderWriter shaderWriter, TaskNode taskNode, CompilationContext context, bool declareBindingLocals)
        {
            InitInstancing(shaderWriter, taskNode, context);
            InitContainers(shaderWriter, taskNode);
            InitBindings(shaderWriter, taskNode, declareBindingLocals);
        }

        void InitContainers(ShaderWriter shaderWriter, TaskNode taskNode)
        {
            bool initContainers = false;
            foreach (var dataNode in taskNode.DataNodes)
            {
                var dataContainer = dataNode.DataContainer;
                var dataDescription = dataContainer.RootDataView.DataDescription;
                if (IsCompositeData(dataDescription) && HasContainerVariable(dataDescription))
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

            bool isOutput = templatedTask.IsOutput;
            if(isOutput)
                shaderWriter.WriteLine($"uint linearThreadIndex = (id >> 2) + input.instanceID * {k_QuadParticlesPerDrawInstance}u;");
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

            if (templatedTask.Stage == VfxTaskStage.Initialize)
                return false;

            if (!context.data.Get<ParticleSystemContainer>().TryGetOwner(taskNode.Id, out var particleSystem))
                throw new InvalidOperationException($"Task {taskNode.Name} belongs to no particle system.");

            // TODO: strips also force a fixed size update once they are supported.
            const UnityEngine.VFX.VFXSystemFlag fixedSizeUpdateFlags =
                UnityEngine.VFX.VFXSystemFlag.SystemHasKill | UnityEngine.VFX.VFXSystemFlag.SystemReceivedEventGPU;
            return (particleSystem.SystemFlags & fixedSizeUpdateFlags) != 0;
        }

        void InitBindings(ShaderWriter shaderWriter, TaskNode taskNode, bool declareLocals)
        {
            var templatedTask = taskNode.Task as VfxTemplatedTask;
            bool initBindings = false;
            foreach (var dataBinding in taskNode.DataBindings)
            {
                var kind = GetBindingVariableKind(templatedTask, dataBinding);
                if (kind == BindingVariableKind.None)
                    continue;

                shaderWriter.NewLine();
                if (!initBindings)
                {
                    shaderWriter.WriteLine("// Init bindings");
                    initBindings = true;
                }

                string localType = declareLocals ? GetBindingVariableType(kind, dataBinding) : null;
                if (kind == BindingVariableKind.View)
                {
                    if (localType != null)
                        shaderWriter.WriteVariableDeclaration(localType, dataBinding.BindingDataKey.ToString());
                    WriteBindingInit(shaderWriter, dataBinding);
                }
                else
                {
                    WriteBindingRemap(shaderWriter, dataBinding, localType);
                }
            }
        }

        void InitTask(ShaderWriter shaderWriter, TaskNode taskNode)
        {
            var templatedTask = taskNode.Task as VfxTemplatedTask;

            shaderWriter.NewLine();
            shaderWriter.WriteLine("// Init task");
            shaderWriter.WriteLine($"{GetTaskStructName(templatedTask)} task;");
            foreach (var dataBinding in taskNode.DataBindings)
            {
                if (GetBindingVariableKind(templatedTask, dataBinding) == BindingVariableKind.None)
                    continue;

                var bindingName = dataBinding.BindingDataKey.ToString();
                shaderWriter.WriteLine($"task.{bindingName} = {bindingName};");
            }
        }

        void WriteBindingInit(ShaderWriter shaderWriter, DataBinding dataBinding)
        {
            shaderWriter.Indent();
            shaderWriter.Write(dataBinding.BindingDataKey.ToString());
            shaderWriter.Write(".Init(");
            WriteSubdataPath(shaderWriter, dataBinding.DataNode.DataContainer, dataBinding.DataView);
            if (dataBinding.DataView.DataDescription is EventListData)
                shaderWriter.WriteLine(", ContextData);", ShaderWriter.WriteLineOptions.NoIndent);
            else
                shaderWriter.WriteLine(");", ShaderWriter.WriteLineOptions.NoIndent);
        }

        void WriteBindingRemap(ShaderWriter shaderWriter, DataBinding dataBinding, string localType)
        {
            shaderWriter.Indent();
            if (localType != null)
            {
                shaderWriter.Write(localType);
                shaderWriter.Write(" ");
            }
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

        void GenerateSnippetFunctions(ShaderWriter shaderWriter, TaskNode taskNode, VfxTemplatedTask templatedTask)
        {
            shaderWriter.NewLine();
            WriteProcessSubtasksDeclaration(shaderWriter, templatedTask);
            shaderWriter.NewLine();
            shaderWriter.OpenBlock();

            // Subtask sources are written against plain attribute names, so the attribute struct is copied
            // into locals of those names for the duration of the function, and copied back once every subtask
            // has run. That is also what makes a block's write visible to the subtasks after it.
            var attributeLocals = AttributeLocals.Collect(taskNode, templatedTask);

            attributeLocals.WriteDeclarations(shaderWriter);

            foreach (var block in templatedTask.Subtasks)
            {
                if (block.Task is TemplateSubtask templateSubtask)
                {
                    shaderWriter.WriteTemplateSubtask(templateSubtask);
                }
            }

            attributeLocals.WriteStores(shaderWriter);

            shaderWriter.CloseBlock();
        }

        /// <summary>
        /// The locals VFXProcessSubtasks copies the current attribute struct into and back out of, so that the
        /// block sources can use plain attribute names and see each other's writes. Source attributes need
        /// none: a block never names one in its source, it only reaches them through expressions, which
        /// ExpressionWriter writes as sourceAttributes.name.
        /// </summary>
        readonly struct AttributeLocals
        {
            readonly DataView m_Attributes;

            AttributeLocals(DataView attributes)
            {
                m_Attributes = attributes;
            }

            public static AttributeLocals Collect(TaskNode taskNode, VfxTemplatedTask templatedTask)
            {
                DataView attributes = new();
                foreach (var attributeKeyMapping in templatedTask.AttributeKeyMappings)
                {
                    if (!attributeKeyMapping.Key.Equals(IntermediateGraphBuilder.DefaultAttributeKey))
                        continue;

                    var (bindingKey, subDataPath) = attributeKeyMapping.Value;
                    TryGetAttributeDataView(taskNode, bindingKey, subDataPath, out attributes);
                    break;
                }

                return new AttributeLocals(attributes);
            }

            public void WriteDeclarations(ShaderWriter shaderWriter)
            {
                if (!m_Attributes.Valid)
                    return;

                var structName = GetAttributeStructName(IntermediateGraphBuilder.DefaultAttributeKey);
                foreach (var attributeDataView in m_Attributes.Children)
                {
                    var attribute = GetAttribute(attributeDataView);
                    shaderWriter.WriteLine($"{HlslCodeHelper.GetTypeName(attribute.Type)} {attribute.Name} = {structName}.{attribute.Name};");
                }
            }

            /// <summary>
            /// Copies every local back into the struct, which the template reads once this returns. Not
            /// narrowed to the declared write paths: an output has none — they never reach the buffer, so
            /// <see cref="ParticleSystemBuilder"/> clears them — yet its blocks still assign these locals.
            /// </summary>
            public void WriteStores(ShaderWriter shaderWriter)
            {
                if (!m_Attributes.Valid)
                    return;

                var structName = GetAttributeStructName(IntermediateGraphBuilder.DefaultAttributeKey);
                foreach (var attributeDataView in m_Attributes.Children)
                {
                    var attribute = GetAttribute(attributeDataView);
                    shaderWriter.WriteLine($"{structName}.{attribute.Name} = {attribute.Name};");
                }
            }
        }

        // A description a writer is registered for is one the generated code reaches through that writer,
        // rather than declaring it inline as a plain value.
        bool IsCompositeData(IDataDescription dataDescription)
            => m_DataWriter.TryGetDataDescriptionWriter(dataDescription, out _);
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
