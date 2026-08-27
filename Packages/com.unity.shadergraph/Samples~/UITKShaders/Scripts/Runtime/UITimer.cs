using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Shaders.UITK.Sample
{
    /// <summary>
    /// Drives a normalized [0, 1] value over time using a <see cref="VisualElement"/>'s
    /// scheduler and invokes a tick callback on each update. Used by the progress-bar
    /// example shaders to animate <c>_MeterValue</c> when no game logic is available.
    /// </summary>
    public static class UITimer
    {
        /// <summary>
        /// Schedules a recurring tick that ramps a normalized time value from 0 to 1,
        /// wraps back to 0, and calls <paramref name="onTick"/> on each update.
        /// </summary>
        /// <param name="anchor">Any element in the same panel; controls scheduler lifetime.</param>
        /// <param name="durationSeconds">Time to complete one 0→1 ramp.</param>
        /// <param name="onTick">Callback invoked with the current normalized value.</param>
        /// <param name="initialTime">Phase offset in [0, 1] added to the ramp.</param>
        public static IVisualElementScheduledItem Start(
            VisualElement anchor,
            float durationSeconds,
            Action<float> onTick,
            float initialTime = 0f)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            if (onTick == null) throw new ArgumentNullException(nameof(onTick));

            float phase = Mathf.Clamp01(initialTime);
            float duration = Mathf.Max(0.001f, durationSeconds);

            // The two clocks differ by hours of editor uptime, so a tick that survives an
            // edit<->play transition (Enter Play Mode Options keep the tree alive) would lurch
            // to an unrelated value at the switch. Exiting play folds the difference into a
            // basis offset so the ramp continues smoothly; entering play resets the basis so
            // the value is again a pure function of Time.time — a clean restart from the phase
            // rather than a jump, keeping deterministic captures exactly reproducible.
            bool wasPlaying = Application.isPlaying;
            double lastNow = wasPlaying ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
            double basis = 0;

            return anchor.schedule.Execute(() =>
            {
                // Game time while playing (deterministic: value = (phase + t / duration) mod 1
                // under fixed Time.captureDeltaTime stepping); editor realtime otherwise, so
                // the panel still animates while game time stands still.
                bool playing = Application.isPlaying;
                double now = playing ? Time.timeAsDouble : Time.realtimeSinceStartupAsDouble;
                if (playing != wasPlaying)
                {
                    basis = playing ? 0 : basis + (lastNow - now) / duration;
                    wasPlaying = playing;
                }
                lastNow = now;
                onTick((float)(((phase + basis + now / duration) % 1.0 + 1.0) % 1.0));
            }).Every(16);
        }
    }
}
