using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class GenerationTarget : ISerializationCallbackReceiver
    {
        [SerializeField]
        ITarget m_Target;

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializedImplementations = new List<SerializationHelper.JSONSerializedElement>();

        [SerializeField]
        int m_ActiveImplementationBitmask;

        List<ITargetImplementation> m_Implementations;
        List<ITargetImplementation> m_ActiveImplementations;
        string[] m_ImplementationNames;
        Dictionary<ITargetImplementation, bool> m_ImplementationFoldouts;

        public GenerationTarget(ITarget target)
        {
            // Set data
            m_Target = target;
            m_Implementations = new List<ITargetImplementation>();
            m_ActiveImplementations = new List<ITargetImplementation>();
            m_ImplementationFoldouts = new Dictionary<ITargetImplementation, bool>();
            m_ActiveImplementationBitmask = -1;

            // Get all TargetImplementation types
            var typeCollection = TypeCache.GetTypesDerivedFrom(typeof(ITargetImplementation));
            foreach(var type in typeCollection)
            {
                if(!type.IsAbstract)
                {
                    // Instantiate and add TargetImplementation
                    var implementation = Activator.CreateInstance(type) as ITargetImplementation;
                    if(implementation.targetType.Equals(m_Target.GetType()))
                    {
                        m_Implementations.Add(implementation);
                    }
                }
            }

            // Update state
            UpdateImplementationNames();
            UpdateActiveImplementations();
        }

        public ITarget target => m_Target;
        public List<ITargetImplementation> implementations => m_Implementations;
        public List<ITargetImplementation> activeImplementations => m_ActiveImplementations;

        void UpdateImplementationNames()
        {
            // Get TargetImplementation names
            // This is used for Target Settings object
            var implementationCount = m_Implementations.Count;
            m_ImplementationNames = new string[implementationCount];
            for(int i = 0; i < implementationCount; i++)
            {
                m_ImplementationNames[i] = m_Implementations[i].displayName;
            }
        }

        void UpdateActiveImplementations()
        {
            // Update active TargetImplementation list
            m_ActiveImplementations.Clear();
            var implementationCount = m_Implementations.Count;
            for(int i = 0; i < implementationCount; i++)
            {
                if(((1 << i) & m_ActiveImplementationBitmask) == (1 << i))
                {
                    m_ActiveImplementations.Add(m_Implementations[i]);
                }   
            }
        }

        void UpdateDeserializedImplementations()
        {
            // Get all TargetImplementation types
            var typeCollection = TypeCache.GetTypesDerivedFrom(typeof(ITargetImplementation));

            // Search for missing serialized TargetImplementations
            for(int i = 0; i < m_Implementations.Count; i++)
            {
                var implementation = m_Implementations[i];
                var implementationType = implementation.GetType();
                if(!typeCollection.Contains(implementationType))
                {
                    m_Implementations.Remove(implementation);
                }
            }

            // Search for new TargetImplementations
            foreach(var type in typeCollection)
            {
                // TargetImplementation already serialized
                if(m_Implementations.Any(x => x.GetType() == type))
                    continue;

                // Instantiate and add TargetImplementation
                var implementation = Activator.CreateInstance(type) as ITargetImplementation;
                if(implementation.targetType.Equals(m_Target.GetType()))
                {
                    m_Implementations.Add(implementation);
                }
            }

            // Update state
            UpdateImplementationNames();
            UpdateActiveImplementations();
        }

        public VisualElement GetSettings(Action onChange)
        {
            var element = new VisualElement() { name = "targetSettings" };

            // Title
            var title = new Label("Target Settings") { name = "titleLabel" };
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            element.Add(new Drawing.PropertyRow(title));

            // Implementation Dropdown Field
            element.Add(new Drawing.PropertyRow(new Label("Implementations")), (row) =>
                {
                    row.Add(new IMGUIContainer(() => {
                        EditorGUI.BeginChangeCheck();
                        m_ActiveImplementationBitmask = EditorGUILayout.MaskField(m_ActiveImplementationBitmask, m_ImplementationNames, GUILayout.Width(100f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateActiveImplementations();
                            onChange();
                        }
                    }));
                });

            // Iterate active TargetImplementations
            foreach(var implementation in m_Implementations)
            {
                // Ensure enabled state is being tracked and get value
                bool foldoutActive = true;
                if(!m_ImplementationFoldouts.TryGetValue(implementation, out foldoutActive))
                {
                    m_ImplementationFoldouts.Add(implementation, foldoutActive);
                }

                // Create foldout
                var foldout = new Foldout() { text = implementation.displayName, value = foldoutActive };
                element.Add(foldout);
                foldout.RegisterValueChangedCallback(evt => 
                {
                    // Update foldout value and rebuild
                    m_ImplementationFoldouts[implementation] = evt.newValue;
                    foldout.value = evt.newValue;
                    onChange();
                });
                
                if(foldout.value)
                {
                    // Get settings for TargetImplementation
                    var implementationSettings = implementation.GetSettings(onChange);

                    // Virtual method returns null
                    // Settings are only added if this is overriden
                    if(implementationSettings != null)
                    {
                        element.Add(implementationSettings);
                    }
                }
            }
            return element;
        }

        public void OnBeforeSerialize()
        {
            // Serialize fields
            m_SerializedImplementations = SerializationHelper.Serialize<ITargetImplementation>(m_Implementations);
        }

        public void OnAfterDeserialize()
        {
            // Deserialize fields
            m_Implementations = SerializationHelper.Deserialize<ITargetImplementation>(m_SerializedImplementations, GraphUtil.GetLegacyTypeRemapping());

            // Post deserialization
            UpdateDeserializedImplementations();

            // Clear data
            m_SerializedImplementations = null;
        }
    }
}
