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

        /// <summary>
        /// Returns true if any of the debug settings are currently active.
        /// </summary>
        public virtual bool AreAnySettingsActive
        {
            get
            {
                foreach (IDebugDisplaySettingsData setting in m_Settings)
                {
                    if (setting.AreAnySettingsActive)
                        return true;
                }

                return false;
            }
        }

        public virtual bool IsPostProcessingAllowed { get; }

        /// <summary>
        /// Returns true if lighting is active for current state of debug settings.
        /// </summary>
        public virtual bool IsLightingActive
        {
            get
            {
                bool lightingActive = true;
                foreach (IDebugDisplaySettingsData setting in m_Settings)
                    lightingActive &= setting.IsLightingActive;
                return lightingActive;
            }
        }
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

        /// <summary>
        /// Attempts to get the color that should be used to clear the screen according to current debug settings.
        /// </summary>
        /// <param name="color">A reference to the screen clear color to use.</param>
        /// <returns>True if the color reference was updated, and false otherwise.</returns>
        public virtual bool TryGetScreenClearColor(ref Color color)
        {
            foreach (IDebugDisplaySettingsData setting in m_Settings)
            {
                if (setting.TryGetScreenClearColor(ref color))
                    return true;
            }

            return false;
        }
    }
}
