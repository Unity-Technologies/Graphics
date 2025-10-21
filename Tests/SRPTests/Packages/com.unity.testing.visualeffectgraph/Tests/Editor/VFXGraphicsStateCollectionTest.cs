using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test
{
    public class VFXGraphicsStateCollectionTest
    {
        [UnityTest]
        public IEnumerator GraphicsStateCollection_IsFilled()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/GraphicsStateCollectionTest.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            yield return null;


            var gsc = new GraphicsStateCollection();

            GraphicsStateCollection.GraphicsState graphicsState = GetValidGraphicsState();

            var attachments = new NativeArray<AttachmentDescriptor>(graphicsState.attachments, Allocator.Temp);
            var subPasses = new NativeArray<SubPassDescriptor>(graphicsState.subPasses, Allocator.Temp);

            var vfxPath = VFXTestCommon.tempBasePath + "GraphicsStateCollection/PSOTest.vfx";
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(vfxAsset, $"Failed to load VisualEffectAsset at path: {vfxPath}");

            // Test AddGraphicsStates
            gsc.AddGraphicsStates(new[] { vfxAsset }, graphicsState.sampleCount, attachments, subPasses, graphicsState.subPassIndex, graphicsState.depthAttachmentIndex, graphicsState.shadingRateIndex);
            List<GraphicsStateCollection.ShaderVariant> variants = new List<GraphicsStateCollection.ShaderVariant>();
            gsc.GetVariants(variants);

            // 3 outputs, each with 4 passes, with and without instancing (x2) = 24 variants
            Assert.IsTrue(variants.Count == 24, $"Expected 24 variants in the GraphicsStateCollection, but got {variants.Count}.");

            foreach (var variant in variants)
            {
                List<GraphicsStateCollection.GraphicsState> states = new List<GraphicsStateCollection.GraphicsState>();
                gsc.GetGraphicsStatesForVariant(variant, states);
                Assert.IsTrue(states.Count > 0, $"Expected at least one graphics state for variant {variant.shader?.name }");

                foreach (var state in states)
                {
                    Assert.AreEqual(1, state.attachments.Length, "Attachments length mismatch.");
                    Assert.AreEqual(1, state.subPasses.Length, "SubPasses length mismatch.");
                }
            }
            gsc.ClearVariants();
            variants.Clear();

            // Test AddGraphicsStatesFromReference
            gsc.AddGraphicsStatesFromReference(graphicsState, new[] { vfxAsset });
            gsc.GetVariants(variants);

            // 3 outputs, each with 4 passes, with and without instancing (x2) = 24 variants
            Assert.IsTrue(variants.Count == 24, $"Expected 24 variants in the GraphicsStateCollection, but got {variants.Count}.");

            foreach (var variant in variants)
            {
                List<GraphicsStateCollection.GraphicsState> states = new List<GraphicsStateCollection.GraphicsState>();
                gsc.GetGraphicsStatesForVariant(variant, states);
                Assert.IsTrue(states.Count > 0, $"Expected at least one graphics state for variant {variant.shader?.name }");

                foreach (var state in states)
                {
                    Assert.AreEqual(1, state.attachments.Length, "Attachments length mismatch.");
                    Assert.AreEqual(1, state.subPasses.Length, "SubPasses length mismatch.");
                }
            }

            attachments.Dispose();
            subPasses.Dispose();
        }

        GraphicsStateCollection.GraphicsState GetValidGraphicsState()
        {
            GraphicsStateCollection.GraphicsState graphicsState = new GraphicsStateCollection.GraphicsState();

            AttachmentDescriptor attachment = new AttachmentDescriptor();
            attachment.format = RenderTextureFormat.ARGB32;
            attachment.storeAction = RenderBufferStoreAction.DontCare;
            graphicsState.attachments = new[] { attachment };

            SubPassDescriptor subPass = new SubPassDescriptor();
            subPass.colorOutputs = new AttachmentIndexArray(new int[] { 0 });
            subPass.flags = SubPassFlags.None;
            graphicsState.subPasses = new[] { subPass };
            graphicsState.sampleCount = 1;
            return graphicsState;
        }
    }
}
