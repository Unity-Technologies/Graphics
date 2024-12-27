using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Represents the state of a Volume blending update within the Volume system.
    /// </summary>
    /// <remarks>
    ///
    /// This class is responsible for storing the blending of Volume components across multiple scenes and cameras,
    /// By default, a global volume stack is provided by the <see cref="VolumeManager"/> to handle the blending of Volume data across your project.
    /// This global stack simplifies managing and blending volume data at a project-wide level. However, if you require more granular control over
    /// the blending process or want to store and manage the blending results separately (e.g., per camera or scene), you can create custom volume
    /// stacks using <see cref="VolumeManager.CreateStack"/>.
    /// The blending of volumes is based on a combination of several factors:
    /// - **Volume Weight:** Determines how strongly a particular volume influences the final result.
    /// - **Volume Parameters:** These can be visual settings such as post-processing effects, lighting adjustments, or other specialized effects defined
    ///   in the Volume components.
    /// - **Camera Volume Stack:** Volume blending can vary per camera, allowing different volumes to be blended for different camera views or scenes.
    ///
    /// While the default global volume stack works for most use cases, custom stacks provide greater flexibility and control, allowing developers
    /// to manage and store volume blending results at a per-scene or per-camera level. This can be particularly useful in complex rendering setups
    /// or when you want to apply different volume blends for different gameplay contexts or visual effects.
    ///
    /// Keep in mind that frequent updates to the volume blending process (e.g., every frame) may have an impact on performance, especially when
    /// dealing with large numbers of volumes or complex blending operations.
    /// </remarks>
    /// <seealso cref="Volume"/>
    /// <seealso cref="VolumeProfile"/>
    /// <seealso cref="VolumeComponent"/>
    /// <seealso cref="VolumeParameter"/>
    public sealed class VolumeStack : IDisposable
    {
        // Holds the state of _all_ component types you can possibly add on volumes
        internal readonly Dictionary<Type, VolumeComponent> components = new();

        // Flat list of every volume parameter for faster per-frame stack reset.
        internal VolumeParameter[] parameters;

        // Flag indicating that some properties have received overrides, therefore they must be reset in the next update.
        internal bool requiresReset = true;

        // Flag indicating that default state has changed, therefore all properties in the stack must be reset in the next update.
        internal bool requiresResetForAllProperties = true;

        internal VolumeStack()
        {
        }

        internal void Clear()
        {
            foreach (var component in components)
                CoreUtils.Destroy(component.Value);

            components.Clear();

            parameters = null;
        }

        internal void Reload(Type[] componentTypes)
        {
            Clear();

            requiresReset = true;
            requiresResetForAllProperties = true;

            List<VolumeParameter> parametersList = new();
            foreach (var type in componentTypes)
            {
                var component = (VolumeComponent)ScriptableObject.CreateInstance(type);
                components.Add(type, component);
                parametersList.AddRange(component.parameters);
            }

            parameters = parametersList.ToArray();

            isValid = true;
        }

        /// <summary>
        /// Gets the current state of the <see cref="VolumeComponent"/> of type <typeparamref name="T"/>
        /// in the stack.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <returns>The current state of the <see cref="VolumeComponent"/> of type <typeparamref name="T"/>
        /// in the stack.</returns>
        public T GetComponent<T>()
            where T : VolumeComponent
        {
            var comp = GetComponent(typeof(T));
            return (T)comp;
        }

        /// <summary>
        /// Gets the current state of the <see cref="VolumeComponent"/> of the specified type in the
        /// stack.
        /// </summary>
        /// <param name="type">The type of <see cref="VolumeComponent"/> to look for.</param>
        /// <returns>The current state of the <see cref="VolumeComponent"/> of the specified type,
        /// or <c>null</c> if the type is invalid.</returns>
        public VolumeComponent GetComponent(Type type)
        {
            components.TryGetValue(type, out var comp);
            return comp;
        }

        /// <summary>
        /// Cleans up the content of this stack. Once a <c>VolumeStack</c> is disposed, it shouldn't
        /// be used anymore.
        /// </summary>
        public void Dispose()
        {
            Clear();

            isValid = false;
        }

        /// <summary>
        /// Check if the stack is in valid state and can be used.
        /// </summary>
        public bool isValid { get; private set; }
    }
}
