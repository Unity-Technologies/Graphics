using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

// Unit tests to check parts of the RenderRequest API not generating any graphics output
// Rest of the API is tested in Scenes/9x_Other/9960-RPCRenderRequest integration test
class UserRenderRequestTests
{
    GameObject m_Obj;
    Camera m_Camera;

    /// <summary>
    /// Data structure describing the data for a NON supported render request
    /// </summary>
    public class NonSupportedRequest
    {
        public RenderTexture destination = null;
        public int mipLevel = 0;
        public CubemapFace face = CubemapFace.Unknown;
        public int slice = 0;
    }

    [SetUp]
    public void Setup()
    {
        m_Obj = new GameObject();
        m_Camera = m_Obj.AddComponent<Camera>();
        m_Obj.AddComponent<HDAdditionalCameraData>();   

        // Create main render loop history
        HDCamera.GetOrCreate(m_Camera);

        // Creating all user history channels
        for(HDCamera.HistoryChannel channel = HDCamera.HistoryChannel.CustomUserHistory0; channel <= HDCamera.HistoryChannel.CustomUserHistory7; ++channel)
        {
            HDCamera.HistoryChannel newGeneratedChannel = HDCamera.GetFreeUserHistoryChannel(m_Camera);
            HDCamera.GetOrCreate(m_Camera, 0, newGeneratedChannel);
            Assert.IsTrue(newGeneratedChannel == channel);
        }
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(m_Obj);
        m_Obj = null;
        m_Camera = null;
    }
    
    [Test, Description("Given a HDRP render pipeline, when checking its render request support, then we see that only Standard Request is supported")]
    public void CheckRenderRequestSupport()
    {
        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest();
        Assert.IsTrue(RenderPipeline.SupportsRenderRequest(m_Camera, request));

        NonSupportedRequest nonSupportedRequest = new NonSupportedRequest();
        Assert.IsFalse(RenderPipeline.SupportsRenderRequest(m_Camera, nonSupportedRequest));
    }

    // History Channel API is only for internal use for now, to be publicly available when implemented in SRP Core
    [Test, Description("Given a camera with all history channels generated, when checking if they exist, then the channels are found")]
    public void CheckExistingHistoryChannels()
    {
        // All history channels should have been created at Setup (user ones + main render loop)
        for(HDCamera.HistoryChannel channel = HDCamera.HistoryChannel.CustomUserHistory0; channel <= HDCamera.HistoryChannel.RenderLoopHistory; ++channel)
        {
            Assert.IsTrue(HDCamera.IsHistoryChannelExisting(m_Camera, 0, channel));
        }
    }

    [Test, Description("Given a camera with all history channels generated, when trying to create a new user history channel, then an exception is thrown")]
    public void ThrowWhenTryingToCreateExtraUserHistoryChannel()
    {
        // No more available channel, all created at Setup, function will throw an exception
        Assert.Throws<Exception>(() => HDCamera.GetFreeUserHistoryChannel(m_Camera));
    }

    [Test, Description("Given a camera with all history channels generated, when deleting a specific user history channel, then the user channel is deleted")]
    public void FreeSpecificHistoryChannel()
    {
        // Clearing user channels one by one
        for(HDCamera.HistoryChannel channel = HDCamera.HistoryChannel.CustomUserHistory0; channel <= HDCamera.HistoryChannel.CustomUserHistory7; ++channel)
        {
            Assert.IsTrue(HDCamera.FreeUserHistoryChannel(m_Camera, 0, channel));
            // Can't free channel again, already gone
            Assert.IsFalse(HDCamera.FreeUserHistoryChannel(m_Camera, 0, channel));
        }

        // Can never free main render loop history
        Assert.IsFalse(HDCamera.FreeUserHistoryChannel(m_Camera, 0, HDCamera.HistoryChannel.RenderLoopHistory));
    }

    [Test, Description("Given a camera with all history channels generated, when trying to delete all user history channels, then all user channels are deleted")]
    public void FreeAllHistoryChannels()
    {
        // Clearing all user history channels at once
        HDCamera.FreeAllUserHistoryChannels(m_Camera);
        
        // Making sure all user history channels are gone
        for(HDCamera.HistoryChannel channel = HDCamera.HistoryChannel.CustomUserHistory0; channel <= HDCamera.HistoryChannel.CustomUserHistory7; ++channel)
        {
            Assert.IsFalse(HDCamera.IsHistoryChannelExisting(m_Camera, 0, channel));
        }

        // Can never free main render loop history
        Assert.IsTrue(HDCamera.IsHistoryChannelExisting(m_Camera, 0, HDCamera.HistoryChannel.RenderLoopHistory));
    }
}
