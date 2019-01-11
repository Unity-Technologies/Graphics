using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Motion Blur")]
    public sealed class MotionBlur : VolumeComponent, IPostProcessComponent
    {


        public MinIntParameter sampleCount = new MinIntParameter(8, 2);
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Sets the intensity of the motion blur effect. Acts as a multiplier for velocities.")]
        public MinFloatParameter intensity = new MinFloatParameter(1.0f, 0.0f);
        [Tooltip("Sets the maximum velocity, in pixels, for everything except Camera rotation. Larger values make the limitation of the algorithm more evident, but result in a wider blur. Suggested range is [32, 128].")]
        public ClampedFloatParameter maxVelocity = new ClampedFloatParameter(64.0f, 0.0f, 256.0f);
        [Tooltip("Sets the minimum velocity, in pixels, that a GameObject must have to contribute to the motion blur effect.")]
        public ClampedFloatParameter minVelInPixels = new ClampedFloatParameter(2.0f, 0.0f, 64.0f);
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have.")]
        public ClampedFloatParameter cameraRotationVelocityClamp = new ClampedFloatParameter(0.1f, 0.0f, 0.2f);

        // Hidden settings. 
        // This control how much min and max velocity in a tile need to be similar to allow for the fast path. Lower this value, more pixels will go to the slow path. 
        public MinFloatParameter tileMinMaxVelRatioForHighQuality => new MinFloatParameter(0.25f, 0.0f);

        public bool IsActive()
        {
            return enabled && intensity > 0.0f;
        }
    }
}
