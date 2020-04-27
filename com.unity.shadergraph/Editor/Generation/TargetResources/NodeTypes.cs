using System.Linq;

namespace UnityEditor.ShaderGraph
{
    internal static class NodeTypes
    {
        public static class Artistic
        {
            public static NodeTypeCollection Adjustment = new NodeTypeCollection()
            {
                typeof(ChannelMixerNode ),
                typeof(ContrastNode      ),
                typeof(HueNode           ),
                typeof(InvertColorsNode  ),
                typeof(ReplaceColorNode  ),
                typeof(SaturationNode    ),
                typeof(WhiteBalanceNode  )

            };

            public static NodeTypeCollection Blend = new NodeTypeCollection()
            {
                  typeof(BlendNode)
            };

            public static NodeTypeCollection Filter = new NodeTypeCollection()
            {
                typeof(DitherNode)
            };

            public static NodeTypeCollection Mask = new NodeTypeCollection()
            {
                typeof(ChannelMaskNode ),
                typeof(ColorMaskNode )
            };

            public static NodeTypeCollection Normal = new NodeTypeCollection()
            {
                typeof(NormalBlendNode ),
                typeof(NormalFromHeightNode ),
                typeof(NormalFromTextureNode ),
                typeof(NormalReconstructZNode ),
                typeof(NormalStrengthNode ),
                typeof(NormalUnpackNode )

            };

            public static NodeTypeCollection Utility = new NodeTypeCollection()
            {
                typeof(ColorspaceConversionNode)
            };

            public static NodeTypeCollection All = new NodeTypeCollection()
            {
                Adjustment,
                Blend,
                Filter,
                Mask,
                Normal,
                Utility
            };
        }

        public static NodeTypeCollection Channel = new NodeTypeCollection()
        {
            typeof(CombineNode ),
            typeof(FlipNode ),
            typeof(SplitNode ),
            typeof(SwizzleNode )
        };

        public static class Input
        {
            public static NodeTypeCollection Basic = new NodeTypeCollection()
            {
                typeof(BooleanNode ),
                typeof(ColorNode ),
                typeof(ConstantNode ),
                typeof(IntegerNode ),
                typeof(SliderNode ),
                typeof(TimeNode ),
                typeof(Vector1Node ),
                typeof(Vector2Node ),
                typeof(Vector3Node ),
                typeof(Vector4Node )

            };

            public static NodeTypeCollection Geometry = new NodeTypeCollection()
            {
                typeof(BitangentVectorNode ),
                typeof(NormalVectorNode ),
                typeof(PositionNode ),
                typeof(ScreenPositionNode ),
                typeof(TangentVectorNode ),
                typeof(UVNode ),
                typeof(VertexColorNode ),
                typeof(ViewDirectionNode )
            };

            public static NodeTypeCollection Gradient = new NodeTypeCollection()
            {
                typeof(BlackbodyNode ),
                typeof(GradientNode ),
                typeof(SampleGradient )
            };

            public static NodeTypeCollection Lighting = new NodeTypeCollection()
            {
                typeof(AmbientNode ),
                typeof(BakedGINode ),
                typeof(ReflectionProbeNode )
            };

            public static NodeTypeCollection Matrix = new NodeTypeCollection()
            {
                typeof(Matrix2Node ),
                typeof(Matrix3Node ),
                typeof(Matrix4Node ),
                typeof(TransformationMatrixNode )
            };

            public static NodeTypeCollection PBR = new NodeTypeCollection()
            {
                typeof(DielectricSpecularNode ),
                typeof(MetalReflectanceNode )
            };

            public static NodeTypeCollection Scene = new NodeTypeCollection()
            {
                typeof(CameraNode ),
                typeof(FogNode ),
                typeof(ObjectNode ),
                typeof(SceneColorNode ),
                typeof(SceneDepthNode ),
                typeof(ScreenNode )
            };

            public static NodeTypeCollection Texture = new NodeTypeCollection()
            {
                typeof(CubemapAssetNode ),
                typeof(SampleCubemapNode ),
                typeof(SampleTexture2DArrayNode ),
                typeof(SampleTexture2DLODNode ),
                typeof(SampleTexture2DNode ),
                typeof(SampleTexture3DNode ),
                typeof(SamplerStateNode ),
                typeof(Texture2DPropertiesNode ),
                typeof(Texture2DArrayAssetNode ),
                typeof(Texture2DAssetNode ),
                typeof(Texture3DAssetNode )
            };

            public static NodeTypeCollection All = new NodeTypeCollection()
            {
                Basic,
                Geometry,
                Gradient,
                Lighting,
                Matrix,
                PBR,
                Scene,
                Texture,
                typeof(PropertyNode)
            };
        }

        public static class Math
        {
            public static NodeTypeCollection Advanced = new NodeTypeCollection()
            {
                typeof(AbsoluteNode ),
                typeof(ExponentialNode ),
                typeof(LengthNode ),
                typeof(LogNode ),
                typeof(ModuloNode ),
                typeof(NegateNode ),
                typeof(NormalizeNode ),
                typeof(PosterizeNode ),
                typeof(ReciprocalNode ),
                typeof(ReciprocalSquareRootNode )
            };

            public static NodeTypeCollection Basic = new NodeTypeCollection()
            {
                typeof(AddNode ),
                typeof(DivideNode ),
                typeof(MultiplyNode ),
                typeof(PowerNode ),
                typeof(SquareRootNode ),
                typeof(SubtractNode )
            };

            public static NodeTypeCollection Derivative = new NodeTypeCollection()
            {
                typeof(DDXNode ),
                typeof(DDXYNode ),
                typeof(DDYNode )
            };

            public static NodeTypeCollection Interpolation = new NodeTypeCollection()
            {
                typeof(InverseLerpNode ),
                typeof(LerpNode ),
                typeof(SmoothstepNode )
            };

            public static NodeTypeCollection Matrix = new NodeTypeCollection()
            {
                typeof(MatrixConstructionNode ),
                typeof(MatrixDeterminantNode ),
                typeof(MatrixSplitNode ),
                typeof(MatrixTransposeNode )
            };

            public static NodeTypeCollection Range = new NodeTypeCollection()
            {
                typeof(ClampNode ),
                typeof(FractionNode ),
                typeof(MaximumNode ),
                typeof(MinimumNode ),
                typeof(OneMinusNode ),
                typeof(RandomRangeNode ),
                typeof(RemapNode ),
                typeof(SaturateNode )
            };

            public static NodeTypeCollection Round = new NodeTypeCollection()
            {
                typeof(CeilingNode ),
                typeof(FloorNode ),
                typeof(RoundNode ),
                typeof(SignNode ),
                typeof(StepNode ),
                typeof(TruncateNode )
            };

            public static NodeTypeCollection Trigonometry = new NodeTypeCollection()
            {
                typeof(ArccosineNode ),
                typeof(ArcsineNode ),
                typeof(Arctangent2Node ),
                typeof(ArctangentNode ),
                typeof(CosineNode ),
                typeof(DegreesToRadiansNode ),
                typeof(HyperbolicCosineNode ),
                typeof(HyperbolicSineNode ),
                typeof(HyperbolicTangentNode ),
                typeof(RadiansToDegreesNode ),
                typeof(SineNode ),
                typeof(TangentNode )
            };

            public static NodeTypeCollection Vector = new NodeTypeCollection()
            {
                typeof(CrossProductNode ),
                typeof(DistanceNode ),
                typeof(DotProductNode ),
                typeof(FresnelNode ),
                typeof(ProjectionNode ),
                typeof(ReflectionNode ),
                typeof(RejectionNode ),
                typeof(RotateAboutAxisNode ),
                typeof(SphereMaskNode ),
                typeof(TransformNode )
            };

            public static NodeTypeCollection Wave = new NodeTypeCollection()
            {
                typeof(NoiseSineWaveNode ),
                typeof(SawtoothWaveNode ),
                typeof(SquareWaveNode ),
                typeof(TriangleWaveNode )
            };

            public static NodeTypeCollection All = new NodeTypeCollection()
            {
                Advanced,
                Basic,
                Derivative,
                Interpolation,
                Matrix,
                Range,
                Round,
                Trigonometry,
                Vector,
                Wave
            };
        }
        public static class Procedural
        {
            public static NodeTypeCollection Noise = new NodeTypeCollection()
            {
                typeof(GradientNoiseNode ),
                typeof(NoiseNode ),
                typeof(VoronoiNode )
            };

            public static NodeTypeCollection Shape = new NodeTypeCollection()
            {
                typeof(EllipseNode ),
                typeof(PolygonNode ),
                typeof(RectangleNode ),
                typeof(RoundedPolygonNode ),
                typeof(RoundedRectangleNode )
            };

            public static NodeTypeCollection All = new NodeTypeCollection()
            {
                Noise,
                Shape,
                typeof(CheckerboardNode)
            };
        }

        public static NodeTypeCollection UV = new NodeTypeCollection()
        {
            typeof(FlipbookNode ),
            typeof(PolarCoordinatesNode ),
            typeof(RadialShearNode ),
            typeof(RotateNode ),
            typeof(SpherizeNode ),
            typeof(TilingAndOffsetNode ),
            typeof(TriplanarNode ),
            typeof(TwirlNode )
        };

        public static class Utility
        {
            public static NodeTypeCollection Logic = new NodeTypeCollection()
            {
                typeof(AllNode ),
                typeof(AndNode ),
                typeof(AnyNode ),
                typeof(BranchNode ),
                typeof(ComparisonNode ),
                typeof(IsFrontFaceNode ),
                typeof(IsInfiniteNode ),
                typeof(IsNanNode ),
                typeof(NandNode ),
                typeof(NotNode ),
                typeof(OrNode )
            };

            public static NodeTypeCollection Misc = new NodeTypeCollection()
            {
                typeof(CustomFunctionNode),
                typeof(KeywordNode),
                typeof(PreviewNode),
                typeof(SubGraphNode)
            };

            public static NodeTypeCollection All = new NodeTypeCollection()
            {
                Logic,
                Misc
            };
        }

        public static NodeTypeCollection AllBuiltin = new NodeTypeCollection()
        {
            Artistic.All,
            Channel,
            Input.All,
            Math.All,
            Procedural.All,
            UV,
            Utility.All,
            typeof(GeometryNode),
            typeof(SubGraphOutputNode)
        };
        //Vertex Skinning 	LinearBlendSkinningNode 
    }
}
