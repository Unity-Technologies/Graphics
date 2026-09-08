using UnityEngine;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalBlockFields
    {
        [GenerateBlocks("Universal Render Pipeline")]
        public struct VertexDescription
        {
            public static string name = "VertexDescription";
            public static BlockFieldDescriptor MotionVector = new BlockFieldDescriptor(VertexDescription.name, "MotionVector", "Motion Vector", "VERTEXDESCRIPTION_MOTIONVECTOR",
                new Vector3Control(new Vector3(0.0f, 0.0f, 0.0f)), ShaderStage.Vertex);

            // The offset a caster point is thrown by, for the ShadowCaster2D SubTarget. Direction and
            // magnitude together, so the projection is no longer required to be radial from the light:
            // a constant displacement gives parallel (directional) shadows, and a varying one gives
            // shear, lean and per-point skew that a scalar distance cannot express.
            //
            // This REPLACED a scalar Shadow Distance, whose comment argued that a scalar "expresses
            // every projection worth having while making the direction invariant unbreakable". The
            // first half was simply wrong -- parallel projection is not expressible as a distance from
            // a point. The second half was right about the risk but wrong about the remedy: what the
            // construction actually needs is that the throw be the same function of a point regardless
            // of which vertex is asking, and a displacement satisfies that exactly as a distance did.
            // See SoftShadowProjectVertex.hlsl, which now carries no light position at all.
            //
            // Vector2, not Vector3: the projection is strictly planar -- SoftShadowVert works in 2D and
            // SoftShadowToHClip pins z to 0 -- so a Z component would be silently discarded. Fake-3D
            // height math still works; it just has to resolve to a 2D offset inside the graph, which a
            // projection built on SoftShadowLightLocal / SoftShadowPointToLocal does.
            //
            // Read in the CASTER'S OWN FRAME, not in world. That frame reaches world through an
            // isometry, so this is a world-LENGTH offset along the sprite's local axes: (0, -2) means
            // "two units along this sprite's own down". World would be the more obvious choice and it is
            // the wrong one -- a graph literal does not rotate with the scene while the caster's inverse
            // matrix does, so rotating an entire scene as a rigid body would swing every shadow by the
            // scene's own rotation. See ShadowCaster2DPass.hlsl for the argument in full. A graph built
            // from world quantities converts with a Transform (World -> Object) node, or sidesteps the
            // question by working from SoftShadowLightLocal / SoftShadowPointToLocal, which report the
            // light and the point in the caster's frame already.
            //
            // Default is a downward throw rather than anything radial: an unconnected port is a
            // CONSTANT, and "2 units along this point's own ray from the light" is not a constant. This
            // is a deliberate behaviour change from the scalar port's 2.0 default.
            public static BlockFieldDescriptor ShadowDisplacement = new BlockFieldDescriptor(VertexDescription.name, "ShadowDisplacement", "Shadow Displacement", "VERTEXDESCRIPTION_SHADOWDISPLACEMENT",
                new Vector2Control(new Vector2(0.0f, -2.0f)), ShaderStage.Vertex);

            // Per-point penumbra width, split by which boundary of the shadow it widens, and expressed
            // as a SCALE on the light's own Shadow Softness rather than an absolute angle.
            //
            // Multipliers and not angles, for the same reason Shadow Displacement's default caused a
            // regression: an unconnected port is a constant, and a constant angle would silently
            // override the light and take the artist-facing Shadow Softness slider out of the loop. As
            // scales, unwired ports reproduce today's behaviour exactly and the slider keeps working. The
            // names say "Softness" rather than "Softness Scale" because that is what they are called on
            // the master node; they are multipliers, and 1 means "whatever the light says".
            //
            // Saturated to [0, 1] where they are read (ShadowCaster2DPass.hlsl): 0 is a hard edge, 1 is
            // the light's own softness, and there is nothing above.
            //
            // Not an arbitrary bound. The product is clamped to _SoftShadowMaxAngle downstream, so a
            // scale went inert at 1 / shadowSoftness -- a ceiling that moved per light and was invisible
            // to the graph, which made an authored 8 mean "8x" on one light and "no change at all" on
            // another. A fraction of the light's setting means the same thing on every light.
            //
            // Widening past the light is not lost capability so much as capability that was never
            // deliverable: 15 degrees is both the validated envelope and the fan resolution baked into
            // the mesh. Raise that ceiling and this clamp is the thing to revisit.
            //
            // Both carry the displacement's contract: a function of the point and uniforms only. Noise
            // seeded on anything but position makes the per-point evaluations disagree about a shared
            // vertex and tears the geometry.
            //
            // Products are clamped in the shader to [0, k_MaxShadowSoftnessAngle] -- see
            // _SoftShadowMaxAngle in SoftShadowProjectVertex.hlsl for why the ceiling is real.

            // The fin: the exterior half-wedge at a silhouette pole, which forms the shadow's lateral
            // edge. Evaluated at ONE of the five points, since the fin is that vertex's own geometry.
            public static BlockFieldDescriptor ShadowSideSoftness = new BlockFieldDescriptor(VertexDescription.name, "ShadowSideSoftness", "Side Softness", "VERTEXDESCRIPTION_SHADOWSIDESOFTNESS",
                new FloatControl(1.0f), ShaderStage.Vertex);

            // The band: the offset along each away-facing edge, which projected forms the shadow's far
            // boundary, plus the convex fan, the concave fillet and the join fan's target radius.
            // Evaluated at THREE of the five, because the fillet budget measures a neighbour's width
            // against the line that neighbour will itself sit on.
            //
            // Driving this away from Side Softness is a supported decoupling but not a free one: it puts
            // a step between the fin's reach and the band's offset at every pole, which the join fan
            // bridges by lerping radius. That bridge is resolved by the caster's baked fan segment
            // count, so a large ratio on a coarsely fanned caster reads as faceted.
            public static BlockFieldDescriptor ShadowBackSoftness = new BlockFieldDescriptor(VertexDescription.name, "ShadowBackSoftness", "Back Softness", "VERTEXDESCRIPTION_SHADOWBACKSOFTNESS",
                new FloatControl(1.0f), ShaderStage.Vertex);
        }

        [GenerateBlocks("Universal Render Pipeline")]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor SpriteMask = new BlockFieldDescriptor(SurfaceDescription.name, "SpriteMask", "Sprite Mask", "SURFACEDESCRIPTION_SPRITEMASK",
                new ColorRGBAControl(new Color(1, 1, 1, 1)), ShaderStage.Fragment);

            public static BlockFieldDescriptor NormalAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "NormalAlpha", "Normal Alpha", "SURFACEDESCRIPTION_NORMALALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor MAOSAlpha = new BlockFieldDescriptor(SurfaceDescription.name, "MAOSAlpha", "MAOS Alpha", "SURFACEDESCRIPTION_MAOSALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);

            // Light2D master node's fragment Color port. Distinct from BlockFields.SurfaceDescription.BaseColor
            // so GraphData.DeserializeContextData (matches by tag+name) always resolves to
            // this exact descriptor instance — that reference-stability is what makes the
            // Generator.cs:647 block lookup succeed and the user's authored slot value
            // (rather than a temp inline block's descriptor default) drive codegen.
            //
            // White default is the natural resting state for a light: multiplied by
            // _L2D_COLOR in Light2DPass.hlsl, a defaulted graph emits the authored light
            // color at full intensity.
            public static BlockFieldDescriptor Light2DColor = new BlockFieldDescriptor(SurfaceDescription.name, "Light2DColor", "Color", "SURFACEDESCRIPTION_LIGHT2DCOLOR",
                new ColorControl(Color.white, false), ShaderStage.Fragment);

            // Shadow color for the Light2D non-volumetric pass. Blended in at the shadow
            // step: where the pipeline shadow attenuates the light, this color is mixed in
            // proportional to (1 - appliedFactor), so it appears only in shadowed regions
            // and scales with shadow intensity. Black default = pure darkening, matching
            // legacy behavior when the port is left unwired.
            //
            // Only active for the base (pipeline-shadow) SubTarget — the Custom Shadows
            // variant skips pipeline shadow blending and expects the graph to handle its
            // own shadow color, so exposing this port there would be a dead input.
            public static BlockFieldDescriptor Light2DShadowColor = new BlockFieldDescriptor(SurfaceDescription.name, "Light2DShadowColor", "Shadow Color", "SURFACEDESCRIPTION_LIGHT2DSHADOWCOLOR",
                new ColorControl(Color.black, false), ShaderStage.Fragment);

            // Light2D volumetric-pass counterpart of Light2DColor. Distinct
            // BlockFieldDescriptor instance so the volumetric pass mask can select this
            // (and only this) while the non-volumetric pass mask selects the original —
            // that per-pass mask split is what routes each Color node into the
            // correct SurfaceDescription variant without a runtime branch.
            public static BlockFieldDescriptor Light2DVolumetricColor = new BlockFieldDescriptor(SurfaceDescription.name, "Light2DVolumetricColor", "Volumetric Color", "SURFACEDESCRIPTION_LIGHT2DVOLUMETRICCOLOR",
                new ColorControl(Color.white, false), ShaderStage.Fragment);
        }
    }
}
