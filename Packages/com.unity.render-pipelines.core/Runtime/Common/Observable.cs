using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Represents an observable value of type T. Subscribers can be notified when the value changes.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public struct Observable<T>
    {
        /// <summary>
        /// Event that is triggered when the value changes.
        /// </summary>
        public event Action<T> onValueChanged;

        private T m_Value;

        /// <summary>
        /// The current value.
        /// </summary>
        public T value
        {
            get => m_Value;
            set
            {
                // Only invoke the event if the new value is different from the current value
                if (!EqualityComparer<T>.Default.Equals(value, m_Value))
                {
                    m_Value = value;

                    // Notify subscribers when the value changes
                    onValueChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Constructor with value
        /// </summary>
        /// <param name="newValue">The new value to be assigned.</param>
        public Observable(T newValue)
        {
            m_Value = newValue;
            onValueChanged = null;
        }
    }
}
