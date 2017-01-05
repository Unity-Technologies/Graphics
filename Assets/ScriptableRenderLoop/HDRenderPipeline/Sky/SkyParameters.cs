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
        protected class Unhashed : System.Attribute {}

        public float                    rotation = 0.0f;
        public float                    exposure = 0.0f;
        public float                    multiplier = 1.0f;
        public SkyResolution            resolution = SkyResolution.SkyResolution256;
        public EnvironementUpdateMode   updateMode = EnvironementUpdateMode.OnChanged;
        public float                    updatePeriod = 0.0f;

        private FieldInfo[] m_Properties;

        protected void OnEnable()
        {
            HDRenderPipeline renderPipeline = Utilities.GetHDRenderPipeline();
            if (renderPipeline == null)
            {
                return;
            }

            // Enumerate properties in order to compute the hash more quickly later on.
            m_Properties = GetType()
                            .GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .ToArray();

            if (renderPipeline.skyManager.skyParameters == null || renderPipeline.skyManager.skyParameters.GetType() != this.GetType()) // We allow override of parameters only if the type is different. It means that we changed the Sky Renderer and might need a new set of parameters.
                renderPipeline.skyManager.skyParameters = this;
            else if (renderPipeline.skyManager.skyParameters != this && renderPipeline.skyManager.skyParameters.GetType() == this.GetType())
                Debug.LogWarning("Tried to setup another SkyParameters component although there is already one enabled.");
        }

        protected void OnDisable()
        {
            HDRenderPipeline renderPipeline = Utilities.GetHDRenderPipeline();
            if (renderPipeline == null)
            {
                return;
            }

            // Reset the current sky parameter on the render loop
            if (renderPipeline.skyManager.skyParameters == this)
                renderPipeline.skyManager.skyParameters = null;
        }

        public int GetHash()
        {
            unchecked
            {
                int hash = 13;
                foreach (var p in m_Properties)
                {
                    bool unhashedAttribute = p.GetCustomAttributes(typeof(Unhashed), true).Length != 0;
                    System.Object obj = p.GetValue(this);
                    if (obj != null && !unhashedAttribute) // Sometimes it can be a null reference.
                        hash = hash * 23 + obj.GetHashCode();
                }
                return hash;
            }
        }
    }
}
