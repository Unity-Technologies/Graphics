using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public abstract class DebugDisplaySettings<T> : IDebugDisplaySettings
        where T : IDebugDisplaySettings, new()
    {
        protected readonly HashSet<IDebugDisplaySettingsData> m_Settings = new HashSet<IDebugDisplaySettingsData>();

        private static readonly Lazy<T> s_Instance = new Lazy<T>(() =>
        {
            var instance = new T();
            instance.Reset();
            return instance;
        });

        /// <summary>
        /// The singleton instance that contains the current settings of Rendering Debugger.
        /// </summary>
        public static T Instance => s_Instance.Value;

        #region IDebugDisplaySettingsQuery
        public abstract bool AreAnySettingsActive { get; }
        public abstract bool IsPostProcessingAllowed { get; }
        public abstract bool IsLightingActive { get; }
        #endregion

        protected TData Add<TData>(TData newData) where TData : IDebugDisplaySettingsData
        {
            m_Settings.Add(newData);
            return newData;
        }

        public void ForEach(Action<IDebugDisplaySettingsData> onExecute)
        {
            foreach (IDebugDisplaySettingsData setting in m_Settings)
            {
                onExecute(setting);
            }
        }

        public virtual void Reset()
        {
            m_Settings.Clear();
        }

        public abstract bool TryGetScreenClearColor(ref Color color);
    }
}
