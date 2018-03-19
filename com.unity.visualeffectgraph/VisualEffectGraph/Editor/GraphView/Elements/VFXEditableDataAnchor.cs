using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    public class VFXDataGUIStyles
    {
        public static VFXDataGUIStyles instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new VFXDataGUIStyles();
                return s_Instance;
            }
        }

        static VFXDataGUIStyles s_Instance;

        public GUIStyle baseStyle;

        VFXDataGUIStyles()
        {
            baseStyle = GUI.skin.textField;
        }

        public GUIStyle GetGUIStyleForExpandableType(Type type)
        {
            GUIStyle style = null;

            if (typeStyles.TryGetValue(type, out style))
            {
                return style;
            }

            GUIStyle typeStyle = new GUIStyle(baseStyle);
            typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_plus");
            if (typeStyle.normal.background == null)
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default_plus");
            typeStyle.active.background = typeStyle.focused.background = null;
            typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_minus");
            if (typeStyle.onNormal.background == null)
                typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/Default_minus");
            typeStyle.border.top = 0;
            typeStyle.border.left = 0;
            typeStyle.border.bottom = typeStyle.border.right = 0;
            typeStyle.padding.top = 3;

            typeStyles.Add(type, typeStyle);


            return typeStyle;
        }

        public GUIStyle GetGUIStyleForType(Type type)
        {
            GUIStyle style = null;

            if (typeStyles.TryGetValue(type, out style))
            {
                return style;
            }

            GUIStyle typeStyle = new GUIStyle(baseStyle);
            typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name);
            if (typeStyle.normal.background == null)
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default");
            typeStyle.active.background = typeStyle.focused.background = null;
            typeStyle.border.top = 0;
            typeStyle.border.left = 0;
            typeStyle.border.bottom = typeStyle.border.right = 0;

            typeStyles.Add(type, typeStyle);


            return typeStyle;
        }

        static Dictionary<Type, GUIStyle> typeStyles = new Dictionary<Type, GUIStyle>();

        public void Reset()
        {
            typeStyles.Clear();
        }

        public float lineHeight
        { get { return 16; } }
    }

    partial class VFXEditableDataAnchor : VFXDataAnchor
    {
        IMGUIContainer  m_Container;


        PropertyRM      m_PropertyRM;

        VFXView m_View;


        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXEditableDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            Profiler.BeginSample("VFXEditableDataAnchor.Create");
            var anchor = new VFXEditableDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            Profiler.EndSample();
            return anchor;
        }

        protected VFXEditableDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            m_View = GetFirstAncestorOfType<VFXView>();
            m_View.allDataAnchors.Add(this);
        }

        void OnDetachFromPanel(DetachFromPanelEvent e)
        {
            if (m_View != null)
                m_View.allDataAnchors.Remove(this);
        }

        public float GetPreferredLabelWidth()
        {
            if (m_PropertyRM == null) return 0;
            return m_PropertyRM.GetPreferredLabelWidth();
        }

        public float GetPreferredControlWidth()
        {
            if (m_PropertyRM == null) return 0;
            return m_PropertyRM.GetPreferredControlWidth();
        }

        public void SetLabelWidth(float label)
        {
            m_PropertyRM.SetLabelWidth(label);
        }

        public void ForceUpdate()
        {
            m_PropertyRM.ForceUpdate();
        }

        void BuildProperty()
        {
            if (m_PropertyRM != null)
            {
                Remove(m_PropertyRM);
            }
            m_PropertyRM = PropertyRM.Create(controller, 100);
            if (m_PropertyRM != null)
            {
                Add(m_PropertyRM);
                if (m_Container != null)
                    Remove(m_Container);
                m_Container = null;
            }
        }

        Type m_EditedType;

        public override void SelfChange(int change)
        {
            base.SelfChange(change);

            if (m_PropertyRM == null || !m_PropertyRM.IsCompatible(controller))
            {
                BuildProperty();
                m_EditedType = controller.portType;
            }

            OnRecompile();
        }

        public void OnRecompile()
        {
            if (m_PropertyRM != null && controller != null)
            {
                controller.UpdateInfos();
                m_PropertyRM.propertyEnabled = controller.editable && controller.expandedInHierachy;
                m_PropertyRM.Update();
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return rect.Contains(localPoint);
        }
    }
}
