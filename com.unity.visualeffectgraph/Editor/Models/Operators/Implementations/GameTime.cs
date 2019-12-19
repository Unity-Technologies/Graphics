using System;
using UnityEngine;


namespace UnityEditor.VFX
{
    [VFXInfo(category = "BuiltIn")]
    class GameTime : VFXOperator
    {
        public override string libraryName => "Game Time";
        public override string name => libraryName;

        public class OutputProperties
        {
            [Tooltip("The completion time in seconds since the last frame")]
            public float deltaTime;
            [Tooltip("The timeScale-independent interval in seconds from the last frame to the current one")]
            public float unscaledDeltaTime;
            [Tooltip("A smoothed out Time.deltaTime")]
            public float smoothDeltaTime;
            [Tooltip("The time at the beginning of this frame. This is the time in seconds since the start of the game.")]
            public float totalTime;
            [Tooltip("The timeScale-independent interval in seconds from the last frame to the current one.")]
            public float unscaledTotalTime;
            [Tooltip("The time this frame has started. This is the time in seconds since the last level has been loaded.")]
            public float totalTimeSinceSceneLoad;
            [Tooltip("The scale at which time passes")]
            public float timeScale;
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[]
            {
                VFXBuiltInExpression.GameDeltaTime,
                VFXBuiltInExpression.GameUnscaledDeltaTime,
                VFXBuiltInExpression.GameSmoothDeltaTime,
                VFXBuiltInExpression.GameTotalTime,
                VFXBuiltInExpression.GameUnscaledTotalTime,
                VFXBuiltInExpression.GameTotalTimeSinceSceneLoad,
                VFXBuiltInExpression.GameTimeScale
            };
        }
    }
}
