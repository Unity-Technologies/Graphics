/*  Author : Mallik Yalamanchili
 *  Date: 11/25/2020
 *  Purpose: Test coding exercise - added/modified a test case
 */

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal.Internal;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public class MyTests
{

    // When changing URP settings, all settings should be valid and any out of range 
    // setting attempts are corrected to nearest boundary values. This test is a 
    // correction/modification of existing URP Editor test 'ValidateAssetSettings' and
    // can replace it - test definitely can be improved but I wanted to illustrate the 
    // incremental fix; also note the note about what I think is a bug
    [Test]
    public void ValidateAssetSettingsForBoundaryCorrections()
    {
        // Create a new asset
        ForwardRendererData data = ScriptableObject.CreateInstance<ForwardRendererData>();
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create(data);
        if (asset != null)
        {
            // Set desired properties to invalid values and ensure that they get set
            // correctly to nearest boundary values
            asset.shadowDistance = -1.0f;
            Assert.AreEqual(asset.shadowDistance, 0.0f);

            asset.shadowDistance = float.MaxValue;
            Assert.GreaterOrEqual(asset.shadowDistance,float.MaxValue);

            /* NOTE: Uncomment this chunk to see a bug in code. When the value is set as below,
             * the property should be set/corrected to float.MaxValue - instead the debug diagnostic 
             * below shows 'infinity' as the value. 
            //asset.shadowDistance = float.MaxValue * 2.0f;
            //Assert.GreaterOrEqual(asset.shadowDistance, float.MaxValue);
            //Debug.Log(asset.shadowDistance);
            */

            asset.renderScale = 0.0f;
            Assert.AreEqual(asset.renderScale, UniversalRenderPipeline.minRenderScale);

            asset.renderScale = 32.0f;
            Assert.AreEqual(asset.renderScale, UniversalRenderPipeline.maxRenderScale);

            asset.shadowNormalBias = -1.0f;
            Assert.AreEqual(asset.shadowNormalBias, 0.0f);

            asset.shadowNormalBias = 32.0f;
            Assert.AreEqual(asset.shadowNormalBias, UniversalRenderPipeline.maxShadowBias);

            asset.shadowDepthBias = -1.0f;
            Assert.AreEqual(asset.shadowDepthBias, 0.0f);

            asset.shadowDepthBias = 32.0f;
            Assert.AreEqual(asset.shadowDepthBias, UniversalRenderPipeline.maxShadowBias);

            asset.maxAdditionalLightsCount = -1;
            Assert.AreEqual(asset.maxAdditionalLightsCount, 0);

            asset.maxAdditionalLightsCount = 32;
            Assert.AreEqual(asset.maxAdditionalLightsCount, UniversalRenderPipeline.maxPerObjectLights);
        }
        ScriptableObject.DestroyImmediate(asset);
        ScriptableObject.DestroyImmediate(data);
    }
}
