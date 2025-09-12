using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

namespace UnityEngine.Rendering.RenderGraphModule
{
    public partial class RenderGraph
    {
        internal static class RenderGraphExceptionMessages
        {
            internal static bool enableCaller = true;

            internal const string k_RenderGraphExecutionError = "Render Graph Execution error";

            static readonly Dictionary<RenderGraphState, string> m_RenderGraphStateMessages = new()
            {
                {
                    RenderGraphState.RecordingPass,
                    "This API cannot be called when Render Graph records a pass. Call the API within SetRenderFunc() or outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass()."
                },
                {
                    RenderGraphState.RecordingGraph,
                    "This API cannot be called during the Render Graph high-level recording step. Call the API within AddUnsafePass()/AddComputePass()/AddRasterRenderPass() or outside of RecordRenderGraph()."
                },
                {
                    RenderGraphState.RecordingPass | RenderGraphState.Executing,
                    "This API cannot be called when Render Graph records a pass or executes it. Call the API outside of AddUnsafePass()/AddComputePass()/AddRasterRenderPass()."
                },
                {
                    RenderGraphState.Executing,
                    "This API cannot be called during the Render Graph execution. Call the API outside of SetRenderFunc()."
                },
                {
                    RenderGraphState.Active,
                    "This API cannot be called when Render Graph is active. Call the API outside of RecordRenderGraph()."
                }
            };

            // General Errors
            const string k_ErrorDefaultMessage = "Invalid render graph state, impossible to log the exception.";

            internal const string k_NonTextureAsAttachmentError = "Only textures can be used as a fragment attachment.";

            // Compiler Context Data
            internal const string k_OneResourceTwoVersionsError =
                "A pass is using SetAttachment or UseTexture on two versions of the same resource. Make sure you only access the latest version.";

            internal const string k_UseTextureRandWriteTwoVersionsError =
                "A pass is using UseTextureRandomWrite on two versions of the same resource.  Make sure you only access the latest version.";

            // Passes Data
            internal const string k_InvalidGetRenderTargetInfoResultsError =
                "GetRenderTargetInfo returned invalid results. Check that the width, height, and number of MSAA samples is not 0.";

            internal const string k_CannotDetermineSubPassFlagNoDepth =
                "SubPassFlag for merging cannot be determined if native pass doesn't have a depth attachment. Make sure your pass has a depth attachment.";

            internal const string k_AddingOlderAttachmentVersion = "The pass adds an older version while a higher version is already registered with the pass. Make sure you only access the latest version.";

            // Users shouldn't be seeing these, they are a sort of catch for internal RG mistakes.
            internal const string k_NonIncrementalCreationCall =
                "Something went wrong when compiling the graph. The Creation lists must be set-up incrementally for all passes, but AddFirstUse is called in an arbitrary non-incremental way.";

            internal const string k_NonIncrementalDestructionCall =
                "Something went wrong when compiling the graph. The Destruction lists must be set-up incrementally for all passes, AddLastUse is called in an arbitrary non-incremental way.";

            internal static string MismatchInDimensions(string name, int fragWidth, int fragHeight, int fragVolumeDepth,
                ResourceUnversionedData resInfo)
                =>
                    $"Mismatch in fragment dimensions when using resource '{name}'. Expected {fragWidth} x {fragHeight} x {fragVolumeDepth} " +
                    $"but got {resInfo.width} x {resInfo.height} x {resInfo.volumeDepth} instead.";

            internal static string MismatchInMSAASamlpes(string name, int expectedSamples, int actualSamples)
            {
                var expectedSamplesString = expectedSamples == 1 ? "None" : expectedSamples.ToString();
                var actualSamplesString = actualSamples == 1 ? "None" : actualSamples.ToString();
                return $"Mismatch in number of MSAA samples when using resource '{name}'. Expected {expectedSamplesString} but got {actualSamplesString} instead.";
            }

            // RenderGraphBuilders
            internal const string k_UndisposedBuilderPreviousPass =
                "Finish building the previous pass first by disposing of the pass builder object before adding a new pass. You can manually dispose of the builder with 'builder.Dispose()'.";

            internal const string k_WriteToVersionedResource =
                "The pass writes to a versioned resource handle. You can only write to unversioned resource handles to avoid branches in the resource history.";

            internal const string k_WriteToResourceTwice = "The pass writes to a resource twice. You can only write the same resource once within a pass.";

            internal const string k_TextureAlreadyBeingUsedThroughSetAttachment =
                "UseTexture is called on a texture that is already used through SetRenderAttachment. Check your code and make sure the texture is only used once.";

            internal const string k_SetRenderAttachmentTextureAlreadyUsed =
                "SetRenderAttachment is called on a texture that is already used through UseTexture/SetRenderAttachment. Check your code and make sure the texture is only used once.";

            internal const string k_SetRenderAttachmentOnDepthTexture =
                "SetRenderAttachment is called on a texture that has a depth format. Use a texture with a color format instead, or call SetRenderDepthAttachment.";

            internal const string k_SetRenderAttachmentOnGlobalTexture =
                "SetRenderAttachment is called on a texture that is currently bound to a global texture slot. Shaders might be using the texture using samplers. Make sure textures are not set as globals when using them as fragment attachments.";

            internal const string k_InvalidResource = "Using an invalid resource. Invalid resources can be resources leftover from a previous execution.";

            internal const string k_ReadWriteTransient =
                "This pass is reading or writing a transient resource. Transient resources are always assumed to be both read and written using 'AccessFlags.ReadWrite'.";

            internal static string NoGlobalTextureAtPropertyID(int propertyId) =>
                $"This pass is trying to read the global texture property {propertyId} but no previous pass in the graph bound a value to this global.";

            internal static string UseDepthWithColorFormat(GraphicsFormat colorFormat) =>
                $"SetRenderAttachmentDepth is called on a texture that has a color format {colorFormat}. Use a texture with a depth format instead, or call SetRenderAttachment.";

            internal static string UseTransientTextureInWrongPass(int transientIndex) =>
                $"This pass is using a transient resource from a different pass (pass index {transientIndex}). A transient resource should only be used in a single pass.";

            internal static string IncompatibleTextureUVOrigin(TextureUVOriginSelection origin, string attachmentType, string attachmentName, RenderGraphResourceType attachmentResourceType, int attachmentResourceIndex, TextureUVOriginSelection attachmentOrigin) =>
                $"TextureUVOrigin `{origin}` is not compatible with existing {attachmentType} attachment `{attachmentType}` of type `{attachmentResourceType}` at index `{attachmentResourceIndex}` with TextureUVOrigin `{attachmentOrigin}`";

            internal static string IncompatibleTextureUVOriginUseTexture(TextureUVOriginSelection origin) =>
                $"UseTexture() of a resource with `{origin}` is not compatible with Unity's standard UV origin for texture reading {TextureUVOrigin.BottomLeft}. Are you trying to UseTexture() on a backbuffer?";

            // RenderGraphPass
            internal const string k_MoreThanOneResourceForMRTIndex =
                "You can only bind a single texture to a single index in a multiple render texture (MRT). Verify your indexes are correct.";

            internal const string k_MoreThanOneTextureForFragInputIndex =
                "You can only bind a single texture to a fragment input index. Verify your indexes are correct.";

            internal const string k_MoreThanOneTextureRandomWriteInputIndex =
                "You can only bind a single texture to a random write input index. Verify your indexes are correct.";

            internal const string k_MultipleDepthTextures = "You can only set a single depth texture per pass.";

            //NativePassCompiler
            internal const string k_LoadingMemorylessResource = "This pass is loading a resource marked as memoryless.";

            internal const string k_ResolvignMemorylessResource =
                "This pass is storing or resolving a resource marked as memoryless";

            internal const string k_RenderPassIsEmpty = "Empty render pass";

            internal const string k_RenderPassHasInvalidProperties =
                "Invalid render pass properties. One or more properties are zero.";

            internal const string k_ShadingRateImageAttachmentDoesNotMatch =
                "Low level rendergraph error: Shading rate image attachment in renderpass does not match.";

            internal const string k_AttachmentsDoNotMatch =
                "Low level rendergraph error: Attachments in renderpass do not match.";

            internal const string k_MultisampledShaderResolveInvalidAttachmentSetup =
                "Low level rendergraph error: last subpass with shader resolve must have one color attachment.";

            internal const string k_MultisampledShaderResolveInputAttachmentNotMemoryless =
                "Low level rendergraph error: last subpass with shader resolve must have all input attachments as memoryless attachments.";

            internal const string k_InvalidMRTSetup = "Multiple render texture (MRT) setup is invalid. Some indices are not used.";

            internal const string k_NoDepthBufferMRT = "Setting multiple render textures (MRTs) without a depth buffer is not supported.";

            internal const string k_InvalidDepthAndColorTargets = "Neither depth nor color render targets are correctly set up.";

            internal const string k_InvalidResourceType = "Invalid resource type, expected texture or buffer";

            internal const string k_NoRenderFunction = "RenderPass was not provided with an execute function.";

            internal const string k_BeginNoActivePass =
                "Compiler error: Pass is marked as beginning a native sub pass but no pass is currently active.";

            internal const string k_NoActivePassForSubpass =
                "Compiler error: Generated a subpass pass but no pass is currently active.";
            internal static string UsingLegacyRenderGraph(string passName) =>
                "Pass '" + passName + "' is using the legacy rendergraph API." +
                " You cannot use legacy passes with the Native Render Pass Compiler." +
                " The APIs that are compatible with the Native Render Pass Compiler are AddUnsafePass, AddComputePass and AddRasterRenderPass.";

            internal static string IncompatibleTextureUVOriginStore(string firstAttachmentName, TextureUVOriginSelection firstAttachmentOrigin, string secondAttachmentName, TextureUVOriginSelection secondAttachmentOrigin) =>
                $"Texture attachment {firstAttachmentName} with uv origin {firstAttachmentOrigin} does not match with texture attachment {secondAttachmentName} with uv origin {secondAttachmentOrigin}. Storing both would result in contents being flipped.";

            internal static string GetExceptionMessage(RenderGraphState state)
            {
                string caller = GetHigherCaller();
                if (!m_RenderGraphStateMessages.TryGetValue(state, out var messageException))
                {
                    return enableCaller ? $"[{caller}] {k_ErrorDefaultMessage}" : k_ErrorDefaultMessage;
                }

                return enableCaller ? $"[{caller}] {messageException}" : messageException;
            }

            static string GetHigherCaller()
            {
                // k_CurrentStackCaller is used here to skip three levels in the call stack:
                // Level 0: GetHigherCaller() itself.
                // Level 1: GetExceptionMessage() or any other wrapper method that calls GetHigherCaller().
                // Level 2: The function that directly calls GetMessage() (e.g CheckNotUsedWhenExecute).
                // Level 3: The actual function we are interested in, our public API.
                const int k_CurrentStackCaller = 3;

                var stackTrace = new StackTrace(k_CurrentStackCaller, false);

                if (stackTrace.FrameCount > 0)
                {
                    var frame = stackTrace.GetFrame(0);
                    return frame?.GetMethod()?.Name ?? "UnknownCaller";
                }

                return "UnknownCaller";
            }
        }
    }
}
