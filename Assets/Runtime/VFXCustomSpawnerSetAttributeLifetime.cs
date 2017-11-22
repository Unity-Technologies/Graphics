using System;
using UnityEngine;
using UnityEngine.VFX;

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;


namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerSetAttributeLifetime : VFXSpawnerFunction
    {
        public class InputProperties
        {
            public float Lifetime;
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
            if (state.vfxEventAttribute.HasFloat("lifetime"))
            {
                state.vfxEventAttribute.SetFloat("lifetime", vfxValues.GetFloat("Lifetime"));
            }
        }

        public override void OnStart(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }
    }
}
