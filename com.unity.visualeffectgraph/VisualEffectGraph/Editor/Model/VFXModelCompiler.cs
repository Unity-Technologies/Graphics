using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public static class CommonAttrib
    {
        public static VFXAttribute Seed =               new VFXAttribute("seed", VFXValueType.kUint);
        public static VFXAttribute Position =           new VFXAttribute("position", VFXValueType.kFloat3);
        public static VFXAttribute Velocity =           new VFXAttribute("velocity", VFXValueType.kFloat3);
        public static VFXAttribute Color =              new VFXAttribute("color", VFXValueType.kFloat3);
        public static VFXAttribute Alpha =              new VFXAttribute("alpha", VFXValueType.kFloat);
        public static VFXAttribute Phase =              new VFXAttribute("phase", VFXValueType.kFloat);
        public static VFXAttribute Size =               new VFXAttribute("size", VFXValueType.kFloat2);
        public static VFXAttribute Lifetime =           new VFXAttribute("lifetime", VFXValueType.kFloat);
        public static VFXAttribute Age =                new VFXAttribute("age", VFXValueType.kFloat);
        public static VFXAttribute Angle =              new VFXAttribute("angle", VFXValueType.kFloat);
        public static VFXAttribute AngularVelocity =    new VFXAttribute("angularVelocity", VFXValueType.kFloat);
        public static VFXAttribute TexIndex =           new VFXAttribute("texIndex", VFXValueType.kFloat);
        public static VFXAttribute Pivot =              new VFXAttribute("pivot", VFXValueType.kFloat3);
        public static VFXAttribute ParticleId =         new VFXAttribute("particleId", VFXValueType.kUint);

        // Output orientation
        public static VFXAttribute Front =              new VFXAttribute("front", VFXValueType.kFloat3);
        public static VFXAttribute Side =               new VFXAttribute("side", VFXValueType.kFloat3);
        public static VFXAttribute Up =                 new VFXAttribute("up", VFXValueType.kFloat3);
    }

    public class VFXSystemRuntimeData
    {
        public Dictionary<VFXExpression, string> uniforms;
        public Dictionary<VFXExpression, string> outputUniforms;
        
        ComputeShader simulationShader; 
        public ComputeShader SimulationShader { get { return simulationShader; } }

        Shader outputShader;
        public Shader OutputShader { get { return outputShader; } }

        public VFXGeneratedTextureData m_GeneratedTextureData = null;
        public VFXContextDesc.Type m_ColorTextureContexts;
        public VFXContextDesc.Type m_FloatTextureContexts;

        public HashSet<VFXExpression> m_RawExpressions = null;

        int initKernel = -1;
        public int InitKernel { get { return initKernel; } }
        int updateKernel = -1;
        public int UpdateKernel { get { return updateKernel; } }

        public uint outputType; // tmp value to pass to C++
        public bool hasKill;

        public int outputBufferSize;

        public VFXBufferDesc[] buffersDesc;

        public VFXSystemRuntimeData(ComputeShader computeShader,Shader shader)
        {
            simulationShader = computeShader;
            outputShader = shader;

            // FindKernel throws instead of setting value to -1
            try { initKernel = simulationShader.FindKernel("CSVFXInit"); }
            catch(Exception) { initKernel = -1; }
            try { updateKernel = simulationShader.FindKernel("CSVFXUpdate"); }
            catch(Exception) { updateKernel = -1; }
        }
    }

    public class AttributeBuffer
    {
        public AttributeBuffer(int index, VFXAttribute.Usage usage)
        {
            m_Index = index;
            m_Usage = usage;
            m_Attribs = new List<VFXAttribute>();
        }

        public void Add(VFXAttribute attrib)
        {
            m_Attribs.Add(attrib);
        }

        public void Add(AttributeBuffer other)
        {
            for (int i = 0; i < other.Count; ++i)
                m_Attribs.Add(other[i]);
            m_Usage |= other.m_Usage;
        }

        public int Index
        {
            get { return m_Index; }
        }

        // return usage per pass + RW
        public VFXAttribute.Usage Usage
        {
            get { return m_Usage; }
        }

        public int Count
        {
            get { return m_Attribs.Count; }
        }

        public VFXAttribute this[int index]
        {
            get { return m_Attribs[index]; }
        }

        public bool Used(VFXContextDesc.Type type)
        {
            return VFXAttribute.Used(m_Usage, type);
        }

        public bool Writable(VFXContextDesc.Type type)
        {
            return VFXAttribute.Writable(m_Usage,type);
        }

        public int GetSizeInBytes(bool withPadding = false)
        {
            int size = 0;
            foreach (VFXAttribute attrib in m_Attribs)
                size += VFXValue.TypeToSize(attrib.m_Type);
            if (withPadding)
                size = (size + 3) & ~0x3; // size is multiple of dword
            return size << 2;
        }

        int m_Index;
        VFXAttribute.Usage m_Usage;
        List<VFXAttribute> m_Attribs;
    }

    public class AttribComparer : IEqualityComparer<VFXAttribute>
    {
        public bool Equals(VFXAttribute attr0, VFXAttribute attr1)
        {
            return attr0.m_Name == attr1.m_Name && attr0.m_Type == attr1.m_Type;
        }

        public int GetHashCode(VFXAttribute attr)
        {
            return 13 * attr.m_Name.GetHashCode() + attr.m_Type.GetHashCode(); // Simple factored sum
        }
    }

    public class ShaderMetaData
    {
        public VFXSystemModel system;

        public List<VFXBlockModel> initBlocks;
        public List<VFXBlockModel> updateBlocks;
        public List<VFXBlockModel> outputBlocks;

        public bool hasKill;
        public bool hasRand;
        public bool hasCull;

        public List<AttributeBuffer> attributeBuffers;
        public Dictionary<VFXAttribute, AttributeBuffer> attribToBuffer;
        public Dictionary<VFXAttribute, VFXAttribute.Usage> localAttribs;

        public HashSet<VFXExpression> globalUniforms;
        public HashSet<VFXExpression> initUniforms;
        public HashSet<VFXExpression> updateUniforms;

        public HashSet<VFXExpression> globalSamplers;
        public HashSet<VFXExpression> initSamplers;
        public HashSet<VFXExpression> updateSamplers;

        public AttributeBuffer outputBuffer;
        public HashSet<VFXExpression> outputUniforms;
        public HashSet<VFXExpression> outputSamplers;

        public Dictionary<VFXExpression, string> paramToName;
        public Dictionary<VFXExpression, string> outputParamToName;

        public VFXGeneratedTextureData generatedTextureData;
        public VFXContextDesc.Type colorTextureContexts;
        public VFXContextDesc.Type floatTextureContexts;

        public Dictionary<VFXExpression, VFXExpression> extraUniforms;

        public bool HasAttribute(VFXAttribute attrib) // TODO Check against usage ?
        {
            return attribToBuffer.ContainsKey(attrib) || localAttribs.ContainsKey(attrib);
        }

        public bool UseOutputData()
        {
            return outputBuffer != null;
        }
    }

    public static class VFXModelCompiler
    {
        const bool USE_DYNAMIC_AABB = false; // experimental

        public static VFXSystemRuntimeData CompileSystem(VFXSystemModel system)
        {
            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Gather blocks");

            // Create output compiler
            VFXOutputShaderGeneratorModule outputGenerator = null;
            VFXShaderGeneratorModule initGenerator = null;
            VFXShaderGeneratorModule updateGenerator = null;

            for (int i = 0; i < system.GetNbChildren(); ++i)
            {
                var model = system.GetChild(i);
                var desc = model.Desc;
                switch (desc.m_Type)
                {
                    case VFXContextDesc.Type.kTypeInit: initGenerator = desc.CreateShaderGenerator(model); break;
                    case VFXContextDesc.Type.kTypeUpdate: updateGenerator = desc.CreateShaderGenerator(model); break;
                    case VFXContextDesc.Type.kTypeOutput: outputGenerator = desc.CreateShaderGenerator(model) as VFXOutputShaderGeneratorModule; break;
                }
            }

            if (outputGenerator == null || (initGenerator == null && updateGenerator == null)) // Tmp: we need the 3 contexts atm
                return null;

            // BLOCKS
            List<VFXBlockModel> initBlocks = new List<VFXBlockModel>();
            List<VFXBlockModel> updateBlocks = new List<VFXBlockModel>();
            List<VFXBlockModel> outputBlocks = new List<VFXBlockModel>();
            bool initHasRand = false;
            bool updateHasRand = false;
            bool updateHasKill = false;
            bool outputHasCull = false;

            // Collapses the contexts into one big init and update
            for (int i = 0; i < system.GetNbChildren(); ++i)
            {
                VFXContextModel context = system.GetChild(i);

                List<VFXBlockModel> currentList = null; ;
                switch (context.GetContextType())
                {
                    case VFXContextDesc.Type.kTypeInit: currentList = initBlocks; break;
                    case VFXContextDesc.Type.kTypeUpdate: currentList = updateBlocks; break;
                    case VFXContextDesc.Type.kTypeOutput: currentList = outputBlocks; break;
                }

                if (currentList == null)
                    continue;

                bool hasRand = false;
                bool hasKill = false;
                for (int j = 0; j < context.GetNbChildren(); ++j)
                {
                    VFXBlockModel blockModel = context.GetChild(j);
                    if (blockModel.Enabled)
                    {
                        hasRand |= blockModel.Desc.IsSet(VFXBlockDesc.Flag.kHasRand);
                        hasKill |= blockModel.Desc.IsSet(VFXBlockDesc.Flag.kHasKill);
                        currentList.Add(blockModel);
                    }
                }

                switch (context.GetContextType())
                {
                    case VFXContextDesc.Type.kTypeInit: 
                        initHasRand |= hasRand; 
                        break;
                    case VFXContextDesc.Type.kTypeUpdate: 
                        updateHasRand |= hasRand;
                        updateHasKill |= hasKill;
                        break;
                    case VFXContextDesc.Type.kTypeOutput:
                        outputHasCull |= hasKill;
                        break;
                }
            }

            if (initBlocks.Count == 0 && updateBlocks.Count == 0)
            {
                // Invalid system, not compiled
                VFXEditor.Log("System is invalid: Empty");
                return null;
            }

            // ATTRIBUTES (TODO Refactor the code !)
            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Generate attributes");

            Dictionary<VFXAttribute, VFXAttribute.Usage> attribs = new Dictionary<VFXAttribute, VFXAttribute.Usage>(new AttribComparer());

            CollectAttributes(attribs, initBlocks, VFXContextDesc.Type.kTypeInit);
            CollectAttributes(attribs, updateBlocks, VFXContextDesc.Type.kTypeUpdate);
            CollectAttributes(attribs, outputBlocks, VFXContextDesc.Type.kTypeOutput);

            // Update flags with generators
            VFXBlockDesc.Flag initGeneratorFlags = VFXBlockDesc.Flag.kNone;
            if (initGenerator != null && !initGenerator.UpdateAttributes(attribs, ref initGeneratorFlags))
                return null;

            initHasRand |= (initGeneratorFlags & VFXBlockDesc.Flag.kHasRand) != 0;

            VFXBlockDesc.Flag updateGeneratorFlags = VFXBlockDesc.Flag.kNone;
            if (updateGenerator != null && !updateGenerator.UpdateAttributes(attribs, ref updateGeneratorFlags))
                return null;

            updateHasRand |= (updateGeneratorFlags & VFXBlockDesc.Flag.kHasRand) != 0;
            updateHasKill |= (updateGeneratorFlags & VFXBlockDesc.Flag.kHasKill) != 0;

            VFXBlockDesc.Flag dummy = VFXBlockDesc.Flag.kNone;
            if (!outputGenerator.UpdateAttributes(attribs, ref dummy))
                return null;

            if (VFXEditor.Graph.systems.PhaseShift)
            {
                if (attribs.ContainsKey(CommonAttrib.Position) && attribs.ContainsKey(CommonAttrib.Velocity))
                {
                    attribs[CommonAttrib.Phase] = VFXAttribute.Usage.kInitRW | VFXAttribute.Usage.kUpdateR; // Add phase attribute   
                    attribs[CommonAttrib.Position] = attribs[CommonAttrib.Position] | VFXAttribute.Usage.kInitRW | VFXAttribute.Usage.kUpdateRW; // Ensure position is writable in init and update
                    attribs[CommonAttrib.Velocity] = attribs[CommonAttrib.Velocity] | VFXAttribute.Usage.kInitRW | VFXAttribute.Usage.kUpdateRW; // Ensure velocity is readable in init and update

                    initHasRand = true; // phase needs rand as initialization
                }
                else
                {
                    VFXEditor.Graph.systems.PhaseShift = false;
                    return null;
                }
            }

            // Add the seed attribute in case we need PRG
            if (initHasRand || updateHasRand)
                attribs[CommonAttrib.Seed] = (initHasRand ? VFXAttribute.Usage.kInitRW : VFXAttribute.Usage.kNone) | (updateHasRand ? VFXAttribute.Usage.kUpdateRW : VFXAttribute.Usage.kNone);

            // Find unitialized attribs and remove 
            List<VFXAttribute> unitializedAttribs = new List<VFXAttribute>(); 
            foreach (var attrib in attribs)
            {
                if ((attrib.Value & VFXAttribute.Usage.kInitRW) == 0 && (attrib.Value & VFXAttribute.Usage.kUpdateRW) != 0) // Unitialized attribute
                {
                    if (attrib.Key.m_Name != "seed" || attrib.Key.m_Name != "age") // Dont log anything for those as initialization is implicit
                        Debug.LogWarning(attrib.Key.m_Name + " is used in update but not initialized. Use default value (0)");
                    unitializedAttribs.Add(attrib.Key);
                }               
                // TODO attrib to remove (when written and never used for instance) ! But must also remove blocks using them...
            }

            // Update the usage
            foreach (var attrib in unitializedAttribs)
                attribs[attrib] = attribs[attrib] | VFXAttribute.Usage.kInitRW;

            // Find local attributes and store them aside (as they dont go into buffers but are used locally in shaders)
            Dictionary<VFXAttribute, VFXAttribute.Usage> localAttribs = new Dictionary<VFXAttribute, VFXAttribute.Usage>(new AttribComparer()); 
            foreach (var attrib in attribs)
            {
                // local attribs are the ones used only in init or in output
                switch (attrib.Value)
                {
                    case VFXAttribute.Usage.kInitR:
                    case VFXAttribute.Usage.kInitRW:
                    case VFXAttribute.Usage.kOutputR:
                    case VFXAttribute.Usage.kOutputRW:
                        localAttribs.Add(attrib.Key,attrib.Value);
                        break;
                }
            }

            foreach (var attrib in localAttribs)
            {
                //Debug.Log("attrib " + attrib.Key.m_Name + " is local (usage:" + attrib.Value + ")");
                attribs.Remove(attrib.Key);
            }
           

            // Sort attrib by usage and by size
            List<AttributeBuffer> buffers = VFXAttributePacker.Pack(attribs,6); // 8 being the maximum UAV, reserve 2 for dead list and flags (TODO On mobile it is 4 max)

            if (buffers.Count > 6)
            {
                // TODO : Merge appropriate buffers in that case
                Debug.LogError("Too many buffers used: "+buffers.Count);
                return null;
            }

            // Associate attrib to buffer
            var attribToBuffer = new Dictionary<VFXAttribute, AttributeBuffer>(new AttribComparer());
            foreach (var buffer in buffers)
                for (int i = 0; i < buffer.Count; ++i)
                    attribToBuffer.Add(buffer[i], buffer);

            //--------------------------------------------------------------------------
            // OUTPUT BUFFER GENERATION
            bool useOutputBuffer = updateHasKill && system.BlendingMode != BlendMode.kAlpha; // Let's not use indirect output with alpha blend till we have sorting
            var outputAttribs = new List<VFXAttribute>();
            AttributeBuffer outputBuffer = useOutputBuffer ? new AttributeBuffer(-1,VFXAttribute.Usage.kUpdateW | VFXAttribute.Usage.kOutputR) : null;
            if (updateHasKill)
            {
                // Buckets by size
                var attribsPerSize = new Queue<VFXAttribute>[4];
                for (int i = 0; i < 4; ++i) // Assuming sizes cannot be more than 4 bytes
                    attribsPerSize[i] = new Queue<VFXAttribute>();

                // Gather output attributes and add to correct size bucket
                foreach (var attrib in attribToBuffer)
                    if (attrib.Value.Used(VFXContextDesc.Type.kTypeOutput))
                    {
                        VFXValue.TypeToSize(attrib.Key.m_Type);
                        attribsPerSize[VFXValue.TypeToSize(attrib.Key.m_Type) - 1].Enqueue(attrib.Key);
                    }

                // First add 4 dwords types
                while (attribsPerSize[3].Count > 0) 
                    outputAttribs.Add(attribsPerSize[3].Dequeue());
                // Then 3 paired with 1
                while (attribsPerSize[2].Count > 0)
                {
                    outputAttribs.Add(attribsPerSize[2].Dequeue());
                    if (attribsPerSize[0].Count > 0)
                        outputAttribs.Add(attribsPerSize[0].Dequeue());
                }
                // 2 paired with 2 or two 1s
                while (attribsPerSize[1].Count > 0)
                {
                    outputAttribs.Add(attribsPerSize[1].Dequeue());
                    if (attribsPerSize[1].Count > 0)
                        outputAttribs.Add(attribsPerSize[1].Dequeue());
                    else
                    {
                        if (attribsPerSize[0].Count > 0)
                            outputAttribs.Add(attribsPerSize[0].Dequeue());
                        if (attribsPerSize[0].Count > 0)
                            outputAttribs.Add(attribsPerSize[0].Dequeue());
                    }
                }
                // Finally add the 1s
                while (attribsPerSize[0].Count > 0)
                    outputAttribs.Add(attribsPerSize[0].Dequeue());

                // Debug log
                int offset = 0;
                Debug.Log("OUTPUT BUFFER:");
                foreach (var attrib in outputAttribs)
                {
                    int size = VFXValue.TypeToSize(attrib.m_Type);
                    int alignment = size == 3 ? 4 : size;
                    int padding = (alignment - (offset % alignment)) % alignment;
                    if (padding != 0)
                        Debug.Log("_PADDING - " + padding);
                    Debug.Log(attrib.m_Name+" - "+attrib.m_Type);
                    offset += size + padding;

                    if (useOutputBuffer)
                        outputBuffer.Add(attrib);
                }
                // Pad the end to have size multiple of 4 dwords
                if ((offset & 3) != 0)
                {
                    int padding = 4 - (offset & 3);
                    Debug.Log("_PADDING - " + padding);
                }
            }
            //--------------------------------------------------------------------------

            VFXEditor.Log("Nb Attributes : " + attribs.Count);
            VFXEditor.Log("Nb Attribute buffers: " + buffers.Count);
            for (int i = 0; i < buffers.Count; ++i)
            {
                string str = "\t " + i + " |";
                for (int j = 0; j < buffers[i].Count; ++j)
                {
                    str += buffers[i][j].m_Name + "|";
                }
                str += " " + buffers[i].GetSizeInBytes() + "bytes";
                VFXEditor.Log(str);
            }
                
            // UNIFORMS
            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Generate uniforms");

            // TMP Clean that
            SpaceRef spaceRef = system.GetSpaceRef();
            HashSet<VFXExpression> rawExpressions = new HashSet<VFXExpression>();

            foreach (VFXBlockModel block in initBlocks)
                for (int i = 0; i < block.Desc.Properties.Length; ++i)
                    block.GetSlot(i).CollectExpressions(rawExpressions, spaceRef);

            foreach (VFXBlockModel block in updateBlocks)
                for (int i = 0; i < block.Desc.Properties.Length; ++i)
                    block.GetSlot(i).CollectExpressions(rawExpressions, spaceRef);

            foreach (VFXBlockModel block in outputBlocks)
                for (int i = 0; i < block.Desc.Properties.Length; ++i)
                    block.GetSlot(i).CollectExpressions(rawExpressions, spaceRef);

            HashSet<VFXExpression> initUniforms = CollectUniforms(initBlocks, spaceRef);
            if (initGenerator != null)
                initGenerator.UpdateUniforms(initUniforms);
            HashSet<VFXExpression> updateUniforms = CollectUniforms(updateBlocks, spaceRef);
            if (updateGenerator != null)
                updateGenerator.UpdateUniforms(updateUniforms);
            HashSet<VFXExpression> outputUniforms = CollectUniforms(outputBlocks, spaceRef);
            if (outputGenerator != null)
                outputGenerator.UpdateUniforms(outputUniforms);

            // Generate potential extra uniforms  
            Dictionary<VFXExpression, VFXExpression> initGeneratedUniforms = GenerateExtraUniforms(initBlocks, spaceRef);
            Dictionary<VFXExpression, VFXExpression> updateGeneratedUniforms = GenerateExtraUniforms(updateBlocks, spaceRef);
            Dictionary<VFXExpression, VFXExpression> outputGeneratedUniforms = GenerateExtraUniforms(outputBlocks, spaceRef);

            // Keep track of all generated uniforms
            Dictionary<VFXExpression, VFXExpression> generatedUniforms = new Dictionary<VFXExpression, VFXExpression>();

            // add generated uniforms to uniform list
            foreach (var uniform in initGeneratedUniforms)
            {
                rawExpressions.Add(uniform.Value);
                if (!generatedUniforms.ContainsKey(uniform.Key))
                {
                    generatedUniforms.Add(uniform.Key,uniform.Value);
                    if (uniform.Value.Reduce().IsValue())
                        initUniforms.Add(uniform.Value);
                }
            }
            foreach (var uniform in updateGeneratedUniforms)
            {
                rawExpressions.Add(uniform.Value);
                if (!generatedUniforms.ContainsKey(uniform.Key))
                {
                    generatedUniforms.Add(uniform.Key,uniform.Value);
                    if (uniform.Value.Reduce().IsValue())
                        updateUniforms.Add(uniform.Value);
                }
            }
            foreach (var uniform in outputGeneratedUniforms)
            {
                rawExpressions.Add(uniform.Value);
                if (!generatedUniforms.ContainsKey(uniform.Key))
                {
                    generatedUniforms.Add(uniform.Key, uniform.Value);
                    if (uniform.Value.Reduce().IsValue())
                        outputUniforms.Add(uniform.Value);
                }
            }
 
            // collect samplers
            HashSet<VFXExpression> initSamplers = CollectAndRemoveSamplers(initUniforms);
            HashSet<VFXExpression> updateSamplers = CollectAndRemoveSamplers(updateUniforms);

            // Check what context needs signal textures
            VFXContextDesc.Type colorTextureContexts = VFXContextDesc.Type.kTypeNone;
            if (HasValueOfType(initUniforms, VFXValueType.kColorGradient))      colorTextureContexts |= VFXContextDesc.Type.kTypeInit;
            if (HasValueOfType(updateUniforms, VFXValueType.kColorGradient))    colorTextureContexts |= VFXContextDesc.Type.kTypeUpdate;
            if (HasValueOfType(outputUniforms, VFXValueType.kColorGradient))    colorTextureContexts |= VFXContextDesc.Type.kTypeOutput;

            VFXContextDesc.Type floatTextureContexts = VFXContextDesc.Type.kTypeNone;
            if (HasValueOfType(initUniforms, VFXValueType.kCurve))              floatTextureContexts |= VFXContextDesc.Type.kTypeInit;
            if (HasValueOfType(updateUniforms, VFXValueType.kCurve))            floatTextureContexts |= VFXContextDesc.Type.kTypeUpdate;
            if (HasValueOfType(outputUniforms, VFXValueType.kCurve))            floatTextureContexts |= VFXContextDesc.Type.kTypeOutput;

            // Collect the intersection between init and update uniforms / samplers
            HashSet<VFXExpression> globalUniforms = CollectIntersection(initUniforms, updateUniforms);
            HashSet<VFXExpression> globalSamplers = CollectIntersection(initSamplers, updateSamplers);

            // Output stuff
            //HashSet<VFXExpression> outputUniforms = new HashSet<VFXExpression>();
            //outputGenerator.UpdateUniforms(outputUniforms);
            HashSet<VFXExpression> outputSamplers = CollectAndRemoveSamplers(outputUniforms);

            // TODO Change that!
            outputGenerator.UpdateUniforms(rawExpressions);

            // Associate VFXValue to generated name
            var paramToName = new Dictionary<VFXExpression, string>();
            GenerateParamNames(paramToName, globalUniforms, "globalUniform");
            GenerateParamNames(paramToName, initUniforms, "initUniform");
            GenerateParamNames(paramToName, updateUniforms, "updateUniform");

            GenerateParamNames(paramToName, globalSamplers, "globalSampler");
            GenerateParamNames(paramToName, initSamplers, "initSampler");
            GenerateParamNames(paramToName, updateSamplers, "updateSampler");

            var outputParamToName = new Dictionary<VFXExpression, string>();
            GenerateParamNames(outputParamToName, outputUniforms, "outputUniform");
            GenerateParamNames(outputParamToName, outputSamplers, "outputSampler");

            // Log result
            VFXEditor.Log("Nb init blocks: " + initBlocks.Count);
            VFXEditor.Log("Nb update blocks: " + updateBlocks.Count);
            VFXEditor.Log("Nb global uniforms: " + globalUniforms.Count);
            VFXEditor.Log("Nb init uniforms: " + initUniforms.Count);
            VFXEditor.Log("Nb update uniforms: " + updateUniforms.Count);

            ShaderMetaData shaderMetaData = new ShaderMetaData();
            shaderMetaData.system = system;
            shaderMetaData.initBlocks = initBlocks;
            shaderMetaData.updateBlocks = updateBlocks;
            shaderMetaData.outputBlocks = outputBlocks;
            shaderMetaData.hasRand = initHasRand || updateHasRand;
            shaderMetaData.hasKill = updateHasKill;
            shaderMetaData.hasCull = outputHasCull;
            shaderMetaData.attributeBuffers = buffers;
            shaderMetaData.attribToBuffer = attribToBuffer;
            shaderMetaData.localAttribs = localAttribs;
            shaderMetaData.globalUniforms = globalUniforms;
            shaderMetaData.initUniforms = initUniforms;
            shaderMetaData.updateUniforms = updateUniforms;
            shaderMetaData.globalSamplers = globalSamplers;
            shaderMetaData.initSamplers = initSamplers;
            shaderMetaData.updateSamplers = updateSamplers;
            shaderMetaData.outputUniforms = outputUniforms;
            shaderMetaData.outputSamplers = outputSamplers;
            shaderMetaData.paramToName = paramToName;
            shaderMetaData.outputParamToName = outputParamToName;
            shaderMetaData.generatedTextureData = system.GeneratedTextureData;
            shaderMetaData.colorTextureContexts = colorTextureContexts;
            shaderMetaData.floatTextureContexts = floatTextureContexts;
            shaderMetaData.extraUniforms = generatedUniforms;
            shaderMetaData.outputBuffer = outputBuffer;

            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Generate shader code");
            string shaderSource = WriteComputeShader(shaderMetaData,initGenerator,updateGenerator);
            string outputShaderSource = WriteOutputShader(system,shaderMetaData,outputGenerator);

            string shaderName = "";
            if (VFXEditor.asset != null)
            {
                shaderName = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(VFXEditor.asset));
                shaderName += '_';
            }

            shaderName += system.Id;

            VFXEditor.Log("\n**** SHADER CODE ****");
            VFXEditor.Log(shaderSource);
            VFXEditor.Log(outputShaderSource);
            VFXEditor.Log("\n*********************");

            // Write to file
            string shaderPath = VFXEditor.GlobalShaderDir();
            System.IO.Directory.CreateDirectory(shaderPath);

            string computeShaderPath = shaderPath + shaderName + ".compute";
            string outputShaderPath = shaderPath + shaderName + ".shader";

            string oldShaderSource = "";
            try
            {
                oldShaderSource = System.IO.File.ReadAllText(computeShaderPath);
            }
            catch (Exception) {}

            string oldOutputShaderSource = "";
            try
            {
                oldOutputShaderSource = System.IO.File.ReadAllText(outputShaderPath);
            }
            catch (Exception) {}

            string localShaderPath = VFXEditor.LocalShaderDir() + shaderName;
            string simulationShaderPath = localShaderPath + ".compute";
            string renderShaderPath = localShaderPath + ".shader";

            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Write simulation shader");
            if (oldShaderSource != shaderSource) // Rewrite shader only if source has changed, this saves a lot of processing time!
            {
                System.IO.File.WriteAllText(computeShaderPath, shaderSource);
                AssetDatabase.ImportAsset(simulationShaderPath);
            }

            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Write output shader");
            if (oldOutputShaderSource != outputShaderSource) // Rewrite shader only if source has changed, this saves a lot of processing time!
            {
                System.IO.File.WriteAllText(outputShaderPath, outputShaderSource);
                AssetDatabase.ImportAsset(renderShaderPath);
            }

            ProgressBarHelper.IncrementStep("Compile system " + system.Id + ": Reload shaders");
            ComputeShader simulationShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(simulationShaderPath);
            Shader outputShader = AssetDatabase.LoadAssetAtPath<Shader>(renderShaderPath);

            VFXSystemRuntimeData rtData = new VFXSystemRuntimeData(simulationShader,outputShader);

            rtData.outputType = outputGenerator.GetSingleIndexBuffer(shaderMetaData) != null ? 1u : 0u; // This is temp
            rtData.hasKill = shaderMetaData.hasKill;

            rtData.outputBufferSize = outputBuffer != null ? outputBuffer.GetSizeInBytes(true) : 0;
            Debug.Log("OUTPUTBUFFER SIZE: " + rtData.outputBufferSize);

            rtData.m_GeneratedTextureData = system.GeneratedTextureData;
            rtData.m_ColorTextureContexts = colorTextureContexts;
            rtData.m_FloatTextureContexts = floatTextureContexts;

            // Build the buffer desc to send to component
            var buffersDesc = new List<VFXBufferDesc>();
            foreach (var attribBuffer in shaderMetaData.attributeBuffers)
            {
                VFXBufferDesc bufferDesc = new VFXBufferDesc();

                int structSize = attribBuffer.GetSizeInBytes();
                if (structSize == 12)
                    structSize = 16;
                bufferDesc.size = (uint)structSize;

                string bufferName = "attribBuffer" + attribBuffer.Index;
                if (attribBuffer.Used(VFXContextDesc.Type.kTypeInit))
                    bufferDesc.initName = bufferName + (attribBuffer.Writable(VFXContextDesc.Type.kTypeInit) ? "" : "_RO");
                if (attribBuffer.Used(VFXContextDesc.Type.kTypeUpdate) || (outputBuffer != null && attribBuffer.Used(VFXContextDesc.Type.kTypeOutput)))
                    bufferDesc.updateName = bufferName + (attribBuffer.Writable(VFXContextDesc.Type.kTypeUpdate) ? "" : "_RO");
                if (outputBuffer == null && attribBuffer.Used(VFXContextDesc.Type.kTypeOutput))
                    bufferDesc.outputName = bufferName;

                buffersDesc.Add(bufferDesc);
            }

            rtData.buffersDesc = buffersDesc.ToArray();

            // Add uniforms mapping
            rtData.uniforms = shaderMetaData.paramToName;
            rtData.outputUniforms = shaderMetaData.outputParamToName;

            rtData.m_RawExpressions = rawExpressions;

            return rtData;
        }

        public static Dictionary<VFXExpression, VFXExpression> GenerateExtraUniforms(List<VFXBlockModel> blocks, SpaceRef spaceRef)
        {
            var generated = new Dictionary<VFXExpression, VFXExpression>();

            List<VFXNamedValue> collectedValues = new List<VFXNamedValue>();
            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.Properties.Length; ++i)
                {
                    collectedValues.Clear();
                    block.GetSlot(i).CollectNamedValues(collectedValues,spaceRef);
                    foreach (var arg in collectedValues)
                        if (arg.m_Value.IsValue() && arg.m_Value.ValueType == VFXValueType.kTransform && block.Desc.IsSet(VFXBlockDesc.Flag.kNeedsInverseTransform))
                        {
                            var inverseValue = new VFXExpressionInverseTRS(arg.m_Value);
                            generated.Add(arg.m_Value,inverseValue);
                        }
                }

            return generated;
        }

        public static HashSet<VFXExpression> CollectUniforms(List<VFXBlockModel> blocks, SpaceRef spaceRef)
        {
            HashSet<VFXExpression> uniforms = new HashSet<VFXExpression>();

            List<VFXNamedValue> collectedValues = new List<VFXNamedValue>();
            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.Properties.Length; ++i)
                {
                    collectedValues.Clear();
                    block.GetSlot(i).CollectNamedValues(collectedValues,spaceRef);
                    foreach (var arg in collectedValues)
                        if (arg.m_Value.IsValue())
                            uniforms.Add(arg.m_Value);
                }

            return uniforms;
        }

        public static HashSet<VFXExpression> CollectAndRemoveSamplers(HashSet<VFXExpression> uniforms)
        {
            HashSet<VFXExpression> samplers = new HashSet<VFXExpression>();

            // Collect samplers
            foreach (var param in uniforms)
                if (param.ValueType == VFXValueType.kTexture2D || param.ValueType == VFXValueType.kTexture3D)
                    samplers.Add(param);

            // Remove samplers from uniforms
            foreach (var param in samplers)
                uniforms.Remove(param);

            return samplers;
        }

        public static bool HasValueOfType(HashSet<VFXExpression> uniforms,VFXValueType type)
        {
            // Collect samplers
            foreach (var param in uniforms)
                if (param.ValueType == type)
                    return true;

            return false;
        }

        public static HashSet<VFXExpression> CollectIntersection(HashSet<VFXExpression> params0, HashSet<VFXExpression> params1)
        {
            HashSet<VFXExpression> globalParams = new HashSet<VFXExpression>();

            foreach (VFXExpression param in params0)
                if (params1.Contains(param))
                    globalParams.Add(param);

            foreach (VFXExpression param in globalParams)
            {
                params0.Remove(param);
                params1.Remove(param);
            }

            return globalParams;
        }

        public static void GenerateParamNames(Dictionary<VFXExpression, string> paramToName, HashSet<VFXExpression> parameters, string name)
        {
            int counter = 0;
            foreach (var param in parameters)
                if (!paramToName.ContainsKey(param))
                {
                    string fullName = name + counter;
                    paramToName.Add(param, fullName);
                    ++counter;
                }
        }

        // Collect all attributes from blocks and fills them in attribs
        public static void CollectAttributes(Dictionary<VFXAttribute, VFXAttribute.Usage> attribs, List<VFXBlockModel> blocks, VFXContextDesc.Type context)
        {
            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.Attributes.Length; ++i)
                {
                    VFXAttribute attr = block.Desc.Attributes[i];
                    VFXAttribute.Usage usage;
                    attribs.TryGetValue(attr, out usage);
                    VFXAttribute.Usage currentUsage = VFXAttribute.ContextToUsage(context, attr.m_Writable);
                    attribs[attr] = usage | currentUsage;
                }
        }

        private static string WriteComputeShader(ShaderMetaData data,VFXShaderGeneratorModule initGenerator,VFXShaderGeneratorModule updateGenerator)
        {
            int NB_THREAD_PER_GROUP = 256;
            if (data.system.MaxNb < NB_THREAD_PER_GROUP)
                NB_THREAD_PER_GROUP = (int)data.system.MaxNb;

            bool hasInit = initGenerator != null; //data.initBlocks.Count > 0;
            bool hasUpdate = updateGenerator != null; //data.updateBlocks.Count > 0;

            ShaderSourceBuilder builder = new ShaderSourceBuilder();

            if (hasInit)
                builder.WriteLine("#pragma kernel CSVFXInit");
            if (hasUpdate)
                builder.WriteLine("#pragma kernel CSVFXUpdate");
            builder.WriteLine();

            //builder.WriteLine("#include \"UnityCG.cginc\"");
            builder.WriteLine("#include \"HLSLSupport.cginc\"");
            builder.WriteLine("#include \"../VFXCommon.cginc\"");
            builder.WriteLine();

            builder.Write("#define NB_THREADS_PER_GROUP ");
            builder.Write(NB_THREAD_PER_GROUP);
            builder.WriteLine();
            builder.WriteLine();

            builder.WriteLine("CBUFFER_START(GlobalInfo)");
            builder.WriteLine("\tfloat deltaTime;");
            builder.WriteLine("\tfloat totalTime;");
            builder.WriteLine("\tuint nbMax;");
            if (data.hasRand)
                builder.WriteLine("\tuint systemSeed;");
            builder.WriteLine("CBUFFER_END");
            builder.WriteLine();

            if (hasInit)
            {
                builder.WriteLine("CBUFFER_START(SpawnInfo)");
                builder.WriteLine("\tuint nbSpawned;");
                builder.WriteLine("\tuint spawnIndex;");
                builder.WriteLine("CBUFFER_END");
                builder.WriteLine();
            } 

            // Uniforms buffer
            builder.WriteCBuffer("GlobalUniforms", data.globalUniforms, data.paramToName);
            builder.WriteCBuffer("initUniforms", data.initUniforms, data.paramToName);
            builder.WriteCBuffer("updateUniforms", data.updateUniforms, data.paramToName);

            // Write samplers
            builder.WriteSamplers(data.globalSamplers, data.paramToName);
            builder.WriteSamplers(data.initSamplers, data.paramToName);
            builder.WriteSamplers(data.updateSamplers, data.paramToName);

            // Write generated texture samplers
            if ((data.colorTextureContexts & VFXContextDesc.Type.kInitAndUpdate) != 0)
                builder.WriteSampler(VFXValueType.kTexture2D, "gradientTexture");
            if ((data.floatTextureContexts & VFXContextDesc.Type.kInitAndUpdate) != 0)
                builder.WriteSampler(VFXValueType.kTexture2D, "curveTexture");

            // Write attribute struct
            foreach (var attribBuffer in data.attributeBuffers)
                builder.WriteAttributeBuffer(attribBuffer);

            // Write attribute buffer
            foreach (var attribBuffer in data.attributeBuffers)
            {
                builder.Write("RWStructuredBuffer<Attribute");
                builder.Write(attribBuffer.Index);
                builder.Write("> attribBuffer");
                builder.Write(attribBuffer.Index);
                builder.WriteLine(";");

                if (!attribBuffer.Writable(VFXContextDesc.Type.kTypeUpdate) && (attribBuffer.Used(VFXContextDesc.Type.kTypeUpdate) || (data.UseOutputData() && attribBuffer.Used(VFXContextDesc.Type.kTypeOutput))))
                {
                    builder.Write("StructuredBuffer<Attribute");
                    builder.Write(attribBuffer.Index);
                    builder.Write("> attribBuffer");
                    builder.Write(attribBuffer.Index);
                    builder.WriteLine("_RO;");
                }
            }
            if (data.attributeBuffers.Count > 0)
                builder.WriteLine();

            if (data.UseOutputData())
            {
                builder.WriteAttributeBuffer(data.outputBuffer, true);
                builder.WriteLine("AppendStructuredBuffer<OutputData> outputBuffer;");
                builder.WriteLine();
            }

            // Write deadlists
            if (data.hasKill)
            {
                builder.WriteLine("RWStructuredBuffer<int> flags;");
                builder.WriteLine("ConsumeStructuredBuffer<uint> deadListIn;");
                builder.WriteLine("AppendStructuredBuffer<uint> deadListOut;");
                builder.WriteLine("Buffer<uint> deadListCount; // This is bad to use a SRV to fetch deadList count but Unity API currently prevent from copying to CB");
                builder.WriteLine();
            }

            builder.WriteLine("RWStructuredBuffer<uint3> bounds;");
            builder.WriteLine();

            // Write functions
            if (data.hasRand)
            {
                builder.WriteLine("float rand(inout uint seed)");
                builder.EnterScope();

                builder.WriteLine("seed = 1664525 * seed + 1013904223;");
                builder.WriteLine("return float(seed) / 4294967296.0;");

                // XOR Style 
                /*
                builder.WriteLine("seed ^= (seed << 13);");
                builder.WriteLine("seed ^= (seed >> 17);");
                builder.WriteLine("seed ^= (seed << 5);");
                builder.WriteLine("return float(seed) / 4294967296.0;");
                */

                builder.ExitScope();
                builder.WriteLine();
            }

            if ((data.colorTextureContexts & VFXContextDesc.Type.kInitAndUpdate) != 0)
            {
                data.generatedTextureData.WriteSampleGradientFunction(builder);
                builder.WriteLine();
            }

            if ((data.floatTextureContexts & VFXContextDesc.Type.kInitAndUpdate) != 0)
            {
                data.generatedTextureData.WriteSampleCurveFunction(builder);
                builder.WriteLine();
            }

            if (hasUpdate && USE_DYNAMIC_AABB)
            {
                builder.WriteLine("groupshared uint3 boundsLDS[2];");
                builder.WriteLine();
            }

            var functionNames = new HashSet<string>();
            foreach (var block in data.initBlocks)
                builder.WriteFunction(block, functionNames, data.generatedTextureData);
            foreach (var block in data.updateBlocks)
                builder.WriteFunction(block, functionNames, data.generatedTextureData);

            if (initGenerator != null)
                initGenerator.WriteFunctions(builder, data);
            if (updateGenerator != null)
                updateGenerator.WriteFunctions(builder, data);

            bool HasPhaseShift = VFXEditor.Graph.systems.PhaseShift;

            // Write init kernel
            if (hasInit)
            {
                builder.WriteKernelHeader("CSVFXInit");
                if (data.hasKill)
                    builder.WriteLine("if (id.x < min(nbSpawned,deadListCount[0]))");
                else
                    builder.WriteLine("if (id.x < nbSpawned)");
                builder.EnterScope();
                if (data.hasKill)
                    builder.WriteLine("uint index = deadListIn.Consume();");
                else
                    builder.WriteLine("uint index = id.x + spawnIndex;");
                builder.WriteLine();

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    builder.Write("Attribute");
                    builder.Write(attribBuffer.Index);
                    builder.Write(" attrib");
                    builder.Write(attribBuffer.Index);              

                    // TODO tmp
                    // Initialize to avoid warning as error while compiling
                    builder.Write(" = (Attribute");
                    builder.Write(attribBuffer.Index);
                    builder.WriteLine(")0;");
                }
                builder.WriteLine();

                builder.WriteLocalAttribDeclaration(data, VFXContextDesc.Type.kTypeInit);

                foreach (var sampler in data.initSamplers)
                    builder.WriteInitVFXSampler(sampler.ValueType, data.paramToName[sampler]);
                foreach (var sampler in data.globalSamplers)
                    builder.WriteInitVFXSampler(sampler.ValueType, data.paramToName[sampler]);
                builder.WriteLine();

                // Init random
                if (data.hasRand)
                {
                    // Find rand attribute
                    builder.WriteLine("uint seed = (id.x + spawnIndex) ^ systemSeed;");
                    builder.WriteLine("seed = (seed ^ 61) ^ (seed >> 16);");
                    builder.WriteLine("seed *= 9;");
                    builder.WriteLine("seed = seed ^ (seed >> 4);");
                    builder.WriteLine("seed *= 0x27d4eb2d;");
                    builder.WriteLine("seed = seed ^ (seed >> 15);");
                    builder.WriteAttrib(CommonAttrib.Seed, data);
                    builder.WriteLine(" = seed;");
                    builder.WriteLine();
                }

                if (data.HasAttribute(CommonAttrib.ParticleId))
                {
                    builder.WriteAttrib(CommonAttrib.ParticleId, data);
                    builder.WriteLine(" = spawnIndex + id.x;");
                }

                // Init phase
                if (HasPhaseShift)
                {
                    builder.WriteAttrib(CommonAttrib.Phase, data);
                    builder.Write(" = rand(");
                    builder.WriteAttrib(CommonAttrib.Seed, data);
                    builder.WriteLine(");");
                    builder.WriteLine();
                }

                initGenerator.WritePreBlock(builder, data);
                
                foreach (var block in data.initBlocks)
                    builder.WriteFunctionCall(block, functionNames, data, false);
                builder.WriteLine();

                initGenerator.WritePostBlock(builder, data);

                // Remove phase shift
                if (HasPhaseShift)
                {
                    builder.WriteRemovePhaseShift(data);
                    builder.WriteLine();
                }

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    builder.Write("attribBuffer");
                    builder.Write(attribBuffer.Index);
                    builder.Write("[index] = attrib");
                    builder.Write(attribBuffer.Index);
                    builder.WriteLine(";");
                }

                if (data.hasKill)
                {
                    builder.WriteLine();
                    builder.WriteLine("flags[index] = 1;");
                }

                builder.ExitScope();
                builder.ExitScope();
                builder.WriteLine();
            }

            // Write update kernel
            if (hasUpdate)
            {
                builder.WriteKernelHeader("CSVFXUpdate");

                if (USE_DYNAMIC_AABB)
                {
                    builder.WriteLine("if (groupId.x == 0)");
                    builder.EnterScope();

                    builder.WriteLine("boundsLDS[0] = (uint3)0xFFFFFFFF;");
                    builder.WriteLine("boundsLDS[1] = (uint3)0;");

                    builder.ExitScope();
                    builder.WriteLine();

                    builder.WriteLine("GroupMemoryBarrierWithGroupSync();");
                    builder.WriteLine();
                }

                builder.Write("if (id.x < nbMax");
                if (data.hasKill)
                    builder.WriteLine(" && flags[id.x] == 1)");
                else
                    builder.WriteLine(")");
                builder.EnterScope();
                builder.WriteLine("uint index = id.x;");

                if (data.hasKill)
                    builder.WriteLine("bool kill = false;");

                builder.WriteLine();
         
                foreach (var attribBuffer in data.attributeBuffers)
                {
                    if (attribBuffer.Used(VFXContextDesc.Type.kTypeUpdate) || (data.UseOutputData() && attribBuffer.Used(VFXContextDesc.Type.kTypeOutput)))
                    {
                        builder.Write("Attribute");
                        builder.Write(attribBuffer.Index);
                        builder.Write(" attrib");
                        builder.Write(attribBuffer.Index);
                        builder.Write(" = attribBuffer");
                        builder.Write(attribBuffer.Index);
                        if (!attribBuffer.Writable(VFXContextDesc.Type.kTypeUpdate))
                            builder.Write("_RO");
                        builder.WriteLine("[index];");
                    }
                }
                builder.WriteLine();

                foreach (var sampler in data.updateSamplers)
                    builder.WriteInitVFXSampler(sampler.ValueType, data.paramToName[sampler]);
                foreach (var sampler in data.globalSamplers)
                    builder.WriteInitVFXSampler(sampler.ValueType, data.paramToName[sampler]);
                builder.WriteLine();

                builder.WriteLocalAttribDeclaration(data, VFXContextDesc.Type.kTypeUpdate);

                // Add phase shift
                if (HasPhaseShift)
                {
                    builder.WriteAddPhaseShift(data);
                    builder.WriteLine();
                }

                updateGenerator.WritePreBlock(builder, data);

                foreach (var block in data.updateBlocks)
                    builder.WriteFunctionCall(block, functionNames, data, false);
                builder.WriteLine(); 

                updateGenerator.WritePostBlock(builder, data);

                // Remove phase shift
                if (HasPhaseShift)
                {
                    builder.WriteRemovePhaseShift(data);
                    builder.WriteLine();
                }

                if (data.hasKill)
                {
                    builder.WriteLine("if (kill)");
                    builder.EnterScope();
                    builder.WriteLine("flags[index] = 0;");
                    builder.WriteLine("deadListOut.Append(index);");
                    builder.ExitScope();

                    builder.WriteLine("else");
                    builder.EnterScope();
                }

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    if (attribBuffer.Writable(VFXContextDesc.Type.kTypeUpdate))
                    {
                        builder.Write("attribBuffer");
                        builder.Write(attribBuffer.Index);
                        builder.Write("[index] = attrib");
                        builder.Write(attribBuffer.Index);
                        builder.WriteLine(";");
                    }
                }

                // Needs to push outputData to buffer
                if (data.UseOutputData())
                {
                    builder.WriteLine();
                    builder.WriteLine("OutputData outputData = (OutputData)0;");
                    for (int i = 0; i < data.outputBuffer.Count; ++i)
                    {
                        VFXAttribute attrib = data.outputBuffer[i];
                        builder.WriteFormat("outputData.{0} = ",attrib.m_Name);
                        builder.WriteAttrib(attrib, data);
                        builder.WriteLine(";");
                    }
                    builder.WriteLine("outputBuffer.Append(outputData);");
                }

                if (USE_DYNAMIC_AABB)
                {
                    builder.WriteLine();
                    builder.Write("uint3 sortablePos = ConvertFloatToSortableUint(");
                    builder.WriteAttrib(CommonAttrib.Position, data);
                    builder.WriteLine(");");
                    builder.WriteLine();
                    builder.WriteLine("InterlockedMin(boundsLDS[0].x,sortablePos.x);");
                    builder.WriteLine("InterlockedMin(boundsLDS[0].y,sortablePos.y);");
                    builder.WriteLine("InterlockedMin(boundsLDS[0].z,sortablePos.z);");
                    builder.WriteLine();
                    builder.WriteLine("InterlockedMax(boundsLDS[1].x,sortablePos.x);");
                    builder.WriteLine("InterlockedMax(boundsLDS[1].y,sortablePos.y);");
                    builder.WriteLine("InterlockedMax(boundsLDS[1].z,sortablePos.z);");    
                }

                if (data.hasKill)
                    builder.ExitScope();

                builder.ExitScope();

                if (USE_DYNAMIC_AABB)
                {
                    builder.WriteLine();
                    builder.WriteLine("GroupMemoryBarrierWithGroupSync();");
                    builder.WriteLine();

                    builder.WriteLine("if (groupId.x == 0)");
                    builder.EnterScope();
                    builder.WriteLine("InterlockedMin(bounds[0].x,boundsLDS[0].x);");
                    builder.WriteLine("InterlockedMin(bounds[0].y,boundsLDS[0].y);");
                    builder.WriteLine("InterlockedMin(bounds[0].z,boundsLDS[0].z);");
                    builder.WriteLine();
                    builder.WriteLine("InterlockedMax(bounds[1].x,boundsLDS[1].x);");
                    builder.WriteLine("InterlockedMax(bounds[1].y,boundsLDS[1].y);");
                    builder.WriteLine("InterlockedMax(bounds[1].z,boundsLDS[1].z);");
                    builder.ExitScope();
                }

                builder.ExitScope();
                builder.WriteLine();
            }

            return builder.ToString();
        }

        private static string WriteOutputShader(VFXSystemModel system, ShaderMetaData data, VFXOutputShaderGeneratorModule outputGenerator)
        {
            ShaderSourceBuilder builder = new ShaderSourceBuilder();

            builder.Write("Shader \"Hidden/VFX_");
            builder.Write(system.Id);
            builder.WriteLine("\"");
            builder.EnterScope();
            builder.WriteLine("SubShader");
            builder.EnterScope();

            BlendMode blendMode = system.BlendingMode;

            if (blendMode != BlendMode.kMasked && blendMode != BlendMode.kDithered)
            {
                string offset = system.RenderQueueDelta == 0 ? "" : (system.RenderQueueDelta > 0 ? "+" : "-") + Mathf.Abs(system.RenderQueueDelta) ;
                builder.WriteLine("Tags { \"Queue\"=\"Transparent"+offset+"\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\" }");
            }
            builder.WriteLine("Pass");
            builder.EnterScope();
            if (outputGenerator.CanUseDeferred())
                builder.WriteLine("Tags { \"LightMode\" = \"Deferred\" }");
            if (blendMode == BlendMode.kAdditive)
                builder.WriteLine("Blend SrcAlpha One");
            else if (blendMode == BlendMode.kAlpha)
                builder.WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
            builder.WriteLine("ZTest LEqual");
            if (blendMode == BlendMode.kMasked || blendMode == BlendMode.kDithered)
                builder.WriteLine("ZWrite On");
            else
                builder.WriteLine("ZWrite Off");
            builder.WriteLine("Cull Off");
            builder.WriteLine();
            builder.WriteLine("CGPROGRAM");
            builder.WriteLine("#pragma target 5.0");
            builder.WriteLine();
            builder.WriteLine("#pragma vertex vert");
            builder.WriteLine("#pragma fragment frag");
            builder.WriteLine();

            if (system.WorldSpace)
                builder.WriteLine("#define VFX_WORLD_SPACE");
            else
                builder.WriteLine("#define VFX_LOCAL_SPACE");

            builder.WriteLine();
            builder.WriteLine("#include \"UnityCG.cginc\"");
            builder.WriteLine("#include \"UnityStandardUtils.cginc\"");
            builder.WriteLine("#include \"HLSLSupport.cginc\"");
            builder.WriteLine("#include \"../VFXCommon.cginc\"");
            builder.WriteLine();

            builder.WriteCBuffer("outputUniforms", data.outputUniforms, data.outputParamToName);
            builder.WriteSamplers(data.outputSamplers, data.outputParamToName);

            // Write generated texture samplers
            if ((data.colorTextureContexts & VFXContextDesc.Type.kTypeOutput) != 0)
                builder.WriteSampler(VFXValueType.kTexture2D,"gradientTexture");
            if ((data.floatTextureContexts & VFXContextDesc.Type.kTypeOutput) != 0)
                builder.WriteSampler(VFXValueType.kTexture2D, "curveTexture");

            if (system.HasSoftParticles())
            {
                builder.WriteLine("sampler2D_float _CameraDepthTexture;");
                builder.WriteLine();
            }

            if (data.UseOutputData())
            {
                builder.WriteAttributeBuffer(data.outputBuffer, true);
                builder.WriteLine("StructuredBuffer<OutputData> outputBuffer;");
            }
            else
            {
                foreach (AttributeBuffer buffer in data.attributeBuffers)
                    if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                        builder.WriteAttributeBuffer(buffer);

                foreach (AttributeBuffer buffer in data.attributeBuffers)
                    if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                    {
                        builder.Write("StructuredBuffer<Attribute");
                        builder.Write(buffer.Index);
                        builder.Write("> attribBuffer");
                        builder.Write(buffer.Index);
                        builder.WriteLine(";");
                    }
            }

            if (data.hasKill && !data.UseOutputData())
                builder.WriteLine("StructuredBuffer<int> flags;");

            builder.WriteLine();
            builder.WriteLine("struct ps_input");
            builder.EnterScope();
            builder.WriteLine("/*linear noperspective centroid*/ float4 pos : SV_POSITION;");

            bool hasColor = data.HasAttribute(CommonAttrib.Color);
            bool hasAlpha = data.HasAttribute(CommonAttrib.Alpha);
            bool needsVertexColor = hasColor || hasAlpha;

            if (needsVertexColor)
                builder.WriteLine("nointerpolation float4 col : COLOR0;");

            outputGenerator.WriteAdditionalVertexOutput(builder, data);

            if (system.HasSoftParticles())
                builder.WriteLine("float4 projPos : TEXCOORD2;"); // TODO use a counter to set texcoord index

            builder.ExitScopeStruct();
            builder.WriteLine();

            if ((data.colorTextureContexts & VFXContextDesc.Type.kTypeOutput) != 0)
            {
                data.generatedTextureData.WriteSampleGradientFunction(builder);
                builder.WriteLine();
            }

            if ((data.floatTextureContexts & VFXContextDesc.Type.kTypeOutput) != 0)
            {
                data.generatedTextureData.WriteSampleCurveFunction(builder);
                builder.WriteLine();
            }

            var functionNames = new HashSet<string>();
            foreach (var block in data.outputBlocks)
                builder.WriteFunction(block, functionNames, data.generatedTextureData);

            outputGenerator.WriteFunctions(builder, data);

            builder.WriteLine("ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)");
            builder.EnterScope();
            builder.WriteLine("ps_input o;");

            outputGenerator.WriteIndex(builder, data);

            if (data.hasKill && !data.UseOutputData())
            {
                builder.WriteLine("if (flags[index] == 1)");
                builder.EnterScope();
            }

            if (data.hasCull)
            {
                builder.WriteLine("bool kill = false;");
                builder.WriteLine();
            }

            if (data.UseOutputData())
                builder.WriteLine("OutputData outputData = outputBuffer[index];");
            else
            {
                foreach (var buffer in data.attributeBuffers)
                    if (buffer.Used(VFXContextDesc.Type.kTypeOutput))
                    {
                        builder.Write("Attribute");
                        builder.Write(buffer.Index);
                        builder.Write(" attrib");
                        builder.Write(buffer.Index);
                        builder.Write(" = attribBuffer");
                        builder.Write(buffer.Index);
                        builder.WriteLine("[index];");
                    }
            }
            builder.WriteLine();

            builder.WriteLocalAttribDeclaration(data, VFXContextDesc.Type.kTypeOutput);

            outputGenerator.WritePreBlock(builder, data);

            foreach (var block in data.outputBlocks)
                builder.WriteFunctionCall(block, functionNames, data, true);
            builder.WriteLine();

            outputGenerator.WritePostBlock(builder, data);

            // Soft particles
            if (system.HasSoftParticles())
                builder.WriteLine("o.projPos = ComputeScreenPos(o.pos); // For depth texture fetch");

            if (needsVertexColor)
            {
                builder.Write("o.col = float4(");

                if (hasColor)
                {
                    builder.WriteAttrib(CommonAttrib.Color, data, true);
                    builder.Write(".xyz,");
                }
                else
                    builder.Write("1.0,1.0,1.0,");

                if (hasAlpha)
                {
                    builder.WriteAttrib(CommonAttrib.Alpha, data, true);
                    builder.WriteLine(");");
                }
                else
                    builder.WriteLine("0.5);");
            }

            if (data.hasCull)
            {
                builder.WriteLine("if (kill)");
                builder.EnterScope();
                builder.WriteLine("o.pos = -1.0;");
                if (hasColor)
                    builder.WriteLine("o.col = 0;");
                builder.ExitScope();
            }

            if (data.hasKill && !data.UseOutputData())
            {
                // clip the vertex if not alive
                builder.ExitScope();
                builder.WriteLine("else");
                builder.EnterScope();
                builder.WriteLine("o.pos = -1.0;");

                if (hasColor)
                    builder.WriteLine("o.col = 0;");

                builder.ExitScope();
                builder.WriteLine();
            }

            builder.WriteLine("return o;");
            builder.ExitScope();
            builder.WriteLine();

            builder.WriteLine("struct ps_output");
            builder.EnterScope();
            builder.WriteLine("float4 col : SV_Target0;");
            outputGenerator.WriteAdditionalPixelOutput(builder, data);
            builder.ExitScopeStruct();
            builder.WriteLine();

            builder.WriteLine("ps_output frag (ps_input i)");

            builder.EnterScope();

            builder.WriteLine("ps_output o = (ps_output)0;");
            builder.WriteLine();

            if (hasColor || hasAlpha)
                builder.WriteLine("float4 color = i.col;");
            else
                builder.WriteLine("float4 color = float4(1.0,1.0,1.0,0.5);");

            outputGenerator.WritePixelShader(builder, data);

            // Soft particles
            if (system.HasSoftParticles())
            {
                builder.WriteLine();
                builder.WriteLine("// Soft particles");
                builder.WriteFormat("const float INV_FADE_DISTANCE = {0};",(1.0f / system.SoftParticlesFadeDistance));
                builder.WriteLine();
                builder.WriteLine("float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));");
                builder.WriteLine("float fade = saturate(INV_FADE_DISTANCE * (sceneZ - i.projPos.w));");
                builder.WriteLine("fade = fade * fade * (3.0 - (2.0 * fade)); // Smoothsteping the fade");

                // NVIDIA Piecewise function 
                //builder.WriteLine("float output = 0.5 * pow(saturate(2*(( fade > 0.5) ? 1-fade : fade)),1); ");
                //builder.WriteLine("fade = ( fade > 0.5) ? 1-output : output;");

                builder.WriteLine("color.a *= fade;");
            }

            builder.WriteLine();
            builder.WriteLine("o.col = color;");
            builder.WriteLine("return o;");

            builder.ExitScope();
            builder.WriteLine();
            builder.WriteLine("ENDCG");
            builder.ExitScope();
            builder.ExitScope();
            builder.WriteLine("FallBack Off");
            builder.ExitScope();

            return builder.ToString();
        }
    }
}
