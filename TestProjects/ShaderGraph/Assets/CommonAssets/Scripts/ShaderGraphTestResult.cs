using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[Serializable]
public class ShaderGraphTestResult : UnityEngine.Object
{
    private ShaderGraphTestAsset m_testAsset;
    public ShaderGraphTestResult(ShaderGraphTestAsset test)
    {
        m_testAsset = test;
    }

    List<HashTaggedTestResult> imageResults;

}

[Serializable]
public class HashTaggedTestResult 
{
    public int hash;
    [SerializeField]
    private byte[] t_asPng;
    [SerializeField]
    private int t_width;
    [SerializeField]
    private int t_height;
    [SerializeField]
    private TextureFormat t_format;

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
