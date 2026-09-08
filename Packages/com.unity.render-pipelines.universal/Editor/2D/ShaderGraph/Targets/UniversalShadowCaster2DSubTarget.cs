using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using Unity.Rendering.Universal;   // ShaderUtils.ShaderID lives here, not in UnityEditor.Rendering.Universal

// Deliberately NOT `using UnityEngine.Rendering.Universal` -- that namespace carries a ShaderUtils type
// of its own, which would make the unqualified `ShaderUtils.ShaderID` below ambiguous. The three runtime
// types this file needs are aliased instead.
using ShadowRendering = UnityEngine.Rendering.Universal.ShadowRendering;
using Shadow2DGeneratorTag = UnityEngine.Rendering.Universal.Shadow2DGeneratorTag;
using SoftShadowGeometryGenerator = UnityEngine.Rendering.Universal.SoftShadowGeometryGenerator;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    /// <summary>
    /// Generates a five-pass 2D shadow shader for <c>ShadowCaster2D.material</c>, reading the
    /// <c>Unity.SoftShadow</c> geometry generator's 14-float payload.
    ///
    /// Two properties of URP's 2D shadow path drive the whole shape of this SubTarget:
    ///
    /// - URP resolves a shadow pass by NAME, per phase of a four-phase stencil handshake, and
    ///   <c>ShadowCastingOptions</c> is mutable at runtime. So all five roles are emitted
    ///   unconditionally; a material validated against the current option would break silently the
    ///   moment that option changed, with no inspector in a player to say so.
    /// - The three caster passes read POSITION alone while the two projected passes read the full
    ///   payload, so this shader carries two different vertex layouts. ShaderGraph supports that
    ///   natively: each pass emits its own <c>Attributes</c> struct inside its own HLSLPROGRAM, and
    ///   the per-pass <c>requiredFields</c> decides which optional members appear.
    /// </summary>
    sealed class UniversalShadowCaster2DSubTarget : UniversalSubTarget
    {
        static readonly UnityEngine.GUID kSourceCodeGuid = new UnityEngine.GUID("3f6c1d94a7b25e4488c0d3f21a95be07"); // UniversalShadowCaster2DSubTarget.cs

        public UniversalShadowCaster2DSubTarget()
        {
            displayName = "Shadow Caster 2D";
        }

        protected override ShaderUtils.ShaderID shaderID => ShaderUtils.ShaderID.SG_ShadowCaster2D;

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
            base.Setup(ref context);
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
            context.AddSubShader(PostProcessSubShader(SubShaders.ShadowCaster2D(target)));
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            context.AddField(UniversalFields.SurfaceTransparent);

            // Fields.GraphVertex needs no special handling here: UniversalTarget.GetFields sets it from
            // whether Position is among the active blocks, and GetActiveBlocks registers Position -- it
            // has to, because Varyings.hlsl dereferences vertexDescription.Position unguarded and HLSL
            // type-checks that function body in every pass that includes it, called or not.
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            // One port: an opacity on the VISIBLE shadow. Caster coverage is reproduced from
            // Hidden/Shadow2D rather than authored, so the graph contributes only this.
            //
            // It scales R (Self, ProjectedSelf) and G (ProjectedUnshadow) but never B
            // (UnshadowUnmark), because the light composites max(R, G*(1-B)) and that assignment makes
            // the result exactly linear in Alpha: max(R*a, G*a*(1-B)) == a * max(R, G*(1-B)). Scaling B
            // would make a fading shadow darker inside the caster's own footprint.
            //
            // Normal, Tangent and BaseColor are excluded -- UniversalTarget.GetActiveBlocks would
            // otherwise prepend all three plus Position, so this SubTarget is in its useCoreBlocks
            // exclusion alongside Fullscreen / Canvas / UITK. Removing the Normal and Tangent PORTS is
            // still not sufficient on its own, because the nodes remain in the graph; see
            // IsNodeAllowedBySubTarget.
            //
            // Position is REQUIRED, not optional. Setting Fields.GraphVertex -- which the projected
            // passes need in order to evaluate Shadow Displacement at all -- makes Varyings.hlsl compile
            // `input.positionOS = vertexDescription.Position;` unguarded, and HLSL type-checks that
            // function body even in passes that never call it. So the moment there is a vertex graph,
            // VertexDescription must carry a Position member. The projected passes honour it explicitly
            // (see ShadowCaster2DPass.hlsl); the caster passes get it through BuildVaryings.
            context.AddBlock(BlockFields.VertexDescription.Position);
            context.AddBlock(UniversalBlockFields.VertexDescription.ShadowDisplacement);
            context.AddBlock(UniversalBlockFields.VertexDescription.ShadowSideSoftness);
            context.AddBlock(UniversalBlockFields.VertexDescription.ShadowBackSoftness);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);

        }

        // Nothing on the Target-level surface applies: blend, ColorMask and stencil are the stencil
        // handshake and are fixed per pass, the shader is never depth-tested or culled, and it is only
        // ever drawn by ShadowRendering.
        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo) { }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            // No _ShadowLength property. The Shadow Displacement PORT owns the projection throw now, and a
            // material slider for the same quantity would be a second place to set it -- with the port
            // always winning, so the slider would appear inert. An author who wants it material-tweakable
            // exposes a Float on the blackboard and wires it into the port, which is the ShaderGraph way
            // and actually works. The port's default matches this property's old default, so nothing
            // about a freshly created graph's output changed.
            //
            // _FilletRadiusScale stays: it is not a port, so there is nothing for it to conflict with.
            // It is declared by SoftShadowProjectVertex.hlsl, so this entry exists only to put it in the
            // Properties block with the fixture's default -- hence DoNotDeclare, the same pattern
            // CanvasProperties uses for _StencilComp. Without a Properties entry it would default to
            // zero, which silently disables the concave fillet.
            collector.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = ShaderPropertyNames.k_FilletRadiusScale,
                displayName = "Fillet Radius (x band width)",
                floatType = FloatType.Slider,
                rangeValues = new UnityEngine.Vector2(0.0f, 3.0f),
                value = 1.0f,
                generatePropertyBlock = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
            });

            // The sprite caster's texture and tint. Coverage is reproduced from Hidden/Shadow2D, so
            // ShadowCaster2DGlobals.hlsl declares these itself -- but they still need Properties-block
            // entries, because the sprite caster draw is cmdBuffer.DrawRenderer against the
            // SpriteRenderer and the sprite texture arrives through the renderer's binding of _MainTex.
            // Without a Properties entry there is nothing for it to bind to.
            collector.AddShaderProperty(new Texture2DShaderProperty()
            {
                overrideReferenceName = ShaderPropertyNames.k_MainTex,
                displayName = "Texture",
                defaultType = Texture2DShaderProperty.DefaultType.White,
                value = new SerializableTexture(),
                generatePropertyBlock = true,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
            });

            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = ShaderPropertyNames.k_Color,
                displayName = "Tint",
                value = UnityEngine.Color.white,
                generatePropertyBlock = true,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
            });
        }

        /// <summary>
        /// Nodes that would pull <c>normalOS</c> or <c>tangentOS</c> into a pass's Attributes struct
        /// are rejected outright. On the caster passes those semantics are merely absent from the mesh
        /// and reading them is meaningless; on the projected passes TANGENT carries
        /// <c>(role, fanParam, prev.xy)</c>, so reading it as a tangent produces a plausible-looking
        /// wrong shadow -- the same silent failure class the Shadow2DGenerators tag exists to prevent.
        ///
        /// Denied by explicit TYPE, deliberately, as CanvasSubTarget does for the same problem.
        ///
        /// Filtering by requirement interface -- the FullscreenSubTarget approach -- cannot work here.
        /// CodeFunctionNode itself implements IMayRequireNormal, IMayRequireTangent and
        /// IMayRequireBitangent to service its Binding slots, Type.GetInterfaces() reports inherited
        /// interfaces, and 110 node types derive from CodeFunctionNode. An interface filter therefore
        /// rejects Add, Lerp, Sine, Length, Sample Texture 2D and most of the node library -- including
        /// this SubTarget's own two nodes. It was written that way and did exactly that.
        ///
        /// A hand-maintained list does not catch a geometry node added to ShaderGraph later, which is a
        /// real cost. ValidateNodeCompatibility covers the breadth instead: it inspects a subgraph's
        /// COMPUTED requirements rather than its type's interfaces, so it reports what a graph actually
        /// asks for without over-blocking.
        /// </summary>
        public override bool IsNodeAllowedBySubTarget(Type nodeType)
        {
            if (nodeType == typeof(NormalVectorNode) ||
                nodeType == typeof(TangentVectorNode) ||
                nodeType == typeof(BitangentVectorNode))
                return false;

            return base.IsNodeAllowedBySubTarget(nodeType);
        }

        /// <summary>
        /// The second half of the normal/tangent denial, covering what the type filter cannot see.
        ///
        /// <see cref="IsNodeAllowedBySubTarget"/> has to let <c>SubGraphNode</c> through, because a
        /// subgraph inherits every requirement interface and would otherwise be denied outright. That
        /// leaves a hole: a Normal Vector or Tangent Vector node hidden inside a subgraph reaches
        /// codegen and pulls <c>normalOS</c> / <c>tangentOS</c> into the pass's Attributes struct, with
        /// the same consequence as using it directly.
        ///
        /// Checking the subgraph's aggregate <c>requirements</c> is what closes it -- cheaper and more
        /// robust than walking its nodes, since ShaderGraph has already computed exactly this.
        /// </summary>
        public override bool ValidateNodeCompatibility(AbstractMaterialNode node, out string errorMessage,
                                                      out ShaderCompilerMessageSeverity severity)
        {
            errorMessage = null;
            severity = ShaderCompilerMessageSeverity.Error;

            if (node is not SubGraphNode subGraphNode)
                return false;

            var asset = subGraphNode.asset;
            if (asset == null)
                return false;

            var requirements = asset.requirements;
            bool needsNormal = requirements.requiresNormal != NeededCoordinateSpace.None;
            bool needsTangent = requirements.requiresTangent != NeededCoordinateSpace.None;
            bool needsBitangent = requirements.requiresBitangent != NeededCoordinateSpace.None;

            if (!needsNormal && !needsTangent && !needsBitangent)
                return false;

            var wanted = new System.Collections.Generic.List<string>();
            if (needsNormal) wanted.Add("normals");
            if (needsTangent) wanted.Add("tangents");
            if (needsBitangent) wanted.Add("bitangents");

            errorMessage =
                $"Sub Graph \"{asset.name}\" requires {string.Join(" and ", wanted)}, which a 2D shadow " +
                "shader cannot supply. The caster passes carry neither semantic, and on the projected " +
                "passes TANGENT is the geometry generator's payload (role, fanParam, prev.xy) rather " +
                "than a tangent -- reading it produces a plausible-looking wrong shadow rather than an " +
                "error.";
            return true;
        }

        #region PropertyNames
        internal static class ShaderPropertyNames
        {
            internal const string k_FilletRadiusScale = "_FilletRadiusScale";
            internal const string k_MainTex = "_MainTex";
            internal const string k_Color = "_Color";
        }
        #endregion

        #region SubShader
        static class SubShaders
        {
            // Built from the generator's own id constant rather than a literal. The URP Editor assembly
            // can see the 2D runtime's internals (Runtime/2D/AssemblyInfo.cs), and a literal here would
            // be a second place for the id to drift -- which the tag handoff's drift guard exists to
            // stop. Emitting NO tag would declare Unity.Legacy, the one layout these passes cannot
            // read, and the assignment switch would then actively move casters onto it.
            internal static string generatorTag =>
                $"\"{Shadow2DGeneratorTag.k_TagName}\" = \"{SoftShadowGeometryGenerator.k_Id}\"";

            public static SubShaderDescriptor ShadowCaster2D(UniversalTarget target)
            {
                return new SubShaderDescriptor()
                {
                    pipelineTag = UniversalTarget.kPipelineTag,
                    customTags = generatorTag,
                    renderType = $"{RenderType.Transparent}",
                    renderQueue = $"{UnityEditor.ShaderGraph.RenderQueue.Transparent}",
                    generatesPreview = true,
                    passes = new PassCollection
                    {
                        // Order mirrors Hidden/Shadow2D. URP draws by resolved pass index rather than
                        // by position, so this is for readability against the built-in shader, not
                        // correctness -- but keeping them aligned makes a diff of the two legible.
                        { ShadowCaster2DPasses.Self(target) },
                        { ShadowCaster2DPasses.UnshadowMark(target) },
                        { ShadowCaster2DPasses.UnshadowUnmark(target) },
                        { ShadowCaster2DPasses.ProjectedSelf(target) },
                        { ShadowCaster2DPasses.ProjectedUnshadow(target) },
                    },
                };
            }
        }
        #endregion

        #region Passes
        static class ShadowCaster2DPasses
        {
            // Per-pass compile-time markers. Predefined + Local so they are a plain #define in one
            // pass's own program rather than a multi_compile that variants all five -- the pattern
            // UniversalLight2DSubTarget uses for LIGHT2D_VOLUMETRIC_PASS.
            static readonly KeywordDescriptor k_ProjectedPassDefine = new KeywordDescriptor()
            {
                displayName = "ShadowCaster2D Projected Pass",
                referenceName = "SHADOWCASTER2D_PROJECTED_PASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            static readonly KeywordDescriptor k_AlphaClipPassDefine = new KeywordDescriptor()
            {
                displayName = "ShadowCaster2D Alpha Clip Pass",
                referenceName = "SHADOWCASTER2D_ALPHA_CLIP_PASS",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Local,
            };

            // displayName becomes the ShaderLab `Name` (Generator.cs: `Name "{pass.displayName}"`),
            // which is what URP's FindPass resolves against -- so these MUST be the C# constants and
            // not retyped strings. A rename not mirrored in C# resolves to -1 and silently stops a
            // phase drawing; nothing else catches it.
            //
            // referenceName becomes `#define SHADERPASS <value>`. SHADERPASS_SPRITEUNLIT is used for
            // all five deliberately: if SHADERPASS is left undefined the preprocessor treats it as 0,
            // which is SHADERPASS_FORWARD, and that triggers the lightmap/SH block in Varyings.hlsl
            // referencing an `output.sh` these Varyings do not carry.
            static PassDescriptor CasterPass(string passName, RenderStateCollection renderStates,
                                             bool alphaClip, bool useInPreview = false)
            {
                var defines = new DefineCollection();
                if (alphaClip)
                    defines.Add(k_AlphaClipPassDefine, 1);

                return new PassDescriptor()
                {
                    // Definition
                    displayName = passName,
                    referenceName = "SHADERPASS_SPRITEUNLIT",
                    useInPreview = useInPreview,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = ShadowCaster2DBlockMasks.Vertex,
                    validPixelBlocks = ShadowCaster2DBlockMasks.Fragment,

                    // Fields -- the sprite caster inputs. No payload attributes, so this layout stays
                    // distinct from the projected passes'.
                    structs = CoreStructCollections.Default,
                    requiredFields = ShadowCaster2DRequiredFields.Caster,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = renderStates,
                    pragmas = CorePragmas._2DDefault,
                    defines = defines,
                    keywords = ShadowCaster2DKeywords.Caster,
                    includes = ShadowCaster2DIncludes.ShadowCaster2D,

                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
            }

            static PassDescriptor ProjectedPass(string passName, RenderStateCollection renderStates)
            {
                return new PassDescriptor()
                {
                    // Definition
                    displayName = passName,
                    referenceName = "SHADERPASS_SPRITEUNLIT",
                    useInPreview = false,

                    // Template
                    passTemplatePath = UniversalTarget.kUberTemplatePath,
                    sharedTemplateDirectories = UniversalTarget.kSharedTemplateDirectories,

                    // Port Mask
                    validVertexBlocks = ShadowCaster2DBlockMasks.Vertex,
                    validPixelBlocks = ShadowCaster2DBlockMasks.Fragment,

                    // Fields -- the fat payload layout.
                    structs = CoreStructCollections.Default,
                    requiredFields = ShadowCaster2DRequiredFields.Projected,
                    fieldDependencies = CoreFieldDependencies.Default,

                    // Conditional State
                    renderStates = renderStates,
                    pragmas = CorePragmas._2DDefault,
                    defines = new DefineCollection { { k_ProjectedPassDefine, 1 } },
                    keywords = ShadowCaster2DKeywords.Projected,
                    includes = ShadowCaster2DIncludes.ShadowCaster2D,

                    customInterpolators = CoreCustomInterpDescriptors.Common
                };
            }

            // Self is the pass the master preview renders through, and EXACTLY ONE pass has to be.
            //
            // Generator.cs skips every pass whose useInPreview is false when generating the preview, so
            // with none marked the SubShader comes out with no Pass at all -- which is invalid ShaderLab
            // and fails with "syntax error, unexpected '}'" on the SubShader's closing brace. The master
            // preview is generated from THIS SubShader, not from PreviewTarget; PreviewTarget serves
            // node previews.
            //
            // Self is the right one. It uses the standard BuildVaryings vertex path, so it works on a
            // stock preview mesh, and with SHADOW_SPRITE_CASTER off its coverage is the constant 1 --
            // leaving the fragment as just the Alpha port, which is a meaningful thing to preview. A
            // projected pass would need the 14-float payload a preview mesh does not have, and would
            // decode garbage.
            public static PassDescriptor Self(UniversalTarget target)
                => CasterPass(ShadowRendering.k_SelfPassName,
                              ShadowCaster2DRenderStates.Self, alphaClip: false, useInPreview: true);

            public static PassDescriptor UnshadowMark(UniversalTarget target)
                => CasterPass(ShadowRendering.k_UnshadowMarkPassName,
                              ShadowCaster2DRenderStates.UnshadowMark, alphaClip: true);

            public static PassDescriptor UnshadowUnmark(UniversalTarget target)
                => CasterPass(ShadowRendering.k_UnshadowUnmarkPassName,
                              ShadowCaster2DRenderStates.UnshadowUnmark, alphaClip: true);

            public static PassDescriptor ProjectedSelf(UniversalTarget target)
                => ProjectedPass(ShadowRendering.k_ProjectedSelfPassName,
                                 ShadowCaster2DRenderStates.ProjectedSelf);

            public static PassDescriptor ProjectedUnshadow(UniversalTarget target)
                => ProjectedPass(ShadowRendering.k_ProjectedUnshadowPassName,
                                 ShadowCaster2DRenderStates.ProjectedUnshadow);
        }
        #endregion

        #region PortMasks
        static class ShadowCaster2DBlockMasks
        {
            // Shared by all five passes even though only the projected two consume Shadow Displacement.
            // The caster passes evaluate it and discard the result, which the compiler eliminates --
            // whereas a pass whose vertex mask is empty while FEATURES_GRAPH_VERTEX is set would
            // generate a VertexDescription struct with no members.
            public static readonly BlockFieldDescriptor[] Vertex = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
                UniversalBlockFields.VertexDescription.ShadowDisplacement,
                UniversalBlockFields.VertexDescription.ShadowSideSoftness,
                UniversalBlockFields.VertexDescription.ShadowBackSoftness,
            };

            public static readonly BlockFieldDescriptor[] Fragment = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
            };
        }
        #endregion

        #region RequiredFields
        static class ShadowCaster2DRequiredFields
        {
            // Opts the projected passes in to the optional Attributes members that carry the
            // Unity.SoftShadow payload. The caster passes list none, so their Attributes is positionOS
            // alone -- two layouts in one generated shader, which is legal because each pass emits its
            // own struct inside its own HLSLPROGRAM.
            //
            // texCoord3 is the interpolator the shading value travels in, written directly by the vertex
            // program rather than through the graph. Deliberately not texCoord0: on these passes
            // Attributes.uv0 is TEXCOORD0, which the SoftShadow generator leaves free, so UV0 reads as
            // a harmless zero. Putting the penumbra encoding there would make a UV-driven node in the
            // graph silently sample the wedge coordinate instead.
            public static readonly FieldCollection Projected = new FieldCollection()
            {
                StructFields.Attributes.tangentOS,   // TANGENT   -> role, fanParam, prev.xy
                StructFields.Attributes.uv1,         // TEXCOORD1 -> next.xy, windingSign
                StructFields.Attributes.uv2,         // TEXCOORD2 -> prev2.xy, next2.xy
                StructFields.Varyings.texCoord3,
            };

            // The sprite caster variant samples _MainTex at the sprite UV and tints by _Color.a times
            // the vertex colour, matching Shadow2DCasterVertex.hlsl. Requested unconditionally rather
            // than under the keyword because requiredFields is static per pass -- on the geometry
            // caster draw the mesh carries neither semantic and both read zero, which is harmless
            // since that variant's coverage is the constant 1.
            public static readonly FieldCollection Caster = new FieldCollection()
            {
                StructFields.Attributes.uv0,
                StructFields.Attributes.color,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.color,
            };
        }
        #endregion

        #region RenderStates
        // Reproduces Hidden/Shadow2D pass for pass. URP does not set any of this from script, so
        // getting it wrong produces a wrong-looking shadow rather than a missing one -- which is much
        // harder to notice. Two API notes: RenderState.ColorMask emits its argument verbatim (unlike
        // every sibling helper, which prepends its keyword), and the typed Blend overloads are
        // single-target, so the projected passes' two-target form needs the raw-string overload.
        static class ShadowCaster2DRenderStates
        {
            // Cull Off / ZWrite Off / ZTest Always sit at SubShader scope in the hand-written shader.
            // ShaderGraph only emits render state per pass, so every collection repeats them.
            static void AddCommon(RenderStateCollection states)
            {
                states.Add(RenderState.Cull(Cull.Off));
                states.Add(RenderState.ZWrite(ZWrite.Off));
                states.Add(RenderState.ZTest(ZTest.Always));
            }

            static RenderStateCollection Build(params RenderStateDescriptor[] specific)
            {
                var states = new RenderStateCollection();
                AddCommon(states);
                foreach (var s in specific)
                    states.Add(s);
                return states;
            }

            // Phase 2. Self-shadow coverage into R. No Stencil block, and it must keep none.
            // Single-target `Blend One One`, distinct from the projected two-target form.
            public static readonly RenderStateCollection Self = Build(
                RenderState.BlendOp(BlendOp.Max),
                RenderState.Blend(Blend.One, Blend.One),
                RenderState.ColorMask("ColorMask R"));

            // Phase 1. Stencil ref 1 where the caster covers, so ProjectedSelf (Comp NotEqual 1) is
            // excluded from the caster's own silhouette. Writes no colour.
            public static readonly RenderStateCollection UnshadowMark = Build(
                RenderState.ColorMask("ColorMask 0"),
                RenderState.Stencil(new StencilDescriptor()
                {
                    Ref = "1",
                    Comp = "Always",
                    Pass = "Replace",
                }));

            // Phase 4. Clears the stencil back to 0 and records coverage in B, which the light reads
            // as (1 - B) when compositing max(R, G * (1 - B)).
            public static readonly RenderStateCollection UnshadowUnmark = Build(
                RenderState.BlendOp(BlendOp.Add),
                RenderState.Blend(Blend.One, Blend.One),
                RenderState.ColorMask("ColorMask B"),
                RenderState.Stencil(new StencilDescriptor()
                {
                    Ref = "0",
                    Comp = "Always",
                    Pass = "Replace",
                }));

            // Phase 3. Max-blend so overlapping casters compose as "most shadowed wins".
            public static readonly RenderStateCollection ProjectedSelf = Build(
                RenderState.BlendOp(BlendOp.Max),
                RenderState.Blend("Blend One One, One One"),
                RenderState.ColorMask("ColorMask R"),
                RenderState.Stencil(new StencilDescriptor()
                {
                    Ref = "1",
                    Comp = "NotEqual",
                    Pass = "Keep",
                }));

            public static readonly RenderStateCollection ProjectedUnshadow = Build(
                RenderState.BlendOp(BlendOp.Max),
                RenderState.Blend("Blend One One, One One"),
                RenderState.ColorMask("ColorMask G"),
                RenderState.Stencil(new StencilDescriptor()
                {
                    Ref = "1",
                    Comp = "Equal",
                    Pass = "Keep",
                }));
        }
        #endregion

        #region Keywords
        static class ShadowCaster2DKeywords
        {
            // MultiCompile, not ShaderFeature: ShadowRendering records these per draw through the
            // command buffer (SetCustomCasterKeywords), and shader_feature variants would be stripped.
            // Every such write is guarded by keywordSpace.FindKeyword(...).isValid, so a keyword this
            // shader fails to declare is skipped silently rather than reported.
            static readonly KeywordDescriptor k_NotTransformable = new KeywordDescriptor()
            {
                displayName = "Not Transformable",
                referenceName = "NOT_TRANSFORMABLE",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            // Coverage is reproduced from the built-in shader, so both caster keywords are needed:
            // SHADOW_SPRITE_CASTER selects sprite alpha over the geometry variant's constant 1, and
            // SKINNED_SPRITE the 2D-Animation deformation. ShadowRendering writes both per draw.
            static readonly KeywordDescriptor k_SpriteCaster = new KeywordDescriptor()
            {
                displayName = "Shadow Sprite Caster",
                referenceName = "SHADOW_SPRITE_CASTER",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.MultiCompile,
                scope = KeywordScope.Local,
            };

            public static readonly KeywordCollection Caster = new KeywordCollection
            {
                { k_SpriteCaster },
                { CoreKeywordDescriptors.UseSkinnedSprite },
            };

            // The projected passes extrude shadowCaster.mesh, never a sprite renderer, so they carry
            // no caster keywords at all.
            public static readonly KeywordCollection Projected = new KeywordCollection
            {
                { k_NotTransformable },
            };
        }
        #endregion

        #region Includes
        static class ShadowCaster2DIncludes
        {
            const string kShadowCaster2DGlobals = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/ShadowCaster2DGlobals.hlsl";
            const string kShadowCaster2DPass = "Packages/com.unity.render-pipelines.universal/Editor/2D/ShaderGraph/Includes/ShadowCaster2DPass.hlsl";

            // One collection for all five passes; the per-pass defines select the body. Globals must
            // precede nothing in particular here, but it does have to come before anything that reads
            // SoftShadowPayload -- and it is where SOFT_SHADOW_EXTERNAL_ATTRIBUTES is defined.
            public static readonly IncludeCollection ShadowCaster2D = new IncludeCollection
            {
                // Pre-graph
                { CoreIncludes.CorePregraph },
                { CoreIncludes.ShaderGraphPregraph },
                { kShadowCaster2DGlobals, IncludeLocation.Pregraph },

                // Post-graph
                { CoreIncludes.CorePostgraph },
                { kShadowCaster2DPass, IncludeLocation.Postgraph },
            };
        }
        #endregion
    }
}
