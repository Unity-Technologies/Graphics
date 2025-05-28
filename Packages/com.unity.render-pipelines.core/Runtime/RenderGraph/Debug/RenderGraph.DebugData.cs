using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        // Convenience class that wraps execution id and name for display purposes
        internal class DebugExecutionItem
        {
            public EntityId id { get; }

            public string name { get; }

            public DebugExecutionItem(EntityId id, string name)
            {
                this.id = id;
                this.name = name;
            }
        }

        /// <summary>
        /// Debug data interface shared by HDRP RG Compiler and NRP RG Compiler.
        /// Some data is optional, depending on the value of isNRPCompiler.
        /// </summary>
        [Serializable]
        internal class DebugData
        {
            public DebugData(string executionName)
            {
                this.executionName = executionName;
            }

            public string executionName;

            // Set to true when data has been set
            public bool valid = false;

            // Compilation hash of the render graph that produced this DebugData.
            public int graphHash;

            // If true, the data was output by NRP compiler, in which case PassData.nrpInfo is available.
            public bool isNRPCompiler;

            [Serializable]
            public class ResourceLists<T>
            {
                [SerializeField] private List<T> m_Textures = new();
                [SerializeField] private List<T> m_Buffers = new();
                [SerializeField] private List<T> m_AccelerationStructures = new();

                // Indexer to access the lists by index
                public List<T> this[int index]
                {
                    get
                    {
                        return index switch
                        {
                            (int)RenderGraphResourceType.Texture => m_Textures,
                            (int)RenderGraphResourceType.Buffer => m_Buffers,
                            (int)RenderGraphResourceType.AccelerationStructure => m_AccelerationStructures,
                            _ => throw new ArgumentOutOfRangeException(nameof(index))
                        };
                    }
                    set
                    {
                        switch (index)
                        {
                            case (int)RenderGraphResourceType.Texture:
                                m_Textures = value ?? new List<T>();
                                break;
                            case (int)RenderGraphResourceType.Buffer:
                                m_Buffers = value ?? new List<T>();
                                break;
                            case (int)RenderGraphResourceType.AccelerationStructure:
                                m_AccelerationStructures = value ?? new List<T>();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(index));
                        }
                    }
                }

                public void Clear()
                {
                    for (int i = 0; i < (int)RenderGraphResourceType.Count; ++i)
                    {
                        if (this[i] == null)
                            this[i] = new List<T>();
                        else
                            this[i].Clear();
                    }
                }
            }

            // Debug data for passes.
            public List<PassData> passList = new List<PassData>();

            // Debug data for resources.
            [Serializable]
            public class ResourceDataLists : ResourceLists<ResourceData>
            { }

            public ResourceDataLists resourceLists = new();

            // NOTE: Cannot use NativePassAttachment for DebugData directly because readonly fields are not serialized.
            //       This class duplicates the relevant contents.
            [Serializable]
            public class SerializableNativePassAttachment
            {
                public RenderBufferLoadAction loadAction;
                public RenderBufferStoreAction storeAction;
                public bool memoryless;
                public int mipLevel;
                public int depthSlice;

                public SerializableNativePassAttachment(NativePassAttachment att)
                {
                    loadAction = att.loadAction;
                    storeAction = att.storeAction;
                    memoryless = att.memoryless;
                    mipLevel = att.mipLevel;
                    depthSlice = att.depthSlice;
                }
            }

            [Serializable]
            [DebuggerDisplay("PassDebug: {name}")]
            public struct PassData
            {
                // Render pass name.
                public string name;

                // Render graph pass type.
                public RenderGraphPassType type;

                [Serializable]
                public class ResourceIdLists : ResourceLists<int>
                {}

                // List of ResourceHandle.index's for each resource type read by this pass.
                public ResourceIdLists resourceReadLists;

                // List of ResourceHandle.index's for each resource type written by this pass.
                public ResourceIdLists resourceWriteLists;

                // Whether the pass was culled.
                public bool culled;

                // Whether the pass is an async compute pass.
                public bool async;

                // Native subpass index.
                public int nativeSubPassIndex;

                // Index of the pass that needs to be waited for.
                public int syncToPassIndex;

                // Smaller pass index that waits for this pass.
                public int syncFromPassIndex;

                // We have this member instead of removing the pass altogether because we need the full list of passes in
                // order to be able to remap them correctly when we remove them from display in the viewer.
                public bool generateDebugData;

                [Serializable]
                public class NRPInfo
                {
                    [Serializable]
                    public class NativeRenderPassInfo
                    {
                        [Serializable]
                        public class AttachmentInfo
                        {
                            public string resourceName;
                            public string loadReason;
                            public string storeReason;
                            public string storeMsaaReason;
                            public int attachmentIndex;
                            public SerializableNativePassAttachment attachment;
                        }

                        [Serializable]
                        public struct PassCompatibilityInfo
                        {
                            public string message;
                            public bool isCompatible;
                        }

                        public string passBreakReasoning;
                        public List<AttachmentInfo> attachmentInfos;

                        public SerializedDictionary<int, PassCompatibilityInfo> passCompatibility;

                        public List<int> mergedPassIds;
                    }

                    // If this pass is part of a Native Render Pass, this is available, null otherwise.
                    [SerializeReference]
                    public NativeRenderPassInfo nativePassInfo;

                    // List of ResourceHandle.index's for each texture resource that is accessed using framebuffer fetch.
                    public List<int> textureFBFetchList = new();

                    // List of ResourceHandle.index's for each texture resource that this pass called PostSetGlobalTexture() for.
                    public List<int> setGlobals = new();

                    // Fragment info
                    public int width;
                    public int height;
                    public int volumeDepth;
                    public int samples;
                    public bool hasDepth;
                }

                // Only available when isNRPCompiler = true, null otherwise.
                [SerializeReference]
                public NRPInfo nrpInfo;

                // File path and line number where the render pass is defined.
                public PassScriptInfo scriptInfo;
            }

            [Serializable]
            public class BufferResourceData
            {
                // Number of elements in buffer.
                public int count;

                // Size of one element in the buffer.
                public int stride;

                // Buffer usage type.
                public GraphicsBuffer.Target target;

                // Buffer usage flags.
                public GraphicsBuffer.UsageFlags usage;
            }

            [Serializable]
            public class TextureResourceData
            {
                // Texture width & height.
                public int width;
                public int height;

                // Texture depth (volume texture).
                public int depth;

                // Whether the texture is bound with multi sampling.
                public bool bindMS;

                // Number of texture MSAA samples.
                public int samples;

                // Render texture graphics format.
                public GraphicsFormat format;

                // Whether texture is cleared on first use.
                public bool clearBuffer;
            }

            [Serializable]
            [DebuggerDisplay("ResourceDebug: {name} [{creationPassIndex}:{releasePassIndex}]")]
            public struct ResourceData
            {
                // Resource name.
                public string name;

                // Whether the resource is imported outside of render graph.
                public bool imported;

                // Pass that creates the resource (for imported resources, the first pass that uses the resource).
                public int creationPassIndex;

                // Pass that releases the resource (for imported resources, the last pass that uses the resource).
                public int releasePassIndex;

                // List of passes that read the resource.
                public List<int> consumerList;

                // List of passes that write the resource.
                public List<int> producerList;

                // Whether the resource is memoryless (i.e resources that are created/destroyed within the same native render pass).
                // Available if isNRPCompiler = true.
                public bool memoryless;

                // Texture-specific resource data.
                [SerializeReference]
                public TextureResourceData textureData;

                // Buffer-specific resource data.
                [SerializeReference]
                public BufferResourceData bufferData;
            }

            public void Clear()
            {
                passList.Clear();
                resourceLists.Clear();
                valid = false;
            }

            // Pass script metadata.
            [Serializable]
            public struct PassScriptInfo
            {
                public string filePath;
                public int line;
            }
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        void AddPassDebugMetadata(RenderGraphPass renderPass, string file, int line)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Does nothing unless debug session is active.
            if (!RenderGraphDebugSession.hasActiveDebugSession)
                return;

            renderPass.debugScriptInfo = new DebugData.PassScriptInfo { filePath = file, line = line };
#endif
        }
    }
}
