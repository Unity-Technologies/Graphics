using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Utilities to remove <see cref="MonoBehaviour"/> implementing <see cref="IAdditionalData"/>
    /// </summary>
    public static class RemoveAdditionalDataUtils
    {
        /// <summary>
        /// Removes a <see cref="IAdditionalData"/> and it's components defined by <see cref="RequireComponent"/>
        /// </summary>
        /// <typeparam name="T">A <see cref="MonoBehaviour"/> that is an <see cref="IAdditionalData"/></typeparam>
        /// <exception cref="Exception">If the given <see cref="MonoBehaviour"/> is not an <see cref="IAdditionalData"/></exception>
        public static void RemoveAdditionalData<T>([DisallowNull] MenuCommand command)
            where T : Component, IAdditionalData
        {
            var additionalData = command.context as IAdditionalData;
            using (ListPool<Type>.Get(out var componentsToRemove))
            {
                if (!RemoveComponentUtils.TryGetComponentsToRemove(command.context as IAdditionalData, componentsToRemove, out var error))
                    throw error;

                if (EditorUtility.DisplayDialog(
                    title: $"Are you sure you want to proceed?",
                    message: $"This operation will also remove {string.Join($"{Environment.NewLine} - ", componentsToRemove)}.",
                    ok: $"Remove everything",
                    cancel: "Cancel"))
                {
                    RemoveAdditionalDataComponent(additionalData, componentsToRemove);
                }
            }
        }

        internal static void RemoveAdditionalDataComponent([DisallowNull] IAdditionalData additionalData, [DisallowNull] List<Type> componentsTypeToRemove)
        {
            var additionalDataComponent = additionalData as Component;
            using (ListPool<Component>.Get(out var components))
            {
                // Fetch all components
                foreach (var type in componentsTypeToRemove)
                {
                    components.AddRange(additionalDataComponent.GetComponents(type));
                }

                // Remove all of them
                foreach (var mono in components)
                {
                    RemoveComponentUtils.RemoveComponent(mono);
                }
            }
        }
    }
}
