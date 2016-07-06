using UnityEngine;
using UnityEngine.Experimental.VFX;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental
{
    class VFXAttributePacker
    {
        // This code is dirty, clean it
        public static List<AttributeBuffer> Pack(Dictionary<VFXAttribute, int> attribs, int MaxBuffers)
        {
            var sortedAttribs = new Dictionary<int, List<VFXAttribute>[]>();
            foreach (var attrib in attribs)
            {
                List<VFXAttribute>[] attribsForUsage;
                sortedAttribs.TryGetValue(attrib.Value, out attribsForUsage);

                if (attribsForUsage == null) // Not yet initialized
                {
                    attribsForUsage = new List<VFXAttribute>[4];
                    for (int i = 0; i < 4; ++i) // Assuming sizes cannot be more than 4 bytes
                        attribsForUsage[i] = new List<VFXAttribute>();

                    sortedAttribs[attrib.Value] = attribsForUsage;
                }

                int sizeInBytes = VFXValue.TypeToSize(attrib.Key.m_Type);
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

           
            List<AttributeBuffer> buffersToErase = new List<AttributeBuffer>();




            // TODO Merge R and RW buffers used in the same context in case of holes
            // for instance, for a given context flag
            // R : X -> 4 bytes
            // RW : XXX0 -> 12 bytes
            // => Merge this to one buffer of 16

          
            while (buffers.Count - buffersToErase.Count > MaxBuffers)
            {
                bool hasMerged = false;

                // First fill out holes
                for (int i = 0; i < buffers.Count; ++i)
                    if (buffers[i].GetSizeInBytes() == 12)
                    {
                        //int usage = buffers[i].MergedUsage;
                        for (int j = 0; j < buffers.Count; ++j)
                            if (buffers[j].GetSizeInBytes() == 4/* && ((buffers[j].MergedUsage & usage) == buffers[j].MergedUsage)*/)
                            {
                                if (!buffersToErase.Contains(buffers[j])) // Not alreay used
                                {
                                    VFXEditor.Log("MERGE BUFFER " + j + " in " + i);
                                    hasMerged = true;
                                    buffers[i].Add(buffers[j]);
                                    buffersToErase.Add(buffers[j]);
                                    break;
                                }
                            }
                    }

                if (!hasMerged)
                    break;
            }

            MergeBuffers(buffers, buffersToErase, 4, MaxBuffers);
            MergeBuffers(buffers, buffersToErase, 8, MaxBuffers);
            // Dont test 16 bytes for now

            foreach (var buffer in buffersToErase)
                buffers.Remove(buffer);

            return buffers;
        }

        private static void MergeBuffers(List<AttributeBuffer> buffers,List<AttributeBuffer> buffersToErase,int size,int MaxNb)
        {   
            while (buffers.Count - buffersToErase.Count > MaxNb)
            {
                bool hasMerged = false;
                bool needsBreak = false;

                for (int i = 0; i < buffers.Count; ++i)
                {
                    if (buffers[i].GetSizeInBytes() == size && !buffersToErase.Contains(buffers[i]))
                    {
                        for (int j = 0; j < buffers.Count; ++j)
                            if (i != j && buffers[j].GetSizeInBytes() == size)
                            {
                                if (!buffersToErase.Contains(buffers[j])) // Not already used
                                {
                                    VFXEditor.Log("MERGE BUFFER " + j + " in " + i);
                                    buffers[i].Add(buffers[j]);
                                    buffersToErase.Add(buffers[j]);
                                    needsBreak = true;
                                    hasMerged = true;
                                    break;
                                }
                            }
                    }
                    if (needsBreak)
                    {
                        needsBreak = false;
                        break;
                    }
                }

                if (!hasMerged)
                    break;
            }
        }
    }
}