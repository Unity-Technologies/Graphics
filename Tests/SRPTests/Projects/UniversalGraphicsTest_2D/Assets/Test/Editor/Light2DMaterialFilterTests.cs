using NUnit.Framework;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// The Light2D material field offers only Materials whose shader declares the passes the renderer
// draws, the same way the shadow material field does.
class Light2DMaterialFilterTests
{
    // Declares both Light2D passes.
    const string kCompatibleShader = @"Shader ""Hidden/Light2DMaterialFilterTests/Compatible""
{
    SubShader
    {
        Pass { Name ""Light2D"" CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return 0; }
        ENDCG }
        Pass { Name ""Light2DVolumetric"" CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return 0; }
        ENDCG }
    }
}";

    // Declares the light pass but not the volumetric one.
    const string kPartialShader = @"Shader ""Hidden/Light2DMaterialFilterTests/Partial""
{
    SubShader
    {
        Pass { Name ""Light2D"" CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return 0; }
        ENDCG }
    }
}";

    [Test]
    public void MaterialDeclaringBothLightPasses_IsCompatible()
    {
        RunWithMaterial(kCompatibleShader, material =>
        {
            Assert.AreEqual(string.Empty, RendererLighting.GetMissingLightPasses(material));
            Assert.IsTrue(RendererLighting.IsCompatibleLightMaterial(material));
            Assert.IsTrue(RendererLighting.IsCompatibleLightShader(material.shader));
        });
    }

    [Test]
    public void MaterialMissingTheVolumetricPass_IsNotCompatibleAndNamesIt()
    {
        RunWithMaterial(kPartialShader, material =>
        {
            Assert.AreEqual(RendererLighting.k_LightVolumetricPassName, RendererLighting.GetMissingLightPasses(material));
            Assert.IsFalse(RendererLighting.IsCompatibleLightMaterial(material));
            Assert.IsFalse(RendererLighting.IsCompatibleLightShader(material.shader));
        });
    }

    [Test]
    public void MaterialWithUnrelatedShader_IsNotCompatibleAndNamesBothPasses()
    {
        var material = new Material(Shader.Find("Hidden/Internal-Colored"));
        try
        {
            string missing = RendererLighting.GetMissingLightPasses(material);
            StringAssert.Contains(RendererLighting.k_LightPassName, missing);
            StringAssert.Contains(RendererLighting.k_LightVolumetricPassName, missing);
            Assert.IsFalse(RendererLighting.IsCompatibleLightMaterial(material));
        }
        finally
        {
            Object.DestroyImmediate(material);
        }
    }

    [Test]
    public void NullMaterial_IsNotCompatible()
    {
        Assert.IsFalse(RendererLighting.IsCompatibleLightMaterial(null));
        Assert.IsFalse(RendererLighting.IsCompatibleLightShader(null));
    }

    // The object picker is filtered, but dragging an asset onto the field bypasses it, so the drag
    // itself is judged before the object field can assign.
    [Test]
    public void DraggingACompatibleMaterial_IsAccepted()
    {
        RunWithMaterial(kCompatibleShader, material =>
        {
            Assert.IsTrue(Light2DEditorUtility.IsMaterialDragAcceptable(
                new Object[] { material }, RendererLighting.IsCompatibleLightMaterial));
        });
    }

    [Test]
    public void DraggingAnIncompatibleMaterial_IsRejected()
    {
        RunWithMaterial(kPartialShader, material =>
        {
            Assert.IsFalse(Light2DEditorUtility.IsMaterialDragAcceptable(
                new Object[] { material }, RendererLighting.IsCompatibleLightMaterial));
        });
    }

    // Several Materials at once: which one a drop would land on is not visible to the user, so the
    // drag is taken only when the answer cannot matter.
    [Test]
    public void DraggingSeveralMaterials_IsRejectedUnlessEveryOneIsCompatible()
    {
        RunWithMaterial(kCompatibleShader, compatible =>
        {
            RunWithMaterial(kPartialShader, incompatible =>
            {
                Assert.IsTrue(Light2DEditorUtility.IsMaterialDragAcceptable(
                    new Object[] { compatible, compatible }, RendererLighting.IsCompatibleLightMaterial));
                Assert.IsFalse(Light2DEditorUtility.IsMaterialDragAcceptable(
                    new Object[] { compatible, incompatible }, RendererLighting.IsCompatibleLightMaterial));
            }, "Assets/Light2DMaterialFilterTests_Temp2.shader");
        });
    }

    // A payload with no Material in it is left to the object field, which refuses it on type.
    [Test]
    public void DraggingSomethingThatIsNotAMaterial_IsLeftToTheObjectField()
    {
        var texture = new Texture2D(1, 1);
        try
        {
            Assert.IsTrue(Light2DEditorUtility.IsMaterialDragAcceptable(
                new Object[] { texture }, RendererLighting.IsCompatibleLightMaterial));
            Assert.IsTrue(Light2DEditorUtility.IsMaterialDragAcceptable(
                null, RendererLighting.IsCompatibleLightMaterial));
            Assert.IsTrue(Light2DEditorUtility.IsMaterialDragAcceptable(
                new Object[0], RendererLighting.IsCompatibleLightMaterial));
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    // The shadow Material field uses the same drag filter, keyed on the shadow passes.
    [Test]
    public void DraggingAMaterialWithoutShadowPasses_IsRejectedByTheShadowFilter()
    {
        RunWithMaterial(kCompatibleShader, lightMaterial =>
        {
            Assert.IsFalse(Light2DEditorUtility.IsMaterialDragAcceptable(
                new Object[] { lightMaterial }, ShadowRendering.IsCompatibleShadowMaterial));
        });
    }

    static void RunWithMaterial(string shaderSource, System.Action<Material> body, string path = "Assets/Light2DMaterialFilterTests_Temp.shader")
    {
        try
        {
            System.IO.File.WriteAllText(path, shaderSource);
            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);

            var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.IsNotNull(shader, "test shader failed to import");

            var material = new Material(shader);
            try
            {
                body(material);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }
        finally
        {
            UnityEditor.AssetDatabase.DeleteAsset(path);
        }
    }
}
