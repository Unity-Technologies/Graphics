namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct HDProbeCullState
    {
        CullingGroup m_CullingGroup;
        HDProbe[] m_HDProbes;
        Hash128 m_StateHash;

        internal CullingGroup cullingGroup => m_CullingGroup;
        internal HDProbe[] hdProbes => m_HDProbes;
        internal Hash128 stateHash => m_StateHash;

        internal HDProbeCullState(CullingGroup cullingGroup, HDProbe[] hdProbes, Hash128 stateHash)
        {
            m_CullingGroup = cullingGroup;
            m_HDProbes = hdProbes;
            m_StateHash = stateHash;
        }
    }
}
