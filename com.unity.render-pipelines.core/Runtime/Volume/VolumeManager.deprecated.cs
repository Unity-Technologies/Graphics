using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Rendering
{
    using UnityObject = UnityEngine.Object;

    public sealed partial class VolumeManager
    {
        /// <summary>
        /// A reference to the main <see cref="VolumeStack"/>.
        /// </summary>
        /// <seealso cref="VolumeStack"/>
        [Obsolete("Use custom VolumeStack instances instead.")]
        public VolumeStack stack { get; set; }

        /// <summary>
        /// The current list of all available types that derive from <see cref="VolumeComponent"/>.
        /// </summary>
        [Obsolete("Please use baseComponentTypeArray instead.")]
        public IEnumerable<Type> baseComponentTypes
        {
            get => baseComponentTypeArray;
            private set => Debug.LogWarning("VolumeManager.baseComponentTypes is obsolete, please use baseComponentTypeArray instead.");
        }

        /// <summary>
        /// The current list of all available types that derive from <see cref="VolumeComponent"/>.
        /// </summary>
        [Obsolete("Use VolumeComponentArchetype to get the appropriate type array.")]
        public Type[] baseComponentTypeArray => null;

        /// <summary>
        /// Destroy a Volume Stack
        /// </summary>
        /// <param name="stack">Volume Stack that needs to be destroyed.</param>
        [Obsolete("Use VolumeStack.Dispose instead.")]
        public void DestroyStack(VolumeStack stack)
        {
            stack.Dispose();
        }

        /// <summary>
        /// Updates the global state of the Volume manager. Unity usually calls this once per Camera
        /// in the Update loop before rendering happens.
        /// </summary>
        /// <param name="trigger">A reference Transform to consider for positional Volume blending
        /// </param>
        /// <param name="layerMask">The LayerMask that the Volume manager uses to filter Volumes that it should consider
        /// for blending.</param>
        [Obsolete("Use the overload with the stack parameter.")]
        public void Update(Transform trigger, LayerMask layerMask)
        {
        }

        /// <summary>
        /// Checks the state of the base type library. This is only used in the editor to handle
        /// entering and exiting of play mode and domain reload.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Obsolete("Not required anymore")]
        public void CheckBaseTypes()
        {

        }

        /// <summary>
        /// Checks the state of a given stack. This is only used in the editor to handle entering
        /// and exiting of play mode and domain reload.
        /// </summary>
        /// <param name="stack">The stack to check.</param>
        [Conditional("UNITY_EDITOR")]
        [Obsolete("Use VolumeStack.CheckStack() instead.")]
        public void CheckStack(VolumeStack stack)
        {
            stack.CheckStack();
        }

        /// <summary>
        /// Creates and returns a new <see cref="VolumeStack"/> to use when you need to store
        /// the result of the Volume blending pass in a separate stack.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="VolumeStack"/>
        /// <seealso cref="Update(VolumeStack,Transform,LayerMask)"/>
        [Obsolete("Please use new VolumeStack(VolumeComponentArchetype) instead to create a stack.")]
        public VolumeStack CreateStack()
        {
            return null;
        }

        /// <summary>
        /// Resets the main stack to be the default one.
        /// Call this function if you've assigned the main stack to something other than the default one.
        /// </summary>
        [Obsolete("Not required")]
        public void ResetMainStack()
        {
        }
    }

    /// <summary>
    /// A scope in which a Camera filters a Volume.
    /// </summary>
    [Obsolete("VolumeIsolationScope is deprecated, it does not have any effect anymore.")]
    public struct VolumeIsolationScope : IDisposable
    {
        /// <summary>
        /// Constructs a scope in which a Camera filters a Volume.
        /// </summary>
        /// <param name="unused">Unused parameter.</param>
        public VolumeIsolationScope(bool unused) { }

        /// <summary>
        /// Stops the Camera from filtering a Volume.
        /// </summary>
        void IDisposable.Dispose() { }
    }
}
