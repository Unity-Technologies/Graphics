namespace UnityEngine.Rendering.Tests
{
    class TestAnimationCurveVolumeComponent : VolumeComponent
    {
        public AnimationCurveParameter testParameter = new (AnimationCurve.Linear(0.5f, 10.0f, 1.0f, 15.0f), true);
    }
}
