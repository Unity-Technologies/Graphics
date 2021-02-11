using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class ShaderGraphTestResult : ScriptableObject 
{
    //------------I dont think we can have a direct ref to another scriptable object in an assetbundle -------- need to doublec check
    [SerializeField]
    ShaderGraphTestAsset m_testAsset;

    public ShaderGraphTestAsset TestAsset => m_testAsset;

    [SerializeField]
    List<HashTaggedTestResult> m_individualResults;

    public void Initialize(ShaderGraphTestAsset test)
    {
        m_testAsset = test;
        m_individualResults = new List<HashTaggedTestResult>();
        foreach(var individual in test.testMaterial)
        {
            m_individualResults.Add(new HashTaggedTestResult(individual.material.name,
                                                             new Texture2D(test.settings.TargetWidth, test.settings.TargetHeight, TextureFormat.ARGB32, false, test.settings.UseHDR),
                                                             individual.hash));
            
        }
    }

}

[Serializable]
public class HashTaggedTestResult 
{
    public string name;
    public int hash;
    [SerializeField]
    private byte[] t_asPng;
    [SerializeField]
    private int t_width;
    [SerializeField]
    private int t_height;
    [SerializeField]
    private TextureFormat t_format;

    public HashTaggedTestResult(string testName, Texture2D testResult, int testHash)
    {
        name = testName;
        SetImage(testResult, testHash);
    }

    public void SetImage(Texture2D newResultImage, int newTestHash)
    {
        Assert.AreNotEqual(hash, newTestHash);
        Assert.IsNotNull(newResultImage);
        t_width = newResultImage.width;
        t_height = newResultImage.height;
        t_format = newResultImage.format;
        t_asPng = newResultImage.EncodeToPNG();
    }

    public Texture2D GetImage()
    {
        if(t_asPng == null || t_asPng.Length == 0 || t_width == 0 || t_height == 0)
        {
            return null;
        }
        else
        {
            Texture2D output = new Texture2D(t_width, t_height, t_format, false);
            if(ImageConversion.LoadImage(output, t_asPng))
            {
                return output;
            }
            else
            {
                return null;
            }
        }
    }
}
