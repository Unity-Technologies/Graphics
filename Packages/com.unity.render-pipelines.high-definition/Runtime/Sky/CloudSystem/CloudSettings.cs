using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This attribute is used to associate a unique ID to a cloud class.
    /// This is needed to be able to automatically register cloud classes and avoid collisions and refactoring class names causing data compatibility issues.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CloudUniqueID : Attribute
    {
        internal readonly int uniqueID;

        /// <summary>
        /// Attribute CloudUniqueID constructor.
        /// </summary>
        /// <param name="uniqueID">Cloud unique ID. Needs to be different from all other registered unique IDs.</param>
        public CloudUniqueID(int uniqueID)
        {
            this.uniqueID = uniqueID;
        }
    }

    /// <summary>
    /// Base class for custom Cloud Settings.
    /// </summary>
    [Preserve]
    [RequireDerived]
    public abstract class CloudSettings : VolumeComponent
    {
        static Dictionary<Type, int> s_CloudUniqueIDs = null;

        /// <summary>
        /// Returns the hash code of the cloud parameters.
        /// </summary>
        /// <param name="camera">The camera we want to use to compute the hash of the cloud.</param>
        /// <returns>The hash code of the cloud parameters.</returns>
        public virtual int GetHashCode(Camera camera)
        {
            // By default we don't need to consider the camera position.
            return GetHashCode();
        }

        /// <summary>
        /// Returns the cloud type unique ID.
        /// Use this to override the cloudType in the Visual Environment volume component.
        /// </summary>
        /// <typeparam name="T">Type of clouds.</typeparam>
        /// <returns>The unique ID for the requested cloud type.</returns>
        public static int GetUniqueID<T>()
        {
            return GetUniqueID(typeof(T));
        }

        /// <summary>
        /// Returns the cloud type unique ID.
        /// Use this to override the cloudType in the Visual Environment volume component.
        /// </summary>
        /// <param name="type">Type of clouds.</param>
        /// <returns>The unique ID for the requested cloud type.</returns>
        public static int GetUniqueID(Type type)
        {
            s_CloudUniqueIDs ??= new Dictionary<Type, int>();

            int uniqueID;
            if (!s_CloudUniqueIDs.TryGetValue(type, out uniqueID))
            {
                var uniqueIDs = type.GetCustomAttributes(typeof(CloudUniqueID), false);
                uniqueID = (uniqueIDs.Length == 0) ? -1 : ((CloudUniqueID)uniqueIDs[0]).uniqueID;
                s_CloudUniqueIDs[type] = uniqueID;
            }

            return uniqueID;
        }

        /// <summary>
        /// Returns the class type of the CloudRenderer associated with this Cloud Settings.
        /// </summary>
        /// <returns>The class type of the CloudRenderer associated with this Cloud Settings.</returns>
        public abstract Type GetCloudRendererType();

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            s_CloudUniqueIDs?.Clear();
            s_CloudUniqueIDs = null;
        }
#endif
    }
}
