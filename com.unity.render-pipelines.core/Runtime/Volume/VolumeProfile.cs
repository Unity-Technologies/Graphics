using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An Asset which holds a set of settings to use with a <see cref="Volume"/>.
    /// </summary>
    [HelpURL(Documentation.baseURLHDRP + Documentation.releaseVersion + Documentation.subURL + "Volume-Profile" + Documentation.endURL)]
    public sealed class VolumeProfile : ScriptableObject
    {
        /// <summary>
        /// A list of every setting that this Volume Profile stores.
        /// </summary>
        public List<VolumeComponent> components = new List<VolumeComponent>();

        /// <summary>
        /// A dirty check used to redraw the profile inspector when something has changed. This is
        /// currently only used in the editor.
        /// </summary>
        [NonSerialized]
        public bool isDirty = true; // Editor only, doesn't have any use outside of it

        void OnEnable()
        {
            // Make sure every setting is valid. If a profile holds a script that doesn't exist
            // anymore, nuke it to keep the volume clean. Note that if you delete a script that is
            // currently in use in a volume you'll still get a one-time error in the console, it's
            // harmless and happens because Unity does a redraw of the editor (and thus the current
            // frame) before the recompilation step.
            components.RemoveAll(x => x == null);
        }

        /// <summary>
        /// Resets the dirty state of the Volume Profile. Unity uses this to force-refresh and redraw the
        /// Volume Profile editor when you modify the Asset via script instead of the Inspector.
        /// </summary>
        public void Reset()
        {
            isDirty = true;
        }

        /// <summary>
        /// Adds a <see cref="VolumeComponent"/> to this Volume Profile.
        /// </summary>
        /// <remarks>
        /// You can only have a single component of the same type per Volume Profile.
        /// </remarks>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <param name="overrides">Specifies whether Unity should automatically override all the settings when
        /// you add a <see cref="VolumeComponent"/> to the Volume Profile.</param>
        /// <returns>The instance for the given type that you added to the Volume Profile</returns>
        /// <seealso cref="Add"/>
        public T Add<T>(bool overrides = false)
            where T : VolumeComponent
        {
            return (T)Add(typeof(T), overrides);
        }

        /// <summary>
        /// Adds a <see cref="VolumeComponent"/> to this Volume Profile.
        /// </summary>
        /// <remarks>
        /// You can only have a single component of the same type per Volume Profile.
        /// </remarks>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <param name="overrides">Specifies whether Unity should automatically override all the settings when
        /// you add a <see cref="VolumeComponent"/> to the Volume Profile.</param>
        /// <returns>The instance created for the given type that has been added to the profile</returns>
        /// <see cref="Add{T}"/>
        public VolumeComponent Add(Type type, bool overrides = false)
        {
            if (Has(type))
                throw new InvalidOperationException("Component already exists in the volume");

            var component = (VolumeComponent)CreateInstance(type);
            component.SetAllOverridesTo(overrides);
            components.Add(component);
            isDirty = true;
            return component;
        }

        /// <summary>
        /// Removes a <see cref="VolumeComponent"/> from this Volume Profile.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the type does not exist in the Volume Profile.
        /// </remarks>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <seealso cref="Remove"/>
        public void Remove<T>()
            where T : VolumeComponent
        {
            Remove(typeof(T));
        }

        /// <summary>
        /// Removes a <see cref="VolumeComponent"/> from this Volume Profile.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the type does not exist in the Volume Profile.
        /// </remarks>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <seealso cref="Remove{T}"/>
        public void Remove(Type type)
        {
            int toRemove = -1;

            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].GetType() == type)
                {
                    toRemove = i;
                    break;
                }
            }

            if (toRemove >= 0)
            {
                components.RemoveAt(toRemove);
                isDirty = true;
            }
        }

        /// <summary>
        /// Checks if this Volume Profile contains the <see cref="VolumeComponent"/> you pass in.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <returns><c>true</c> if the <see cref="VolumeComponent"/> exists in the Volume Profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="Has"/>
        /// <seealso cref="HasSubclassOf"/>
        public bool Has<T>()
            where T : VolumeComponent
        {
            return Has(typeof(T));
        }

        /// <summary>
        /// Checks if this Volume Profile contains the <see cref="VolumeComponent"/> you pass in.
        /// </summary>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <returns><c>true</c> if the <see cref="VolumeComponent"/> exists in the Volume Profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="Has{T}"/>
        /// <seealso cref="HasSubclassOf"/>
        public bool Has(Type type)
        {
            foreach (var component in components)
            {
                if (component.GetType() == type)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this Volume Profile contains the <see cref="VolumeComponent"/>, which is a subclass of <paramref name="type"/>,
        /// that you pass in.
        /// </summary>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <returns><c>true</c> if the <see cref="VolumeComponent"/> exists in the Volume Profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="Has"/>
        /// <seealso cref="Has{T}"/>
        public bool HasSubclassOf(Type type)
        {
            foreach (var component in components)
            {
                if (component.GetType().IsSubclassOf(type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="VolumeComponent"/> of the specified type, if it exists.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <param name="component">The output argument that contains the <see cref="VolumeComponent"/>
        /// or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="VolumeComponent"/> is in the Volume Profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(Type, out T)"/>
        /// <seealso cref="TryGetSubclassOf{T}"/>
        /// <seealso cref="TryGetAllSubclassOf{T}"/>
        public bool TryGet<T>(out T component)
            where T : VolumeComponent
        {
            return TryGet(typeof(T), out component);
        }

        /// <summary>
        /// Gets the <see cref="VolumeComponent"/> of the specified type, if it exists.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/></typeparam>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <param name="component">The output argument that contains the <see cref="VolumeComponent"/>
        /// or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="VolumeComponent"/> is in the Volume Profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(out T)"/>
        /// <seealso cref="TryGetSubclassOf{T}"/>
        /// <seealso cref="TryGetAllSubclassOf{T}"/>
        public bool TryGet<T>(Type type, out T component)
            where T : VolumeComponent
        {
            component = null;

            foreach (var comp in components)
            {
                if (comp.GetType() == type)
                {
                    component = (T)comp;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the <seealso cref="VolumeComponent"/>, which is a subclass of <paramref name="type"/>, if
        /// it exists.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <param name="component">The output argument that contains the <see cref="VolumeComponent"/>
        /// or <c>null</c>.</param>
        /// <returns><c>true</c> if the <see cref="VolumeComponent"/> is in the Volume Profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(Type, out T)"/>
        /// <seealso cref="TryGet{T}(out T)"/>
        /// <seealso cref="TryGetAllSubclassOf{T}"/>
        public bool TryGetSubclassOf<T>(Type type, out T component)
            where T : VolumeComponent
        {
            component = null;

            foreach (var comp in components)
            {
                if (comp.GetType().IsSubclassOf(type))
                {
                    component = (T)comp;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all the <seealso cref="VolumeComponent"/> that are subclasses of the specified type,
        /// if there are any.
        /// </summary>
        /// <typeparam name="T">A type of <see cref="VolumeComponent"/>.</typeparam>
        /// <param name="type">A type that inherits from <see cref="VolumeComponent"/>.</param>
        /// <param name="result">The output list that contains all the <seealso cref="VolumeComponent"/>
        /// if any. Note that Unity does not clear this list.</param>
        /// <returns><c>true</c> if any <see cref="VolumeComponent"/> have been found in the profile,
        /// <c>false</c> otherwise.</returns>
        /// <seealso cref="TryGet{T}(Type, out T)"/>
        /// <seealso cref="TryGet{T}(out T)"/>
        /// <seealso cref="TryGetSubclassOf{T}"/>
        public bool TryGetAllSubclassOf<T>(Type type, List<T> result)
            where T : VolumeComponent
        {
            Assert.IsNotNull(components);
            int count = result.Count;

            foreach (var comp in components)
            {
                if (comp.GetType().IsSubclassOf(type))
                    result.Add((T)comp);
            }

            return count != result.Count;
        }


        /// <summary>
        /// A custom hashing function that Unity uses to compare the state of parameters.
        /// </summary>
        /// <returns>A computed hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                for (int i = 0; i < components.Count; i++)
                    hash = hash * 23 + components[i].GetHashCode();

                return hash;
            }
        }

        internal int GetComponentListHashCode()
        {
            unchecked
            {
                int hash = 17;

                for (int i = 0; i < components.Count; i++)
                    hash = hash * 23 + components[i].GetType().GetHashCode();

                return hash;
            }
        }
    }
}
