using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class PreIntegratedFGD
    {
        [GenerateHLSL]
        public enum FGDTexture
        {
            Resolution = 64
        }

        static PreIntegratedFGD s_Instance;

        public static PreIntegratedFGD instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new PreIntegratedFGD();

                return s_Instance;
            }
        }

        public enum FGDIndex
        {
            FGD_GGXAndDisneyDiffuse = 0,
            FGD_CharlieAndFabricLambert = 1,
            FGD_Marschner = 2,
            Count = 3
        }

        bool[] m_isInit = new bool[(int)FGDIndex.Count];
        int[] m_refCounting = new int[(int)FGDIndex.Count];

        Material[] m_PreIntegratedFGDMaterial = new Material[(int)FGDIndex.Count];
        RenderTexture[] m_PreIntegratedFGD = new RenderTexture[(int)FGDIndex.Count];

        PreIntegratedFGD()
        {
            for (int i = 0; i < (int)FGDIndex.Count; ++i)
            {
                m_isInit[i] = false;
                m_refCounting[i] = 0;
            }
        }

        private Shader GetShaderForIndex(FGDIndex index)
        {
            if (HDRenderPipelineGlobalSettings.instance == null)
            {
                Debug.LogError($"Invalid resources to get shader for {index}.");
                return null;
            }

            var shaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();

            switch (index)
            {
                case FGDIndex.FGD_GGXAndDisneyDiffuse: return shaders.preIntegratedFGD_GGXDisneyDiffusePS;
                case FGDIndex.FGD_CharlieAndFabricLambert: return shaders.preIntegratedFGD_CharlieFabricLambertPS;
                case FGDIndex.FGD_Marschner: return shaders.preIntegratedFGD_MarschnerPS;
                default: Debug.LogError($"Unable to get shader for index: {index}."); break;
            }

            return null;
        }

        public void Build(FGDIndex index)
        {
            Debug.Assert(index != FGDIndex.Count);
            Debug.Assert(m_refCounting[(int)index] >= 0);

            if (m_refCounting[(int)index] == 0)
            {
                Shader pixelShader = GetShaderForIndex(index);
                int res = (int)FGDTexture.Resolution;
                m_PreIntegratedFGDMaterial[(int)index] = CoreUtils.CreateEngineMaterial(pixelShader);
                m_PreIntegratedFGD[(int)index] = new RenderTexture(res, res, 0, GraphicsFormat.A2B10G10R10_UNormPack32)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = CoreUtils.GetRenderTargetAutoName(res, res, 1, GraphicsFormat.A2B10G10R10_UNormPack32, $"preIntegrated{index}")
                };
                m_PreIntegratedFGD[(int)index].Create();
                m_isInit[(int)index] = false;
            }

            m_refCounting[(int)index]++;
        }

        public void RenderInit(FGDIndex index, CommandBuffer cmd)
        {
            // Here we have to test IsCreated because in some circumstances (like loading RenderDoc), the texture is internally destroyed but we don't know from C# side.
            // In this case IsCreated will return false, allowing us to re-render the texture (setting the texture as current RT during DrawFullScreen will automatically re-create it internally)
            if (m_isInit[(int)index] && m_PreIntegratedFGD[(int)index].IsCreated())
                return;

            // If we are in wireframe mode, the drawfullscreen will not work as expected, but we don't need the LUT anyway
            // So create the texture to avoid errors, it will be initialized by the first render without wireframe
            if (GL.wireframe)
            {
                m_PreIntegratedFGD[(int)index].Create();
                return;
            }

            CoreUtils.DrawFullScreen(cmd, m_PreIntegratedFGDMaterial[(int)index], new RenderTargetIdentifier(m_PreIntegratedFGD[(int)index]));
            m_isInit[(int)index] = true;
        }

        public void Cleanup(FGDIndex index)
        {
            m_refCounting[(int)index]--;

            if (m_refCounting[(int)index] == 0)
            {
                CoreUtils.Destroy(m_PreIntegratedFGDMaterial[(int)index]);
                CoreUtils.Destroy(m_PreIntegratedFGD[(int)index]);

                m_isInit[(int)index] = false;
            }

            Debug.Assert(m_refCounting[(int)index] >= 0);
        }

        public void Bind(CommandBuffer cmd, FGDIndex index)
        {
            switch (index)
            {
                case FGDIndex.FGD_GGXAndDisneyDiffuse:
                    cmd.SetGlobalTexture(HDShaderIDs._PreIntegratedFGD_GGXDisneyDiffuse, m_PreIntegratedFGD[(int)index]);
                    break;

                case FGDIndex.FGD_CharlieAndFabricLambert:
                    cmd.SetGlobalTexture(HDShaderIDs._PreIntegratedFGD_CharlieAndFabric, m_PreIntegratedFGD[(int)index]);
                    break;

                case FGDIndex.FGD_Marschner:
                    cmd.SetGlobalTexture(HDShaderIDs._PreIntegratedFGD_CharlieAndFabric, m_PreIntegratedFGD[(int)index]);
                    break;

                default:
                    break;
            }
        }
    }
}
