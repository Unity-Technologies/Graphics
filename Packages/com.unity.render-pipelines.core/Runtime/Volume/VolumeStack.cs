using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Holds the state of a Volume blending update. A global stack is
    /// available by default in <see cref="VolumeManager"/> but you can also create your own using
    /// <see cref="VolumeManager.CreateStack"/> if you need to update the manager with specific
    /// settings and store the results for later use.
    /// </summary>
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
