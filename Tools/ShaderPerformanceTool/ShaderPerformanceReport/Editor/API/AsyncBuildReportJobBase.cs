using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools
{
    public abstract class AsyncBuildReportJobBase : AsyncJobBase
    {
        public BuildTarget target { get; private set; }

        protected AsyncBuildReportJobBase(BuildTarget target)
        {
            this.target = target;
        }

        public abstract ShaderBuildReport GetBuildReport();
    }
}
