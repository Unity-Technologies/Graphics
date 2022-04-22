using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The state component for the <see cref="ProcessOnIdleAgent"/>.
    /// </summary>
    public class ProcessOnIdleStateComponent : StateComponent<ProcessOnIdleStateComponent.StateUpdater>
    {
        /// <summary>
        /// The state updater.
        /// </summary>
        public class StateUpdater : BaseUpdater<ProcessOnIdleStateComponent>
        {
            /// <summary>
            /// Records the fact that the mouse was idle.
            /// </summary>
            public void SetTriggerUpdate()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }
    }

    /// <summary>
    /// An agent responsible for triggering graph processing when the mouse stays idle for some period of time.
    /// </summary>
    public class ProcessOnIdleAgent
    {
        internal const int idleTimeBeforeGraphProcessingMs = 1000;
        internal const int idleTimeBeforeGraphProcessingMsPlayMode = 1000;

        readonly Stopwatch m_IdleTimer;
        Preferences m_Preferences;

        /// <summary>
        /// The state of the agent.
        /// </summary>
        public ProcessOnIdleStateComponent StateComponent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessOnIdleAgent"/> class.
        /// </summary>
        /// <param name="preferences">The tool preferences.</param>
        public ProcessOnIdleAgent(Preferences preferences)
        {
            m_IdleTimer = new Stopwatch();
            m_Preferences = preferences;
            StateComponent = new ProcessOnIdleStateComponent();
        }

        void ResetTimer()
        {
            m_IdleTimer.Restart();
        }

        /// <summary>
        /// Stops the timer used to compute the mouse idle delay.
        /// </summary>
        public void StopTimer()
        {
            m_IdleTimer.Stop();
        }

        /// <summary>
        /// Callback for <see cref="MouseMoveEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        public void OnMouseMove(MouseMoveEvent e)
        {
            ResetTimer();
        }

        /// <summary>
        /// Updates the agent state if the mouse stays idle long enough.
        /// </summary>
        public void Execute()
        {
            if (m_Preferences.GetBool(BoolPref.OnlyProcessWhenIdle))
            {
                if (!m_IdleTimer.IsRunning)
                {
                    ResetTimer();
                }
            }
            else
            {
                if (m_IdleTimer.IsRunning)
                {
                    StopTimer();
                }
            }

            var elapsedTime = m_IdleTimer.ElapsedMilliseconds;
            if (elapsedTime >= (EditorApplication.isPlaying
                ? idleTimeBeforeGraphProcessingMsPlayMode
                : idleTimeBeforeGraphProcessingMs))
            {
                m_IdleTimer.Restart();

                using (var updater = StateComponent.UpdateScope)
                {
                    updater.SetTriggerUpdate();
                }
            }
        }
    }
}
