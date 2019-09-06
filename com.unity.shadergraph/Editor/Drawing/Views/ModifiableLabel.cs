using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    public class ModifiableLabel : VisualElement
    {
        public static string s_EditableLabelName = "unity-editable-label";

        protected Label m_label;
        protected TextField m_textField;

        public Label label => m_label;
        public TextField textField => m_textField;

        private bool m_isEditing;
        public bool isEditing
        {
            get { return m_isEditing; }
            set
            {
                m_label.SetEnabled( !value );
                m_label.visible = !value;
                m_label.style.overflow = value ? Overflow.Hidden : Overflow.Visible;
                m_label.style.display = value ? DisplayStyle.None : DisplayStyle.Flex;
                m_label.style.position = value ? Position.Absolute : Position.Relative;

                m_textField.SetEnabled( value );
                m_textField.visible = value;
                m_textField.style.overflow = !value ? Overflow.Hidden : Overflow.Visible;
                m_textField.style.display = !value ? DisplayStyle.None : DisplayStyle.Flex;
                m_textField.style.position = !value ? Position.Absolute : Position.Relative;

                if( value )
                {
                    m_textField.SetValueWithoutNotify( m_label.text );
                    
#if UNITY_2019_1_OR_NEWER
                    EditorApplication.delayCall += () => { m_textField.Q( "unity-text-input" ).Focus(); };
#else
                    m_textField.Focus();
#endif
                }
                else
                {
                    m_label.text = m_textField.value;
                }

                m_isEditing = value;
            }
        }

        private string m_text;
        public string text
        {
            get
            {
                return m_label.text;
            }
            set
            {
                m_label.text = value;
                m_textField.value = value;
            }
        }

        public ModifiableLabel( string text )
        {
            m_label = new Label( text );
            m_textField = new TextField();
            m_textField.SetValueWithoutNotify( text );

            m_label.RegisterCallback< MouseDownEvent >(
                ( evt ) =>
                {
                    if( evt.clickCount == 2 )
                    {
                        isEditing = true;

                        evt.StopImmediatePropagation();
                    }
                }
            );

#if UNITY_2019_1_OR_NEWER
            m_textField.Q( "unity-text-input" ).RegisterCallback< MouseDownEvent >(
                ( evt ) =>
                {
                    evt.StopImmediatePropagation();
                }
            );
#else
            m_textField.RegisterCallback< MouseDownEvent >(
                ( evt ) =>
                {
                    evt.StopImmediatePropagation();
                }
            );
#endif
            m_textField.RegisterCallback< FocusOutEvent >(
                ( evt ) =>
                {
                    isEditing = false;
                }
            );

            Add( m_label );
            Add( m_textField );
            
            isEditing = false;
        }
    }
}