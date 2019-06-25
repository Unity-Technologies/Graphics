using System;
using UnityEngine.Rendering;


namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using RTHandle = RTHandleSystem.RTHandle;

    public class EyeProfileDataManager
    {

        public EyeDataInfo currentProfile = new EyeDataInfo();

        // TODO: Most of the inputs should either live on the profile or in the manager, not passed through.
        // This logic should be on the profile in a final version.
        public void UpdateProfileGeneratedData(CommandBuffer cmd, Material generateNormalMat, RTHandle normal, int eyeNormalSize, Texture2D noiseTex)
        {
           // if(currentProfile.NeedsUpdating())
            {
                HDUtils.SetRenderTarget(cmd, normal);
                generateNormalMat.SetTexture(HDShaderIDs._OutputTexture, normal);
                generateNormalMat.SetInt(HDShaderIDs._EyeMapSize, eyeNormalSize);

                //TODO: SET OUTSIDE.
                cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, noiseTex);

                cmd.DrawProcedural(Matrix4x4.identity, generateNormalMat, 0, MeshTopology.Triangles, 3, 1, null);

          //      currentProfile.ToggleUpdateDone();
            }
        }
    }
}
