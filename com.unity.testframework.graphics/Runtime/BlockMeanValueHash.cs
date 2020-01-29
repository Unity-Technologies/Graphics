using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

[Serializable]
public class BlockMeanValueHash
{
    public int numBlocksX = DEFAULT_XRES, numBlocksY = DEFAULT_YRES;
    private int blockPixelWidth, blockPixelHeight;

    protected float[,] blockData;
    public bool[,] medianData;

    protected const int DEFAULT_XRES = 12;
    protected const int DEFAULT_YRES = 12;

    public BlockMeanValueHash() { }

    public BlockMeanValueHash(float[,] data, int xRes = DEFAULT_XRES, int yRes = DEFAULT_YRES) {
        numBlocksX = xRes;
        numBlocksY = yRes;

        InitMedianData(data);
    }
    
    public BlockMeanValueHash(RenderTexture sourceRendTexture, int xRes = DEFAULT_XRES, int yRes = DEFAULT_YRES) {
        numBlocksX = xRes;
        numBlocksY = yRes;

        Texture2D sourceTexture = RendTexToTexture2D(sourceRendTexture);
        float[,] avgData = Tex2DToFloatAvgArr(sourceTexture);
        InitMedianData(avgData);
    }

    public BlockMeanValueHash(Texture2D sourceTexture, int xRes = DEFAULT_XRES, int yRes = DEFAULT_YRES) {
        numBlocksX = xRes;
        numBlocksY = yRes;

        float[,] avgData = Tex2DToFloatAvgArr(sourceTexture);
        InitMedianData(avgData);
    }

    protected float[,] Tex2DToFloatAvgArr(Texture2D sourceTexture) {
        // Square each channel and take the square root for a more accurate average
        Color[] colorData = sourceTexture.GetPixels();
        float[,] avgData = new float[sourceTexture.width, sourceTexture.height];

        for (int i = 0; i < sourceTexture.width * sourceTexture.height; i++) {
            int x = i % sourceTexture.width, y = i / sourceTexture.width;
            float channelAvg = Mathf.Sqrt(
                colorData[i].r * colorData[i].r +
                colorData[i].g * colorData[i].g +
                colorData[i].b * colorData[i].b
                );
            avgData[x, y] = channelAvg;
        }
        return avgData;
    }

    protected virtual void InitMedianData(float[,] data) {
        blockPixelWidth = data.GetLength(0) / numBlocksX;
        blockPixelHeight = data.GetLength(1) / numBlocksY;

        blockData = new float[numBlocksX, numBlocksY];
        for (int x = 0; x < numBlocksX; x++) {
            for (int y = 0; y < numBlocksY; y++) {
                blockData[x, y] = GetBlockVal(data, x, y);
            }
        }
        float median = Median(blockData);

        medianData = new bool[numBlocksX, numBlocksY];
        int aboveTotal = 0;
        for (int x = 0; x < numBlocksX; x++) {
            for (int y = 0; y < numBlocksY; y++) {
                medianData[x, y] = blockData[x, y] > median;
                if (medianData[x, y]) aboveTotal++;
            }
        }
    }

    // From: https://stackoverflow.com/questions/44264468/convert-rendertexture-to-texture2d
    public static Texture2D RendTexToTexture2D(RenderTexture rTex) {
        Texture2D tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    protected float GetBlockVal(float[,] data, int xBlockIndex, int yBlockIndex) {
        float total = 0;
        for (int x = xBlockIndex * blockPixelWidth; x < (xBlockIndex + 1) * blockPixelWidth; x++) {
            for (int y = yBlockIndex * blockPixelHeight; y < (yBlockIndex + 1) * blockPixelHeight; y++) {
                total += data[x, y];
            }
        }
        return total / (blockPixelWidth * blockPixelHeight);
    }

    protected float Median(float[,] data) {
        int numberCount = numBlocksX * numBlocksY;
        int halfIndex = numberCount / 2;

        // Flattens and sorts list
        List<float> dataList = data.Cast<float>().OrderBy(x => x).ToList();
        
        if ((numberCount % 2) == 0) {
            return (dataList.ElementAt(halfIndex) + dataList.ElementAt(halfIndex - 1)) / 2;
        } else {
            return dataList.ElementAt(halfIndex);
        }
    }

    public static int Compare(BlockMeanValueHash lhs, BlockMeanValueHash rhs) {
        if (lhs.numBlocksX != rhs.numBlocksX || lhs.numBlocksY != rhs.numBlocksY)
            throw new Exception("lhs and rhs are not of comparable sizes");

        int differentCells = 0;
        for (int x = 0; x < lhs.numBlocksX; x++) {
            for (int y = 0; y < lhs.numBlocksY; y++) {
                if (lhs.medianData[x, y] != rhs.medianData[x, y]) {
                    differentCells++;
                }
            }
        }
        Debug.Log(differentCells);
        return differentCells;
    }

    public static void GenCompImage(BlockMeanValueHash lhs, BlockMeanValueHash rhs, string fileName, int resultScalar = 10) {
        if (lhs.numBlocksX != rhs.numBlocksX || lhs.numBlocksY != rhs.numBlocksY)
            throw new Exception("lhs and rhs are not of comparable sizes");


        Texture2D tex = new Texture2D(lhs.numBlocksX * resultScalar, lhs.numBlocksY * resultScalar, TextureFormat.RGB24, false);
        
        for (int xCell = 0; xCell < lhs.numBlocksX; xCell++) {
            for (int yCell = 0; yCell < lhs.numBlocksY; yCell++) {

                bool isSame = lhs.medianData[xCell, yCell] == rhs.medianData[xCell, yCell];

                for (int x = xCell * resultScalar; x < (xCell + 1) * resultScalar; x++) {
                    for (int y = yCell * resultScalar; y < (yCell + 1) * resultScalar; y++) {
                        tex.SetPixel(x, y, isSame ? Color.white : Color.black);
                    }
                }
            }
        }

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + fileName + ".png", bytes);
    }

    public static void GenHashImage(BlockMeanValueHash bmvh, string fileName, int resultScalar = 10) {
        Texture2D tex = new Texture2D(bmvh.numBlocksX * resultScalar, bmvh.numBlocksY * resultScalar, TextureFormat.RGB24, false);

        for (int xCell = 0; xCell < bmvh.numBlocksX; xCell++) {
            for (int yCell = 0; yCell < bmvh.numBlocksY; yCell++) {

                for (int x = xCell * resultScalar; x < (xCell + 1) * resultScalar; x++) {
                    for (int y = yCell * resultScalar; y < (yCell + 1) * resultScalar; y++) {
                        tex.SetPixel(x, y, bmvh.medianData[xCell, yCell] ? Color.white : Color.black);
                    }
                }
            }
        }

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + fileName + ".png", bytes);
    }
}
