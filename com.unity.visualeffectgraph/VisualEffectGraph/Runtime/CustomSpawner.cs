using System;
using UnityEngine;

namespace UnityEngine.Experimental.VFX
{
    public interface ISpawner
    {
        float GetSpawnCount(float deltaTime, float currentTime, VFXExpressionValues vfxValues);
        void ReInit(VFXExpressionValues vfxValues);
    }
}

namespace WorkInProgress
{
    public class CustomSpawnerCallback : UnityEngine.Experimental.VFX.ISpawner
    {
        int m_reinitCount = 0;
        float m_spawnRate = 0.0f;

        public float GetSpawnCount(float deltaTime, float currentTime, VFXExpressionValues vfxValues)
        {
            m_spawnRate -= deltaTime;
            m_spawnRate = Mathf.Clamp(m_spawnRate, 0.0f, 10.0f);
            return 5000.0f * m_spawnRate * deltaTime;
        }

        public void ReInit(VFXExpressionValues vfxValues)
        {
            var a = vfxValues.GetVector4(0);
            int value = (int)a.x;
            int mod = (int)a.y;
            if (m_reinitCount % mod == value)
            {
                m_spawnRate += 1.0f;
            }
            m_reinitCount++;
        }
    }
}
