using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class VFXSystemBorderFactory : UxmlFactory<VFXSystemBorder>
    {}




    class VFXSystemBorder : GraphElement, IControlledElement<VFXSystemController>, IDisposable
    {
        class Content : ImmediateModeElement
        {
            VFXSystemBorder m_Border;

            public Content(VFXSystemBorder border)
            {
                m_Border = border;
            }

            protected override void ImmediateRepaint()
            {
                m_Border.RecreateResources();
                VFXView view = GetFirstAncestorOfType<VFXView>();
                if (view != null && m_Border.m_Mat != null)
                {
                    float radius = m_Border.resolvedStyle.borderTopLeftRadius;

                    float realBorder = m_Border.resolvedStyle.borderLeftWidth * view.scale;

                    Vector4 size = new Vector4(layout.width * .5f, layout.height * 0.5f, 0, 0);
                    m_Border.m_Mat.SetVector("_Size", size);
                    m_Border.m_Mat.SetFloat("_Border", realBorder < 1.75f ? 1.75f / view.scale : m_Border.resolvedStyle.borderLeftWidth);
                    m_Border.m_Mat.SetFloat("_Radius", radius);

                    float opacity = m_Border.resolvedStyle.opacity;


                    Color start = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? m_Border.startColor.gamma : m_Border.startColor;
                    start.a *= opacity;
                    m_Border.m_Mat.SetColor("_ColorStart", start);
                    Color end = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? m_Border.endColor.gamma : m_Border.endColor;
                    end.a *= opacity;
                    m_Border.m_Mat.SetColor("_ColorEnd", end);

                    Color middle = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? m_Border.middleColor.gamma : m_Border.middleColor;
                    middle.a *= opacity;
                    m_Border.m_Mat.SetColor("_ColorMiddle", middle);

                    m_Border.m_Mat.SetPass(0);

                    Graphics.DrawMeshNow(s_Mesh, Matrix4x4.Translate(new Vector3(size.x, size.y, 0)));
                }
            }
        }

        Material m_Mat;

        static Mesh s_Mesh;

        public VFXSystemBorder()
        {
            RecreateResources();

            var tpl = VFXView.LoadUXML("VFXSystemBorder");
            tpl.CloneTree(this);

            this.AddStyleSheetPath("VFXSystemBorder");

            this.style.overflow = Overflow.Visible;

            m_Title = this.Query<Label>("title");
            m_TitleField = this.Query<TextField>("title-field");

            m_TitleField.style.display = DisplayStyle.None;

            m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);

            m_TitleField.Q("unity-text-input").RegisterCallback < FocusOutEvent >(OnTitleBlur);
            m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            m_Title.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);

            Content content = new Content(this);
            content.style.position = UnityEngine.UIElements.Position.Absolute;
            content.style.top = content.style.left = content.style.right = content.style.bottom = 0f;
            content.pickingMode = PickingMode.Ignore;

            pickingMode = PickingMode.Ignore;
            Add(content);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }
        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        public void OnRename()
        {
            m_TitleField.RemoveFromClassList("empty");
            m_TitleField.value = m_Title.text;

            m_TitleField.style.display = DisplayStyle.Flex;
            UpdateTitleFieldRect();
            m_TitleField.Q(TextField.textInputUssName).Focus();
            m_TitleField.SelectAll();
        }


        Label m_Title;
        TextField m_TitleField;


        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                OnRename();
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        void OnTitleRelayout(GeometryChangedEvent e)
        {
            UpdateTitleFieldRect();
            RecomputeBounds();
        }

        void UpdateTitleFieldRect()
        {
            Rect rect = m_Title.layout;

            m_Title.parent.ChangeCoordinatesTo(m_TitleField.parent, rect);


            m_TitleField.style.top = rect.yMin - 6;
            m_TitleField.style.left = rect.xMin - 5;
            m_TitleField.style.right = m_Title.resolvedStyle.marginRight + m_Title.resolvedStyle.borderRightWidth + 2;
            m_TitleField.style.height = rect.height - m_Title.resolvedStyle.marginTop - m_Title.resolvedStyle.marginBottom;
        }

        void OnTitleBlur(FocusOutEvent e)
        {
            title = m_TitleField.value;
            m_TitleField.style.display = DisplayStyle.None;

            VFXView view = GetFirstAncestorOfType<VFXView>();

            controller.title = title;
        }

        void OnContextChanged(GeometryChangedEvent e)
        {
            RecomputeBounds();
        }

        void OnTitleChange(ChangeEvent<string> e)
        {
            title = m_TitleField.value;
            if ( string.IsNullOrEmpty(e.previousValue) != string.IsNullOrEmpty(e.newValue) )
            {
                RecomputeBounds();
            }
        }

        public override string title {
            get
            {
                return m_Title.text;
            }
            set
            {
                if(m_Title.text != value)
                {
                    m_Title.text = value;
                }
            }
        }

        public bool m_WaitingRecompute;

        public void RecomputeBounds()
        {
            if (m_WaitingRecompute)
                return;
            //title width should be at least as wide as a context to be valid.
            float titleWidth = m_Title.layout.width;
            bool invalidTitleWidth = float.IsNaN(titleWidth) || titleWidth < 50;
            bool titleEmpty = string.IsNullOrEmpty(m_Title.text) || invalidTitleWidth;
            if (titleEmpty )
            {
                m_Title.AddToClassList("empty");
            }
            else
            {
                m_Title.RemoveFromClassList("empty");
            }

            Rect rect = Rect.zero;

            if(m_Contexts != null)
            {
                foreach (var context in m_Contexts)
                {
                    if( context != null)
                    {
                        if (rect == Rect.zero)
                        {
                            rect = context.localBound;
                        }
                        else
                        {
                            rect = RectUtils.Encompass(rect, context.GetPosition());
                        }
                    }
                }
            }

            if (float.IsNaN(rect.xMin) || float.IsNaN(rect.yMin) || float.IsNaN(rect.width) || float.IsNaN(rect.height))
                rect = Rect.zero;

            rect = RectUtils.Inflate(rect, 20, titleEmpty ? 20 : m_Title.layout.height, 20, 20);

            if(invalidTitleWidth)
            {
                SetPosition(rect);
                if( !m_WaitingRecompute)
                {
                    m_WaitingRecompute = true;
                    schedule.Execute(()=> { m_WaitingRecompute = false; RecomputeBounds();  }).ExecuteLater(0); // title height might have changed if width have changed
                }
            }
            else
            {
                SetPosition(rect);
            }
        }

        VFXContextUI[] m_Contexts;
        private VFXContextUI[] contexts
        {
            get
            {
                return m_Contexts;
            }
            set
            {
                if( m_Contexts != null)
                {
                    foreach (var context in m_Contexts )
                    {
                        context.UnregisterCallback<GeometryChangedEvent>(OnContextChanged);
                    }
                }
                m_Contexts = value;
                if (m_Contexts != null)
                {
                    foreach (var context in m_Contexts)
                    {
                        context.RegisterCallback<GeometryChangedEvent>(OnContextChanged);
                    }
                }
                RecomputeBounds();
            }
        }

        void RecreateResources()
        {
            if (s_Mesh == null)
            {
                s_Mesh = new Mesh();
                int verticeCount = 20;

                var vertices = new Vector3[verticeCount];
                var uvsBorder = new Vector2[verticeCount];
                var uvsDistance = new Vector2[verticeCount];

                for (int ix = 0; ix < 4; ++ix)
                {
                    for (int iy = 0; iy < 4 ; ++iy)
                    {
                        vertices[ix + iy * 4] = new Vector3(ix < 2 ? -1 : 1, iy < 2 ? -1 : 1, 0);
                        uvsBorder[ix + iy * 4] = new Vector2(ix == 0 || ix == 3 ? 1 : 0, iy == 0 || iy == 3 ? 1 : 0);
                        uvsDistance[ix + iy * 4] = new Vector2(iy < 2 ? ix / 2 : 2 - ix / 2, iy < 2 ? 0 : 1);
                    }
                }

                for(int i = 16; i < 20; ++i)
                {
                    vertices[i] = vertices[i - 16];
                    uvsBorder[i] = uvsBorder[i - 16];
                    uvsDistance[i] = new Vector2(2, 2);
                }

                vertices[16] = vertices[0];
                vertices[17] = vertices[1];
                vertices[18] = vertices[4];
                vertices[19] = vertices[5];

                uvsBorder[16] = uvsBorder[0];
                uvsBorder[17] = uvsBorder[1];
                uvsBorder[18] = uvsBorder[4];
                uvsBorder[19] = uvsBorder[5];

                uvsDistance[16] = new Vector2(2, 2);
                uvsDistance[17] = new Vector2(2, 2);
                uvsDistance[18] = new Vector2(2, 2);
                uvsDistance[19] = new Vector2(2, 2);

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
                        if (quadIndex == 3)
                        {
                            indices[vertIndex] = 18;
                            indices[vertIndex + 3] = 19;
                        }
                    }
                }

                s_Mesh.vertices = vertices;
                s_Mesh.uv = uvsBorder;
                s_Mesh.uv2 = uvsDistance;
                s_Mesh.SetIndices(indices, MeshTopology.Quads, 0);
            }
            if( m_Mat == null)
                m_Mat = new Material(Shader.Find("Hidden/VFX/GradientDashedBorder"));
        }

        void IDisposable.Dispose()
        {
            UnityObject.DestroyImmediate(m_Mat);
        }

        Color m_StartColor;
        public Color startColor
        {
            get
            {
                return m_StartColor;
            }
            set
            {
                m_StartColor = value;
            }
        }
        Color m_EndColor;
        public Color endColor
        {
            get
            {
                return m_EndColor;
            }
            set
            {
                m_EndColor = value;
            }
        }
        Color m_MiddleColor;
        public Color middleColor
        {
            get
            {
                return m_MiddleColor;
            }
            set
            {
                m_MiddleColor = value;
            }
        }

        static readonly CustomStyleProperty<Color> s_StartColorProperty = new CustomStyleProperty<Color>("--start-color");
        static readonly CustomStyleProperty<Color> s_EndColorProperty = new CustomStyleProperty<Color>("--end-color");
        static readonly CustomStyleProperty<Color> s_MiddleColorProperty = new CustomStyleProperty<Color>("--middle-color");
        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var customStyle = e.customStyle;
            customStyle.TryGetValue(s_StartColorProperty, out m_StartColor);
            customStyle.TryGetValue(s_EndColorProperty, out m_EndColor);
            customStyle.TryGetValue(s_MiddleColorProperty, out m_MiddleColor);
        }

        VFXSystemController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXSystemController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }

        public void Update()
        {
            VFXView view = GetFirstAncestorOfType<VFXView>();
            if (view == null || m_Controller == null)
                return;
            contexts = controller.contexts.Select(t => view.GetGroupNodeElement(t) as VFXContextUI).ToArray();

            title = controller.title;
        }
        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            Update();
        }
    }
}
