using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public static class CommonAttrib
    {
        public static VFXAttrib Seed =              new VFXAttrib("seed", VFXParam.Type.kTypeUint);
        public static VFXAttrib Position =          new VFXAttrib("position", VFXParam.Type.kTypeFloat3);
        public static VFXAttrib Velocity =          new VFXAttrib("velocity", VFXParam.Type.kTypeFloat3);
        public static VFXAttrib Color =             new VFXAttrib("color", VFXParam.Type.kTypeFloat3);
        public static VFXAttrib Alpha =             new VFXAttrib("alpha", VFXParam.Type.kTypeFloat);
        public static VFXAttrib Phase =             new VFXAttrib("phase", VFXParam.Type.kTypeFloat);
        public static VFXAttrib Size =              new VFXAttrib("size", VFXParam.Type.kTypeFloat2);
        public static VFXAttrib Lifetime =          new VFXAttrib("lifetime", VFXParam.Type.kTypeFloat);
        public static VFXAttrib Age =               new VFXAttrib("age", VFXParam.Type.kTypeFloat);
        public static VFXAttrib Angle =             new VFXAttrib("angle", VFXParam.Type.kTypeFloat);
        public static VFXAttrib AngularVelocity =   new VFXAttrib("angularVelocity", VFXParam.Type.kTypeFloat);
        public static VFXAttrib TexIndex =          new VFXAttrib("texIndex", VFXParam.Type.kTypeFloat);
    }

    public class VFXSystemRuntimeData
    {
        public Dictionary<VFXParamValue,string> uniforms = new Dictionary<VFXParamValue,string>();
        public Dictionary<VFXParamValue, string> outputUniforms = new Dictionary<VFXParamValue, string>();
        
        ComputeShader simulationShader;
        public ComputeShader SimulationShader { get { return simulationShader; } }

        public Material m_Material = null;

        int initKernel = -1;
        public int InitKernel { get { return initKernel; } }
        int updateKernel = -1;
        public int UpdateKernel { get { return updateKernel; } }

        public uint outputType; // tmp value to pass to C++

        private List<ComputeBuffer> buffers = new List<ComputeBuffer>();

        public VFXSystemRuntimeData(ComputeShader shader)
        {
            simulationShader = shader;

            // FindKernel throws instead of setting value to -1
            try { initKernel = simulationShader.FindKernel("CSVFXInit"); }
            catch(Exception e) { initKernel = -1; }
            try { updateKernel = simulationShader.FindKernel("CSVFXUpdate"); }
            catch(Exception e) { updateKernel = -1; }
        }

        public void AddBuffer(int kernelIndex, string name, ComputeBuffer buffer)
        {
            simulationShader.SetBuffer(kernelIndex,name,buffer);
            buffers.Add(buffer);
        }

        public void DisposeBuffers()
        {
            foreach (var buffer in buffers)
            {
                VFXEditor.Log("Dispose buffer "+buffer.ToString());
                buffer.Release();
                buffer.Dispose();
            }
        }

        public void UpdateAllUniforms()
        {
            foreach (var uniform in uniforms)
                UpdateUniform(uniform.Key,false);

            foreach (var uniform in outputUniforms)
                UpdateUniform(uniform.Key, true);
        }

        public void UpdateUniform(VFXParamValue paramValue,bool output)
        {
            var currentUniforms = uniforms;
            if (output)
                currentUniforms = outputUniforms;

            string uniformName = currentUniforms[paramValue];
            switch (paramValue.ValueType)
            {
                case VFXParam.Type.kTypeFloat:
                    if (output)
                        m_Material.SetFloat(uniformName, paramValue.GetValue<float>());
                    else
                        simulationShader.SetFloat(uniformName,paramValue.GetValue<float>());
                    break;
                case VFXParam.Type.kTypeFloat2:
                {
                    Vector2 value = paramValue.GetValue<Vector2>();
                    if (output)
                        m_Material.SetVector(uniformName, value);
                    else
                    {
                        float[] buffer = new float[2];                        
                        buffer[0] = value.x;
                        buffer[1] = value.y;
                        simulationShader.SetFloats(uniformName,buffer);  
                    }
                    break;
                }
                case VFXParam.Type.kTypeFloat3:
                {     
                    Vector3 value = paramValue.GetValue<Vector3>();
                    if (output)
                        m_Material.SetVector(uniformName, value);
                    else
                    {
                        float[] buffer = new float[3];
                        buffer[0] = value.x;
                        buffer[1] = value.y;
                        buffer[2] = value.z;
                        simulationShader.SetFloats(uniformName, buffer);
                    }
                    break;
                }
                case VFXParam.Type.kTypeFloat4:
                    if (output)
                        m_Material.SetVector(uniformName, paramValue.GetValue<Vector4>());
                    else
                        simulationShader.SetVector(uniformName,paramValue.GetValue<Vector4>());
                    break;
                case VFXParam.Type.kTypeInt:
                    if (output)
                        m_Material.SetInt(uniformName, paramValue.GetValue<int>());
                    else
                        simulationShader.SetInt(uniformName,paramValue.GetValue<int>());
                    break;
                case VFXParam.Type.kTypeUint:
                    if (output)
                        m_Material.SetInt(uniformName, (int)paramValue.GetValue<uint>());
                    else
                        simulationShader.SetInt(uniformName,(int)paramValue.GetValue<uint>());
                    break;

                case VFXParam.Type.kTypeTexture2D:
                {
                    Texture2D tex = paramValue.GetValue<Texture2D>();
                    if (tex != null)
                    {
                        if (output)
                            m_Material.SetTexture(uniformName, tex);
                        else
                        {
                            bool inInit = uniformName.Contains("init");
                            bool inUpdate = uniformName.Contains("update");
                            if (uniformName.Contains("global"))
                                inInit = inUpdate = true;

                            if (inInit)
                                simulationShader.SetTexture(initKernel, uniformName, tex);
                            if (inUpdate)
                                simulationShader.SetTexture(updateKernel, uniformName, tex);
                        }
                    }

                    break;
                }

                case VFXParam.Type.kTypeTexture3D: // Texture 3D not handled yet
                case VFXParam.Type.kTypeUnknown:
                    // Not yet implemented
                    break;
            }
        }
    }

    public class AttributeBuffer
    {
        public AttributeBuffer(int index, int usage)
        {
            m_Index = index;
            m_Usage = usage;
            m_Attribs = new List<VFXAttrib>();
        }

        public void Add(VFXAttrib attrib)
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
        public int Usage
        {
            get { return m_Usage; }
        }

        // return usage per pass
        public int MergedUsage
        {
            get { return ((m_Usage & 0xAA >> 1) | m_Usage) & 0x55; }
        }

        public int Count
        {
            get { return m_Attribs.Count; }
        }

        public VFXAttrib this[int index]
        {
            get { return m_Attribs[index]; }
        }

        public bool Used(VFXContextDesc.Type type)
        {
            return (m_Usage & (0x3 << (((int)type - 1) * 2))) != 0;
        }

        public bool Writable(VFXContextDesc.Type type)
        {
            return (m_Usage & (0x2 << (((int)type - 1) * 2))) != 0;
        }

        public int GetSizeInBytes()
        {
            int size = 0;
            foreach (VFXAttrib attrib in m_Attribs)
                size += VFXParam.GetSizeFromType(attrib.m_Param.m_Type) * 4;
            return size;
        }

        int m_Index;
        int m_Usage;
        List<VFXAttrib> m_Attribs;
    }

    public class AttribComparer : IEqualityComparer<VFXAttrib>
    {
        public bool Equals(VFXAttrib attr0, VFXAttrib attr1)
        {
            return attr0.m_Param.m_Name == attr1.m_Param.m_Name && attr0.m_Param.m_Type == attr1.m_Param.m_Type;
        }

        public int GetHashCode(VFXAttrib attr)
        {
            return 13 * attr.m_Param.m_Name.GetHashCode() + attr.m_Param.m_Type.GetHashCode(); // Simple factored sum
        }
    }

    public class ShaderMetaData
    {
        public List<VFXBlockModel> initBlocks = new List<VFXBlockModel>();
        public List<VFXBlockModel> updateBlocks = new List<VFXBlockModel>();

        public bool hasKill;
        public bool hasRand;

        public List<AttributeBuffer> attributeBuffers = new List<AttributeBuffer>();
        public Dictionary<VFXAttrib, AttributeBuffer> attribToBuffer = new Dictionary<VFXAttrib, AttributeBuffer>(new AttribComparer());

        public HashSet<VFXParamValue> globalUniforms = new HashSet<VFXParamValue>();
        public HashSet<VFXParamValue> initUniforms = new HashSet<VFXParamValue>();
        public HashSet<VFXParamValue> updateUniforms = new HashSet<VFXParamValue>();

        public HashSet<VFXParamValue> globalSamplers = new HashSet<VFXParamValue>();
        public HashSet<VFXParamValue> initSamplers = new HashSet<VFXParamValue>();
        public HashSet<VFXParamValue> updateSamplers = new HashSet<VFXParamValue>();

        public HashSet<VFXParamValue> outputUniforms = new HashSet<VFXParamValue>();
        public HashSet<VFXParamValue> outputSamplers = new HashSet<VFXParamValue>();

        public Dictionary<VFXParamValue, string> paramToName = new Dictionary<VFXParamValue, string>();
        public Dictionary<VFXParamValue, string> outputParamToName = new Dictionary<VFXParamValue, string>();
    }

    public static class VFXModelCompiler
    {
        public static VFXSystemRuntimeData CompileSystem(VFXSystemModel system)
        {
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

            if (outputGenerator == null || initGenerator == null || updateGenerator == null) // Tmp: we need the 3 contexts atm
                return null;

            // BLOCKS
            List<VFXBlockModel> initBlocks = new List<VFXBlockModel>();
            List<VFXBlockModel> updateBlocks = new List<VFXBlockModel>();
            bool initHasRand = false;
            bool updateHasRand = false;
            bool updateHasKill = false;

            // Collapses the contexts into one big init and update
            for (int i = 0; i < system.GetNbChildren(); ++i)
            {
                VFXContextModel context = system.GetChild(i);

                List<VFXBlockModel> currentList = null; ;
                switch (context.GetContextType())
                {
                    case VFXContextDesc.Type.kTypeInit: currentList = initBlocks; break;
                    case VFXContextDesc.Type.kTypeUpdate: currentList = updateBlocks; break;
                }

                if (currentList == null)
                    continue;

                bool hasRand = false;
                bool hasKill = false;
                for (int j = 0; j < context.GetNbChildren(); ++j)
                {
                    VFXBlockModel blockModel = context.GetChild(j);
                    hasRand |= (blockModel.Desc.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0;
                    hasKill |= (blockModel.Desc.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0;
                    currentList.Add(blockModel);
                }

                switch (context.GetContextType())
                {
                    case VFXContextDesc.Type.kTypeInit: initHasRand |= hasRand; break;
                    case VFXContextDesc.Type.kTypeUpdate: 
                        updateHasRand |= hasRand;
                        updateHasKill |= hasKill;
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
            Dictionary<VFXAttrib, int> attribs = new Dictionary<VFXAttrib, int>(new AttribComparer());

            CollectAttributes(attribs, initBlocks, 0);
            CollectAttributes(attribs, updateBlocks, 1);
 
            if (VFXEditor.AssetModel.PhaseShift)     
            {
                if (attribs.ContainsKey(CommonAttrib.Position) && attribs.ContainsKey(CommonAttrib.Velocity))
                {
                    attribs[CommonAttrib.Phase] = 0x7; // Add phase attribute   
                    attribs[CommonAttrib.Position] = attribs[CommonAttrib.Position] | 0xF; // Ensure position is writable in init and update
                    attribs[CommonAttrib.Velocity] = attribs[CommonAttrib.Velocity] | 0x7; // Ensure velocity is readable in init and update

                    initHasRand = true; // phase needs rand as initialization
                }
                else
                {
                    VFXEditor.AssetModel.PhaseShift = false;
                    return null;
                }
            }

            // Update flags with generators
            int initGeneratorFlags = 0;
            if (!initGenerator.UpdateAttributes(attribs, ref initGeneratorFlags))
                return null;

            initHasRand |= (initGeneratorFlags & (int)VFXBlock.Flag.kHasRand) != 0;

            int updateGeneratorFlags = 0;
            if (!updateGenerator.UpdateAttributes(attribs, ref updateGeneratorFlags))
                return null;

            updateHasRand |= (updateGeneratorFlags & (int)VFXBlock.Flag.kHasRand) != 0;
            updateHasKill |= (updateGeneratorFlags & (int)VFXBlock.Flag.kHasKill) != 0;

            int dummy = 0;
            if (!outputGenerator.UpdateAttributes(attribs, ref dummy))
                return null;

            // Add the seed attribute in case we need PRG
            if (initHasRand || updateHasRand)
            {
                updateHasRand = true;
                attribs[CommonAttrib.Seed] = (initHasRand ? 0x3 : 0x0) | (updateHasRand ? 0xC : 0x0);
            }

            // Find unitialized attribs and remove 
            List<VFXAttrib> unitializedAttribs = new List<VFXAttrib>(); 
            foreach (var attrib in attribs)
            {
                if ((attrib.Value & 0x3) == 0) // Unitialized attribute
                {
                    if (attrib.Key.m_Param.m_Name != "seed" || attrib.Key.m_Param.m_Name != "age") // Dont log anything for those as initialization is implicit
                        VFXEditor.Log("WARNING: " + attrib.Key.m_Param.m_Name + " is not initialized. Use default value");
                    unitializedAttribs.Add(attrib.Key);
                }
                // TODO attrib to remove (when written and never used for instance) ! But must also remove blocks using them...
            }

            // Update the usage
            foreach (var attrib in unitializedAttribs)
                attribs[attrib] = attribs[attrib] | 0x3;

            // Sort attrib by usage and by size
            List<AttributeBuffer> buffers = VFXAttributePacker.Pack(attribs,6);

            if (buffers.Count > 6)
            {
                // TODO : Merge appropriate buffers in that case
                VFXEditor.Log("ERROR: too many buffers used (max is 6 + 2 reserved)");
                return null;
            }

            // Associate attrib to buffer
            var attribToBuffer = new Dictionary<VFXAttrib, AttributeBuffer>(new AttribComparer());
            foreach (var buffer in buffers)
                for (int i = 0; i < buffer.Count; ++i)
                    attribToBuffer.Add(buffer[i], buffer);

            VFXEditor.Log("Nb Attributes : " + attribs.Count);
            VFXEditor.Log("Nb Attribute buffers: " + buffers.Count);
            for (int i = 0; i < buffers.Count; ++i)
            {
                string str = "\t " + i + " |";
                for (int j = 0; j < buffers[i].Count; ++j)
                {
                    str += buffers[i][j].m_Param.m_Name + "|";
                }
                str += " " + buffers[i].GetSizeInBytes() + "bytes";
                VFXEditor.Log(str);
            }
                
            // UNIFORMS
            HashSet<VFXParamValue> initUniforms = CollectUniforms(initBlocks);
            initGenerator.UpdateUniforms(initUniforms);
            HashSet<VFXParamValue> updateUniforms = CollectUniforms(updateBlocks);
            updateGenerator.UpdateUniforms(updateUniforms);

            // collect samplers
            HashSet<VFXParamValue> initSamplers = CollectAndRemoveSamplers(initUniforms);
            HashSet<VFXParamValue> updateSamplers = CollectAndRemoveSamplers(updateUniforms);

            // Collect the intersection between init and update uniforms / samplers
            HashSet<VFXParamValue> globalUniforms = CollectIntersection(initUniforms,updateUniforms);
            HashSet<VFXParamValue> globalSamplers = CollectIntersection(initSamplers, updateSamplers);

            // Output stuff
            HashSet<VFXParamValue> outputUniforms = new HashSet<VFXParamValue>();
            outputGenerator.UpdateUniforms(outputUniforms);
            HashSet<VFXParamValue> outputSamplers = CollectAndRemoveSamplers(outputUniforms);

            // Associate VFXParamValue to generated name
            var paramToName = new Dictionary<VFXParamValue, string>();
            GenerateParamNames(paramToName, globalUniforms, "globalUniform");
            GenerateParamNames(paramToName, initUniforms, "initUniform");
            GenerateParamNames(paramToName, updateUniforms, "updateUniform");

            GenerateParamNames(paramToName, globalSamplers, "globalSampler");
            GenerateParamNames(paramToName, initSamplers, "initSampler");
            GenerateParamNames(paramToName, updateSamplers, "updateSampler");

            var outputParamToName = new Dictionary<VFXParamValue, string>();
            GenerateParamNames(outputParamToName, outputUniforms, "outputUniform");
            GenerateParamNames(outputParamToName, outputSamplers, "outputSampler");

            // Log result
            VFXEditor.Log("Nb init blocks: " + initBlocks.Count);
            VFXEditor.Log("Nb update blocks: " + updateBlocks.Count);
            VFXEditor.Log("Nb global uniforms: " + globalUniforms.Count);
            VFXEditor.Log("Nb init uniforms: " + initUniforms.Count);
            VFXEditor.Log("Nb update uniforms: " + updateUniforms.Count);

            ShaderMetaData shaderMetaData = new ShaderMetaData();
            shaderMetaData.initBlocks = initBlocks;
            shaderMetaData.updateBlocks = updateBlocks;
            shaderMetaData.hasRand = initHasRand || updateHasRand;
            shaderMetaData.hasKill = updateHasKill;
            shaderMetaData.attributeBuffers = buffers;
            shaderMetaData.attribToBuffer = attribToBuffer;
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
   
            string shaderSource = WriteComputeShader(shaderMetaData,initGenerator,updateGenerator);
            string outputShaderSource = WriteOutputShader(shaderMetaData,outputGenerator);
           
            VFXEditor.Log("\n**** SHADER CODE ****");
            VFXEditor.Log(shaderSource);
            VFXEditor.Log(outputShaderSource);
            VFXEditor.Log("\n*********************");

            // Write to file
            string shaderPath = Application.dataPath + "/VFXEditor/Generated/";
            System.IO.Directory.CreateDirectory(shaderPath);
            System.IO.File.WriteAllText(shaderPath + "VFX.compute", shaderSource);
            System.IO.File.WriteAllText(shaderPath + "VFX.shader", outputShaderSource);

            ComputeShader simulationShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/VFXEditor/Generated/VFX.compute");
            Shader outputShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/VFXEditor/Generated/VFX.shader");
            AssetDatabase.Refresh();

            VFXSystemRuntimeData rtData = new VFXSystemRuntimeData(simulationShader);

            rtData.m_Material = new Material(outputShader);

            // Create buffer for system
            foreach (var attribBuffer in shaderMetaData.attributeBuffers)
            {
                string bufferName = "attribBuffer" + attribBuffer.Index;
                int structSize = attribBuffer.GetSizeInBytes();
                if (structSize == 12)
                    structSize = 16;
                ComputeBuffer computeBuffer = new ComputeBuffer(1 << 20, structSize, ComputeBufferType.GPUMemory);
                if (attribBuffer.Used(VFXContextDesc.Type.kTypeInit))
                    rtData.AddBuffer(rtData.InitKernel,bufferName + (attribBuffer.Writable(VFXContextDesc.Type.kTypeInit) ? "" : "_RO"),computeBuffer);
                if (attribBuffer.Used(VFXContextDesc.Type.kTypeUpdate))
                    rtData.AddBuffer(rtData.UpdateKernel, bufferName + (attribBuffer.Writable(VFXContextDesc.Type.kTypeUpdate) ? "" : "_RO"), computeBuffer);
                if (attribBuffer.Used(VFXContextDesc.Type.kTypeOutput))
                    rtData.m_Material.SetBuffer(bufferName, computeBuffer);
            }

            rtData.outputType = outputGenerator.GetSingleIndexBuffer(shaderMetaData) != null ? 1u : 0u; // This is temp

            if (shaderMetaData.hasKill)
            {
                ComputeBuffer flagBuffer = new ComputeBuffer(1 << 20,4, ComputeBufferType.GPUMemory);
                ComputeBuffer deadList = new ComputeBuffer(1 << 20, 4, ComputeBufferType.Append);

                const int NB_PARTICLES = 1 << 20;
                uint[] deadIdx = new uint[NB_PARTICLES];
                for (int i = 0; i < NB_PARTICLES; ++i)
                {
                    deadIdx[i] = NB_PARTICLES - (uint)i - 1;
                }
                deadList.SetData(deadIdx);
                deadList.SetCounterValue((uint)NB_PARTICLES);

                if (rtData.InitKernel != -1)
                {
                    rtData.AddBuffer(rtData.InitKernel, "flags", flagBuffer);
                    rtData.AddBuffer(rtData.InitKernel, "deadListIn", deadList);
                }
                if (rtData.UpdateKernel != -1)
                {
                    rtData.AddBuffer(rtData.UpdateKernel,"flags",flagBuffer);
                    rtData.AddBuffer(rtData.UpdateKernel, "deadListOut", deadList);
                }

                // bind flags to vertex shader
                rtData.m_Material.SetBuffer("flags", flagBuffer);
            }

            // Add uniforms mapping
            rtData.uniforms = shaderMetaData.paramToName;
            rtData.outputUniforms = shaderMetaData.outputParamToName;

            // Finally set uniforms
            rtData.UpdateAllUniforms();

            return rtData;
        }

        public static HashSet<VFXParamValue> CollectUniforms(List<VFXBlockModel> blocks)
        {
            HashSet<VFXParamValue> uniforms = new HashSet<VFXParamValue>();

            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.m_Params.Length; ++i)
                    uniforms.Add(block.GetParamValue(i));

            return uniforms;
        }

        public static HashSet<VFXParamValue> CollectAndRemoveSamplers(HashSet<VFXParamValue> uniforms)
        {
            HashSet<VFXParamValue> samplers = new HashSet<VFXParamValue>();

            // Collect samplers
            foreach (var param in uniforms)
                if (param.ValueType == VFXParam.Type.kTypeTexture2D || param.ValueType == VFXParam.Type.kTypeTexture3D)
                    samplers.Add(param);

            // Remove samplers from uniforms
            foreach (var param in samplers)
                uniforms.Remove(param);

            return samplers;
        }

        public static HashSet<VFXParamValue> CollectIntersection(HashSet<VFXParamValue> params0,HashSet<VFXParamValue> params1)
        {
            HashSet<VFXParamValue> globalParams = new HashSet<VFXParamValue>();

            foreach (VFXParamValue param in params0)
                if (params1.Contains(param))
                    globalParams.Add(param);

            foreach (VFXParamValue param in globalParams)
            {
                params0.Remove(param);
                params1.Remove(param);
            }

            return globalParams;
        }

        public static void GenerateParamNames(Dictionary<VFXParamValue, string> paramToName, HashSet<VFXParamValue> parameters, string name)
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
        public static void CollectAttributes(Dictionary<VFXAttrib, int> attribs, List<VFXBlockModel> blocks, int index)
        {
            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.m_Attribs.Length; ++i)
                {
                    VFXAttrib attr = block.Desc.m_Attribs[i];
                    int usage;
                    attribs.TryGetValue(attr, out usage);
                    int currentUsage = (0x1 | (attr.m_Writable ? 0x2 : 0x0)) << (index * 2);
                    attribs[attr] = usage | currentUsage;
                }
        }

        private static string WriteComputeShader(ShaderMetaData data,VFXShaderGeneratorModule initGenerator,VFXShaderGeneratorModule updateGenerator)
        {
            const int NB_THREAD_PER_GROUP = 256;

            bool hasInit = initGenerator != null; //data.initBlocks.Count > 0;
            bool hasUpdate = updateGenerator != null; //data.updateBlocks.Count > 0;

            ShaderSourceBuilder builder = new ShaderSourceBuilder();

            if (hasInit)
                builder.WriteLine("#pragma kernel CSVFXInit");
            if (hasUpdate)
                builder.WriteLine("#pragma kernel CSVFXUpdate");
            builder.WriteLine();

            builder.Write("#define NB_THREADS_PER_GROUP ");
            builder.Write(NB_THREAD_PER_GROUP);
            builder.WriteLine();
            builder.WriteLine();

            builder.WriteLine("#include \"UnityCG.cginc\"");
            builder.WriteLine("#include \"HLSLSupport.cginc\"");
            builder.WriteLine();

            builder.WriteLine("CBUFFER_START(GlobalInfo)");
            builder.WriteLine("\tfloat deltaTime;");
            builder.WriteLine("\tfloat totalTime;");
            builder.WriteLine("\tuint nbMax;");
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

                if (attribBuffer.Used(VFXContextDesc.Type.kTypeUpdate) && !attribBuffer.Writable(VFXContextDesc.Type.kTypeUpdate))
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

            // Write deadlists
            if (data.hasKill)
            {
                builder.WriteLine("RWStructuredBuffer<int> flags;");
                builder.WriteLine("ConsumeStructuredBuffer<uint> deadListIn;");
                builder.WriteLine("AppendStructuredBuffer<uint> deadListOut;");
                builder.WriteLine("Buffer<uint> deadListCount; // This is bad to use a SRV to fetch deadList count but Unity API currently prevent from copying to CB");
                builder.WriteLine();
            }

            // Write functions
            if (data.hasRand)
            {
                builder.WriteLine("float rand(inout uint seed)");
                builder.EnterScope();
                builder.WriteLine("seed = 1664525 * seed + 1013904223;");
                builder.WriteLine("return float(seed) / 4294967296.0;");
                builder.ExitScope();
                builder.WriteLine();

                // XOR Style 
                /*
                builder.WriteLine("float rand(inout uint seed)");
                builder.EnterScope();
                builder.WriteLine("seed ^= (seed << 13);");
                builder.WriteLine("seed ^= (seed >> 17);");
                builder.WriteLine("seed ^= (seed << 5);");
                builder.WriteLine("return float(seed) / 4294967296.0;");
                builder.ExitScope();
                builder.WriteLine();
                */
            }

            var functionNames = new Dictionary<Hash128,string>();
            foreach (var block in data.initBlocks)
                builder.WriteFunction(block, functionNames);
            foreach (var block in data.updateBlocks)
                builder.WriteFunction(block, functionNames);

            bool HasPhaseShift = VFXEditor.AssetModel.PhaseShift;

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

                // Init random
                if (data.hasRand)
                {
                    // Find rand attribute
                    builder.WriteLine("uint seed = id.x + spawnIndex;");
                    builder.WriteLine("seed = (seed ^ 61) ^ (seed >> 16);");
                    builder.WriteLine("seed *= 9;");
                    builder.WriteLine("seed = seed ^ (seed >> 4);");
                    builder.WriteLine("seed *= 0x27d4eb2d;");
                    builder.WriteLine("seed = seed ^ (seed >> 15);");
                    builder.WriteAttrib(CommonAttrib.Seed, data);
                    builder.WriteLine(" = seed;");
                    builder.WriteLine();
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
                    builder.WriteFunctionCall(block, functionNames, data);
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
                    if (attribBuffer.Used(VFXContextDesc.Type.kTypeUpdate))
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

                // Add phase shift
                if (HasPhaseShift)
                {
                    builder.WriteAddPhaseShift(data);
                    builder.WriteLine();
                }

                updateGenerator.WritePreBlock(builder, data);

                foreach (var block in data.updateBlocks)
                    builder.WriteFunctionCall(block, functionNames, data);
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
                    builder.WriteLine("return;");
                    builder.ExitScope();
                    builder.WriteLine();
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

                builder.ExitScope();
                builder.ExitScope();
                builder.WriteLine();
            }

            return builder.ToString();
        }

        private static string WriteOutputShader(ShaderMetaData data,VFXOutputShaderGeneratorModule outputGenerator)
        {
            ShaderSourceBuilder builder = new ShaderSourceBuilder();

            builder.WriteLine("Shader \"Custom/PointShader\""); // TODO Rename that
            builder.EnterScope();
            builder.WriteLine("\tSubShader");
            builder.EnterScope();

            BlendMode blendMode = VFXEditor.AssetModel.BlendingMode;

            if (blendMode != BlendMode.kMasked)
                builder.WriteLine("Tags { \"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\" }");
            builder.WriteLine("Pass");
            builder.EnterScope();
            if (blendMode == BlendMode.kAdditive)
                builder.WriteLine("Blend SrcAlpha One");
            else if (blendMode == BlendMode.kAlpha)
                builder.WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
            builder.WriteLine("ZTest LEqual");
            if (blendMode == BlendMode.kMasked)
                builder.WriteLine("ZWrite On");
            else
                builder.WriteLine("ZWrite Off");
            builder.WriteLine("CGPROGRAM");
            builder.WriteLine("#pragma target 5.0");
            builder.WriteLine();
            builder.WriteLine("#pragma vertex vert");
            builder.WriteLine("#pragma fragment frag");
            builder.WriteLine();
            builder.WriteLine("#include \"UnityCG.cginc\"");
            builder.WriteLine();

            builder.WriteCBuffer("outputUniforms", data.outputUniforms, data.outputParamToName);
            builder.WriteSamplers(data.outputSamplers, data.outputParamToName);

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

            if (data.hasKill)
                builder.WriteLine("StructuredBuffer<int> flags;");

            builder.WriteLine();
            builder.WriteLine("struct ps_input");
            builder.EnterScope();
            builder.WriteLine("float4 pos : SV_POSITION;");

            bool hasColor = data.attribToBuffer.ContainsKey(CommonAttrib.Color);
            bool hasAlpha = data.attribToBuffer.ContainsKey(CommonAttrib.Alpha);

            if (hasColor || hasAlpha)
                builder.WriteLine("nointerpolation float4 col : COLOR0;");

            outputGenerator.WriteAdditionalVertexOutput(builder, data);

            builder.ExitScopeStruct();
            builder.WriteLine();
            builder.WriteLine("ps_input vert (uint id : SV_VertexID, uint instanceID : SV_InstanceID)");
            builder.EnterScope();
            builder.WriteLine("ps_input o;");

            outputGenerator.WriteIndex(builder, data);

            if (data.hasKill)
            {
                builder.WriteLine("if (flags[index] == 1)");
                builder.EnterScope();
            }

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
            builder.WriteLine();

            outputGenerator.WritePreBlock(builder, data);
            outputGenerator.WritePostBlock(builder, data);

            if (hasColor || hasAlpha)
            {
                builder.Write("o.col = float4(");

                if (hasColor)
                {
                    builder.WriteAttrib(CommonAttrib.Color, data);
                    builder.Write(".xyz,");
                }
                else
                    builder.Write("1.0,1.0,1.0,");

                if (hasAlpha)
                {
                    builder.WriteAttrib(CommonAttrib.Alpha, data);
                    builder.WriteLine(");");
                }
                else
                    builder.WriteLine("0.5);");
            }

            if (data.hasKill)
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
            builder.WriteLine("float4 frag (ps_input i) : COLOR");
            builder.EnterScope();

            if (hasColor || hasAlpha)
                builder.WriteLine("float4 color = i.col;");
            else
                builder.WriteLine("float4 color = float4(1.0,1.0,1.0,0.5);");

            outputGenerator.WritePixelShader(builder, data);

            builder.WriteLine("return color;");

            builder.ExitScope();
            builder.WriteLine();
            builder.WriteLine("\t\t\tENDCG");
            builder.ExitScope();
            builder.ExitScope();
            builder.WriteLine("\tFallBack Off");
            builder.ExitScope();

            return builder.ToString();
        }
    }
}