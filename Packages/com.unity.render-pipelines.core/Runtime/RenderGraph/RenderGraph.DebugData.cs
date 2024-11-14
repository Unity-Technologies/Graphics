using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        /// <summary>
        /// Debug data interface shared by HDRP RG Compiler and NRP RG Compiler.
        /// Some data is optional, depending on the value of isNRPCompiler.
        /// </summary>
        internal class DebugData
        {
            public DebugData()
            {
                for (int i = 0; i < (int) RenderGraphResourceType.Count; ++i)
                    resourceLists[i] = new List<ResourceData>();
            }

            // Debug data for passes.
            public readonly List<PassData> passList = new List<PassData>();

            // Debug data for resources.
            public readonly List<ResourceData>[] resourceLists = new List<ResourceData>[(int) RenderGraphResourceType.Count];

            // If true, the data was output by NRP compiler, in which case PassData.nrpInfo is available.
            public bool isNRPCompiler;

            [DebuggerDisplay("PassDebug: {name}")]
            public struct PassData
            {
                // Render pass name.
                public string name;

                // Render graph pass type.
                public RenderGraphPassType type;

                // List of ResourceHandle.index's for each resource type read by this pass.
                public List<int>[] resourceReadLists;

                // List of ResourceHandle.index's for each resource type written by this pass.
                public List<int>[] resourceWriteLists;

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

                public class NRPInfo
                {
                    public class NativeRenderPassInfo
                    {
                        public class AttachmentInfo
                        {
                            public string resourceName;
                            public string loadReason;
                            public string storeReason;
                            public string storeMsaaReason;
                            public int attachmentIndex;
                            public NativePassAttachment attachment;
                        }

                        public struct PassCompatibilityInfo
                        {
                            public string message;
                            public bool isCompatible;
                        }

                        public string passBreakReasoning;
                        public List<AttachmentInfo> attachmentInfos;
                        public Dictionary<int, PassCompatibilityInfo> passCompatibility;
                        public List<int> mergedPassIds;
                    }

                    // If this pass is part of a Native Render Pass, this is available, null otherwise.
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
                public NRPInfo nrpInfo;

                // File path and line number where the render pass is defined.
                public PassScriptInfo scriptInfo;
            }

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
                public TextureResourceData textureData;

                // Buffer-specific resource data.
                public BufferResourceData bufferData;
            }

            public void Clear()
            {
                passList.Clear();
                for (int i = 0; i < (int) RenderGraphResourceType.Count; ++i)
                    resourceLists[i].Clear();
            }

            // Script metadata for passes.
            internal static readonly Dictionary<System.Object, PassScriptInfo> s_PassScriptMetadata = new ();

            // Pass script metadata.
            public class PassScriptInfo
            {
                public string filePath;
                public int line;
            }
        }

        readonly string[] k_PassNameDebugIgnoreList = new string[] { k_BeginProfilingSamplerPassName, k_EndProfilingSamplerPassName };

        [Conditional("UNITY_EDITOR")]
        void AddPassDebugMetadata(RenderGraphPass renderPass, string file, int line)
        {
            // Does nothing unless debug data capture is requested
            if (m_CaptureDebugDataForExecution == null)
                return;

            for (int i = 0; i < k_PassNameDebugIgnoreList.Length; ++i)
                if (renderPass.name == k_PassNameDebugIgnoreList[i])
                    return;

            DebugData.s_PassScriptMetadata.TryAdd(renderPass, new DebugData.PassScriptInfo { filePath = file, line = line });
        }

        [Conditional("UNITY_EDITOR")]
        void ClearPassDebugMetadata()
        {
            DebugData.s_PassScriptMetadata.Clear();
        }
    }
}
