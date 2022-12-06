using UnityEngine.Playables;

namespace UnityEngine.Rendering.HighDefinition
{
    // This structure's only goal is to keep track of the limits of the clip to be able to
    // correctly update the simulation time of the water
    class WaterSurfacePlayableBehaviour : PlayableBehaviour
    {
        public double clipStart { get; set; }
        public double clipEnd { get; set; }
    }
}
