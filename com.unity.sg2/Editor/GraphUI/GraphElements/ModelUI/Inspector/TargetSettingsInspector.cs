using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TargetSettingsInspector : FieldsInspector
    {
        List<Target> m_GraphTargets;

        ReorderableListPropertyField<Target> m_TargetListPropertyField;

        Dictionary<Target, bool> m_TargetFoldouts = new Dictionary<Target, bool>();

        public TargetSettingsInspector(List<Target> activeTargets, string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_GraphTargets = activeTargets;
        }

        static IList<Target> GetTargets()
        {
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            var targetList = new List<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                    targetList.Add(target);
            }

            return targetList;
        }

        void TargetAddedCallback(object targetObject)
        {
            if (targetObject != null)
            {
                m_GraphTargets.Add(targetObject as Target);
                m_TargetListPropertyField.listView.itemsSource = m_GraphTargets;
                m_TargetListPropertyField.listView.Rebuild();

                BuildFields();
            }
        }

        static string GetTargetDisplayName(object targetObject)
        {
            if (targetObject is Target target)
                return target.displayName;

            return "Failed to get Target Display Name.";
        }

        void OnTargetSelectionChanged(IEnumerable<object> selectedObjects)
        {
            for (var i = 0; i < m_GraphTargets.Count; ++i)
            {
                var target = m_GraphTargets[i];
                if (target == null)
                    m_GraphTargets.RemoveAt(i);
            }

            m_TargetListPropertyField.listView.itemsSource = m_GraphTargets;
            m_TargetListPropertyField.listView.Rebuild();
        }

        void OnTargetRemoved()
        {
            BuildFields();
        }

        void OnTargetSettingsChanged()
        {
            Debug.Log("TargetSettingsInspector: Target Settings Change is unimplemented");
        }

        void RegisterActionToUndo(string actionName)
        {
            Debug.Log("TargetSettingsInspector: Register Action To Undo is unimplemented");
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var propertyFieldList = new List<BaseModelPropertyField>();

            // TODO : Add "Active Targets" label above the list here

            m_TargetListPropertyField = new ReorderableListPropertyField<Target>(m_OwnerElement.RootView, m_GraphTargets, GetTargets, GetTargetDisplayName, TargetAddedCallback, OnTargetSelectionChanged, OnTargetRemoved, true);
            propertyFieldList.Add(m_TargetListPropertyField);

            foreach (var activeTarget in m_GraphTargets)
            {
                var targetSettingsView = new TargetSettingsPropertyField(m_OwnerElement.RootView, activeTarget, m_TargetFoldouts, OnTargetSettingsChanged, RegisterActionToUndo);
                propertyFieldList.Add(targetSettingsView);
            }

            return propertyFieldList;
        }
    }
}
