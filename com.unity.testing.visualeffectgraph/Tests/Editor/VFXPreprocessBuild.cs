using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEngine.VFX.Test
{
    public class VFXPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            //Rebuild all vfx before creating a player for test project (could be really slow)
            UnityEditor.VFX.VFXCacheManager.Build();
        }
    }
}
