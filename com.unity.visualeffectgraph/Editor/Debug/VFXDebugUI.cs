using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.VFX.UI
{

    class VFXDebugUI 
    {

        public enum Modes
        {
            None,
            SystemStat
        }

        class CurveContent : ImmediateModeElement
        {
            Mesh m_Mesh;
            Material m_Mat;

            public CurveContent()
            {
                m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                Color color = new Color(1, 0, 0, 1);
                m_Mat.SetColor("_Color", color);

                var vertices = new Vector3[4];
                vertices[0] = new Vector3(-0.5f, 0.5f);
                vertices[1] = new Vector3(0.5f, 0.5f);
                vertices[2] = new Vector3(0.5f, -0.5f);
                vertices[3] = new Vector3(-0.5f, -0.5f);
                var indices = new int[] { 0, 1, 2, 3 };

                m_Mesh = new Mesh();
                m_Mesh.vertices = vertices;
                m_Mesh.SetIndices(indices, MeshTopology.Quads, 0);
            }

            public void SetMesh(Mesh mesh)
            {
                m_Mesh = mesh;
            }

            void DrawMesh()
            {
                if (m_Mat == null)
                    m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                Color color = new Color(1, 0, 0, 1);
                m_Mat.SetColor("_ColorStart", color);
                m_Mat.SetColor("_ColorEnd", color);
                m_Mat.SetColor("_ColorMiddle", color);
                m_Mat.SetPass(0);
                Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
            }

            protected override void ImmediateRepaint()
            {
                DrawMesh();
            }
        }

        VFXComponentBoard m_ComponentBoard;
        CurveContent m_Curve;

        public void SetDebugMode(Modes mode, VFXComponentBoard componentBoard)
        {
            m_ComponentBoard = componentBoard;
            switch (mode)
            {
                case Modes.SystemStat:
                    SystemStat();
                    break;
                case Modes.None:
                    Clear();
                    break;
                default:
                    Clear();
                    break;
            }
        }

        void SystemStat()
        {
            m_Curve = new CurveContent();
            m_ComponentBoard.contentContainer.Add(m_Curve);

        }

        void Clear()
        {
            if (m_ComponentBoard != null)
                m_ComponentBoard.contentContainer.Remove(m_Curve);
            m_ComponentBoard = null;
            m_Curve = null;
        }
    }
}
