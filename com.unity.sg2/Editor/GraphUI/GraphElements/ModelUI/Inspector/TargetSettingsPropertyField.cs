﻿using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TargetSettingsPropertyField : BaseModelPropertyField
    {
        public TargetSettingsPropertyField(
            ICommandTarget commandTarget,
            Target targetObject,
            Dictionary<Target, bool> targetFoldouts,
            Action onRedrawTargetSettings,
            Action onTargetSettingsChanged,
            Action<string> registerActionToUndo)
            : base(commandTarget)
        {
            // Ensure enabled state is being tracked and get value
            if (!targetFoldouts.TryGetValue(targetObject, out var foldoutActive))
            {
                foldoutActive = true;
                targetFoldouts.Add(targetObject, true);
            }

            var foldout = new Foldout() { text = targetObject.displayName, value = foldoutActive, name = "foldout" };
            Add(foldout);
            foldout.AddToClassList("MainFoldout");
            foldout.RegisterValueChangedCallback(evt =>
            {
                // Update foldout value and rebuild
                targetFoldouts[targetObject] = evt.newValue;
                foldout.value = evt.newValue;
                onRedrawTargetSettings();
            });

            if (foldout.value)
            {
                // Get settings for Target
                var context = new TargetPropertyGUIContext(null);
                // Indent the content of the foldout
                context.globalIndentLevel++;
                targetObject.GetPropertiesGUI(ref context, onTargetSettingsChanged, registerActionToUndo);
                context.globalIndentLevel--;
                foldout.Add(context);
            }


        }

        public override void UpdateDisplayedValue()
        {
            throw new System.NotImplementedException();
        }
    }
}
