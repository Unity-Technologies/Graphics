using System;
using UnityEngine;
using UnityEngine.VFX;

class IncrementStripIndexOnStart : VFXSpawnerCallbacks
{
    public class InputProperties
    {
        [Tooltip("Maximum Strip Count (Used to cycle indices)")]
        public uint StripMaxCount = 8;
    }

    static private readonly int stripMaxCountID = Shader.PropertyToID("StripMaxCount");
    static private readonly int stripIndexID = Shader.PropertyToID("stripIndex");

    uint m_Index = 0;

    public override void OnPlay(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        m_Index = (m_Index + 1) % Math.Max(1, vfxValues.GetUInt(stripMaxCountID));
        state.vfxEventAttribute.SetUint(stripIndexID, m_Index);
    }

    public override void OnStop(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {
        m_Index = 0;
    }

    public override void OnUpdate(VFXSpawnerState state, VFXExpressionValues vfxValues, VisualEffect vfxComponent)
    {  

    }
}
