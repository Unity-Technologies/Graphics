using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
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

        public void Update()
        {
            m_Icon.backgroundImage = m_IconStates[m_Presenter.expanded && m_Presenter.expandable ? 1 : 0];
            SetValue(m_Presenter.value);
            m_Label.text = m_Presenter.name;
        }

        public PropertyRM(VFXDataAnchorPresenter presenter)
        {
            m_Presenter = presenter;

            m_Icon =  new VisualElement() {name="icon"};
            AddChild(m_Icon);

            if( presenter.expandable)
            {
                m_IconStates = new Texture2D[]{
                    Resources.Load<Texture2D>("VFX/" + presenter.anchorType.Name + "_plus"),
                    Resources.Load<Texture2D>("VFX/" + presenter.anchorType.Name + "_minus")
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
                    Resources.Load<Texture2D>("VFX/" + presenter.anchorType.Name)
                };

                if( m_IconStates[0] == null )
                {
                    m_IconStates[0] = Resources.Load<Texture2D>("VFX/Default");
                }
            }

            m_Icon.backgroundImage = m_IconStates[0];


            m_Label = new VisualElement(){name="label",text=presenter.name};
            if (presenter.depth != 0)
            {
                for(int i = 0; i < presenter.depth; ++i)
                {
                    VisualElement line = new VisualElement()
                    {
                        width = 1,
                        name = "line",
                        marginLeft= 0.5f * VFXPropertyIM.depthOffset,
                        marginRight=VFXPropertyIM.depthOffset * 0.5f
                    };
                    AddChild(line);
                }
            }
            //m_Label.marginLeft = presenter.depth * VFXPropertyIM.depthOffset;
            AddChild(m_Label);

            AddToClassList("propertyrm");
        }

        static Dictionary<Type,Type> m_TypeDictionary =  new Dictionary<Type,Type>
        {
            {typeof(Vector),typeof(VectorPropertyRM)},
            {typeof(Position),typeof(PositionPropertyRM)},
            {typeof(Spaceable),typeof(SpaceablePropertyRM<Spaceable>)},
            {typeof(bool),typeof(BoolPropertyRM)},
            {typeof(float),typeof(FloatPropertyRM)},
            {typeof(int),typeof(IntPropertyRM)},
            {typeof(Vector2),typeof(Vector2PropertyRM)},
            {typeof(Vector3),typeof(Vector3PropertyRM)},
            {typeof(Vector4),typeof(Vector4PropertyRM)},
            {typeof(Color),typeof(ColorPropertyRM)},
            {typeof(AnimationCurve),typeof(CurvePropertyRM)}
        };

        public static PropertyRM Create(VFXDataAnchorPresenter presenter)
        {
            Type propertyType = null;

            Type type = presenter.anchorType;

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
                m_Presenter.RetractPath();
            }
            else
            {
                m_Presenter.ExpandPath();
            }
        }

        protected VFXDataAnchorPresenter m_Presenter;
    }

    interface IFloatNAffector<T>
    {
        T GetValue(object floatN);
    }

    class FloatNAffector : IFloatNAffector<float>, IFloatNAffector<Vector2>, IFloatNAffector<Vector3>, IFloatNAffector<Vector4>
    {
        float IFloatNAffector<float>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }
        Vector2 IFloatNAffector<Vector2>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }
        Vector3 IFloatNAffector<Vector3>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }
        Vector4 IFloatNAffector<Vector4>.GetValue(object floatN)
        {
            return (FloatN)floatN;
        }

        public static FloatNAffector Default = new FloatNAffector();
    }

    abstract class PropertyRM<T> : PropertyRM
    {

        public PropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {}
        public override void SetValue(object obj)
        {
            if (obj != null)
            {
                if (obj is FloatN)
                {
                    m_Value = ((IFloatNAffector<T>)FloatNAffector.Default).GetValue(obj);
                }
                else
                {
                    m_Value = (T)obj;
                }
            }
            UpdateGUI();
        }

        public override object GetValue()
        {
            return m_Value;
        }

        public abstract void UpdateGUI();

        protected T m_Value;
    }

    class BoolPropertyRM : PropertyRM<bool>
    {
        public BoolPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            m_Toggle =  new Toggle(OnValueChanged);
            AddChild(m_Toggle);


            m_Toggle.enabled = enabled;
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


        public override bool enabled
        {
            set
            {
                base.enabled = value;

                if( m_Toggle != null)
                    m_Toggle.enabled = value;
            }
        }

        Toggle m_Toggle;
    }




    class FloatPropertyRM : PropertyRM<float>
    {
        public FloatPropertyRM(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            m_FloatField = new FloatField(m_Label);
            m_FloatField.onValueChanged = OnValueChanged;

            AddChild(m_FloatField);

            m_FloatField.enabled = enabled;
        }

        public void OnValueChanged()
        {
            float newValue = m_FloatField.GetValue();
            if (newValue != m_Value)
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

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if(m_FloatField != null)
                    m_FloatField.enabled = value;
            }
        }
    }

    class IntPropertyRM : PropertyRM<int>
    {
        public IntPropertyRM(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            m_IntField = new IntField(m_Label);
            m_IntField.onValueChanged = OnValueChanged;

            AddChild(m_IntField);

            m_IntField.enabled = enabled;
        }

        public void OnValueChanged()
        {
            int newValue = m_IntField.GetValue();
            if (newValue != m_Value)
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
        }

        public override void UpdateGUI()
        {
            m_IntField.SetValue(m_Value);
        }

        IntField m_IntField;

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_IntField != null)
                    m_IntField.enabled = value;
            }
        }
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

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_XFloatField != null)
                    m_XFloatField.enabled = value;
                if (m_YFloatField != null)
                    m_YFloatField.enabled = value;
            }
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

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_XFloatField != null)
                    m_XFloatField.enabled = value;
                if (m_YFloatField != null)
                    m_YFloatField.enabled = value;
                if (m_ZFloatField != null)
                    m_ZFloatField.enabled = value;
            }
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
             
            /*
            VisualElement spacer = new VisualElement(){flex=1};

            fieldContainer.AddChild(spacer);*/
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

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_XFloatField != null)
                    m_XFloatField.enabled = value;
                if (m_YFloatField != null)
                    m_YFloatField.enabled = value;
                if (m_ZFloatField != null)
                    m_ZFloatField.enabled = value;
                if (m_WFloatField != null)
                    m_ZFloatField.enabled = value;
            }
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
            mainContainer.AddToClassList("maincontainer");

            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_RFloatField = new FloatField("R");
            m_RFloatField.onValueChanged = OnValueChanged;

            m_GFloatField = new FloatField("G");
            m_GFloatField.onValueChanged = OnValueChanged;

            m_BFloatField = new FloatField("B");
            m_BFloatField.onValueChanged = OnValueChanged;

            m_AFloatField = new FloatField("A");
            m_AFloatField.onValueChanged = OnValueChanged;

            fieldContainer.AddChild(m_RFloatField);
            fieldContainer.AddChild(m_GFloatField);
            fieldContainer.AddChild(m_BFloatField);
            fieldContainer.AddChild(m_AFloatField);

            mainContainer.AddChild(fieldContainer);

            mainContainer.flexDirection = FlexDirection.Column;
            mainContainer.alignItems = Align.Stretch;
            AddChild(mainContainer);
        }

        public void OnValueChanged()
        {
            Color newValue = new Color(m_RFloatField.GetValue(),m_GFloatField.GetValue(),m_BFloatField.GetValue(),m_AFloatField.GetValue());
            if( newValue != m_Value )
            {
                m_Value = newValue;
                NotifyValueChanged();
            }
            else
            {
                newValue = m_ColorField.GetValue();
                if(newValue != m_Value)
                {
                    m_Value = newValue;
                    NotifyValueChanged();
                }
            }
        }

        FloatField m_RFloatField;
        FloatField m_GFloatField;
        FloatField m_BFloatField;
        FloatField m_AFloatField;
        ColorField m_ColorField;

        public override void UpdateGUI()
        {
            m_ColorField.SetValue(m_Value);
            m_RFloatField.SetValue(m_Value.r);
            m_GFloatField.SetValue(m_Value.g);
            m_BFloatField.SetValue(m_Value.b);
            m_AFloatField.SetValue(m_Value.a);
        }

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_RFloatField != null)
                    m_RFloatField.enabled = value;
                if (m_GFloatField != null)
                    m_GFloatField.enabled = value;
                if (m_BFloatField != null)
                    m_BFloatField.enabled = value;
                if (m_AFloatField != null)
                    m_AFloatField.enabled = value;
                if (m_ColorField != null)
                    m_ColorField.enabled = value;
            }
        }
    }


    class CurvePropertyRM : PropertyRM<AnimationCurve>
    {
        public CurvePropertyRM(VFXDataAnchorPresenter presenter) : base(presenter)
        {
            m_CurveField = new CurveField(m_Label);
            m_CurveField.onValueChanged = OnValueChanged;

            AddChild(m_CurveField);
        }

        public void OnValueChanged()
        {
            m_Value = m_CurveField.GetValue();
            NotifyValueChanged();
        }

        CurveField m_CurveField;
        public override void UpdateGUI()
        {
            m_CurveField.SetValue(m_Value);
        }

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_CurveField != null)
                    m_CurveField.enabled = value;
            }
        }
    }


    class SpaceablePropertyRM<T> : PropertyRM<T> where T : Spaceable
    {
        public SpaceablePropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            m_Button = new VisualElement(){text="L"};
            m_Button.AddManipulator(new Clickable(OnButtonClick));
            m_Button.AddToClassList("button");
            AddChild(m_Button);
            AddToClassList("spaceablepropertyrm");
        }

        void OnButtonClick()
        {
            m_Value.space = (CoordinateSpace)((int)(m_Value.space + 1) % (int)CoordinateSpace.SpaceCount);
            NotifyValueChanged();
        }

        public override void UpdateGUI()
        {
            if(m_Value != null)
                m_Button.text = m_Value.space.ToString().Substring(0,1);
        }

        VisualElement m_Button;

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_Button != null)
                    m_Button.enabled = value;
            }
        }
    }

    abstract class Vector3SpacabledPropertyRM<T> : SpaceablePropertyRM<T> where T : Spaceable
    {
        public Vector3SpacabledPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
            VisualContainer fieldContainer = new VisualContainer();
            fieldContainer.AddToClassList("fieldContainer");

            m_XFloatField = new FloatField("X");
            m_XFloatField.onValueChanged = OnValueChanged;

            m_YFloatField = new FloatField("Y");
            m_YFloatField.onValueChanged = OnValueChanged;

            m_ZFloatField = new FloatField("Z");
            m_ZFloatField.onValueChanged = OnValueChanged;
             

            fieldContainer.AddChild(m_XFloatField);
            fieldContainer.AddChild(m_YFloatField);
            fieldContainer.AddChild(m_ZFloatField);

            AddChild(fieldContainer);
        }

        public abstract void OnValueChanged();

        protected FloatField m_XFloatField;
        protected FloatField m_YFloatField;
        protected FloatField m_ZFloatField;

        public override bool enabled
        {
            set
            {
                base.enabled = value;
                if (m_XFloatField != null)
                    m_XFloatField.enabled = value;
                if (m_YFloatField != null)
                    m_YFloatField.enabled = value;
                if (m_ZFloatField != null)
                    m_ZFloatField.enabled = value;
            }
        }

    }

    class VectorPropertyRM : Vector3SpacabledPropertyRM<Vector>
    {
        public VectorPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
        }

        public override void OnValueChanged()
        {
            Vector3 newValue = new Vector3(m_XFloatField.GetValue(),m_YFloatField.GetValue(),m_ZFloatField.GetValue());
            if( newValue != m_Value.vector )
            {
                m_Value.vector = newValue;
                NotifyValueChanged();
            }
        }
        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_XFloatField.SetValue(m_Value.vector.x);
            m_YFloatField.SetValue(m_Value.vector.y);
            m_ZFloatField.SetValue(m_Value.vector.z);
        }
    }

    class PositionPropertyRM : Vector3SpacabledPropertyRM<Position>
    {
        public PositionPropertyRM(VFXDataAnchorPresenter presenter):base(presenter)
        {
        }

        public override void OnValueChanged()
        {
            Vector3 newValue = new Vector3(m_XFloatField.GetValue(),m_YFloatField.GetValue(),m_ZFloatField.GetValue());
            if( newValue != m_Value.position )
            {
                m_Value.position = newValue;
                NotifyValueChanged();
            }
        }
        public override void UpdateGUI()
        {
            base.UpdateGUI();
            m_XFloatField.SetValue(m_Value.position.x);
            m_YFloatField.SetValue(m_Value.position.y);
            m_ZFloatField.SetValue(m_Value.position.z);
        }
    }
}