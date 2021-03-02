using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

[System.Serializable]
public class TestAssetTestData
{
    public string testName;
    [NonSerialized]
    public Texture2D expectedResult;
    [SerializeField]
    private string m_expectedResultPath;
    public string ExpectedResultPath { get => m_expectedResultPath; private set => m_expectedResultPath = value; }
    public int expectedHash;
    [NonSerialized]
    public Material testMaterial;
    [SerializeField]
    private string m_testMaterialPath;
    public string TestMaterialPath { get => m_testMaterialPath; set => m_testMaterialPath = value; }
    public int testHash;
    public bool isCameraPersective;
    [NonSerialized]
    public ImageComparisonSettings imageComparisonSettings;
    [SerializeField]
    private string json_imageComp;
    [NonSerialized]
    public Mesh customMesh;
    [SerializeField]
    private string m_customMeshPath;
    public string CustomMeshPath { get => m_customMeshPath; private set => m_customMeshPath = value; }

    public string ToJson()
    {
        json_imageComp = JsonUtility.ToJson(imageComparisonSettings);
        return JsonUtility.ToJson(this);
    }

    public void FromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        imageComparisonSettings = JsonUtility.FromJson<ImageComparisonSettings>(json_imageComp);
    }

    public TestAssetTestData()
    {

    }

    public TestAssetTestData(ShaderGraphTestAsset testAsset, ShaderGraphTestAsset.MaterialTest individualTest, Texture2D expectedResultImage, int expectedResultHash)
    {
        testName = testAsset.name;
        expectedResult = expectedResultImage;
        expectedHash = expectedResultHash;
        testMaterial = individualTest.material;
        testHash = individualTest.hash;
        isCameraPersective = testAsset.isCameraPerspective;
        imageComparisonSettings = testAsset.settings;
        customMesh = testAsset.customMesh;
        if (expectedResult == null)
        {
            ExpectedResultPath = null;
        }
        else
        {
            ExpectedResultPath = $"{testName}_{testMaterial.name}_image";
        }
        TestMaterialPath = testMaterial.name;
        if (customMesh == null)
        {
            CustomMeshPath = null;
        }
        else
        {
            CustomMeshPath = testAsset.customMesh.name;
        }
    }
}


