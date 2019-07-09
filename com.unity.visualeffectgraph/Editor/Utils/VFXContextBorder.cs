using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    public class VFXContextBorderFactory : UxmlFactory<VFXContextBorder>
    {}

    public class VFXContextBorder : ImmediateModeElement, IDisposable
    {
        Material m_Mat;

        static Mesh s_Mesh;

        public VFXContextBorder()
        {
            RecreateResources();
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void RecreateResources()
        {
            if (s_Mesh == null)
            {
                s_Mesh = new Mesh();
                int verticeCount = 16;

                var vertices = new Vector3[verticeCount];
                var uvsBorder = new Vector2[verticeCount];

                for (int ix = 0; ix < 4; ++ix)
                {
                    for (int iy = 0; iy < 4; ++iy)
                    {
                        vertices[ix + iy * 4] = new Vector3(ix < 2 ? -1 : 1, iy < 2 ? -1 : 1, 0);
                        uvsBorder[ix + iy * 4] = new Vector2(ix == 0 || ix == 3 ? 1 : 0, iy == 0 || iy == 3 ? 1 : 0);
                    }
                }

                var indices = new int[4 * 8];

                for (int ix = 0; ix < 3; ++ix)
                {
                    for (int iy = 0; iy < 3; ++iy)
                    {
                        int quadIndex = (ix + iy * 3);
                        if (quadIndex == 4)
                            continue;
                        else if (quadIndex > 4)
                            --quadIndex;

                        int vertIndex = quadIndex * 4;
                        indices[vertIndex] = ix + iy * 4;
                        indices[vertIndex + 1] = ix + (iy + 1) * 4;
                        indices[vertIndex + 2] = ix + 1 + (iy + 1) * 4;
                        indices[vertIndex + 3] = ix + 1 + iy * 4;
                    }
                }

                s_Mesh.vertices = vertices;
                s_Mesh.uv = uvsBorder;
                s_Mesh.SetIndices(indices, MeshTopology.Quads, 0);
            }
            if(m_Mat == null)
                m_Mat = new Material(Shader.Find("Hidden/VFX/GradientBorder"));
        }

        void IDisposable.Dispose()
        {
            UnityObject.DestroyImmediate(m_Mat);
        }

        Color m_StartColor;
        public Color startColor
        {
            get { return m_StartColor; }
        }

        Color m_EndColor;
        public Color endColor
        {
            get { return m_EndColor; }
        }

        static readonly CustomStyleProperty<Color> s_StartColorProperty = new CustomStyleProperty<Color>("--start-color");
        static readonly CustomStyleProperty<Color> s_EndColorProperty = new CustomStyleProperty<Color>("--end-color");
        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var customStyle = e.customStyle;
            customStyle.TryGetValue(s_StartColorProperty, out m_StartColor);
            customStyle.TryGetValue(s_EndColorProperty, out m_EndColor);
        }

        protected override void ImmediateRepaint()
        {
            RecreateResources();
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view != null && m_Mat != null)
            {
                float radius = resolvedStyle.borderTopLeftRadius;

                float realBorder = style.borderLeftWidth.value * view.scale;

                Vector4 size = new Vector4(layout.width * .5f, layout.height * 0.5f, 0, 0);
                m_Mat.SetVector("_Size", size);
                m_Mat.SetFloat("_Border", realBorder < 1.75f ?  1.75f / view.scale : style.borderLeftWidth.value);
                m_Mat.SetFloat("_Radius", radius);

                m_Mat.SetColor("_ColorStart", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? startColor.gamma : startColor);
                m_Mat.SetColor("_ColorEnd", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? endColor.gamma : endColor);

                m_Mat.SetPass(0);

                Graphics.DrawMeshNow(s_Mesh, Matrix4x4.Translate(new Vector3(size.x, size.y, 0)));
            }
        }
    }
}
