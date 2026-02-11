using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Helper class that serializes Rendering Debugger state across domain reloads.
    /// If you want the state of your debug variables to persist across domain reloads, the setting class should
    /// be marked [Serializable] and implement the <see cref="ISerializedDebugDisplaySettings"/> interface.
    /// Then you can access the serialized instance through <see cref="GetOrCreate{T}"/>.
    /// </summary>
    public sealed class DebugDisplaySerializer
#if UNITY_EDITOR
        : ScriptableSingleton<DebugDisplaySerializer>
#endif
    {
        // For player builds, implement the singleton pattern internally
#if !UNITY_EDITOR
        static Lazy<DebugDisplaySerializer> s_Instance = new (() => new ());

        /// <summary>Gets the instance of the singleton.</summary>
        public static DebugDisplaySerializer instance => s_Instance.Value;
#endif

        [SerializeReference]
        List<ISerializedDebugDisplaySettings> m_Settings = new();

        [SerializeField]
        SerializedDictionary<string, bool> m_FoldoutStates = new();

        ISerializedDebugDisplaySettings GetOrCreate(Type type)
        {
            var setting = Get(type);
            if (setting != null)
                return setting;

            if (!type.IsAbstract && !type.IsInterface)
            {
                setting = Activator.CreateInstance(type, nonPublic: true) as ISerializedDebugDisplaySettings;
                m_Settings.Add(setting);
                return setting;
            }

            return null;
        }

        ISerializedDebugDisplaySettings Get(Type type)
        {
            int numSettings = m_Settings.Count;
            for (int i = 0; i < numSettings; ++i)
            {
                if (m_Settings[i].GetType() == type)
                    return m_Settings[i];
            }

            return null;
        }

        /// <summary>
        /// Returns the serialized instance for the given debug display settings type.
        /// If the instance does not exist yet, it will be created. The next time this method is called with
        /// the same type, the same instance will be returned.
        /// </summary>
        /// <typeparam name="T">The debug display settings type to retrieve.</typeparam>
        /// <returns>The serialized instance for the given debug display settings type.</returns>
        public static T GetOrCreate<T>() where T : class, ISerializedDebugDisplaySettings
        {
            return instance.GetOrCreate(typeof(T)) as T;
        }

        /// <summary>
        /// Returns the serialized instance for the given debug display settings type.
        /// If the instance does not exist, null is returned.
        /// </summary>
        /// <typeparam name="T">The debug display settings type to retrieve.</typeparam>
        /// <returns>The serialized instance for the given debug display settings type if it exists, or null otherwise.</returns>
        public static T Get<T>() where T : class, ISerializedDebugDisplaySettings
        {
            return instance.Get(typeof(T)) as T;
        }

        /// <summary>
        /// Remove all instances of serialized debug display settings that have been created.
        /// </summary>
        public static void Clear()
        {
            instance.m_Settings.Clear();
        }

        /// <summary>
        /// Restore foldout open/closed states from serialized data.
        /// </summary>
        public static void LoadFoldoutStates()
        {
            DebugManager.instance.ForEachWidget(widget =>
            {
                if (widget is DebugUI.Foldout foldout)
                    if (instance.m_FoldoutStates.TryGetValue(widget.queryPath, out bool opened))
                        foldout.opened = opened;
            });
        }

        /// <summary>
        /// Save foldout open/closed states to serialized data.
        /// </summary>
        public static void SaveFoldoutStates()
        {
            instance.m_FoldoutStates.Clear();

            DebugManager.instance.ForEachWidget(widget =>
            {
                if (widget is DebugUI.Foldout foldout)
                    instance.m_FoldoutStates.TryAdd(widget.queryPath, foldout.opened);
            });
        }
    }
}
