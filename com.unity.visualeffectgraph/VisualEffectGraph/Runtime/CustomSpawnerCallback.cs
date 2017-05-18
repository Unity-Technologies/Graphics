using System;
using UnityEngine;


namespace WorkInProgress
{
    public class CustomSpawnerCallback : VFXSpawnerFunction
    {
        int m_reinitCount = 0;
        float m_spawnRate = 0.0f;

        public override float GetSpawnCount(float deltaTime, float currentTime, VFXExpressionValues vfxValues)
        {
            m_spawnRate -= deltaTime;
            m_spawnRate = Mathf.Clamp(m_spawnRate, 0.0f, 10.0f);
            return 5000.0f * m_spawnRate * deltaTime;
        }

        public override void ReInit(VFXExpressionValues vfxValues)
        {
            var a = vfxValues.GetVector4("a");
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
