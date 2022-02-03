using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Helper methods to clone graph elements.
    /// </summary>
    public static class CloneHelpers
    {
        class Holder : ScriptableObject
        {
            [SerializeReference]
            public IGraphElementModel model;
        }

        /// <summary>
        /// Clones a graph element model.
        /// </summary>
        /// <param name="element">The element to clone.</param>
        /// <typeparam name="T">The type of the element to clone.</typeparam>
        /// <returns>A clone of the element.</returns>
        public static T Clone<T>(this T element) where T : IGraphElementModel
        {
            if (element is ICloneable cloneable)
            {
                T copy = (T)cloneable.Clone();
                copy.AssignNewGuid();
                return copy;
            }

            return CloneUsingScriptableObjectInstantiate(element);
        }

        /// <summary>
        /// Clones a constant.
        /// </summary>
        /// <param name="element">The constant to clone.</param>
        /// <typeparam name="T">The type of the constant.</typeparam>
        /// <returns>A clone of the constant.</returns>
        public static T CloneConstant<T>(this T element) where T : IConstant
        {
            T copy = (T)Activator.CreateInstance(element.GetType());
            copy.ObjectValue = element.ObjectValue;
            return copy;
        }

        /// <summary>
        /// Clones a graph element model.
        /// </summary>
        /// <param name="element">The element to clone.</param>
        /// <typeparam name="T">The type of the element to clone.</typeparam>
        /// <returns>A clone of the element.</returns>
        public static T CloneUsingScriptableObjectInstantiate<T>(T element) where T : IGraphElementModel
        {
            Holder h = ScriptableObject.CreateInstance<Holder>();
            h.model = element;

            // TODO: wait for CopySerializedManagedFieldsOnly to be able to copy plain c# objects with [SerializeReference] fields
            //            var clone = (T)Activator.CreateInstance(element.GetType());
            //            EditorUtility.CopySerializedManagedFieldsOnly(element, clone);
            var h2 = Object.Instantiate(h);
            var clone = h2.model;
            clone.AssignNewGuid();

            if (clone is IGraphElementContainer container)
                foreach (var subElement in container.GraphElementModels)
                {
                    if (subElement is ICloneable)
                        Debug.LogError("ICloneable is not supported on elements in IGraphElementsContainer");
                    subElement.AssignNewGuid();
                }

            Object.DestroyImmediate(h);
            Object.DestroyImmediate(h2);
            return (T)clone;
        }
    }
}
