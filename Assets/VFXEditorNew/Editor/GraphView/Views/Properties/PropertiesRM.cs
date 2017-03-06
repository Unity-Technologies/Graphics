using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;
using UnityEditor.RMGUI;
using UnityEditor.VFX;
using UnityEditor.VFX.RMGUI;
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
            {typeof(float),typeof(FloatPropertyRM)},
            {typeof(Vector2),typeof(Vector2PropertyRM)},
            {typeof(Vector3),typeof(Vector3PropertyRM)},
            {typeof(Vector4),typeof(Vector4PropertyRM)},
            {typeof(Color),typeof(ColorPropertyRM)}
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




    class FloatPropertyRM : PropertyRM<float>
    {
        public FloatPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            m_FloatField = new FloatField(m_Label);
            m_FloatField.onValueChanged = OnValueChanged;
            
            AddChild(m_FloatField);
        }

        public void OnValueChanged()
        {
            float newValue = m_FloatField.GetValue();
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI()
        {
            m_FloatField.SetValue(m_Value);
        }

        FloatField m_FloatField;
    }


    class Vector2PropertyRM : PropertyRM<Vector2>
    {
        public Vector2PropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_XFloatField = new FloatField("X");
            m_XFloatField.onValueChanged = OnValueChanged;

            m_YFloatField = new FloatField("Y");
            m_YFloatField.onValueChanged = OnValueChanged;
             

            VisualElement spacer = new VisualElement(){flex=1};

            fieldContainer.AddChild(spacer);
            fieldContainer.AddChild(m_XFloatField);
            fieldContainer.AddChild(m_YFloatField);

            AddChild(fieldContainer);
        }

        public void OnValueChanged()
        {
            Vector2 newValue = new Vector2(m_XFloatField.GetValue(),m_YFloatField.GetValue());
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        FloatField m_XFloatField;
        FloatField m_YFloatField;
        public override void UpdateGUI()
        {
            m_XFloatField.SetValue(m_Value.x);
            m_YFloatField.SetValue(m_Value.y);
        }
    }
    class Vector3PropertyRM : PropertyRM<Vector3>
    {
        public Vector3PropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_XFloatField = new FloatField("X");
            m_XFloatField.onValueChanged = OnValueChanged;

            m_YFloatField = new FloatField("Y");
            m_YFloatField.onValueChanged = OnValueChanged;

            m_ZFloatField = new FloatField("Z");
            m_ZFloatField.onValueChanged = OnValueChanged;
             

            VisualElement spacer = new VisualElement(){flex=1};

            fieldContainer.AddChild(spacer);
            fieldContainer.AddChild(m_XFloatField);
            fieldContainer.AddChild(m_YFloatField);
            fieldContainer.AddChild(m_ZFloatField);

            AddChild(fieldContainer);
        }

        public void OnValueChanged()
        {
            Vector3 newValue = new Vector3(m_XFloatField.GetValue(),m_YFloatField.GetValue(),m_ZFloatField.GetValue());
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        FloatField m_XFloatField;
        FloatField m_YFloatField;
        FloatField m_ZFloatField;
        public override void UpdateGUI()
        {
            m_XFloatField.SetValue(m_Value.x);
            m_YFloatField.SetValue(m_Value.y);
            m_ZFloatField.SetValue(m_Value.z);
        }
    }
    class Vector4PropertyRM : PropertyRM<Vector4>
    {
        public Vector4PropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_XFloatField = new FloatField("X");
            m_XFloatField.onValueChanged = OnValueChanged;

            m_YFloatField = new FloatField("Y");
            m_YFloatField.onValueChanged = OnValueChanged;

            m_ZFloatField = new FloatField("Z");
            m_ZFloatField.onValueChanged = OnValueChanged;

            m_WFloatField = new FloatField("W");
            m_WFloatField.onValueChanged = OnValueChanged;
             

            VisualElement spacer = new VisualElement(){flex=1};

            fieldContainer.AddChild(spacer);
            fieldContainer.AddChild(m_XFloatField);
            fieldContainer.AddChild(m_YFloatField);
            fieldContainer.AddChild(m_ZFloatField);
            fieldContainer.AddChild(m_WFloatField);

            AddChild(fieldContainer);
        }

        public void OnValueChanged()
        {
            Vector4 newValue = new Vector4(m_XFloatField.GetValue(),m_YFloatField.GetValue(),m_ZFloatField.GetValue(),m_WFloatField.GetValue());
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        FloatField m_XFloatField;
        FloatField m_YFloatField;
        FloatField m_ZFloatField;
        FloatField m_WFloatField;
        public override void UpdateGUI()
        {
            m_XFloatField.SetValue(m_Value.x);
            m_YFloatField.SetValue(m_Value.y);
            m_ZFloatField.SetValue(m_Value.z);
            m_WFloatField.SetValue(m_Value.w);
        }
    }
    class ColorPropertyRM : PropertyRM<Color>
    {
        public ColorPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            VisualContainer mainContainer = new VisualContainer();

            m_ColorField = new ColorField(m_Label);
            m_ColorField.onValueChanged = OnValueChanged;

            mainContainer.AddChild(m_ColorField);

            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_XFloatField = new FloatField("R");
            m_XFloatField.onValueChanged = OnValueChanged;

            m_YFloatField = new FloatField("G");
            m_YFloatField.onValueChanged = OnValueChanged;

            m_ZFloatField = new FloatField("B");
            m_ZFloatField.onValueChanged = OnValueChanged;

            m_WFloatField = new FloatField("A");
            m_WFloatField.onValueChanged = OnValueChanged;


            VisualElement spacer = new VisualElement(){flex=1};

            fieldContainer.AddChild(spacer);
            fieldContainer.AddChild(m_XFloatField);
            fieldContainer.AddChild(m_YFloatField);
            fieldContainer.AddChild(m_ZFloatField);
            fieldContainer.AddChild(m_WFloatField);

            mainContainer.AddChild(fieldContainer);

            mainContainer.flexDirection = FlexDirection.Column;
            mainContainer.alignItems = Align.Stretch;
            mainContainer.minWidth= 100;
            mainContainer.minHeight = 13;

            AddChild(mainContainer);
        }

        public void OnValueChanged()
        {
            /*Color newValue = new Color(m_XFloatField.GetValue(),m_YFloatField.GetValue(),m_ZFloatField.GetValue(),m_WFloatField.GetValue());
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }*/
        }

        FloatField m_XFloatField;
        FloatField m_YFloatField;
        FloatField m_ZFloatField;
        FloatField m_WFloatField;

        ColorField m_ColorField;
        public override void UpdateGUI()
        {
            m_ColorField.SetValue(m_Value);
            m_XFloatField.SetValue(m_Value.r);
            m_YFloatField.SetValue(m_Value.g);
            m_ZFloatField.SetValue(m_Value.b);
            m_WFloatField.SetValue(m_Value.a);
        }
    }
}