using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TargetSettingsInspector : SGFieldsInspector
    {
        List<JsonData<Target>> m_GraphTargets;

        TargetSettingsListPropertyField<JsonData<Target>> m_TargetListPropertyField;

        Dictionary<Target, bool> m_TargetFoldouts = new();

        public TargetSettingsInspector(List<JsonData<Target>> activeTargets, string name, IModel model, IModelView ownerElement, string parentClassName)
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

        static IList<object> GetTargetDisplayNames()
        {
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            var targetDisplayNames = new List<object>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                    targetDisplayNames.Add(target.displayName);
            }

            return targetDisplayNames;
        }

        void TargetAddedCallback(object targetObject)
        {
            var targetName = targetObject as String;
            foreach (var target in GetTargets())
            {
                if (target.displayName == targetName)
                {
                    m_GraphTargets.Add(target);
                    m_OwnerElement.RootView.Dispatch(new ChangeActiveTargetsCommand());
                    m_TargetListPropertyField.listView.itemsSource = m_GraphTargets;
                    m_TargetListPropertyField.listView.Rebuild();

                    BuildFields();
                }
            }

            /*if (targetObject is Target target)
            {
                m_GraphTargets.Add(target);
                m_OwnerElement.RootView.Dispatch(new ChangeActiveTargetsCommand());
                m_TargetListPropertyField.listView.itemsSource = m_GraphTargets;
                m_TargetListPropertyField.listView.Rebuild();

                BuildFields();
            }*/
        }

        static string GetTargetDisplayName(object targetObject)
        {
            if (targetObject is Target target)
                return target.displayName;
            if (targetObject is JsonData<Target> jsonData)
                return jsonData.value.displayName;

            return targetObject as String;
        }

        void OnTargetSelectionChanged(IEnumerable<object> selectedObjects)
        {
            // The UI-Toolkit ListView directly adds a null element to the list, when the '+' button is hit
            // Even if we click away from the dropdown menu, that null item will still exist in the list
            // So we scan for any null items and make sure to remove them and rebuild the list view
            for (var i = 0; i < m_GraphTargets.Count; ++i)
            {
                var target = m_GraphTargets[i];
                if (target.value == null)
                    m_GraphTargets.RemoveAt(i);
            }

            m_TargetListPropertyField.listView.itemsSource = m_GraphTargets;
            m_TargetListPropertyField.listView.Rebuild();
        }

        void OnTargetRemoved()
        {
            m_OwnerElement.RootView.Dispatch(new ChangeActiveTargetsCommand());

            BuildFields();
        }

        void OnTargetSettingsChanged()
        {
            m_OwnerElement.RootView.Dispatch(new ChangeTargetSettingsCommand());

            BuildFields();
        }

        void RegisterActionToUndo(string actionName)
        {
            Debug.Log("TargetSettingsInspector: Register Action To Undo is unimplemented");
        }

        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var propertyFieldList = new List<BaseModelPropertyField>();

            var labelField = new LabelPropertyField("Active Targets", m_OwnerElement.RootView);
            propertyFieldList.Add(labelField);

            m_TargetListPropertyField =
                new TargetSettingsListPropertyField<JsonData<Target>>(
                m_OwnerElement.RootView,
                m_GraphTargets,
                GetTargetDisplayNames,
                GetTargetDisplayName,
                TargetAddedCallback,
                OnTargetSelectionChanged,
                OnTargetRemoved,
                true,
                true) { name = "sg-active-targets-list" };
            m_TargetListPropertyField.AddStylesheet("TargetSettingsList.uss");

            propertyFieldList.Add(m_TargetListPropertyField);

            foreach (var activeTarget in m_GraphTargets)
            {
                var targetSettingsView =
                    new TargetSettingsPropertyField(
                    m_OwnerElement.RootView,
                    activeTarget.value,
                    m_TargetFoldouts,
                    BuildFields,
                    OnTargetSettingsChanged,
                    RegisterActionToUndo) { name = "sg-target-settings-field" };
                targetSettingsView.AddStylesheet("TargetSettingsField.uss");
                propertyFieldList.Add(targetSettingsView);
            }

            return propertyFieldList;
        }

        public override bool IsEmpty()
        {
            return false;
        }
    }
}
