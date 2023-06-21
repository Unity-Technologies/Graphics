using UnityEngine.Playables;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Water surface behavior for timeline support.
    /// </summary>
    public class WaterSurfaceBehaviour : PlayableBehaviour
    {
        // This needs to be kept because we it to restore the simulation as active when timeline is disabled
        WaterSurface m_Target = null;

        /// <summary>
        /// Function called to process a frame.
        /// </summary>
        /// <param name="playable">Playable.</param>
        /// <param name="info">FrameData.</param>
        /// <param name="playerData">Target water surface.</param>
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Grab the target water surface (if it has been set and the simulation has been initialized)
            WaterSurface waterSurface = playerData as WaterSurface;
            if (waterSurface != null && waterSurface.simulation != null)
            {
                // Keep track of the water surface
                m_Target = waterSurface;
                // Disable the time steps (when in timeline), right now it is done in lazy fashion
                m_Target.simulation.DisableTimeSteps();

                // Grab the total timeline time
                var duration = playable.GetOutput(0).GetDuration();
                var currentTime = playable.GetTime();
                var numberOfFullLoops = (int)(currentTime / duration);
                currentTime -= numberOfFullLoops * duration;

                // Let's go through the Playables and add the relevant ones (partially)
                double currentTotalTime = 0.0;
                int inputCount = playable.GetInputCount();
                for (int i = 0; i < inputCount; i++)
                {
                    // Grab the input playable (make sure he is of the right type)
                    Playable inputPlayable = playable.GetInput(i);
                    if (inputPlayable.GetPlayableType() != typeof(WaterSurfacePlayableBehaviour))
                        continue;

                    // Let's grab the playable behavior
                    var waterPlayable = (ScriptPlayable<WaterSurfacePlayableBehaviour>)inputPlayable;
                    WaterSurfacePlayableBehaviour wsPB = waterPlayable.GetBehaviour();

                    // The clip is completely before the current time
                    if (wsPB.clipEnd <= currentTime)
                    {
                        currentTotalTime += (wsPB.clipEnd - wsPB.clipStart);
                    }
                    // The clip is partially before the current time
                    else if (wsPB.clipStart < currentTime)
                    {
                        currentTotalTime += (currentTime - wsPB.clipStart);
                    }
                }

                // Set the simulation time
                waterSurface.simulation.deltaTime = 1.0f / 60.0f;
                waterSurface.simulation.simulationTime = (float)(currentTotalTime) * waterSurface.timeMultiplier;
            }
        }

        /// <summary>
        /// Function called when the playable is destroyed.
        /// </summary>
        /// <param name="playable">Playable.</param>
        public override void OnPlayableDestroy(Playable playable)
        {
            if (m_Target != null && m_Target.simulation != null)
            {
                m_Target.simulation.EnableTimeSteps();
            }
        }
    }
}
