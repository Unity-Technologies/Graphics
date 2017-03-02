using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEditor.RMGUI;
using UnityEditor.VFX;
using Object = UnityEngine.Object;
using Type = System.Type;

namespace UnityEditor.VFX.UI
{

    abstract class PropertyRM : VisualContainer
    {
        public abstract void SetValue(object obj);
        public abstract object GetValue();

        public VisualElement m_Icon;

        Texture2D[] m_IconStates;

        public VisualElement m_Label;

        public PropertyRM(VFXDataAnchorPresenter presenter)
        {
            m_Presenter = presenter;

            m_Icon =  new VisualElement() {name="icon"};
            AddChild(m_Icon);

            if( presenter.expandable)
            {
                m_IconStates = new Texture2D[]{
                    Resources.Load<Texture2D>("VFX/" + presenter.type.Name + "_plus"),
                    Resources.Load<Texture2D>("VFX/" + presenter.type.Name + "_minus")
                };

                if( m_IconStates[0] == null )
                {
                    m_IconStates[0] = Resources.Load<Texture2D>("VFX/Default_plus");
                    m_IconStates[1] = Resources.Load<Texture2D>("VFX/Default_minus");
                }
                m_Icon.AddManipulator(new Clickable(OnExpand));
            }
            else
            {
                m_IconStates = new Texture2D[]{
                    Resources.Load<Texture2D>("VFX/" + presenter.type.Name)
                };

                if( m_IconStates[0] == null )
                {
                    m_IconStates[0] = Resources.Load<Texture2D>("VFX/Default");
                }
            }

            m_Icon.backgroundImage = m_IconStates[0];

            m_Icon.marginLeft = presenter.depth * VFXPropertyIM.depthOffset;

            m_Label = new VisualElement(){name="label",text=presenter.name};
            AddChild(m_Label);

            AddToClassList("propertyrm");
        }

        static Dictionary<Type,Type> m_TypeDictionary =  new Dictionary<Type,Type>
        {
            {typeof(Spaceable),typeof(SpaceablePropertyRM)},
            {typeof(bool),typeof(BoolPropertyRM)},
            {typeof(float),typeof(FloatPropertyRM)}
        };

        public static PropertyRM Create(VFXDataAnchorPresenter presenter)
        {
            Type propertyType = null;

            Type type = presenter.type;

            while ( type != typeof(object) && type != null )
            {

                if( ! m_TypeDictionary.TryGetValue(type,out propertyType))
                {
                    foreach(var inter in type.GetInterfaces())
                    {
                        if( m_TypeDictionary.TryGetValue(inter,out propertyType) )
                        {
                            break;
                        }
                    }
                }
                if( propertyType != null)
                {
                    break;
                }
                type = type.BaseType;
            }

            return propertyType!= null ? System.Activator.CreateInstance(propertyType,new object[]{presenter}) as PropertyRM: null;
        }

        protected void NotifyValueChanged()
        {
            m_Presenter.SetPropertyValue(GetValue());
        }

        void OnExpand()
        {
            if( m_Presenter.expanded )
            {
                m_Presenter.nodePresenter.RetractPath(m_Presenter.path);
            }
            else
            {
                m_Presenter.nodePresenter.ExpandPath(m_Presenter.path);
            }
        }

        protected VFXDataAnchorPresenter m_Presenter;
    }


    abstract class PropertyRM<T> : PropertyRM
    {

        public PropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {}
        public override void SetValue(object obj)
        {
            m_Value = (T)obj;
            UpdateGUI();
        }

        public override object GetValue()
        {
            return m_Value;
        }

        public abstract void UpdateGUI();

        protected T m_Value;
    }
    class SpaceablePropertyRM : PropertyRM<Spaceable>
    {
        public SpaceablePropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            m_Button = new VisualElement(){text=presenter.name};
            m_Button.AddManipulator(new Clickable(OnButtonClick));
            m_Button.AddToClassList("button");
            AddChild(m_Button);
        }

        void OnButtonClick()
        {
            m_Value.space = (CoordinateSpace)((int)(m_Value.space + 1) % (int)CoordinateSpace.SpaceCount);
            NotifyValueChanged();
        }

        public override void UpdateGUI()
        {
            m_Button.text = m_Value.space.ToString();
        }

        VisualElement m_Button;
    }

    class BoolPropertyRM : PropertyRM<bool>
    {
        public BoolPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            m_Toggle =  new Toggle(OnValueChanged);
            AddChild(m_Toggle);
        }

        void OnValueChanged()
        {
            m_Value = m_Toggle.on;
            NotifyValueChanged();
        }
        public override void UpdateGUI()
        {
            m_Toggle.on = m_Value;
        }

        Toggle m_Toggle;
    }



    class DragFloatManipulator : Manipulator
    {
        public DragFloatManipulator(IFloatChangeListener listener,object userdata)
        {
            phaseInterest = EventPhase.Capture;
            m_UserData = userdata;
            m_Listener = listener;
        }
        object m_UserData;
        IFloatChangeListener m_Listener;

        float m_OriginalValue;
        bool m_Dragging;
        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)   
		{
            switch( evt.type)
            {
            case EventType.MouseDown:
                if ( evt.button == 0 )
                {
				    EditorGUIUtility.SetWantsMouseJumping(1);
                    this.TakeCapture();
                    m_Dragging = true;
                    m_OriginalValue = m_Listener.GetValue(m_UserData);
                    return EventPropagation.Stop;
		    	}
            break;
		    case EventType.MouseUp:
			    if ( evt.button == 0 )
                {
                    m_Dragging = false;
                    this.ReleaseCapture();
                    EditorGUIUtility.SetWantsMouseJumping(0);
				    return EventPropagation.Stop;
                }
			break;
		    case EventType.MouseDrag:
                if ( evt.button == 0 && m_Dragging)
                {
                    ApplyDelta(HandleUtility.niceMouseDelta);
                }
			break;
		    case EventType.KeyDown:
                if (m_Dragging && evt.keyCode == KeyCode.Escape)
                {
                    m_Listener.SetValue(m_OriginalValue,m_UserData);
                    m_Dragging = false;
                    return EventPropagation.Stop;
                }
			break;
            }
			return EventPropagation.Continue;
		}
        void ApplyDelta(float delta)
        {
            float value = m_Listener.GetValue(m_UserData);

            value += delta * 1.0f;

            m_Listener.SetValue(value,m_UserData);
        }
    }


    interface IFloatChangeListener
    {
        float GetValue(object userData);

        void SetValue(float value,object userData);
    }

    class FloatPropertyRM : PropertyRM<float>, IFloatChangeListener
    {
        public FloatPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            m_TextField = new EditorTextField(20,false,false,'*');
            m_TextField.onTextChanged = OnTextChanged;
            m_TextField.useStylePainter = true;
            AddChild(m_TextField);

            m_Label.AddManipulator(new DragFloatManipulator(this,null));


        }
        public override void UpdateGUI()
        {
            m_TextField.text = m_Value.ToString("0.###");
        }

        public float GetValue(object userData)
        {
            float newValue = 0;
            if( ! float.TryParse(m_TextField.text,out newValue) )
            {
                newValue = 0;
            }
            return newValue;
        }

        public void SetValue(float value,object userData)
        {
            m_Value = value;
            //UpdateGUI();
            NotifyValueChanged();
        }

        public void OnTextChanged()
        {
            float newValue = GetValue(null);
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        EditorTextField m_TextField;
    }
}