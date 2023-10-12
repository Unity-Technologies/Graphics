using System;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    class VFXCustomSpawnerGameObjectPosition : VFXSpawnerCallbacks
    {

        public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }

        static private int s_gameObjectPositionID = Shader.PropertyToID("gameObjectPosition");

        public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
            state.vfxEventAttribute.SetVector3(s_gameObjectPositionID, vfxComponent.gameObject.transform.position);
        }

        public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
        {
        }
    }
}
