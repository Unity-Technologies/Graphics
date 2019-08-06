using System;
using System.Reflection;
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
            VFXDebugUI m_DebugUI;
            int m_ClippingMatrixId;


            public CurveContent(VFXDebugUI debugUI)
            {
                m_DebugUI = debugUI;
                m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");

                var vertices = new Vector3[4];
                vertices[0] = new Vector3(0, 0);
                vertices[1] = new Vector3(0, 1);
                vertices[2] = new Vector3(1, 1);
                vertices[3] = new Vector3(1, 0);
                var indices = new int[] { 0, 1, 2, 3 };

                m_Mesh = new Mesh();
                m_Mesh.vertices = vertices;
                m_Mesh.SetIndices(indices, MeshTopology.Quads, 0);
            }

            private static Func<VisualElement, Rect> GetWorldClipRect()
            {
                var worldClipProp = typeof(VisualElement).GetMethod("get_worldClip", BindingFlags.NonPublic | BindingFlags.Instance);
                if (worldClipProp != null)
                {
                    return delegate (VisualElement elt)
                    {
                        return (Rect)worldClipProp.Invoke(elt, null);
                    };
                }

                Debug.LogError("could not retrieve worldClip");
                return delegate (VisualElement elt)
                {
                    return new Rect();
                };
            }

            private static readonly Func<Box, Rect> k_BoxWorldclip = GetWorldClipRect();

            void DrawMesh()
            {
                if (m_Mat == null)
                {
                    m_Mat = new Material(Shader.Find("Hidden/VFX/SystemStat"));
                    m_ClippingMatrixId = Shader.PropertyToID("_ClipMatrix");
                }
                Color color = new Color(1, 0, 0, 1);
                m_Mat.SetColor("_Color", color);

                var debugRect = m_DebugUI.m_DebugBox.worldBound;
                var clippedDebugRect = k_BoxWorldclip(m_DebugUI.m_DebugBox);
                var windowRect = panel.InternalGetGUIView().position;
                var trans = new Vector4(debugRect.x / windowRect.width, (windowRect.height - (debugRect.y + debugRect.height)) / windowRect.height, 0, 0);
                var scale = new Vector3(debugRect.width / windowRect.width, debugRect.height / windowRect.height, 0);

                var clippedScale = new Vector3(windowRect.width / clippedDebugRect.width, windowRect.height / clippedDebugRect.height, 0);
                var clippedTrans = new Vector3(-clippedDebugRect.x / clippedDebugRect.width, ((clippedDebugRect.y + clippedDebugRect.height) - windowRect.height) / clippedDebugRect.height) ;
                var baseChange = Matrix4x4.TRS(clippedTrans, Quaternion.identity, clippedScale);
                m_Mat.SetMatrix(m_ClippingMatrixId, baseChange);
                /*m_Mat.SetVector("_WinBotLeft", new Vector4(debugRect.x / windowRect.width, (windowRect.height - (debugRect.y + debugRect.height)) / windowRect.height, 0, 0));
                m_Mat.SetFloat("_WinWidth", debugRect.width / windowRect.width);
                m_Mat.SetFloat("_WinHeight", debugRect.height / windowRect.height);*/
                //var clippedScale = new Vector3();
                //var clippedScale = new Vector3();
                m_Mat.SetPass(0);
                Graphics.DrawMeshNow(m_Mesh, Matrix4x4.TRS(trans, Quaternion.identity, scale));
            }

            protected override void ImmediateRepaint()
            {
                DrawMesh();
            }
        }

        VFXComponentBoard m_ComponentBoard;
        CurveContent m_Curve;
        Box m_DebugBox;
        VFXView m_View;

        public VFXDebugUI(VFXView view, Box debugBox)
        {
            m_DebugBox = debugBox;
            m_View = view;
        }

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
            m_Curve = new CurveContent(this);
            m_ComponentBoard.contentContainer.Add(m_Curve);
        }

        void Clear()
        {
            if (m_ComponentBoard != null && m_Curve != null)
                m_ComponentBoard.contentContainer.Remove(m_Curve);
            m_ComponentBoard = null;
            m_Curve = null;
        }
    }
}
