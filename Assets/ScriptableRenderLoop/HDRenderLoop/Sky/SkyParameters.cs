using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [ExecuteInEditMode]
    public class SkyParameters : MonoBehaviour
    {
        public float rotation = 0.0f;
        public float exposure = 0.0f;
        public float multiplier = 1.0f;
        public SkyResolution resolution = SkyResolution.SkyResolution256;

        private FieldInfo[] m_Properties;

        protected void OnEnable()
        {
            HDRenderLoop renderLoop = Utilities.GetHDRenderLoop();
            if (renderLoop == null)
            {
                return;
            }

            // Enumerate properties in order to compute the hash more quickly later on.
            m_Properties = GetType()
                            .GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .ToArray();

            if (renderLoop.skyManager.skyParameters == null || renderLoop.skyManager.skyParameters.GetType() != this.GetType()) // We allow override of parameters only if the type is different. It means that we changed the Sky Renderer and might need a new set of parameters.
                renderLoop.skyManager.skyParameters = this;
            else if (renderLoop.skyManager.skyParameters != this && renderLoop.skyManager.skyParameters.GetType() == this.GetType())
                Debug.LogWarning("Tried to setup another SkyParameters component although there is already one enabled.");
        }

        protected void OnDisable()
        {
            HDRenderLoop renderLoop = Utilities.GetHDRenderLoop();
            if (renderLoop == null)
            {
                return;
            }

            // Reset the current sky parameter on the render loop
            if (renderLoop.skyManager.skyParameters == this)
                renderLoop.skyManager.skyParameters = null;
        }

        public int GetHash()
        {
            unchecked
            {
                int hash = 13;
                foreach (var p in m_Properties)
                {
                    System.Object obj = p.GetValue(this);
                    if (obj != null) // Sometimes it can be a null reference.
                        hash = hash * 23 + obj.GetHashCode();
                }
                return hash;
            }
        }
    }
}
