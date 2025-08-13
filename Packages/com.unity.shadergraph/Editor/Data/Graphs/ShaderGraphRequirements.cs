using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct ShaderGraphRequirements
    {
        [SerializeField] List<NeededTransform> m_RequiresTransforms;
        [SerializeField] NeededCoordinateSpace m_RequiresNormal;
        [SerializeField] NeededCoordinateSpace m_RequiresBitangent;
        [SerializeField] NeededCoordinateSpace m_RequiresTangent;
        [SerializeField] NeededCoordinateSpace m_RequiresViewDir;
        [SerializeField] NeededCoordinateSpace m_RequiresPosition;
        [SerializeField] NeededCoordinateSpace m_RequiresPositionPredisplacement;
        [SerializeField] bool m_RequiresScreenPosition;
        [SerializeField] bool m_RequiresNDCPosition;
        [SerializeField] bool m_RequiresPixelPosition;
        [SerializeField] bool m_RequiresVertexColor;
        [SerializeField] bool m_RequiresFaceSign;
        [SerializeField] List<UVChannel> m_RequiresMeshUVs;
        [SerializeField] bool m_RequiresDepthTexture;
        [SerializeField] bool m_RequiresCameraOpaqueTexture;
        [SerializeField] bool m_RequiresTime;
        [SerializeField] bool m_RequiresVertexSkinning;
        [SerializeField] bool m_RequiresVertexID;
        [SerializeField] bool m_RequiresInstanceID;
        [SerializeField] bool m_RequiresUITK;
        [SerializeField] List<UVChannel> m_RequiresMeshUVDerivatives;

        internal static ShaderGraphRequirements none
        {
            get
            {
                return new ShaderGraphRequirements
                {
                    m_RequiresTransforms = new List<NeededTransform>(),
                    m_RequiresMeshUVs = new List<UVChannel>(),
                    m_RequiresMeshUVDerivatives = new List<UVChannel>()
                };
            }
        }

        public List<NeededTransform> requiresTransforms
        {
            get { return m_RequiresTransforms; }
            internal set { m_RequiresTransforms = value; }
        }

        public NeededCoordinateSpace requiresNormal
        {
            get { return m_RequiresNormal; }
            internal set { m_RequiresNormal = value; }
        }

        public NeededCoordinateSpace requiresBitangent
        {
            get { return m_RequiresBitangent; }
            internal set { m_RequiresBitangent = value; }
        }

        public NeededCoordinateSpace requiresTangent
        {
            get { return m_RequiresTangent; }
            internal set { m_RequiresTangent = value; }
        }

        public NeededCoordinateSpace requiresViewDir
        {
            get { return m_RequiresViewDir; }
            internal set { m_RequiresViewDir = value; }
        }

        public NeededCoordinateSpace requiresPosition
        {
            get { return m_RequiresPosition; }
            internal set { m_RequiresPosition = value; }
        }

        public NeededCoordinateSpace requiresPositionPredisplacement
        {
            get { return m_RequiresPositionPredisplacement; }
            internal set { m_RequiresPositionPredisplacement = value; }
        }

        public bool requiresScreenPosition
        {
            get { return m_RequiresScreenPosition; }
            internal set { m_RequiresScreenPosition = value; }
        }

        public bool requiresNDCPosition
        {
            get { return m_RequiresNDCPosition; }
            internal set { m_RequiresNDCPosition = value; }
        }

        public bool requiresPixelPosition
        {
            get { return m_RequiresPixelPosition; }
            internal set { m_RequiresPixelPosition = value; }
        }

        public bool requiresVertexColor
        {
            get { return m_RequiresVertexColor; }
            internal set { m_RequiresVertexColor = value; }
        }

        public bool requiresFaceSign
        {
            get { return m_RequiresFaceSign; }
            internal set { m_RequiresFaceSign = value; }
        }

        public List<UVChannel> requiresMeshUVs
        {
            get { return m_RequiresMeshUVs; }
            internal set { m_RequiresMeshUVs = value; }
        }

        public List<UVChannel> requiresMeshUVDerivatives
        {
            get { return m_RequiresMeshUVDerivatives; }
            internal set { m_RequiresMeshUVDerivatives = value; }
        }

        public bool requiresDepthTexture
        {
            get { return m_RequiresDepthTexture; }
            internal set { m_RequiresDepthTexture = value; }
        }

        public bool requiresCameraOpaqueTexture
        {
            get { return m_RequiresCameraOpaqueTexture; }
            internal set { m_RequiresCameraOpaqueTexture = value; }
        }

        public bool requiresTime
        {
            get { return m_RequiresTime; }
            internal set { m_RequiresTime = value; }
        }

        public bool requiresVertexSkinning
        {
            get { return m_RequiresVertexSkinning; }
            internal set { m_RequiresVertexSkinning = value; }
        }

        public bool requiresVertexID
        {
            get { return m_RequiresVertexID; }
            internal set { m_RequiresVertexID = value; }
        }

        public bool requiresInstanceID
        {
            get { return m_RequiresInstanceID; }
            internal set { m_RequiresInstanceID = value; }
        }

        internal bool requiresUITK
        {
            get { return m_RequiresUITK; }
            set { m_RequiresUITK = value; }
        }

        internal bool NeedsTangentSpace()
        {
            var compoundSpaces = m_RequiresBitangent | m_RequiresNormal | m_RequiresPosition
                | m_RequiresTangent | m_RequiresViewDir | m_RequiresPosition
                | m_RequiresNormal;

            return (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
        }

        internal ShaderGraphRequirements Union(ShaderGraphRequirements other)
        {
            var newReqs = new ShaderGraphRequirements();
            newReqs.m_RequiresNormal = other.m_RequiresNormal | m_RequiresNormal;
            newReqs.m_RequiresTangent = other.m_RequiresTangent | m_RequiresTangent;
            newReqs.m_RequiresBitangent = other.m_RequiresBitangent | m_RequiresBitangent;
            newReqs.m_RequiresViewDir = other.m_RequiresViewDir | m_RequiresViewDir;
            newReqs.m_RequiresPosition = other.m_RequiresPosition | m_RequiresPosition;
            newReqs.m_RequiresPositionPredisplacement = other.m_RequiresPositionPredisplacement | m_RequiresPositionPredisplacement;
            newReqs.m_RequiresScreenPosition = other.m_RequiresScreenPosition | m_RequiresScreenPosition;
            newReqs.m_RequiresNDCPosition = other.m_RequiresNDCPosition | m_RequiresNDCPosition;
            newReqs.m_RequiresPixelPosition = other.m_RequiresPixelPosition | m_RequiresPixelPosition;
            newReqs.m_RequiresVertexColor = other.m_RequiresVertexColor | m_RequiresVertexColor;
            newReqs.m_RequiresFaceSign = other.m_RequiresFaceSign | m_RequiresFaceSign;
            newReqs.m_RequiresDepthTexture = other.m_RequiresDepthTexture | m_RequiresDepthTexture;
            newReqs.m_RequiresCameraOpaqueTexture = other.m_RequiresCameraOpaqueTexture | m_RequiresCameraOpaqueTexture;
            newReqs.m_RequiresTime = other.m_RequiresTime | m_RequiresTime;
            newReqs.m_RequiresVertexSkinning = other.m_RequiresVertexSkinning | m_RequiresVertexSkinning;
            newReqs.m_RequiresVertexID = other.m_RequiresVertexID | m_RequiresVertexID;
            newReqs.m_RequiresInstanceID = other.m_RequiresInstanceID | m_RequiresInstanceID;
            newReqs.m_RequiresUITK = other.m_RequiresUITK | m_RequiresUITK;

            newReqs.m_RequiresMeshUVs = new List<UVChannel>();
            if (m_RequiresMeshUVs != null)
                newReqs.m_RequiresMeshUVs.AddRange(m_RequiresMeshUVs);
            if (other.m_RequiresMeshUVs != null)
                newReqs.m_RequiresMeshUVs.AddRange(other.m_RequiresMeshUVs);

            newReqs.m_RequiresMeshUVDerivatives = new List<UVChannel>();
            if (m_RequiresMeshUVDerivatives != null)
                newReqs.m_RequiresMeshUVDerivatives.AddRange(m_RequiresMeshUVDerivatives);
            if (other.m_RequiresMeshUVDerivatives != null)
                newReqs.m_RequiresMeshUVDerivatives.AddRange(other.m_RequiresMeshUVDerivatives);

            return newReqs;
        }

        internal static ShaderGraphRequirements FromNodes<T>(IEnumerable<T> nodes, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool includeIntermediateSpaces = true, bool[] texCoordNeedsDerivs = null)
            where T : AbstractMaterialNode
        {
            var reqs = new ShaderGraphRequirements();

            reqs.m_RequiresTransforms = new List<NeededTransform>();
            reqs.m_RequiresMeshUVs = new List<UVChannel>();
            reqs.m_RequiresMeshUVDerivatives = new List<UVChannel>();

            foreach (var node in nodes)
            {
                if (node is IMayRequireTransform a)
                    reqs.m_RequiresTransforms.AddRange(a.RequiresTransform());

                if (node is IMayRequireNormal b)
                    reqs.m_RequiresNormal |= b.RequiresNormal(stageCapability);

                if (node is IMayRequireBitangent c)
                    reqs.m_RequiresBitangent |= c.RequiresBitangent(stageCapability);

                if (node is IMayRequireTangent d)
                    reqs.m_RequiresTangent |= d.RequiresTangent(stageCapability);

                if (node is IMayRequireViewDirection e)
                    reqs.m_RequiresViewDir |= e.RequiresViewDirection(stageCapability);

                if (node is IMayRequirePosition f)
                    reqs.m_RequiresPosition |= f.RequiresPosition(stageCapability);

                if (node is IMayRequirePositionPredisplacement g)
                    reqs.m_RequiresPositionPredisplacement |= g.RequiresPositionPredisplacement(stageCapability);

                if (!reqs.m_RequiresScreenPosition && node is IMayRequireScreenPosition h)
                    reqs.m_RequiresScreenPosition = h.RequiresScreenPosition(stageCapability);

                if (!reqs.m_RequiresNDCPosition && node is IMayRequireNDCPosition i)
                    reqs.m_RequiresNDCPosition = i.RequiresNDCPosition(stageCapability);

                if (!reqs.m_RequiresPixelPosition && node is IMayRequirePixelPosition j)
                    reqs.m_RequiresPixelPosition = j.RequiresPixelPosition(stageCapability);

                if (!reqs.m_RequiresVertexColor && node is IMayRequireVertexColor k)
                    reqs.m_RequiresVertexColor = k.RequiresVertexColor(stageCapability);

                if (!reqs.m_RequiresFaceSign && node is IMayRequireFaceSign l)
                    reqs.m_RequiresFaceSign = l.RequiresFaceSign(stageCapability);

                if (!reqs.m_RequiresDepthTexture && node is IMayRequireDepthTexture m)
                    reqs.m_RequiresDepthTexture = m.RequiresDepthTexture(stageCapability);

                if (!reqs.m_RequiresCameraOpaqueTexture && node is IMayRequireCameraOpaqueTexture n)
                    reqs.m_RequiresCameraOpaqueTexture = n.RequiresCameraOpaqueTexture(stageCapability);

                if (!reqs.m_RequiresTime)
                    reqs.m_RequiresTime = node.RequiresTime();

                if (!reqs.m_RequiresVertexSkinning && node is IMayRequireVertexSkinning o)
                    reqs.m_RequiresVertexSkinning = o.RequiresVertexSkinning(stageCapability);

                if (!reqs.m_RequiresVertexID && node is IMayRequireVertexID p)
                    reqs.m_RequiresVertexID = p.RequiresVertexID(stageCapability);

                if (node is IMayRequireMeshUV q)
                {
                    for (int uvIndex = 0; uvIndex < ShaderGeneratorNames.UVCount; ++uvIndex)
                    {
                        var channel = (UVChannel)uvIndex;
                        if (q.RequiresMeshUV(channel))
                        {
                            reqs.m_RequiresMeshUVs.Add(channel);
                            if (texCoordNeedsDerivs is not null &&
                                uvIndex < texCoordNeedsDerivs.Length &&
                                texCoordNeedsDerivs[uvIndex])
                            {
                                reqs.m_RequiresMeshUVDerivatives.Add(channel);
                            }
                        }
                    }
                }

                if (!reqs.m_RequiresInstanceID && node is IMayRequireInstanceID r)
                    reqs.m_RequiresInstanceID = r.RequiresInstanceID(stageCapability);

                if (!reqs.m_RequiresUITK && node is IMayRequireUITK w)
                    reqs.m_RequiresUITK = w.RequiresUITK(stageCapability);
            }

            reqs.m_RequiresTransforms = reqs.m_RequiresTransforms.Distinct().ToList();

            // if anything needs tangentspace we have make
            // sure to have our othonormal basis!
            if (includeIntermediateSpaces)
            {
                var compoundSpaces = reqs.m_RequiresBitangent | reqs.m_RequiresNormal | reqs.m_RequiresPosition
                    | reqs.m_RequiresTangent | reqs.m_RequiresViewDir | reqs.m_RequiresPosition
                    | reqs.m_RequiresNormal;

                var needsTangentSpace = (compoundSpaces & NeededCoordinateSpace.Tangent) > 0;
                if (needsTangentSpace)
                {
                    reqs.m_RequiresBitangent |= NeededCoordinateSpace.World;
                    reqs.m_RequiresNormal |= NeededCoordinateSpace.World;
                    reqs.m_RequiresTangent |= NeededCoordinateSpace.World;
                }
            }

            return reqs;
        }

        internal static ShaderGraphRequirements FromUvDerivativeList(List<UVChannel> meshUVDerivatives)
        {
            var reqs = new ShaderGraphRequirements()
            {
                m_RequiresMeshUVDerivatives = meshUVDerivatives,
            };

            return reqs;
        }
    }
}
