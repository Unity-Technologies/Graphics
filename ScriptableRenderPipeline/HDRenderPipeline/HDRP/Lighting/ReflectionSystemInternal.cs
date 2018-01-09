using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Internal
{
    class ReflectionSystemInternal
    {
        HashSet<PlanarReflectionProbe> m_PlanarReflectionProbes;
        HashSet<PlanarReflectionProbe> m_DirtyPlanarReflectionProbeBounds;

        Dictionary<PlanarReflectionProbe, BoundingSphere> m_PlanarReflectionProbeBounds;
        PlanarReflectionProbe[] m_PlanarReflectionProbesArray;
        BoundingSphere[] m_PlanarReflectionProbeBoundsArray;

        public ReflectionSystemInternal(ReflectionSystemParameters parameters, ReflectionSystemInternal previous)
        {
            m_PlanarReflectionProbes = new HashSet<PlanarReflectionProbe>();
            m_DirtyPlanarReflectionProbeBounds = new HashSet<PlanarReflectionProbe>();
            m_PlanarReflectionProbeBounds = new Dictionary<PlanarReflectionProbe, BoundingSphere>(parameters.maxPlanarReflectionProbes);
            m_PlanarReflectionProbesArray = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];
            m_PlanarReflectionProbeBoundsArray = new BoundingSphere[parameters.maxPlanarReflectionProbes];

            if (previous != null)
            {
                m_PlanarReflectionProbes.UnionWith(previous.m_PlanarReflectionProbes);
                m_DirtyPlanarReflectionProbeBounds.UnionWith(m_PlanarReflectionProbes);
            }
        }

        public void RegisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Add(planarProbe);
            m_DirtyPlanarReflectionProbeBounds.Add(planarProbe);
        }

        public void UnregisterProbe(PlanarReflectionProbe planarProbe)
        {
            m_PlanarReflectionProbes.Remove(planarProbe);
            m_DirtyPlanarReflectionProbeBounds.Remove(planarProbe);
        }

        public void Cull(Camera camera, ReflectionProbeCullResults results)
        {
            UpdateAllPlanarReflectionProbeBounds();

            var cullingGroup = new CullingGroup();
            cullingGroup.targetCamera = camera;
            cullingGroup.SetBoundingSpheres(m_PlanarReflectionProbeBoundsArray);
            cullingGroup.SetBoundingSphereCount(m_PlanarReflectionProbeBounds.Count);

            results.CullPlanarReflectionProbes(cullingGroup, m_PlanarReflectionProbesArray);
            
            cullingGroup.Dispose();
        }

        public void SetProbeBoundsDirty(PlanarReflectionProbe planarProbe)
        {
            m_DirtyPlanarReflectionProbeBounds.Add(planarProbe);
        }

        void UpdateAllPlanarReflectionProbeBounds()
        {
            if (m_DirtyPlanarReflectionProbeBounds.Count > 0)
            {
                m_DirtyPlanarReflectionProbeBounds.IntersectWith(m_PlanarReflectionProbes);
                foreach (var planarReflectionProbe in m_DirtyPlanarReflectionProbeBounds)
                    UpdatePlanarReflectionProbeBounds(planarReflectionProbe);

                m_PlanarReflectionProbeBounds.Values.CopyTo(m_PlanarReflectionProbeBoundsArray, 0);
                m_PlanarReflectionProbeBounds.Keys.CopyTo(m_PlanarReflectionProbesArray, 0);
            }
        }

        void UpdatePlanarReflectionProbeBounds(PlanarReflectionProbe planarReflectionProbe)
        {
            m_PlanarReflectionProbeBounds[planarReflectionProbe] = planarReflectionProbe.boundingSphere;
        }
    }
}
