using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
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

        // TODO atm it only validates the system
        public static void CompileSystem(VFXSystemModel system)
        {
            // BLOCKS
            List<VFXBlockModel> initBlocks = new List<VFXBlockModel>();
            List<VFXBlockModel> updateBlocks = new List<VFXBlockModel>();
            bool initHasRand = false;
            bool updateHasRand = false;

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
                for (int j = 0; j < context.GetNbChildren(); ++j)
                {
                    VFXBlockModel blockModel = context.GetChild(j);
                    hasRand |= (blockModel.Desc.m_Flags & (int)VFXBlock.Flag.kHasRand) != 0;
                    currentList.Add(blockModel);
                }

                switch (context.GetContextType())
                {
                    case VFXContextModel.Type.kTypeInit: initHasRand |= hasRand; break;
                    case VFXContextModel.Type.kTypeUpdate: updateHasRand |= hasRand; break;
                }
            }

            if (initBlocks.Count == 0 && updateBlocks.Count == 0)
            {
                // Invalid system, not compiled
                VFXEditor.Log("System is invalid: Empty");
                return;
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
            var buffers = new List<List<VFXAttrib>>();
            foreach (var attribsByUsage in sortedAttribs)
            {
                // handle 16 bytes attrib
                var currentAttribs = attribsByUsage.Value[3];
                int index = currentAttribs.Count - 1;
                while (index >= 0)
                {
                    var buffer = new List<VFXAttrib>();
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
                    var buffer = new List<VFXAttrib>();
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
                    var buffer = new List<VFXAttrib>();
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
                var currentBuffer = new List<VFXAttrib>();
                while (index >= 0)
                {
                    currentBuffer.Add(currentAttribs[index]);
                    currentAttribs.RemoveAt(index--);
                    ++currentCount;

                    if (currentCount == 4 || index < 0)
                    {
                        buffers.Add(currentBuffer);
                        currentBuffer = new List<VFXAttrib>();
                        currentCount = 0;
                    }
                }
            }

            if (buffers.Count > 7)
                VFXEditor.Log("ERROR: too many buffers used (max is 7 + 1 reserved)");

            VFXEditor.Log("Nb Attributes : " + attribs.Count);
            VFXEditor.Log("Nb Attribute buffers: " + buffers.Count);
            for (int i = 0; i < buffers.Count; ++i)
            {
                string str = "\t " + i + " |";
                for (int j = 0; j < buffers[i].Count; ++j)
                {
                    str += buffers[i][j].m_Param.m_Name + "|";
                }
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

            // Log result
            VFXEditor.Log("Nb init blocks: " + initBlocks.Count);
            VFXEditor.Log("Nb update blocks: " + updateBlocks.Count);
            VFXEditor.Log("Nb global uniforms: " + globalUniforms.Count);
            VFXEditor.Log("Nb init uniforms: " + initUniforms.Count);
            VFXEditor.Log("Nb update uniforms: " + updateUniforms.Count);
        }

        public static HashSet<VFXParamValue> CollectUniforms(List<VFXBlockModel> blocks)
        {
            HashSet<VFXParamValue> uniforms = new HashSet<VFXParamValue>();

            foreach (VFXBlockModel block in blocks)
                for (int i = 0; i < block.Desc.m_Params.Length; ++i)
                    uniforms.Add(block.GetParamValue(i));

            return uniforms;
        }

        // Collect all attributs from blocks and fills them in attribs
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
    }
}