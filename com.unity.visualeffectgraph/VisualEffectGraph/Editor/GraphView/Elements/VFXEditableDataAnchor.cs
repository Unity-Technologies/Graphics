using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;
using Type = System.Type;

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


        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static new VFXEditableDataAnchor Create(VFXDataAnchorPresenter presenter)
        {
            var anchor = new VFXEditableDataAnchor(presenter.orientation, presenter.direction, presenter.portType);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = presenter;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXEditableDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
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

        void BuildProperty()
        {
            VFXDataAnchorPresenter presenter = controller;
            if (m_PropertyRM != null)
            {
                Remove(m_PropertyRM);
            }

            m_PropertyRM = PropertyRM.Create(presenter, 100);
            if (m_PropertyRM != null)
            {
                Add(m_PropertyRM);
                if (m_Container != null)
                    Remove(m_Container);
                m_Container = null;
            }
        }

        Type m_EditedType;

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            VFXDataAnchorPresenter presenter = controller;

            if (m_PropertyRM == null || m_EditedType != presenter.portType)
            {
                BuildProperty();
                m_EditedType = presenter.portType;
            }

            OnRecompile();
        }

        public void OnRecompile()
        {
            VFXDataAnchorPresenter presenter = controller;
            if (m_PropertyRM != null && presenter != null)
            {
                m_PropertyRM.propertyEnabled = presenter.editable && !presenter.collapsed;
                m_PropertyRM.Update();
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return rect.Contains(localPoint);
        }
    }
}
