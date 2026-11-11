using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    internal sealed class VFXBlackboardTestHelper
    {
        private const double uiTimeoutSeconds = 2.0;
        // Keep trunk's pending callback state local to the fixture using this helper.
        private int m_PendingAddParameterCount;

        internal void AddParameter(VFXBlackboard blackboard, VFXModelDescriptorParameters descriptorParameters)
        {
            var onAddParameterMethod = blackboard.GetType().GetMethod("OnAddParameter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onAddParameterMethod);
            onAddParameterMethod.Invoke(blackboard, new object[] { descriptorParameters });

            // Runs right after the delay call registered by OnAddParameter
            m_PendingAddParameterCount++;
            EditorApplication.delayCall += () => m_PendingAddParameterCount--;
        }

        internal IEnumerator WaitForPendingAddParameters(Func<bool> ready = null)
        {
            return WaitFor(() => m_PendingAddParameterCount == 0 && (ready == null || ready()));
        }

        internal static IEnumerator WaitFor(Func<bool> condition)
        {
            var timeout = EditorApplication.timeSinceStartup + uiTimeoutSeconds;
            while (!condition() && EditorApplication.timeSinceStartup < timeout)
            {
                yield return null;
            }
        }

        internal static IEnumerator IsOpenForEdit(VisualElement parent, string fieldName, string textFieldValue, bool isInEdit = true)
        {
            TextField textField = null;
            var enumerator = WaitFor(() => (textField = parent.Query<TextField>(fieldName).Where(x => x.resolvedStyle.display == DisplayStyle.Flex).First()) != null);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            if (isInEdit)
            {
                Assert.NotNull(textField, $"Could not find field with '{textFieldValue}' value in edit mode.");
                Assert.AreEqual(textFieldValue, textField.value);
            }
        }
    }
}
