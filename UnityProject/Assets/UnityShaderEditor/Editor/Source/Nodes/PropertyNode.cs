using System;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class PropertyNode : BaseMaterialNode
    {
        //[SerializeField]
        //private string m_Name;

        [SerializeField]
        private string m_Description;

        [SerializeField]
        private bool m_Exposed;

        public bool exposed
        {
            get { return m_Exposed; }
        }

        public string description
        {
            get { return m_Description; } 
        }

        public virtual string GetPropertyName()
        {
           // var validExposedName = !string.IsNullOrEmpty(m_Name);
            //if (!validExposedName)
                return name + "_" + Math.Abs(GetInstanceID()) + "_Uniform";

           // return m_Name + "_Uniform";
        }

        public abstract PropertyType propertyType { get; }

        public abstract PreviewProperty GetPreviewProperty();
        
        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return GetPropertyName();
        }
        
        public override float GetNodeUIHeight(float width)
        {
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        protected override void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            properties.Add(GetPreviewProperty());
        }
    }
}
