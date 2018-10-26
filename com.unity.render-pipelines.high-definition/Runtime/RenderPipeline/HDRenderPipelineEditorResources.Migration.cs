#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset
using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class HDRenderPipelineEditorResources
    {
        enum Version
        {
            None
        }

        [HideInInspector, SerializeField]
        Version m_Version;

        public void UpgradeIfNeeded()
        {
            //nothing to do at the moment
        }
    }
}
#endif
