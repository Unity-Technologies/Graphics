using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using CoreBridge = Unity.RenderPipelines.Core.Runtime.Shared.ConstantBufferBackedPropertiesBridge;

namespace UnityEditor.Rendering.Universal.Tests
{
    class ConstantBufferPropertyWarningsTests
    {
        // "float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];", leading whitespace and trailing comment allowed.
        static readonly Regex k_HlslDeclaration = new Regex(@"^\s*\w+\s+(\w+)\s*(?:\[[^\]]*\])?\s*;");

        const string k_TestWarning = "Test warning about '{0}'.";

        static readonly Dictionary<string, string> k_LightingConstantBufferFiles = new Dictionary<string, string>
        {
            { "AdditionalLights", "ShaderLibrary/Input.hlsl" },
            { "AdditionalLightShadows", "ShaderLibrary/Shadows.hlsl" },
            { "AdditionalLightPCSS", "ShaderLibrary/Shadows.hlsl" },
            { "urp_ReflectionProbeBuffer", "ShaderLibrary/Input.hlsl" },
            { "LightCookies", "ShaderLibrary/LightCookie/LightCookieInput.hlsl" },
        };

        [TearDown]
        public void TearDown()
        {
            ConstantBufferPropertyWarnings.Release();
        }

        static Regex WarningRegex(string warning, string propertyName)
        {
            return new Regex(Regex.Escape(warning.Replace("{0}", propertyName)));
        }

        static Regex BoundWarningFor(string propertyName)
        {
            return WarningRegex(ConstantBufferPropertyWarnings.k_GlobalWarningBound, propertyName);
        }

        static Regex FutureWarningFor(string propertyName)
        {
            return WarningRegex(ConstantBufferPropertyWarnings.k_GlobalWarningBoundLater, propertyName);
        }

        static Regex TestWarningFor(string propertyName)
        {
            return WarningRegex(k_TestWarning, propertyName);
        }

        static int MarkWithTestWarning(string propertyName)
        {
            ConstantBufferPropertyWarnings.MarkProperty(propertyName, k_TestWarning);
            return Shader.PropertyToID(propertyName);
        }

        static Material CreateTestMaterial()
        {
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        static string PackageFilePath(string relativePath)
        {
            var package = PackageManager.PackageInfo.FindForAssembly(typeof(UniversalRenderPipeline).Assembly);
            Assert.That(package, Is.Not.Null, "Cannot find the URP package.");
            return Path.Combine(package.resolvedPath, relativePath);
        }

        // Names of the members declared between every CBUFFER_START(cbufferName) and the next CBUFFER_END of the file.
        static void AddConstantBufferMembers(string relativePath, string cbufferName, HashSet<string> members)
        {
            string start = $"CBUFFER_START({cbufferName})";
            bool inside = false;
            bool found = false;

            foreach (string line in File.ReadAllLines(PackageFilePath(relativePath)))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (trimmed.StartsWith(start, StringComparison.Ordinal))
                {
                    inside = true;
                    found = true;
                    continue;
                }

                if (trimmed.StartsWith("CBUFFER_END", StringComparison.Ordinal))
                {
                    inside = false;
                    continue;
                }

                if (!inside)
                    continue;

                var match = k_HlslDeclaration.Match(line);
                if (match.Success)
                    members.Add(match.Groups[1].Value);
            }

            Assert.That(found, Is.True, $"{relativePath} no longer declares {start}.");
        }

        [Test]
        public void LightingConstantBuffers_MatchTheHlslConstantBufferMembers()
        {
            foreach (var constantBuffer in ConstantBufferPropertyWarnings.k_LightingConstantBuffers)
            {
                string hlslPath = k_LightingConstantBufferFiles[constantBuffer.name];
                var hlslMembers = new HashSet<string>();
                AddConstantBufferMembers(hlslPath, constantBuffer.name, hlslMembers);

                CollectionAssert.AreEquivalent(hlslMembers, constantBuffer.members,
                    $"ConstantBufferPropertyWarnings.k_LightingConstantBuffers must list the members of {constantBuffer.name} ({hlslPath}).");
            }
        }

        [Test]
        public void GlobalShaderVariables_MatchTheHlslConstantBufferMembersButThePadding()
        {
            var hlslMembers = new HashSet<string>();
            AddConstantBufferMembers("ShaderLibrary/GlobalShaderVariables.hlsl", "GlobalShaderVariables", hlslMembers);
            hlslMembers.RemoveWhere(name => name.StartsWith("_URPPadding", StringComparison.Ordinal));

            CollectionAssert.AreEquivalent(hlslMembers, ConstantBufferPropertyWarnings.k_GlobalShaderVariables,
                "ConstantBufferPropertyWarnings.k_GlobalShaderVariables must list the members of GlobalShaderVariables but the padding (ShaderLibrary/GlobalShaderVariables.hlsl).");
        }

        [Test]
        public void LooseUniformWrite_DuringRenderInConstantBufferMode_WarnsOncePerProperty()
        {
            int id = Shader.PropertyToID("_MainLightColor");
            Vector4 previous = Shader.GetGlobalVector(id);

            ConstantBufferPropertyWarnings.BeginRender(true);
            try
            {
                LogAssert.Expect(LogType.Warning, BoundWarningFor("_MainLightColor"));
                Shader.SetGlobalVector(id, Vector4.one);
                Shader.SetGlobalVector(id, Vector4.one);
            }
            finally
            {
                ConstantBufferPropertyWarnings.EndRender();
                Shader.SetGlobalVector(id, previous);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LooseUniformWrite_AfterEndRender_DoesNotWarn()
        {
            int id = Shader.PropertyToID("_MainLightPosition");
            Vector4 previous = Shader.GetGlobalVector(id);

            ConstantBufferPropertyWarnings.BeginRender(true);
            ConstantBufferPropertyWarnings.EndRender();
            Shader.SetGlobalVector(id, Vector4.one);
            Shader.SetGlobalVector(id, previous);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LooseUniformWrite_AfterARenderRequestDuringRender_StillWarnsUntilEndRender()
        {
            int id = Shader.PropertyToID("_GlossyEnvironmentColor");
            Vector4 previous = Shader.GetGlobalVector(id);

            ConstantBufferPropertyWarnings.BeginRender(false);
            // A render request made while URP renders.
            ConstantBufferPropertyWarnings.BeginRender(false);
            ConstantBufferPropertyWarnings.EndRender();
            try
            {
                LogAssert.Expect(LogType.Warning, FutureWarningFor("_GlossyEnvironmentColor"));
                Shader.SetGlobalVector(id, Vector4.one);
            }
            finally
            {
                ConstantBufferPropertyWarnings.EndRender();
                Shader.SetGlobalVector(id, previous);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LooseUniformWrite_DuringRenderWithoutTheConstantBufferMode_WarnsAboutTheFutureOnce()
        {
            int id = Shader.PropertyToID("_ScaledScreenParams");
            Vector4 previous = Shader.GetGlobalVector(id);

            ConstantBufferPropertyWarnings.BeginRender(false);
            try
            {
                LogAssert.Expect(LogType.Warning, FutureWarningFor("_ScaledScreenParams"));
                Shader.SetGlobalVector(id, Vector4.one);
                Shader.SetGlobalVector(id, Vector4.one);
            }
            finally
            {
                ConstantBufferPropertyWarnings.EndRender();
                Shader.SetGlobalVector(id, previous);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialWrite_ToALightingConstantBufferMember_WarnsThatItIsIgnoredInBothModes([Values(false, true)] bool useGlobalConstantBuffer)
        {
            Assume.That(RenderingUtils.usePersistentConstantBuffer, Is.True, "URP binds the lighting constant buffers itself on this graphics API only.");
            // A new material, so no earlier array of another size is in the way.
            var material = CreateTestMaterial();

            ConstantBufferPropertyWarnings.BeginRender(useGlobalConstantBuffer);
            ConstantBufferPropertyWarnings.EndRender();
            try
            {
                LogAssert.Expect(LogType.Warning, WarningRegex(ConstantBufferPropertyWarnings.LightingConstantBufferWarning("AdditionalLights"), "_AdditionalLightsColor"));
                material.SetVectorArray("_AdditionalLightsColor", new Vector4[8]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void PipelineWrites_DuringRender_DoNotWarn()
        {
            int id = Shader.PropertyToID("_ScreenParams");
            Vector4 previous = Shader.GetGlobalVector(id);

            ConstantBufferPropertyWarnings.BeginRender(false);
            try
            {
                ConstantBufferPropertyWarnings.BeginPipelineWrites();
                Shader.SetGlobalVector(id, Vector4.one);
                ConstantBufferPropertyWarnings.EndPipelineWrites();

                // The warnings are back on after the pipeline writes.
                LogAssert.Expect(LogType.Warning, FutureWarningFor("_ScreenParams"));
                Shader.SetGlobalVector(id, Vector4.one);
            }
            finally
            {
                ConstantBufferPropertyWarnings.EndRender();
                Shader.SetGlobalVector(id, previous);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UploaderLooseUniformPush_DuringRender_DoesNotWarn()
        {
            var uploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Base | GlobalShaderVariablesGroup.Only3D);
            uploader._ScaledScreenParams = Vector4.one;

            using var nativeCmd = new CommandBuffer();
            ConstantBufferPropertyWarnings.BeginRender(false);
            try
            {
                uploader.PushGlobal(CommandBufferHelpers.GetUnsafeCommandBuffer(nativeCmd));
            }
            finally
            {
                ConstantBufferPropertyWarnings.EndRender();
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void LooseUniformWrite_AfterTheModeChanged_WarnsInTheNewMode([Values(false, true)] bool useGlobalConstantBuffer)
        {
            ConstantBufferPropertyWarnings.BeginRender(!useGlobalConstantBuffer);
            ConstantBufferPropertyWarnings.EndRender();

            int id = Shader.PropertyToID("_SubtractiveShadowColor");
            Vector4 previous = Shader.GetGlobalVector(id);

            ConstantBufferPropertyWarnings.BeginRender(useGlobalConstantBuffer);
            try
            {
                LogAssert.Expect(LogType.Warning, useGlobalConstantBuffer ? BoundWarningFor("_SubtractiveShadowColor") : FutureWarningFor("_SubtractiveShadowColor"));
                Shader.SetGlobalVector(id, Vector4.one);
            }
            finally
            {
                ConstantBufferPropertyWarnings.EndRender();
                Shader.SetGlobalVector(id, previous);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CommandBufferPropertyBlockAndArrayWrites_ToAMarkedProperty_Warn()
        {
            CoreBridge.ClearWarnings();
            int commandBufferId = MarkWithTestWarning("_CBPropertyWarningsTest_CommandBuffer");
            int propertyBlockId = MarkWithTestWarning("_CBPropertyWarningsTest_PropertyBlock");
            int arrayId = MarkWithTestWarning("_CBPropertyWarningsTest_Array");
            CoreBridge.EnableWarnings(true);
            try
            {
                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_CommandBuffer"));
                using (var cmd = new CommandBuffer())
                    cmd.SetGlobalFloat(commandBufferId, 1.0f);

                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_PropertyBlock"));
                new MaterialPropertyBlock().SetColor(propertyBlockId, Color.red);

                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_Array"));
                Shader.SetGlobalVectorArray(arrayId, new[] { Vector4.zero });
            }
            finally
            {
                CoreBridge.EnableWarnings(false);
                CoreBridge.ClearWarnings();
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialWrite_OutsideRenderInConstantBufferMode_WarnsOncePerProperty()
        {
            var material = CreateTestMaterial();

            // After Render(), like a MonoBehaviour.Update() between two frames.
            ConstantBufferPropertyWarnings.BeginRender(true);
            ConstantBufferPropertyWarnings.EndRender();
            try
            {
                LogAssert.Expect(LogType.Warning, BoundWarningFor("_TimeParameters"));
                material.SetVector("_TimeParameters", Vector4.one);
                material.SetVector("_TimeParameters", Vector4.one);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialWrite_AfterURPIsDisposedButStaysActive_StillWarns()
        {
            var material = CreateTestMaterial();

            ConstantBufferPropertyWarnings.BeginRender(true);
            ConstantBufferPropertyWarnings.EndRender();
            ConstantBufferPropertyWarnings.OnPipelineDisposed(true);
            try
            {
                LogAssert.Expect(LogType.Warning, BoundWarningFor("_TimeParameters"));
                material.SetVector("_TimeParameters", Vector4.one);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialWrite_AfterAnotherPipelineTakesOver_DoesNotWarn()
        {
            var material = CreateTestMaterial();

            ConstantBufferPropertyWarnings.BeginRender(true);
            ConstantBufferPropertyWarnings.EndRender();
            ConstantBufferPropertyWarnings.OnPipelineDisposed(false);
            try
            {
                material.SetVector("_TimeParameters", Vector4.one);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialWrite_WithoutRenderGraphValidityChecks_DoesNotWarn()
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphGlobalSettings>(out var renderGraphSettings))
                Assert.Ignore("Enable Validity Checks lives in the Render Graph settings of the active render pipeline.");
            bool validityChecksBefore = renderGraphSettings.enableValidityChecks;
            var material = CreateTestMaterial();

            try
            {
                renderGraphSettings.enableValidityChecks = true;
                ConstantBufferPropertyWarnings.BeginRender(true);
                ConstantBufferPropertyWarnings.EndRender();

                renderGraphSettings.enableValidityChecks = false;
                ConstantBufferPropertyWarnings.BeginRender(true);
                ConstantBufferPropertyWarnings.EndRender();
                material.SetVector("_TimeParameters", Vector4.one);
            }
            finally
            {
                renderGraphSettings.enableValidityChecks = validityChecksBefore;
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialWrite_OutsideRenderWithoutTheConstantBufferMode_WarnsAboutTheFuture()
        {
            var material = CreateTestMaterial();

            ConstantBufferPropertyWarnings.BeginRender(false);
            ConstantBufferPropertyWarnings.EndRender();
            try
            {
                LogAssert.Expect(LogType.Warning, FutureWarningFor("_TimeParameters"));
                material.SetVector("_TimeParameters", Vector4.one);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void MaterialSetters_ToAMarkedProperty_WarnWithTheWarningsDisabled()
        {
            var material = CreateTestMaterial();

            CoreBridge.ClearWarnings();
            int floatId = MarkWithTestWarning("_CBPropertyWarningsTest_MaterialFloat");
            int integerId = MarkWithTestWarning("_CBPropertyWarningsTest_MaterialInteger");
            int colorId = MarkWithTestWarning("_CBPropertyWarningsTest_MaterialColor");
            int matrixId = MarkWithTestWarning("_CBPropertyWarningsTest_MaterialMatrix");
            int arrayId = MarkWithTestWarning("_CBPropertyWarningsTest_MaterialArray");
            int textureId = MarkWithTestWarning("_CBPropertyWarningsTest_MaterialTexture");
            try
            {
                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_MaterialFloat"));
                material.SetFloat(floatId, 1.0f);

                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_MaterialInteger"));
                material.SetInteger(integerId, 1);

                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_MaterialColor"));
                material.SetColor(colorId, Color.red);

                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_MaterialMatrix"));
                material.SetMatrix(matrixId, Matrix4x4.identity);

                LogAssert.Expect(LogType.Warning, TestWarningFor("_CBPropertyWarningsTest_MaterialArray"));
                material.SetVectorArray(arrayId, new[] { Vector4.zero });

                // Textures cannot live in a constant buffer.
                material.SetTexture(textureId, Texture2D.whiteTexture);
            }
            finally
            {
                CoreBridge.ClearWarnings();
                UnityEngine.Object.DestroyImmediate(material);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TextureWrite_ToAMarkedProperty_DoesNotWarn()
        {
            CoreBridge.ClearWarnings();
            int id = MarkWithTestWarning("_CBPropertyWarningsTest_Texture");
            CoreBridge.EnableWarnings(true);
            try
            {
                Shader.SetGlobalTexture(id, Texture2D.whiteTexture);
            }
            finally
            {
                CoreBridge.EnableWarnings(false);
                CoreBridge.ClearWarnings();
            }

            LogAssert.NoUnexpectedReceived();
        }
    }
}
