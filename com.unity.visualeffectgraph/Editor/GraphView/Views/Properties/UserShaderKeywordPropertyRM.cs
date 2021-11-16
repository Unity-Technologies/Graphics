using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;
using Type = System.Type;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using ShaderKeyword = UnityEditor.ShaderGraph.ShaderKeyword;

namespace UnityEditor.VFX.UI
{
    class UserShaderKeywordPropertyRM : PropertyRM<List<VFXUserKeyword>>
    {
        List<VFXEnumValuePopup> m_VariantEnumValues = new List<VFXEnumValuePopup>();
                
        public UserShaderKeywordPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        void PopulateKeywordList(List<VFXUserKeyword> m_VFXUserKeywords)
        {
            if (m_VFXUserKeywords.Count > 0)
            {
                Add(m_Icon);
                Add(m_Label);
                for (int i = 0; i < m_VFXUserKeywords.Count; i++)
                {
                    if (m_VFXUserKeywords[i].KeywordEntries.Length > 0)
                    {
                        m_VariantEnumValues.Add(AddValue(m_VFXUserKeywords[i]));
                    }
                }
            }
        }

        public override void SetValue(object obj)
        {
            if (!object.ReferenceEquals(m_Value, obj))
            {
                Clear();
                if (obj is List<VFXUserKeyword> userKeywords)
                {
                    PopulateKeywordList(userKeywords);
                }
            }
        }

        VFXEnumValuePopup AddValue(VFXUserKeyword shaderKeyword)
        {
            var newEnum = new VFXEnumValuePopup();
            newEnum.name = shaderKeyword.KeywordLabel;
            newEnum.tooltip = shaderKeyword.KeywordLabel;
            newEnum.enumValues = shaderKeyword.KeywordEntries.ToArray();
            newEnum.AddToClassList("unity-enum-field__UserKeywords");
            newEnum.RegisterCallback<ChangeEvent<long>>(OnValueChanged);
            // long idx = shaderKeyword.FindSelectedEntryIndex(shaderKeyword.SelectedIndex);
            // long idx =  shaderKeyword.SelectedIndex > shaderKeyword.KeywordEntries.Length ? 0 : shaderKeyword.SelectedIndex;
            newEnum.SetValueWithoutNotify(shaderKeyword.SelectedIndex);
            Add(newEnum);
            return newEnum;
        }

        private void OnValueChanged(ChangeEvent<long> evt)
        { 
            if ( m_Provider.value != null && m_Provider.value is List<VFXUserKeyword>)
            {
                List<VFXUserKeyword> vfxUserKeywords = m_Provider.value as List<VFXUserKeyword>;
                if( vfxUserKeywords.Count < 1) return;
                
                var target = evt.currentTarget as VFXEnumValuePopup;
                for (int i = 0; i < vfxUserKeywords.Count; i++)
                {
                    if (vfxUserKeywords[i].KeywordLabel == target.name)
                    {
                        int index = Convert.ToInt32(evt.newValue);
                        if (index > -1)
                        {
                            vfxUserKeywords[i].SelectedIndex =  index;
                            vfxUserKeywords[i].SelectedEntry =  vfxUserKeywords[i].KeywordEntries[index];
                            m_Value = vfxUserKeywords;
                            target.SetValueAndNotify(evt.newValue);
                        }
                    }
                }
            }
        }
        
        protected override void UpdateEnabled()
        {
        }

        protected override void UpdateIndeterminate()
        {
        }

        public override void UpdateGUI(bool force)
        {
        }

        public override float GetPreferredControlWidth()
        {
            int min = 120;
             foreach (var enumValue in m_VariantEnumValues)
            {
                Vector2 size = enumValue.Q<TextElement>().MeasureTextSize(enumValue.value.ToString(), 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
            
                size.x += 60;
                if (min < size.x)
                    min = (int)size.x;
            }
            if (min > 200)
                min = 200;
            
            
             return min;
        }

        public override bool showsEverything { get { return true; } }
    }
}
