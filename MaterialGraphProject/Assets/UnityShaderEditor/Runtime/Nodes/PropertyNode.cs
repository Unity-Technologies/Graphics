using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    public abstract class PropertyNode : AbstractMaterialNode
    {
        public enum ExposedState
        {
            Exposed,
            NotExposed
        }

        [SerializeField]
        private string m_PropertyName = string.Empty;

        [SerializeField]
        private string m_Description = string.Empty;

        [SerializeField]
        private ExposedState m_Exposed = ExposedState.NotExposed;

        public ExposedState exposedState
        {
            get
            {
                if (owner is SubGraph)
                    return ExposedState.NotExposed;

                return m_Exposed;
            }
            set
            {
                if (m_Exposed == value)
                    return;

                m_Exposed = value;
                if (onModified != null)
                {
                    onModified(this);
                }
            }
        }

        public string description
        {
            get
            {
                if (string.IsNullOrEmpty(m_Description))
                    return m_PropertyName;

                return m_Description;
            }
            set { m_Description = value; }
        }

        public string propertyName
        {
            get
            {
                if (exposedState == ExposedState.NotExposed || string.IsNullOrEmpty(m_PropertyName))
                    return string.Format("{0}_{1}_Uniform", name, guid.ToString().Replace("-", "_"));

                return m_PropertyName + "_Uniform";
            }
            set { m_PropertyName = value; }
        }

        public abstract PropertyType propertyType { get; }

        public abstract PreviewProperty GetPreviewProperty();

        public override string GetVariableNameForSlot(int slotId)
        {
            return propertyName;
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            properties.Add(GetPreviewProperty());
        }

        protected override bool CalculateNodeHasError()
        {
            if (exposedState == ExposedState.NotExposed)
                return false;

            var propNodes = owner.GetNodes<PropertyNode>();
            foreach (var n in propNodes)
            {
                if (n == this || n.exposedState == ExposedState.NotExposed)
                    continue;

                if (n.propertyName == propertyName)
                {
                    return true;
                }
            }
            return false;
        }

        /*
        public override float GetNodeUIHeight(float width)
        {
            return 2 * EditorGUIUtility.singleLineHeight;
        }

        public override bool OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_Exposed = EditorGUILayout.Toggle("Exposed Property", m_Exposed);
            if (m_Exposed)
                m_PropertyName = EditorGUILayout.DelayedTextField("Property Name", m_PropertyName);

            var modified = EditorGUI.EndChangeCheck();
            if (modified)
            {
                owner.ValidateGraph();
            }

            if (m_Exposed)
                m_Description = EditorGUILayout.TextField("Description", m_Description);

            modified |= base.OnGUI();
            return modified;
        }*/
    }
}
