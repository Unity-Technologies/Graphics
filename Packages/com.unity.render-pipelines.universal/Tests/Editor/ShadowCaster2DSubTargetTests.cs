using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// Guards the ShadowCaster2D SubTarget's generated SubShader against the failures that have no
    /// visible signature.
    ///
    /// Every assertion here is about something that fails SILENTLY if it drifts:
    ///
    /// - A pass name that no longer matches URP's constant resolves to -1 in Material.FindPass, and the
    ///   phase simply stops drawing. Nothing logs, nothing renders differently enough to notice.
    /// - A missing or wrong Shadow2DGenerators tag declares Unity.Legacy by default, which is the one
    ///   vertex layout these passes cannot read -- and assigning the material would then actively move
    ///   casters onto it. TANGENT decodes as a plausible in-range ProjectionType, so the result is a
    ///   wrong-looking shadow rather than a broken one.
    /// - Render state is not set from script anywhere on the shadow path. A wrong ColorMask, BlendOp or
    ///   Stencil produces a wrong-looking shadow, not a missing one.
    /// - If the caster passes ever picked up the payload attributes (or the projected passes lost them)
    ///   the two vertex layouts would silently converge on one.
    ///
    /// These are descriptor-level assertions rather than assertions over generated shader text, because
    /// the descriptors are what the generator consumes and they can be inspected without authoring a
    /// .shadergraph asset.
    /// </summary>
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class ShadowCaster2DSubTargetTests
    {
        static SubShaderDescriptor GetSubShader()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalShadowCaster2DSubTarget)),
                "UniversalShadowCaster2DSubTarget is not a registered SubTarget of UniversalTarget.");

            var context = new TargetSetupContext();
            target.Setup(ref context);

            Assert.AreEqual(1, context.subShaders.Count,
                "Exactly one SubShader is expected. Material.GetTag and Material.FindPass both see only " +
                "the ACTIVE SubShader, so a second one would carry neither the tag nor the five passes " +
                "on some platforms.");

            return context.subShaders[0];
        }

        static PassDescriptor GetPass(string passName)
        {
            var pass = GetSubShader().passes.FirstOrDefault(p => p.descriptor.displayName == passName);
            Assert.IsNotNull(pass, $"No pass named \"{passName}\" was emitted.");
            return pass.descriptor;
        }

        static string RenderState(PassDescriptor pass, RenderStateType type)
        {
            var items = pass.renderStates.Where(s => s.descriptor.type == type).ToList();
            return items.Count == 0 ? null : items.Single().value;
        }

        static readonly string[] k_AllPassNames =
        {
            ShadowRendering.k_SelfPassName,
            ShadowRendering.k_UnshadowMarkPassName,
            ShadowRendering.k_UnshadowUnmarkPassName,
            ShadowRendering.k_ProjectedSelfPassName,
            ShadowRendering.k_ProjectedUnshadowPassName,
        };

        // ------------------------------------------------------------------------------------------
        // The five-pass rule
        // ------------------------------------------------------------------------------------------

        // ShadowCastingOptions is mutable at runtime and the four options between them need all five
        // roles, so a generated shader has to declare every one regardless of what the graph does.
        [Test]
        public void EmitsExactlyTheFivePassRoles()
        {
            var names = GetSubShader().passes.Select(p => p.descriptor.displayName).ToArray();
            Assert.AreEqual(5, names.Length, "Expected five passes, got: " + string.Join(", ", names));
            CollectionAssert.AreEquivalent(k_AllPassNames, names);
        }

        // Asserted through the constants, not literals: a rename in ShadowRendering that is not
        // mirrored here should fail this test rather than silently stop a phase drawing.
        [TestCaseSource(nameof(k_AllPassNames))]
        public void EachPassRoleIsNamedByItsUrpConstant(string passName)
        {
            Assert.IsNotNull(GetPass(passName));
        }

        // A duplicated Name would let two roles collapse onto one pass index.
        [Test]
        public void PassNamesAreDistinct()
        {
            var names = GetSubShader().passes.Select(p => p.descriptor.displayName).ToList();
            CollectionAssert.AllItemsAreUnique(names);
        }

        // Exactly one pass must be preview-enabled, and both failure directions are real.
        //
        // NONE: the generator skips every pass whose useInPreview is false when building the master
        // preview, so the SubShader is emitted with no Pass -- invalid ShaderLab, failing with
        // "syntax error, unexpected '}'" on the SubShader's closing brace. That shipped, and none of the
        // other assertions here noticed, because they all inspect descriptors rather than what survives
        // the preview filter.
        //
        // MORE THAN ONE: a projected pass in the preview would decode the 14-float payload out of a
        // stock preview mesh that has no such vertex data.
        [Test]
        public void ExactlyOnePassIsPreviewEnabledAndItIsSelf()
        {
            var previewPasses = GetSubShader().passes
                .Where(p => p.descriptor.useInPreview)
                .Select(p => p.descriptor.displayName)
                .ToList();

            Assert.AreEqual(1, previewPasses.Count,
                "Expected exactly one preview pass, got: " +
                (previewPasses.Count == 0 ? "<none>" : string.Join(", ", previewPasses)));
            Assert.AreEqual(ShadowRendering.k_SelfPassName, previewPasses[0],
                "Self is the only pass that works on a stock preview mesh: standard vertex path, and " +
                "coverage collapses to 1 without the sprite keyword.");
        }

        // ------------------------------------------------------------------------------------------
        // The generator tag
        // ------------------------------------------------------------------------------------------

        [Test]
        public void DeclaresTheSoftShadowGeneratorTag()
        {
            var tags = GetSubShader().customTags;
            Assert.IsNotNull(tags, "No customTags emitted, which means no Shadow2DGenerators tag.");
            StringAssert.Contains(Shadow2DGeneratorTag.k_TagName, tags);
            StringAssert.Contains(SoftShadowGeometryGenerator.k_Id, tags);
        }

        // Legacy is what an absent tag means, and it is the layout these passes cannot read. Listing it
        // alongside SoftShadow would be worse than omitting the tag: one shader has one Attributes
        // struct, so a shader that declares two generators with different vertex layouts guarantees one
        // of them is read wrong.
        [Test]
        public void DoesNotDeclareTheLegacyGenerator()
        {
            StringAssert.DoesNotContain("Unity.Legacy", GetSubShader().customTags);
        }

        // ------------------------------------------------------------------------------------------
        // Two vertex layouts in one shader
        // ------------------------------------------------------------------------------------------

        static bool RequestsPayloadAttributes(PassDescriptor pass)
        {
            if (pass.requiredFields == null)
                return false;

            var fields = pass.requiredFields.Select(f => f.field).ToList();
            return fields.Contains(StructFields.Attributes.tangentOS)
                && fields.Contains(StructFields.Attributes.uv1)
                && fields.Contains(StructFields.Attributes.uv2);
        }

        // TANGENT / TEXCOORD1 / TEXCOORD2 carry role, fanParam and the neighbour positions. Only the
        // two projected passes decode them; the caster passes read POSITION alone and work off the
        // generator's interior fill.
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void ProjectedPassesRequestThePayloadAttributes(string passName)
        {
            Assert.IsTrue(RequestsPayloadAttributes(GetPass(passName)),
                $"{passName} must request tangentOS, uv1 and uv2 or it cannot decode the " +
                "Unity.SoftShadow payload.");
        }

        [TestCase("Self")]
        [TestCase("UnshadowMark")]
        [TestCase("UnshadowUnmark")]
        public void CasterPassesDoNotRequestThePayloadAttributes(string passName)
        {
            Assert.IsFalse(RequestsPayloadAttributes(GetPass(passName)),
                $"{passName} does not decode the payload. Pulling those attributes into its Attributes " +
                "struct would collapse the two vertex layouts onto one.");
        }

        // Coverage is reproduced from Hidden/Shadow2D, so the caster passes need the sprite inputs it
        // samples: _MainTex at the sprite UV, tinted by _Color.a * vertex colour.
        [TestCase("Self")]
        [TestCase("UnshadowMark")]
        [TestCase("UnshadowUnmark")]
        public void CasterPassesRequestTheSpriteInputs(string passName)
        {
            var fields = GetPass(passName).requiredFields.Select(f => f.field).ToList();
            CollectionAssert.Contains(fields, StructFields.Attributes.uv0);
            CollectionAssert.Contains(fields, StructFields.Attributes.color);
        }

        // The penumbra must NOT ride texCoord0. On the projected passes Attributes.uv0 is TEXCOORD0,
        // which the SoftShadow generator leaves free, so UV0 reads as a harmless zero -- but only if
        // the shading value is somewhere else. Putting it in texCoord0 would make any UV-driven node
        // in the graph silently sample the wedge coordinate instead.
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void ProjectedPassesCarryThePenumbraOffUv0(string passName)
        {
            var fields = GetPass(passName).requiredFields.Select(f => f.field).ToList();
            CollectionAssert.Contains(fields, StructFields.Varyings.texCoord3);
            CollectionAssert.DoesNotContain(fields, StructFields.Varyings.texCoord0);
        }

        // ------------------------------------------------------------------------------------------
        // Render state, pass by pass, against Hidden/Shadow2D
        // ------------------------------------------------------------------------------------------

        [TestCase("Self", "ColorMask R")]
        [TestCase("UnshadowMark", "ColorMask 0")]
        [TestCase("UnshadowUnmark", "ColorMask B")]
        [TestCase("ProjectedSelf", "ColorMask R")]
        [TestCase("ProjectedUnshadow", "ColorMask G")]
        public void ColorMaskMatchesTheBuiltInShader(string passName, string expected)
        {
            Assert.AreEqual(expected, RenderState(GetPass(passName), RenderStateType.ColorMask));
        }

        [TestCase("Self", "BlendOp Max")]
        [TestCase("UnshadowUnmark", "BlendOp Add")]
        [TestCase("ProjectedSelf", "BlendOp Max")]
        [TestCase("ProjectedUnshadow", "BlendOp Max")]
        public void BlendOpMatchesTheBuiltInShader(string passName, string expected)
        {
            Assert.AreEqual(expected, RenderState(GetPass(passName), RenderStateType.BlendOp));
        }

        // Self's single-target `Blend One One` is distinct from the projected passes' two-target
        // `Blend One One, One One`. Getting this wrong is a wrong-looking shadow, not a missing one.
        [Test]
        public void SelfUsesSingleTargetBlend()
        {
            Assert.AreEqual("Blend One One", RenderState(GetPass("Self"), RenderStateType.Blend));
        }

        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void ProjectedPassesUseTwoTargetBlend(string passName)
        {
            Assert.AreEqual("Blend One One, One One",
                RenderState(GetPass(passName), RenderStateType.Blend));
        }

        // Self has no Stencil block in Hidden/Shadow2D and must keep none: it draws the caster's own
        // coverage, which is never excluded by the unshadow mark.
        [Test]
        public void SelfHasNoStencilState()
        {
            Assert.IsNull(RenderState(GetPass("Self"), RenderStateType.Stencil),
                "Self must carry no Stencil block.");
        }

        // UnshadowMark sets ref 1 where the caster covers so ProjectedSelf (NotEqual 1) is excluded
        // from the caster's own silhouette; UnshadowUnmark clears it back to 0. ProjectedUnshadow is
        // the complement of ProjectedSelf (Equal 1).
        [TestCase("UnshadowMark", "1", "Always")]
        [TestCase("UnshadowUnmark", "0", "Always")]
        [TestCase("ProjectedSelf", "1", "NotEqual")]
        [TestCase("ProjectedUnshadow", "1", "Equal")]
        public void StencilMatchesTheBuiltInShader(string passName, string expectedRef, string expectedComp)
        {
            var stencil = RenderState(GetPass(passName), RenderStateType.Stencil);
            Assert.IsNotNull(stencil, $"{passName} must carry a Stencil block.");
            StringAssert.Contains($"Ref {expectedRef}", stencil);
            StringAssert.Contains($"Comp {expectedComp}", stencil);
        }

        // Cull Off / ZWrite Off / ZTest Always sit at SubShader scope in the hand-written shader.
        // ShaderGraph only emits render state per pass, so every pass has to repeat them -- and a pass
        // that forgot would depth-test or cull a full-screen-ish shadow quad.
        [TestCaseSource(nameof(k_AllPassNames))]
        public void EveryPassDisablesCullDepthWriteAndDepthTest(string passName)
        {
            var pass = GetPass(passName);
            Assert.AreEqual("Cull Off", RenderState(pass, RenderStateType.Cull));
            Assert.AreEqual("ZWrite Off", RenderState(pass, RenderStateType.ZWrite));
            Assert.AreEqual("ZTest Always", RenderState(pass, RenderStateType.ZTest));
        }

        // ------------------------------------------------------------------------------------------
        // Keywords
        // ------------------------------------------------------------------------------------------

        // ShadowRendering records NOT_TRANSFORMABLE per draw through the command buffer, guarded by
        // keywordSpace.FindKeyword(...).isValid -- so a shader that fails to declare it is skipped
        // silently rather than reported. MultiCompile, because shader_feature variants would be
        // stripped and the per-draw write would have nothing to select.
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void ProjectedPassesDeclareNotTransformableAsMultiCompile(string passName)
        {
            var keyword = GetPass(passName).keywords
                .Select(k => k.descriptor)
                .SingleOrDefault(k => k.referenceName == "NOT_TRANSFORMABLE");

            Assert.IsNotNull(keyword, $"{passName} must declare NOT_TRANSFORMABLE.");
            Assert.AreEqual(KeywordDefinition.MultiCompile, keyword.definition);
        }

        // Coverage is reproduced from the built-in shader, so both caster keywords have to exist for
        // ShadowRendering's per-draw writes to land. MultiCompile for the same reason as above.
        [TestCase("Self", "SHADOW_SPRITE_CASTER")]
        [TestCase("UnshadowMark", "SHADOW_SPRITE_CASTER")]
        [TestCase("UnshadowUnmark", "SHADOW_SPRITE_CASTER")]
        [TestCase("Self", "SKINNED_SPRITE")]
        [TestCase("UnshadowMark", "SKINNED_SPRITE")]
        [TestCase("UnshadowUnmark", "SKINNED_SPRITE")]
        public void CasterPassesDeclareTheCasterKeywordsAsMultiCompile(string passName, string keywordName)
        {
            var keyword = GetPass(passName).keywords
                .Select(k => k.descriptor)
                .SingleOrDefault(k => k.referenceName == keywordName);

            Assert.IsNotNull(keyword, $"{passName} must declare {keywordName}.");
            Assert.AreEqual(KeywordDefinition.MultiCompile, keyword.definition);
        }

        // The projected passes extrude shadowCaster.mesh, never a sprite renderer, so a caster keyword
        // leaking onto them would let the phase-1 caster draw's recorded value select a variant here.
        [TestCase("ProjectedSelf")]
        [TestCase("ProjectedUnshadow")]
        public void ProjectedPassesDeclareNoCasterKeywords(string passName)
        {
            var names = GetPass(passName).keywords.Select(k => k.descriptor.referenceName).ToList();
            CollectionAssert.DoesNotContain(names, "SHADOW_SPRITE_CASTER");
            CollectionAssert.DoesNotContain(names, "SKINNED_SPRITE");
        }

        // ------------------------------------------------------------------------------------------
        // Node denial
        // ------------------------------------------------------------------------------------------

        static UniversalShadowCaster2DSubTarget NewSubTarget()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalShadowCaster2DSubTarget)));
            return (UniversalShadowCaster2DSubTarget)target.activeSubTarget;
        }

        // Reading a normal is meaningless on the caster passes -- the mesh carries no NORMAL -- and on
        // the projected passes TANGENT is the geometry generator's payload, so a Tangent Vector node
        // decodes (role, fanParam, prev.xy) as a tangent and yields a plausible-looking wrong shadow.
        // Denied by requirement interface rather than by type, so nodes added later are caught too.
        [TestCase(typeof(NormalVectorNode))]
        [TestCase(typeof(TangentVectorNode))]
        [TestCase(typeof(BitangentVectorNode))]
        public void GeometryNodesAreDenied(Type nodeType)
        {
            Assert.IsFalse(NewSubTarget().IsNodeAllowedBySubTarget(nodeType),
                $"{nodeType.Name} pulls a semantic this shader cannot supply and must be rejected.");
        }

        // The filter must not be over-broad, and it WAS: keyed on IMayRequireNormal / Tangent /
        // Bitangent, which CodeFunctionNode itself implements to service its Binding slots. Since
        // Type.GetInterfaces() reports inherited interfaces and 110 node types derive from
        // CodeFunctionNode, that rejected most of the node library -- Add included.
        //
        // Every case below except MultiplyNode is a CodeFunctionNode, which is the point: the original
        // version of this test used only MultiplyNode, one of the few math nodes deriving straight from
        // AbstractMaterialNode, so it passed while Add was being rejected.
        [TestCase(typeof(AddNode))]
        [TestCase(typeof(SubtractNode))]
        [TestCase(typeof(LengthNode))]
        [TestCase(typeof(MultiplyNode))]
        [TestCase(typeof(PositionNode))]
        [TestCase(typeof(SubGraphNode))]
        public void OrdinaryNodesAreAllowed(Type nodeType)
        {
            Assert.IsTrue(NewSubTarget().IsNodeAllowedBySubTarget(nodeType),
                $"{nodeType.Name} requires nothing this shader withholds and must be allowed.");
        }

        // The SubTarget's own node is a CodeFunctionNode, so an interface-based filter denied the very
        // node it ships. Worth asserting explicitly -- it is the clearest signal the mechanism is wrong.
        [TestCase(typeof(ShadowCaster2DLightNode))]
        public void TheSubTargetsOwnNodesAreAllowed(Type nodeType)
        {
            Assert.IsTrue(NewSubTarget().IsNodeAllowedBySubTarget(nodeType),
                $"{nodeType.Name} is shipped by this SubTarget and must not be rejected by it.");
        }

        // A SubGraphNode with no asset assigned must not be reported -- there is nothing to inspect,
        // and flagging it would put an error on a node the user has not finished wiring.
        [Test]
        public void SubGraphNodeWithNoAssetIsNotReported()
        {
            bool reported = NewSubTarget().ValidateNodeCompatibility(
                new SubGraphNode(), out string message, out _);

            Assert.IsFalse(reported, $"An empty Sub Graph node was reported: {message}");
        }

        // ------------------------------------------------------------------------------------------
        // The Create menu
        // ------------------------------------------------------------------------------------------

        // A wrong MenuItem path does not fail to compile -- it just puts the entry somewhere nobody
        // looks, or nowhere at all. Asserting the attribute's own value against the expected path is
        // what makes that visible.
        [Test]
        public void CreateMenuItemIsRegisteredAtTheExpectedPath()
        {
            const string expected = "Assets/Create/Shader Graph/URP/Shadow2D Shader Graph";

            var method = typeof(CreateShadowCaster2DShaderGraph)
                .GetMethod(nameof(CreateShadowCaster2DShaderGraph.CreateShadowCaster2DGraph),
                           BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "The Create-menu entry point is missing.");

            var menuItem = (MenuItem)Attribute.GetCustomAttribute(method, typeof(MenuItem));
            Assert.IsNotNull(menuItem, "The Create-menu entry point carries no [MenuItem] attribute.");
            Assert.AreEqual(expected, menuItem.menuItem);
            Assert.AreEqual(expected, CreateShadowCaster2DShaderGraph.k_MenuPath,
                "k_MenuPath and the [MenuItem] path have diverged.");
        }

        // Every entry under Create > Shader Graph > URP is "<Name> Shader Graph" -- two words, as the
        // suffix. Asserting the convention rather than just this one string is what stops the next
        // SubTarget re-introducing the drift; a menu that reads "Shadergraph" or "ShaderGraph" compiles
        // perfectly and only looks wrong to a human scanning the list.
        [Test]
        public void CreateMenuFollowsTheUrpSubmenuNamingConvention()
        {
            const string prefix = "Assets/Create/Shader Graph/URP/";
            const string suffix = " Shader Graph";

            string path = CreateShadowCaster2DShaderGraph.k_MenuPath;
            StringAssert.StartsWith(prefix, path);
            StringAssert.EndsWith(suffix, path);

            string name = path.Substring(prefix.Length, path.Length - prefix.Length - suffix.Length);
            Assert.IsNotEmpty(name, "The menu entry has no name before \" Shader Graph\".");
            Assert.IsFalse(name.Contains("Shader") || name.Contains("shader"),
                $"\"{name}\" restates Shader Graph; the suffix already carries it.");
        }

        // Seeding a block the SubTarget does not register would give the user a port that codegen then
        // ignores; registering one the menu does not seed would hide it on a freshly created asset.
        [Test]
        public void CreateMenuSeedsExactlyTheRegisteredBlocks()
        {
            var target = new UniversalTarget();
            Assert.IsTrue(target.TrySetActiveSubTarget(typeof(UniversalShadowCaster2DSubTarget)));

            var blockContext = new TargetActiveBlockContext(new System.Collections.Generic.List<BlockFieldDescriptor>(), null);
            target.GetActiveBlocks(ref blockContext);

            CollectionAssert.AreEquivalent(
                new BlockFieldDescriptor[]
                {
                    BlockFields.VertexDescription.Position,
                    UniversalBlockFields.VertexDescription.ShadowDisplacement,
                UniversalBlockFields.VertexDescription.ShadowSideSoftness,
                UniversalBlockFields.VertexDescription.ShadowBackSoftness,
                    BlockFields.SurfaceDescription.Alpha,
                },
                blockContext.activeBlocks,
                "GetActiveBlocks and the Create menu's seeded blocks must agree.");
        }
    }
}
