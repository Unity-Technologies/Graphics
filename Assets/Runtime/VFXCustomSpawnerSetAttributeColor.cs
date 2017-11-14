using System;
using UnityEngine;
using UnityEngine.VFX;

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;


namespace UnityEditor.VFX
{
    public class VFXCustomSpawnerSetAttributeColor : VFXSpawnerFunction
    {
        public class InputProperties
        {
            public Vector3 Color;
        }

        public override void OnStart(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
            if (state.vfxEventAttribute.HasVector3("color"))
            {
                state.vfxEventAttribute.SetVector3("color", vfxValues.GetVector3("Color"));
            }
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues)
        {
        }
    }
}
