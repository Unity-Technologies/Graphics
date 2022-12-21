using System;
using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace UnityEditor.VFX.Test
{
    //Warning: This class is only used for editor test purpose
    class VFXCustomSpawnerCheckGarbage : VFXSpawnerCallbacks
    {
        public class InputProperties
        {
            public bool forcingGarbage = false;
        }

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        private static readonly int kForcingGarbageID = Shader.PropertyToID(nameof(InputProperties.forcingGarbage));
        private static readonly int kPositionID = Shader.PropertyToID("position");
        private static readonly int kColorID = Shader.PropertyToID("color");


        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            if (vfxValues.GetBool(kForcingGarbageID))
            {
                var garbage = "";
                for (int i = 0; i < 1024; ++i)
                    garbage += i + ", ";
                if (garbage.Length < 512)
                    Debug.Log("Won't hit.");
            }
            else
            {
                state.spawnCount += state.deltaTime * 10.0f;
                if (!state.vfxEventAttribute.HasVector3(kPositionID))
                    Debug.LogError("Unexpected missing Position.");

                if (!state.vfxEventAttribute.HasVector3(kColorID))
                    Debug.LogError("Unexpected missing Color.");

                state.vfxEventAttribute.SetVector3(kPositionID, Random.insideUnitSphere);
                state.vfxEventAttribute.SetVector3(kColorID, Random.insideUnitSphere);
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
