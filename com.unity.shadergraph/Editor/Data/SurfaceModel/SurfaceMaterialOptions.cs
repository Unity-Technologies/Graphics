using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SurfaceMaterialOptions
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
        private BlendMode m_SrcBlend = BlendMode.One;

        [SerializeField]
        private BlendMode m_DstBlend = BlendMode.Zero;

        [SerializeField]
        private CullMode m_CullMode = CullMode.Back;

        [SerializeField]
        private ZTest m_ZTest = ZTest.LEqual;

        [SerializeField]
        private ZWrite m_ZWrite = ZWrite.On;

        [SerializeField]
        private RenderQueue m_RenderQueue = RenderQueue.Geometry;

        [SerializeField]
        private RenderType m_RenderType = RenderType.Opaque;

        [SerializeField]
        private int m_LOD = 200;

        public void Init()
        {
            srcBlend = BlendMode.One;
            dstBlend = BlendMode.Zero;
            cullMode = CullMode.Back;
            zTest = ZTest.LEqual;
            zWrite = ZWrite.On;
            renderQueue = RenderQueue.Geometry;
            renderType = RenderType.Opaque;
            lod = 200;
        }

        public void GetTags(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("Tags", false);
            visitor.AddShaderChunk("{", false);
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

        /*private Vector2 m_ScrollPos;
        public void DoGUI()
        {
            GUILayout.BeginVertical();
            m_Expanded = MaterialGraphStyles.Header("Options", m_Expanded);

            if (m_Expanded)
            {
                srcBlend = (BlendMode) EditorGUILayout.EnumPopup("Src Blend", srcBlend);
                dstBlend = (BlendMode) EditorGUILayout.EnumPopup("Dst Blend", dstBlend);
                cullMode = (CullMode) EditorGUILayout.EnumPopup("Cull Mode", cullMode);
                zTest = (ZTest) EditorGUILayout.EnumPopup("Z Test", zTest);
                zWrite = (ZWrite) EditorGUILayout.EnumPopup("Z Write", zWrite);
                renderQueue = (RenderQueue) EditorGUILayout.EnumPopup("Render Queue", renderQueue);
                renderType = (RenderType) EditorGUILayout.EnumPopup("Render Type", renderType);
            }
            GUILayout.EndVertical();
        }*/

        public BlendMode srcBlend { get { return m_SrcBlend; } set { m_SrcBlend = value; } }
        public BlendMode dstBlend { get { return m_DstBlend; } set { m_DstBlend = value; } }
        public CullMode cullMode { get { return m_CullMode; } set { m_CullMode = value; } }
        public ZTest zTest { get { return m_ZTest; } set { m_ZTest = value; } }
        public ZWrite zWrite { get { return m_ZWrite; } set { m_ZWrite = value; } }
        public RenderQueue renderQueue { get { return m_RenderQueue; } set { m_RenderQueue = value; } }
        public RenderType renderType { get { return m_RenderType; } set { m_RenderType = value; } }
        public int lod { get { return m_LOD; } set { m_LOD = value; } }
    }
}
