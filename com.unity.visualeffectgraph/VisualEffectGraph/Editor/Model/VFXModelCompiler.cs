using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    public class VFXSystemRuntimeData
    {
        public Dictionary<VFXParamValue,string> uniforms = new Dictionary<VFXParamValue,string>();
        
        ComputeShader simulationShader;
        public ComputeShader SimulationShader { get { return simulationShader; } }

        public Material m_Material = null;

        int initKernel = -1;
        public int InitKernel { get { return initKernel; } }
        int updateKernel = -1;
        public int UpdateKernel { get { return updateKernel; } }

        private List<ComputeBuffer> buffers = new List<ComputeBuffer>();

        public VFXSystemRuntimeData(ComputeShader shader)
        {
            simulationShader = shader;
            initKernel = simulationShader.FindKernel("CSVFXInit");
            updateKernel = simulationShader.FindKernel("CSVFXUpdate");
        }

        public void AddBuffer(int kernelIndex, string name, ComputeBuffer buffer)
        {
            simulationShader.SetBuffer(kernelIndex,name,buffer);
            buffers.Add(buffer);
        }

        public void DisposeBuffers()
        {
            foreach (var buffer in buffers)
                buffer.Dispose();
        }

        public void UpdateAllUniforms()
        {
            foreach (var uniform in uniforms)
                UpdateUniform(uniform.Key);
        }

        public void UpdateUniform(VFXParamValue paramValue)
        {
            string uniformName = uniforms[paramValue];
            switch (paramValue.ValueType)
            {
                case VFXParam.Type.kTypeFloat:
                    simulationShader.SetFloat(uniformName,paramValue.GetValue<float>());
                    break;
                case VFXParam.Type.kTypeFloat2:
                {
                    float[] buffer = new float[2];
                    Vector2 value = paramValue.GetValue<Vector2>();
                    buffer[0] = value.x;
                    buffer[1] = value.y;
                    simulationShader.SetFloats(uniformName,buffer);
                    break;
                }
                case VFXParam.Type.kTypeFloat3:
                {
                    float[] buffer = new float[3];
                    Vector3 value = paramValue.GetValue<Vector3>();
                    buffer[0] = value.x;
                    buffer[1] = value.y;
                    buffer[2] = value.z;
                    simulationShader.SetFloats(uniformName,buffer);
                    break;
                }
                case VFXParam.Type.kTypeFloat4:
                    simulationShader.SetVector(uniformName,paramValue.GetValue<Vector4>());
                    break;
                case VFXParam.Type.kTypeInt:
                    simulationShader.SetInt(uniformName,paramValue.GetValue<int>());
                    break;
                case VFXParam.Type.kTypeUint:
                    simulationShader.SetInt(uniformName,(int)paramValue.GetValue<uint>());
                    break;

                case VFXParam.Type.kTypeTexture2D:
                case VFXParam.Type.kTypeTexture3D:
                case VFXParam.Type.kTypeUnknown:
                    // Not yet implemented
                    break;
            }
        }
    }

    public static class VFXModelCompiler
    {
        private class AttribComparer : IEqualityComparer<VFXAttrib>
        {
            public bool Equals(VFXAttrib attr0,VFXAttrib attr1)
            {
                return attr0.m_Param.m_Name == attr1.m_Param.m_Name && attr0.m_Param.m_Type == attr1.m_Param.m_Type;
            }

            public int GetHashCode(VFXAttrib attr)
            {
                return 13 * attr.m_Param.m_Name.GetHashCode() + attr.m_Param.m_Type.GetHashCode(); // Simple factored sum
            }
        }

        private class AttributeBuffer
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

            public int Index
            {
                get { return m_Index; }
            }

            public int Usage
            {
                get { return m_Usage; }
            }

            public int Count
            {
                get { return m_Attribs.Count; }
            }

            public VFXAttrib this[int index]
            {
                get { return m_Attribs[index]; }
            }

            public bool Used(VFXContextModel.Type type)
            {
                return (m_Usage & (0x3 << (((int)type - 1) * 2))) != 0;
            }

            public bool Writable(VFXContextModel.Type type) 
            {
                return (m_Usage & (0x2 << (((int)type - 1) * 2))) != 0;
            }

            public int GetSizeInBytes()
            {
                int size = 0;
                foreach (VFXAttrib attrib in m_Attribs)
                    size += VFXParam.GetSizeFromType(attrib.m_Param.m_Type);
                return size;
            }

            int m_Index;
            int m_Usage;
            List<VFXAttrib> m_Attribs;
        }

        public static VFXSystemRuntimeData CompileSystem(VFXSystemModel system)
        {
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
                    case VFXContextModel.Type.kTypeInit: currentList = initBlocks; break;
                    case VFXContextModel.Type.kTypeUpdate: currentList = updateBlocks; break;
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
                    case VFXContextModel.Type.kTypeInit: initHasRand |= hasRand; break;
                    case VFXContextModel.Type.kTypeUpdate: 
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

            // Add the seed attribute in case we need PRG
            if (initHasRand || updateHasRand)
            {
                VFXAttrib seedAttrib = new VFXAttrib();
                VFXParam seedParam = new VFXParam();
                seedParam.m_Name = "seed";
                seedParam.m_Type = VFXParam.Type.kTypeInt;
                seedAttrib.m_Param = seedParam;
                seedAttrib.m_Writable = true;

                attribs[seedAttrib] = (initHasRand ? 0x3 : 0x0) | (updateHasRand ? 0xC : 0x0);
            }

            CollectAttributes(attribs, initBlocks, 0);
            CollectAttributes(attribs, updateBlocks, 1);

            // Find unitialized attribs and remove 
            List<VFXAttrib> unitializedAttribs = new List<VFXAttrib>(); 
            foreach (var attrib in attribs)
            {
                if ((attrib.Value & 0x3) == 0) // Unitialized attribute
                {
                    if (attrib.Key.m_Param.m_Name != "seed" && attrib.Key.m_Param.m_Name != "age") // Dont log anything for those as initialization is implicit
                        VFXEditor.Log("WARNING: " + attrib.Key.m_Param.m_Name + " is not initialized. Use default value");
                    unitializedAttribs.Add(attrib.Key);
                }
                // TODO attrib to remove (when written and never used for instance) ! But must also remove blocks using them...
            }

            // Update the usage
            foreach (var attrib in unitializedAttribs)
                attribs[attrib] = attribs[attrib] | 0x3;

            // Sort attrib by usage and by size
            var sortedAttribs = new Dictionary<int,List<VFXAttrib>[]>();
            foreach (var attrib in attribs)
            {
                List<VFXAttrib>[] attribsForUsage;
                sortedAttribs.TryGetValue(attrib.Value, out attribsForUsage);

                if (attribsForUsage == null) // Not yet initialized
                {
                    attribsForUsage = new List<VFXAttrib>[4];
                    for (int i = 0; i < 4; ++i) // Asumming sizes cannot be more than 4 bytes
                        attribsForUsage[i] = new List<VFXAttrib>();

                    sortedAttribs[attrib.Value] = attribsForUsage;
                }

                int sizeInBytes = VFXParam.GetSizeFromType(attrib.Key.m_Param.m_Type);
                attribsForUsage[sizeInBytes - 1].Add(attrib.Key);
            }

            // Derive SOA based on usage with optimal size of 16 bytes
            var buffers = new List<AttributeBuffer>();
            int currentBufferIndex = 0;
            foreach (var attribsByUsage in sortedAttribs)
            {
                // handle 16 bytes attrib
                var currentAttribs = attribsByUsage.Value[3];
                int index = currentAttribs.Count - 1;
                while (index >= 0)
                {
                    var buffer = new AttributeBuffer(currentBufferIndex++,attribsByUsage.Key);
                    buffer.Add(currentAttribs[index]);
                    buffers.Add(buffer);
                    currentAttribs.RemoveAt(index--);
                }

                // try to pair 12 bytes data with 4 bytes
                currentAttribs = attribsByUsage.Value[2];
                var pairedAttribs = attribsByUsage.Value[0];
                index = currentAttribs.Count - 1;
                while (index >= 0)
                {
                    var buffer = new AttributeBuffer(currentBufferIndex++, attribsByUsage.Key);
                    buffer.Add(currentAttribs[index]);
                    buffers.Add(buffer);
                    currentAttribs.RemoveAt(index--);

                    if (pairedAttribs.Count > 0)
                    {
                        buffer.Add(pairedAttribs[pairedAttribs.Count - 1]);
                        pairedAttribs.RemoveAt(pairedAttribs.Count - 1);
                    } 
                }

                // try to pair 8 bytes data with 8 bytes data or with 2 4 bytes
                currentAttribs = attribsByUsage.Value[1];
                pairedAttribs = attribsByUsage.Value[0];
                index = currentAttribs.Count - 1;
                while (index >= 0)
                {
                    var buffer = new AttributeBuffer(currentBufferIndex++, attribsByUsage.Key);
                    buffer.Add(currentAttribs[index]);
                    buffers.Add(buffer);
                    currentAttribs.RemoveAt(index--);
                   
                    if (index > 0) // pair with 8 bytes
                    {
                        buffer.Add(currentAttribs[index]);
                        currentAttribs.RemoveAt(index--);   
                    }
                    else if (pairedAttribs.Count >= 2) // pair with 2 4 bytes
                    {
                        buffer.Add(pairedAttribs[pairedAttribs.Count - 1]);
                        buffer.Add(pairedAttribs[pairedAttribs.Count - 2]);
                        pairedAttribs.RemoveAt(pairedAttribs.Count - 1);
                        pairedAttribs.RemoveAt(pairedAttribs.Count - 1);
                    }
                }

                // Finally pack 4 bytes data together
                currentAttribs = attribsByUsage.Value[0];
                index = currentAttribs.Count - 1;
                int currentCount = 0;
                AttributeBuffer currentBuffer = null;                    
                while (index >= 0)
                {
                    if (currentBuffer == null)
                        currentBuffer = new AttributeBuffer(currentBufferIndex++, attribsByUsage.Key);

                    currentBuffer.Add(currentAttribs[index]);
                    currentAttribs.RemoveAt(index--);
                    ++currentCount;

                    if (currentCount == 4 || index < 0)
                    {
                        buffers.Add(currentBuffer);
                        currentBuffer = null;
                        currentCount = 0;
                    }
                }
            }

            // TODO Try to merge R and RW buffers used in the same context in case of holes
            // for instance, for a given context flag
            // R : X -> 1 byte
            // RW : XXX0 -> 3 bytes
            // => Merge this to one buffer

            if (buffers.Count > 7)
            {
                // TODO : Merge appropriate buffers in that case
                VFXEditor.Log("ERROR: too many buffers used (max is 7 + 1 reserved)");
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
                str += " " + (buffers[i].GetSizeInBytes() * 4) + "bytes";
                VFXEditor.Log(str);
            }
                
            // UNIFORMS
            HashSet<VFXParamValue> initUniforms = CollectUniforms(initBlocks);
            HashSet<VFXParamValue> updateUniforms = CollectUniforms(updateBlocks);

            // Collect the intersection between init and update uniforms
            HashSet<VFXParamValue> globalUniforms = new HashSet<VFXParamValue>();
            
            foreach (VFXParamValue uniform in initUniforms)
                if (updateUniforms.Contains(uniform))
                    globalUniforms.Add(uniform);

            foreach (VFXParamValue uniform in globalUniforms)
            {
                initUniforms.Remove(uniform);
                updateUniforms.Remove(uniform);
            }

            // Associate VFXParamValue to generated name
            var paramToName = new Dictionary<VFXParamValue, string>();
            GenerateUniformName(paramToName, globalUniforms, "global");
            GenerateUniformName(paramToName, initUniforms, "init");
            GenerateUniformName(paramToName, updateUniforms, "update");

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
            shaderMetaData.paramToName = paramToName;
   
            string shaderSource = WriteComputeShader(shaderMetaData);
            string outputShaderSource = WriteOutputShader(shaderMetaData);
           
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
            VFXSystemRuntimeData rtData = new VFXSystemRuntimeData(simulationShader);

            // Find position buffer
            VFXAttrib posAttrib = new VFXAttrib();
            VFXParam param = new VFXParam();
            param.m_Name = "position";
            param.m_Type = VFXParam.Type.kTypeFloat3;
            posAttrib.m_Param = param;

            AttributeBuffer posBuffer = null;
            attribToBuffer.TryGetValue(posAttrib,out posBuffer);

            if (posBuffer == null || outputShader == null) // No position buffer, we escape
                return null;

            rtData.m_Material = new Material(outputShader);

            // Create buffer for system
            foreach (var attribBuffer in shaderMetaData.attributeBuffers)
            {
                string bufferName = "attribBuffer" + attribBuffer.Index;
                int structSize = attribBuffer.GetSizeInBytes();
                if (structSize == 3)
                    structSize = 4;
                ComputeBuffer computeBuffer = new ComputeBuffer(1 << 20, structSize * 4, ComputeBufferType.GPUMemory);
                if (attribBuffer.Used(VFXContextModel.Type.kTypeInit))
                    rtData.AddBuffer(rtData.InitKernel,bufferName + (attribBuffer.Writable(VFXContextModel.Type.kTypeInit) ? "" : "_RO"),computeBuffer);
                if (attribBuffer.Used(VFXContextModel.Type.kTypeUpdate))
                    rtData.AddBuffer(rtData.UpdateKernel, bufferName + (attribBuffer.Writable(VFXContextModel.Type.kTypeUpdate) ? "" : "_RO"), computeBuffer);
                if (attribBuffer == posBuffer)
                    rtData.m_Material.SetBuffer("pointBuffer", computeBuffer);
                //computeBuffer.Dispose();
            }

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
            }

            // Add uniforms mapping
            rtData.uniforms = shaderMetaData.paramToName;

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

        public static void GenerateUniformName(Dictionary<VFXParamValue, string> paramToName, HashSet<VFXParamValue> uniforms, string prefix)
        {
            int counter = 0;
            foreach (var uniform in uniforms)
            {
                string name = prefix + "Uniform" + counter;
                paramToName.Add(uniform, name);
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

        private class ShaderMetaData
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
     
            public Dictionary<VFXParamValue, string> paramToName = new Dictionary<VFXParamValue, string>();
        }

        private static string WriteOutputShader(ShaderMetaData data)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.AppendLine("Shader \"Custom/PointShader\"");
            buffer.AppendLine("{");
	        buffer.AppendLine("\tSubShader"); 
	        buffer.AppendLine("\t{");	
		    buffer.AppendLine("\t\tPass");
		    buffer.AppendLine("\t\t{");
			buffer.AppendLine("\t\t\tCGPROGRAM");
			buffer.AppendLine("\t\t\t#pragma target 5.0");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\t#pragma vertex vert");
			buffer.AppendLine("\t\t\t#pragma fragment frag");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\t#include \"UnityCG.cginc\"");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\tStructuredBuffer<float4> pointBuffer;");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\tstruct ps_input {");
			buffer.AppendLine("\t\t\t float4 pos : SV_POSITION;");
			buffer.AppendLine("\t\t\t float4 col : COLOR0;");
			buffer.AppendLine("\t\t\t};");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\tps_input vert (uint id : SV_VertexID)");
			buffer.AppendLine("\t\t\t{");
			buffer.AppendLine("\t\t\tps_input o;");
			buffer.AppendLine("\t\t\tfloat3 worldPos = pointBuffer[id].xyz;");
			buffer.AppendLine("\t\t\to.pos = mul (UNITY_MATRIX_VP, float4(worldPos,1.0f));");
			buffer.AppendLine("\t\t\to.col = float4(1,1,1,1);");
			buffer.AppendLine("\t\t\treturn o;");
			buffer.AppendLine("\t\t\t}");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\t//Pixel function returns a solid color for each point.");
			buffer.AppendLine("\t\t\tfloat4 frag (ps_input i) : COLOR");
			buffer.AppendLine("\t\t\t{");
			buffer.AppendLine("\t\t\t return i.col;");
			buffer.AppendLine("\t\t\t}");
            buffer.AppendLine();
			buffer.AppendLine("\t\t\tENDCG");
		    buffer.AppendLine("\t\t}");
	        buffer.AppendLine("\t}");
	        buffer.AppendLine("\tFallBack Off");
            buffer.AppendLine("}");

            return buffer.ToString();
        }

        private static string WriteComputeShader(ShaderMetaData data)
        {
            const int NB_THREAD_PER_GROUP = 256;

            bool hasInit = data.initBlocks.Count > 0;
            bool hasUpdate = data.updateBlocks.Count > 0;

            StringBuilder buffer = new StringBuilder();

            if (hasInit)
                buffer.AppendLine("#pragma kernel CSVFXInit");
            if (hasUpdate)
                buffer.AppendLine("#pragma kernel CSVFXUpdate");
            buffer.AppendLine();
            
            buffer.Append("#define NB_THREADS_PER_GROUP ");
            buffer.Append(NB_THREAD_PER_GROUP);
            buffer.AppendLine();
            buffer.AppendLine();
            
            buffer.AppendLine("#include \"HLSLSupport.cginc\"");
            buffer.AppendLine();

            buffer.AppendLine("CBUFFER_START(GlobalInfo)");
            buffer.AppendLine("\tfloat deltaTime;");
            buffer.AppendLine("\tuint nbMax;");
            buffer.AppendLine("CBUFFER_END");
            buffer.AppendLine();

            if (hasInit)
            {
                buffer.AppendLine("CBUFFER_START(SpawnInfo)");
                buffer.AppendLine("\tuint nbSpawned;");
                buffer.AppendLine("CBUFFER_END");
                buffer.AppendLine();
            } 

            // Uniforms buffer
            WriteCBuffer(buffer, "GlobalUniforms", data.globalUniforms, data.paramToName);
            WriteCBuffer(buffer, "initUniforms", data.initUniforms, data.paramToName);
            WriteCBuffer(buffer, "updateUniforms", data.updateUniforms, data.paramToName);

            // Write samplers
            // TODO

            // Write attribute struct
            foreach (var attribBuffer in data.attributeBuffers)
                WriteAttributeBuffer(buffer, attribBuffer);

            // Write attribute buffer
            foreach (var attribBuffer in data.attributeBuffers)
            {
                buffer.Append("RWStructuredBuffer<Attribute");
                buffer.Append(attribBuffer.Index);
                buffer.Append("> attribBuffer");
                buffer.Append(attribBuffer.Index);
                buffer.AppendLine(";");

                if (attribBuffer.Used(VFXContextModel.Type.kTypeUpdate) && !attribBuffer.Writable(VFXContextModel.Type.kTypeUpdate))
                {
                    buffer.Append("StructuredBuffer<Attribute");
                    buffer.Append(attribBuffer.Index);
                    buffer.Append("> attribBuffer");
                    buffer.Append(attribBuffer.Index);
                    buffer.AppendLine("_RO;");
                }
            }
            if (data.attributeBuffers.Count > 0)
                buffer.AppendLine();

            // Write deadlists
            if (data.hasKill)
            {
                buffer.AppendLine("RWStructuredBuffer<int> flags;");
                buffer.AppendLine("ConsumeStructuredBuffer<uint> deadListIn;");
                buffer.AppendLine("AppendStructuredBuffer<uint> deadListOut;");
                buffer.AppendLine();
            }

            // Write functions
            if (data.hasRand)
            {
                buffer.AppendLine("float rand(inout int seed)");
                buffer.AppendLine("{");
                buffer.AppendLine("\tseed = seed * 22695477 + 1;");
                buffer.AppendLine("\treturn float(seed & 0xFFFF) / 65536.0;");
                buffer.AppendLine("}");
                buffer.AppendLine();
            }

            var functionNames = new Dictionary<Hash128,string>();
            foreach (var block in data.initBlocks)
                WriteFunction(buffer, block, functionNames);
            foreach (var block in data.updateBlocks)
                WriteFunction(buffer, block, functionNames);

            // Write init kernel
            if (hasInit)
            {
                WriteKernelHeader(buffer,"CSVFXInit");
                buffer.AppendLine("\tif (id.x < nbSpawned)");
                buffer.AppendLine("\t{");
                if (data.hasKill)
                    buffer.AppendLine("\t\tuint index = deadListIn.Consume();");
                else
                    buffer.AppendLine("\t\tuint index = id.x; // TODO Not working! Needs to add the current count as offset");
                buffer.AppendLine();

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    buffer.Append("\t\tAttribute");
                    buffer.Append(attribBuffer.Index);
                    buffer.Append(" attrib");
                    buffer.Append(attribBuffer.Index);
                    
                    // No initialization
                    //buffer.AppendLine(";");

                    // TODO tmp
                    // Initialize to avoid warning as error while compiling
                    buffer.Append(" = (Attribute");
                    buffer.Append(attribBuffer.Index);
                    buffer.AppendLine(")0;");
                }
                buffer.AppendLine();

                foreach (var block in data.initBlocks)
                    WriteFunctionCall(buffer, block, functionNames, data.paramToName, data.attribToBuffer);
                buffer.AppendLine();

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    buffer.Append("\t\tattribBuffer");
                    buffer.Append(attribBuffer.Index);
                    buffer.Append("[index] = attrib");
                    buffer.Append(attribBuffer.Index);
                    buffer.AppendLine(";");
                }

                if (data.hasKill)
                {
                    buffer.AppendLine();
                    buffer.AppendLine("\t\tflags[index] = 1;");
                }

                buffer.AppendLine("\t}");
                buffer.AppendLine("}");
                buffer.AppendLine();
            }

            // Write update kernel
            if (hasUpdate)
            {
                WriteKernelHeader(buffer,"CSVFXUpdate");

                buffer.Append("\tif (id.x < nbMax");
                if (data.hasKill)
                    buffer.AppendLine(" && flags[id.x] == 1)");
                else
                    buffer.AppendLine(")");
                buffer.AppendLine("\t{");
                buffer.AppendLine("\t\tuint index = id.x;");

                if (data.hasKill)
                    buffer.AppendLine("\t\tbool kill = false;");
                
                buffer.AppendLine();

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    if (attribBuffer.Used(VFXContextModel.Type.kTypeUpdate))
                    {
                        buffer.Append("\t\tAttribute");
                        buffer.Append(attribBuffer.Index);
                        buffer.Append(" attrib");
                        buffer.Append(attribBuffer.Index);
                        buffer.Append(" = attribBuffer");
                        buffer.Append(attribBuffer.Index);
                        if (!attribBuffer.Writable(VFXContextModel.Type.kTypeUpdate))
                            buffer.Append("_RO");
                        buffer.AppendLine("[index];");
                    }
                }
                buffer.AppendLine();

                foreach (var block in data.updateBlocks)
                    WriteFunctionCall(buffer, block, functionNames, data.paramToName, data.attribToBuffer);
                buffer.AppendLine();

                if (data.hasKill)
                {
                    buffer.AppendLine("\t\tif (kill)");
                    buffer.AppendLine("\t\t{");
                    buffer.AppendLine("\t\t\tflags[index] = 0;");
                    buffer.AppendLine("\t\t\tdeadListOut.Append(index);");
                    buffer.AppendLine("\t\t\treturn;");
                    buffer.AppendLine("\t\t}");
                    buffer.AppendLine();
                }

                foreach (var attribBuffer in data.attributeBuffers)
                {
                    if (attribBuffer.Writable(VFXContextModel.Type.kTypeUpdate))
                    {
                        buffer.Append("\t\tattribBuffer");
                        buffer.Append(attribBuffer.Index);
                        buffer.Append("[index] = attrib");
                        buffer.Append(attribBuffer.Index);
                        buffer.AppendLine(";");
                    }
                }

                buffer.AppendLine("\t}");
                buffer.AppendLine("}");
                buffer.AppendLine();
            }

            return buffer.ToString();
        }

        private static void WriteAttributeBuffer(StringBuilder buffer, AttributeBuffer attributeBuffer)
        {
            buffer.Append("struct ");
            buffer.Append("Attribute");
            buffer.Append(attributeBuffer.Index);
            buffer.AppendLine();
            buffer.AppendLine("{");
            for (int i = 0; i < attributeBuffer.Count; ++i)
            {
                buffer.Append('\t');
                WriteType(buffer,attributeBuffer[i].m_Param.m_Type);
                buffer.Append(" ");
                buffer.Append(attributeBuffer[i].m_Param.m_Name);
                buffer.AppendLine(";");
            }

            if (attributeBuffer.GetSizeInBytes() == 3)
                buffer.AppendLine("\tfloat _PADDING_;");

            buffer.AppendLine("};");
            buffer.AppendLine();
        }

        private static void WriteCBuffer(StringBuilder buffer, string cbufferName, HashSet<VFXParamValue> uniforms, Dictionary<VFXParamValue, string> uniformsToName)
        {
            if (uniforms.Count > 0)
            {
                buffer.Append("CBUFFER_START(");
                buffer.Append(cbufferName);
                buffer.AppendLine(")");
                foreach (var uniform in uniforms)
                {
                    buffer.Append('\t');
                    WriteType(buffer,uniform.ValueType);
                    buffer.Append(" ");
                    buffer.Append(uniformsToName[uniform]);
                    buffer.AppendLine(";");
                }
                buffer.AppendLine("CBUFFER_END");
                buffer.AppendLine();
            }
        }

        private static void WriteType(StringBuilder buffer,VFXParam.Type type)
        {
            buffer.Append(VFXParam.GetNameFromType(type));
        }

        private static void WriteFunction(StringBuilder buffer, VFXBlockModel block, Dictionary<Hash128, string> functions)
        {
            if (!functions.ContainsKey(block.Desc.m_Hash)) // if not already defined
            {
                // generate function name
                string name = new string((from c in block.Desc.m_Name where char.IsLetterOrDigit(c) select c).ToArray());
                functions[block.Desc.m_Hash] = name;

                string source = block.Desc.m_Source;

                // function signature
                buffer.Append("void ");
                buffer.Append(name);
                buffer.Append("(");

                char separator = ' ';
                foreach (var arg in block.Desc.m_Attribs)
                {
                    buffer.Append(separator);
                    separator = ',';

                    if (arg.m_Writable)
                        buffer.Append("inout ");
                    WriteType(buffer, arg.m_Param.m_Type);
                    buffer.Append(" ");
                    buffer.Append(arg.m_Param.m_Name);
                }

                foreach (var arg in block.Desc.m_Params)
                {
                    buffer.Append(separator);
                    separator = ',';

                    WriteType(buffer, arg.m_Type);
                    buffer.Append(" ");
                    buffer.Append(arg.m_Name);
                }

                if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0)
                {
                    buffer.Append(separator);
                    buffer.Append("inout int seed");
                    source = source.Replace("RAND", "rand(seed)"); 
                }

                if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0)
                {
                    buffer.Append(separator);
                    buffer.Append("inout bool kill");
                    source = source.Replace("KILL", "kill = true"); 
                }

                buffer.AppendLine(")");

                // function body
                buffer.AppendLine("{");

                // Add tab for formatting
                //source = source.Replace("\n", "\n\t");
                buffer.Append(source);
                buffer.AppendLine();

                buffer.AppendLine("}");
                buffer.AppendLine();
            }
        }

        private static void WriteFunctionCall(
            StringBuilder buffer,
            VFXBlockModel block,
            Dictionary<Hash128, string> functions,
            Dictionary<VFXParamValue, string> paramToName,
            Dictionary<VFXAttrib, AttributeBuffer> attribToBuffer)
        {
            buffer.Append("\t\t");
            buffer.Append(functions[block.Desc.m_Hash]);
            buffer.Append("(");

            char separator = ' ';
            foreach (var arg in block.Desc.m_Attribs)
            {
                buffer.Append(separator);
                separator = ',';

                int index = attribToBuffer[arg].Index;
                buffer.Append("attrib");
                buffer.Append(index);
                buffer.Append(".");
                buffer.Append(arg.m_Param.m_Name);
            }

            for (int i = 0; i < block.Desc.m_Params.Length; ++i)
            {
                buffer.Append(separator);
                separator = ',';
                buffer.Append(paramToName[block.GetParamValue(i)]);
            }

            if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0)
            {
                buffer.Append(separator);

                // TODO Not the best way to do that...
                VFXAttrib randAttrib = new VFXAttrib();
                VFXParam randParam = new VFXParam();
                randParam.m_Name = "seed";
                randParam.m_Type = VFXParam.Type.kTypeInt;
                randAttrib.m_Param = randParam;

                int index = attribToBuffer[randAttrib].Index;
                buffer.Append("attrib");
                buffer.Append(index);
                buffer.Append(".");
                buffer.Append(randParam.m_Name);
            }

            if ((block.Desc.m_Flags & (int)VFXBlock.Flag.kHasKill) != 0)
            {
                buffer.Append(separator);
                buffer.Append("kill");
            }

            buffer.AppendLine(");");
        }

        private static void WriteKernelHeader(StringBuilder buffer,string name)
        {
            buffer.AppendLine("[numthreads(NB_THREADS_PER_GROUP,1,1)]");
            buffer.Append("void ");
            buffer.Append(name);
            buffer.AppendLine("(uint3 id : SV_DispatchThreadID)");
            buffer.AppendLine("{");
        }
    }
}