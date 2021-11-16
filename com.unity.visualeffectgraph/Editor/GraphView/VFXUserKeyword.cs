using System;
using System.Collections.Generic;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;
using ShaderKeyword = UnityEditor.ShaderGraph.ShaderKeyword;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Serialization;


namespace UnityEditor.VFX
{
    [Serializable]
    internal class VFXUserKeyword
    {
        public string KeywordLabel = string.Empty;
        public string[] KeywordEntries = new string[]{};
        public int SelectedIndex = 0;
        public string SelectedEntry = string.Empty;
        
        public VFXUserKeyword(ShaderKeyword keyword)
        {
            KeywordLabel = keyword.displayName;
            SetShaderKeywordEntries(keyword);
            SetIndexOfEntry();
        }
        
        public bool hasEqualEntries(ShaderKeyword shaderKeyword)
        {
            if (shaderKeyword.keywordType == KeywordType.Enum)
            {
                if (shaderKeyword.entries.Count != KeywordEntries.Length) return false;
                
                for (int i = 0; i < shaderKeyword.entries.Count; i++)
                {
                    if (shaderKeyword.entries[i].displayName != KeywordEntries[i]) return false;
                }
            }
            return true;
        }
        
        public void SetIndexOfEntry()
        {
            if (string.IsNullOrEmpty(SelectedEntry) && KeywordEntries.Length > 0)
            {
                SelectedIndex = 0;
                SelectedEntry = KeywordEntries[SelectedIndex];
            }
            
            if (KeywordEntries.Length > 0)
            {
                int idx = Array.IndexOf(KeywordEntries, SelectedEntry); //entry may have moved order, removed, or renamed
                SelectedIndex = Math.Max(0, idx); 
                SelectedEntry = KeywordEntries[SelectedIndex];
            }
        }

        public VFXUserKeyword(){}
        public void SetShaderKeywordEntries(ShaderKeyword keyword)
        {
            if (keyword.keywordScope == KeywordScope.Local)
            {
                switch (keyword.keywordType)
                {
                    case KeywordType.Enum:
                    {
                        KeywordEntries = keyword.entries.Select(obj => obj.displayName).Distinct().ToArray();
                        break;
                    }
                    case KeywordType.Boolean:
                    {
                        KeywordEntries = new []{ keyword.displayName +"_OFF",keyword.displayName + "_ON"};
                        break;
                    }
                }
            }
        }

        public void FindSelectedEntryIndex(ShaderKeyword keyword)
        {
            int index = SelectedIndex > keyword.entries.Count ? keyword.value : SelectedIndex;;
            if (KeywordEntries.Length > 0)
            {
                if (!string.IsNullOrEmpty(SelectedEntry))
                {
                    int idx = Array.FindIndex(KeywordEntries, x => x.Equals(SelectedEntry));
                    if (idx > 0)
                    {
                        index = idx;
                    }
                }
            }
            
            SelectedIndex = index;
            SelectedEntry = KeywordEntries[SelectedIndex];
        }
        
        public void SetSelectedEntryIndex(int selectedIndex)
        {
            if( selectedIndex <  KeywordEntries.Length &&  selectedIndex >= 0)
            {
                // SelectedEntry = KeywordEntries[SelectedIndex];
                SelectedIndex = selectedIndex;
            }
            else
            {
                // SelectedEntry = KeywordEntries[0];
                SelectedIndex = 0;
            }
            
        }
        
    }
}