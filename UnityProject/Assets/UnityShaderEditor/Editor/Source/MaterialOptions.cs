using System;
using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    public class MaterialOptions : ScriptableObject
    {
        public enum BlendMode
        {
            One,
            Zero,
            SrcColor,
            SrcAlpha,
            DstColor,
            DstAlpha,
            OneMinusSrcColor,
            OneMinusSrcAlpha,
            OneMinusDstColor,
            OneMinusDstAlpha,
        }

        public enum CullMode
        {
            Back,
            Front,
            Off
        }

        public enum ZTest
        {
            Less,
            Greater,
            LEqual,
            GEqual,
            Equal,
            NotEqual,
            Always
        }

        public enum ZWrite
        {
            On,
            Off
        }

        public enum RenderQueue
        {
            Background,
            Geometry,
            Transparent,
            Overlay,
        }

        public enum RenderType
        {
            Opaque,
            Transparent,
            TransparentCutout,
            Background,
            Overlay
        }

        [SerializeField]
        private BlendMode m_SrcBlend;

        [SerializeField]
        private BlendMode m_DstBlend;

        [SerializeField]
        private CullMode m_CullMode;

        [SerializeField]
        private ZTest m_ZTest;

        [SerializeField]
        private ZWrite m_ZWrite;

        [SerializeField]
        private RenderQueue m_RenderQueue;

        [SerializeField]
        private RenderType m_RenderType;

        [SerializeField]
        private bool m_ShadowPass;

        [SerializeField]
        private bool m_FullForwardShadows;

        [SerializeField]
        private bool m_NoAmbient;

        [SerializeField]
        private bool m_NoVertexLights;

        [SerializeField]
        private bool m_NoLightmaps;

        [SerializeField]
        private bool m_NoDirLightmap;

        [SerializeField]
        private bool m_NoForwardAdd;

        [SerializeField]
        private bool m_ApproxView;

        [SerializeField]
        private bool m_HalfAsView;

        public void Init()
        {
            srcBlend = BlendMode.One;
            dstBlend = BlendMode.Zero;
            cullMode = CullMode.Back;
            zTest = ZTest.LEqual;
            zWrite = ZWrite.On;
            renderQueue = RenderQueue.Geometry;
            renderType = RenderType.Opaque;
            shadowPass = false;
            fullForwardShadows = false;
            noAmbient = false;
            noVertexLights = false;
            noLightmaps = false;
            noDirLightmap = false;
            noForwardAdd = false;
            approxView = false;
            halfAsView = false;
        }

        public void GetTags(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("Tags {", false);
            visitor.Indent();
            visitor.AddShaderChunk("\"RenderType\"=\"" + renderType + "\"", false);
            visitor.AddShaderChunk("\"Queue\"=\"" + renderQueue + "\"", false);
            visitor.Deindent();
            visitor.AddShaderChunk("}", false);
        }

        public void GetBlend(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("Blend " + srcBlend + " " + dstBlend, false);
        }

        public void GetCull(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("Cull " + cullMode, false);
        }

        public void GetDepthWrite(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("ZWrite " + zWrite, false);
        }

        public void GetDepthTest(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("ZTest " + zTest, false);
        }

        private Vector2 m_ScrollPos;
        public void DoGUI()
        {
            m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
            GUILayout.BeginVertical();

            srcBlend = (BlendMode)EditorGUILayout.EnumPopup("Src Blend", srcBlend);
            dstBlend = (BlendMode)EditorGUILayout.EnumPopup("Dst Blend", dstBlend);
            cullMode = (CullMode)EditorGUILayout.EnumPopup("Cull Mode", cullMode);
            zTest = (ZTest)EditorGUILayout.EnumPopup("Z Test", zTest);
            zWrite = (ZWrite)EditorGUILayout.EnumPopup("Z Write", zWrite);
            renderQueue = (RenderQueue)EditorGUILayout.EnumPopup("Render Queue", renderQueue);
            renderType = (RenderType)EditorGUILayout.EnumPopup("Render Type", renderType);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public BlendMode srcBlend { get { return m_SrcBlend; } set { m_SrcBlend = value; } }
        public BlendMode dstBlend { get { return m_DstBlend; } set { m_DstBlend = value; } }
        public CullMode cullMode { get { return m_CullMode; } set { m_CullMode = value; } }
        public ZTest zTest { get { return m_ZTest; } set { m_ZTest = value; } }
        public ZWrite zWrite { get { return m_ZWrite; } set { m_ZWrite = value; } }
        public RenderQueue renderQueue { get { return m_RenderQueue; } set { m_RenderQueue = value; } }
        public RenderType renderType { get { return m_RenderType; } set { m_RenderType = value; } }
        public bool shadowPass { get { return m_ShadowPass; } set { m_ShadowPass = value; } }
        public bool fullForwardShadows { get { return m_FullForwardShadows; } set { m_FullForwardShadows = value; } }
        public bool noAmbient { get { return m_NoAmbient; } set { m_NoAmbient = value; } }
        public bool noVertexLights { get { return m_NoVertexLights; } set { m_NoVertexLights = value; } }
        public bool noLightmaps { get { return m_NoLightmaps; } set { m_NoLightmaps = value; } }
        public bool noDirLightmap { get { return m_NoDirLightmap; } set { m_NoDirLightmap = value; } }
        public bool noForwardAdd { get { return m_NoForwardAdd; } set { m_NoForwardAdd = value; } }
        public bool approxView { get { return m_ApproxView; } set { m_ApproxView = value; } }
        public bool halfAsView { get { return m_HalfAsView; } set { m_HalfAsView = value; } }
    }
}
