#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    [System.Serializable]
    public class RequiredSettingBase : IRequiredSetting
    {
        [SerializeField]
        private string m_name = "Property Name";
        [SerializeField]
        private string m_description = "";
        [SerializeField]
        public string propertyPath;
        public string name => m_name;
        public string description => m_description;

        public ValueType valueType = ValueType.Bool;
        public float targetValue = 1f;
        public ValidationType validationType = ValidationType.Equal;

        private string m_propertyPath;
        private string[] m_propertyPathHierarchyCache;
        private string[] m_propertyPathHierarchy
        {
            get
            {
                if (m_propertyPath != null || m_propertyPath != propertyPath)
                {
                    m_propertyPath = propertyPath;

                    m_propertyPathHierarchyCache = new string[2];

                    var temp = propertyPath.Split("."[0]);
                    m_propertyPathHierarchyCache[0] = temp[0];
                    m_propertyPathHierarchyCache[1] = propertyPath.Remove(0, m_propertyPathHierarchyCache[0].Length + 1);
                }
                return m_propertyPathHierarchyCache;
            }
        }

        public virtual string projectSettingsPath { get; }

        public virtual string editorAssemblyName => null;
        public virtual string editorClassName => null;
        public virtual string editorShowFunctionName => null;

        public virtual SerializedProperty property
        {
            get
            {
                var rpAsset = QualitySettings.renderPipeline;
                if (rpAsset == null)
                    rpAsset = GraphicsSettings.defaultRenderPipeline;
                if (rpAsset == null)
                    return null;

                var serializedRPAsset = new SerializedObject(rpAsset);

                var rootProperty = serializedRPAsset.FindProperty(m_propertyPathHierarchy[0]);
                return rootProperty.FindPropertyRelative(m_propertyPathHierarchy[1]);

            }
        }

        public virtual bool state
        {
            get
            {
                if (property == null)
                    return false;

                float floatValue;
                float comparedValue = targetValue;

                switch(valueType)
                {
                    case ValueType.Float:
                        floatValue = property.floatValue;
                        break;
                    case ValueType.Int:
                        floatValue = property.intValue;
                        comparedValue = (int)comparedValue;
                        break;
                    default:
                            return property.boolValue == (targetValue > 0f);
                }

                switch (validationType)
                {
                    case ValidationType.Greater:
                        return floatValue > comparedValue;
                    case ValidationType.Lower:
                        return floatValue < comparedValue;
                    case ValidationType.GreaterEqual:
                        return floatValue >= comparedValue;
                    case ValidationType.LowerEqual:
                        return floatValue <= comparedValue;
                    case ValidationType.Different:
                        return floatValue != comparedValue;
                    default:
                        return floatValue == comparedValue;
                }
            }
        }
    }

    public enum ValueType
    {
        Bool,
        Int,
        Float
    };

    public enum ValidationType
    {
        Equal,
        Greater,
        Lower,
        GreaterEqual,
        LowerEqual,
        Different
    };
}
#endif
