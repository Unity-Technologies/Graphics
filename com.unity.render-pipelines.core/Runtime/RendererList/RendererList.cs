using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // This is a temporary structure that will help transition to render graph
    // Plan is to define this correctly on the C++ side and expose it to C# later.
    public struct RendererList
    {
        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

        public static readonly RendererList nullRendererList = new RendererList();

        public bool                 isValid { get; private set; }
        public CullingResults       cullingResult;
        public DrawingSettings      drawSettings;
        public FilteringSettings    filteringSettings;
        public RenderStateBlock?    stateBlock;

        public static RendererList Create(in RendererListDesc desc)
        {
            RendererList newRenderList = new RendererList();

            // At this point the RendererList is invalid and will be caught when using it.
            // It's fine because to simplify setup code you might not always have a valid desc. The important part is to catch it if used.
            if (!desc.IsValid())
                return newRenderList;

            var sortingSettings = new SortingSettings(desc.camera)
            {
                criteria = desc.sortingCriteria
            };

            var drawSettings = new DrawingSettings(s_EmptyName, sortingSettings)
            {
                perObjectData = desc.rendererConfiguration
            };

            if (desc.passName != ShaderTagId.none)
            {
                Debug.Assert(desc.passNames == null);
                drawSettings.SetShaderPassName(0, desc.passName);
            }
            else
            {
                for (int i = 0; i < desc.passNames.Length; ++i)
                {
                    drawSettings.SetShaderPassName(i, desc.passNames[i]);
                }
            }

            if (desc.overrideMaterial != null)
            {
                drawSettings.overrideMaterial = desc.overrideMaterial;
                drawSettings.overrideMaterialPassIndex = 0;
            }

            var filterSettings = new FilteringSettings(desc.renderQueueRange)
            {
                excludeMotionVectorObjects = desc.excludeObjectMotionVectors
            };

            newRenderList.isValid = true;
            newRenderList.cullingResult = desc.cullingResult;
            newRenderList.drawSettings = drawSettings;
            newRenderList.filteringSettings = filterSettings;
            newRenderList.stateBlock = desc.stateBlock;

            return newRenderList;
        }
    }

    public struct RendererListDesc
    {
        public SortingCriteria sortingCriteria;
        public PerObjectData rendererConfiguration;
        public RenderQueueRange renderQueueRange;
        public RenderStateBlock? stateBlock;
        public Material overrideMaterial;
        public bool excludeObjectMotionVectors;

        // Mandatory parameters passed through constructors
        public CullingResults cullingResult { get; private set; }
        public Camera camera { get; private set; }
        public ShaderTagId passName { get; private set; }
        public ShaderTagId[] passNames { get; private set; }

        public RendererListDesc(ShaderTagId passName, CullingResults cullingResult, Camera camera)
            : this()
        {
            this.passName = passName;
            this.passNames = null;
            this.cullingResult = cullingResult;
            this.camera = camera;
        }

        public RendererListDesc(ShaderTagId[] passNames, CullingResults cullingResult, Camera camera)
            : this()
        {
            this.passNames = passNames;
            this.passName = ShaderTagId.none;
            this.cullingResult = cullingResult;
            this.camera = camera;
        }

        public bool IsValid()
        {
            if (camera == null || (passName == ShaderTagId.none && (passNames == null || passNames.Length == 0)))
                return false;

            return true;
        }
    }
}
