using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CommonSettings 
        : MonoBehaviour
    {
        [SerializeField]
        private string m_SkyRendererTypeName = "";
        public Type skyRendererType
        {
            set { m_SkyRendererTypeName = value != null ? value.FullName : ""; OnSkyRendererChanged(); }
            get { return m_SkyRendererTypeName == "" ? null : Assembly.GetAssembly(typeof(CommonSettings)).GetType(m_SkyRendererTypeName); }
        }


        void OnEnable()
        {
            HDRenderLoop renderLoop = Utilities.GetHDRenderLoop();
            if (renderLoop == null)
            {
                return;
            }

            OnSkyRendererChanged();
        }

        void OnDisable()
        {
        }

        void OnSkyRendererChanged()
        {
            HDRenderLoop renderLoop = Utilities.GetHDRenderLoop();
            if (renderLoop == null)
            {
                return;
            }

            renderLoop.InstantiateSkyRenderer(skyRendererType);

            List<SkyParameters> result = new List<SkyParameters>();
            gameObject.GetComponents<SkyParameters>(result);

            Type skyParamType = renderLoop.skyManager.GetSkyParameterType();

            // Disable all incompatible sky parameters and enable the compatible one
            bool found = false;
            foreach (SkyParameters param in result)
            {
                if (param.GetType() == skyParamType)
                {
                    // This is a workaround the fact that we can't control the order in which components are initialized.
                    // So it can happen that a given SkyParameter is OnEnabled before the CommonSettings and so fail the setup because the SkyRenderer is not yet initialized.
                    // So we disable it to for OnEnable to be called again.
                    param.enabled = false;

                    param.enabled = true;
                    found = true;
                }
                else
                {
                    param.enabled = false;
                }
            }

            // If it does not exist, create the parameters
            if (!found && skyParamType != null)
            {
                gameObject.AddComponent(skyParamType);
            }
        }
    }
}
