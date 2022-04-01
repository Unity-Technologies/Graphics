using NUnit.Framework;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEditor.TestTools.Graphics;
using System.Text;

public class BaseImageOptimization
{
    string assetsFolder = "Assets";
    string refImageFolder = "ReferenceImages";
    string refImageBaseFolder = "ReferenceImagesBase";

    string AssetsRefImagePath { get { return $"{assetsFolder}/{refImageFolder}"; } }
    string AssetsRefImageBasePath { get { return $"{assetsFolder}/{refImageBaseFolder}"; } }

    List<string> guids = new List<string>();
    List<string> refImages = new List<string>();
    List<string> refImagesDistinct = new List<string>();
    List<string> baseImages = new List<string>();
    List<string> newBaseImages = new List<string>();
    List<string> excessImages = new List<string>();

    bool deleteError = false;
    int areEqualTotal = 0;
    int initialRefImages = 0;

    public delegate void LogMessageReceived(string stackTrace, LogType type, float progress);
    public static event LogMessageReceived logMessageReceived;

    public BaseImageOptimization()
    {
    }

    public bool IsOptimized { get { return AssetDatabase.IsValidFolder(AssetsRefImageBasePath); } }

    public string Run()
    {
        return this.Optimization(false);
    }

    public string Optimization(bool firstImage)
    {
        logMessageReceived("Looking for reference images", LogType.Log, 0.1f);
        refImages = FindAssets(AssetsRefImagePath);
        initialRefImages = refImages.Count;
        logMessageReceived("All distinct reference images found\n", LogType.Log, 0.2f);
        EditorUtils.SetupReferenceImageImportSettings(refImages.ToArray());

        if (IsOptimized)
        {
            baseImages = FindAssets(AssetsRefImageBasePath);
            ReOptimization();
        }

        refImagesDistinct = refImages.Select(x => Path.GetFileName(x)).Distinct().ToList();
        logMessageReceived("Looking for images that can be used as base images", LogType.Log, 0.5f);

        if (firstImage) StrategySingleBaseFirstImage();
        else StrategySingleBaseMostCommonImage();

        if (newBaseImages.Count > 0)
        {
            logMessageReceived("All base images found", LogType.Log, 0.6f);
            AssetDatabase.Refresh();

            if (!AssetDatabase.IsValidFolder(AssetsRefImageBasePath)) AssetDatabase.CreateFolder(assetsFolder, refImageBaseFolder);

            AssetDatabase.Refresh();
            MoveImagesToBase();
            logMessageReceived("Adding the base images to the Assets/ReferenceImagesBase folder...", LogType.Log, 0.75f);
        }
        else logMessageReceived("No new base images found!", LogType.Log, 0.9f);

        RemoveFiles();

        logMessageReceived("Optimization done!", LogType.Log, 1f);
        CleanUp();
        return string.Format("Total files moved:{0} / {1}", areEqualTotal, initialRefImages);
    }

    private void StrategySingleBaseFirstImage()
    {
        foreach (string s in refImagesDistinct)
        {
            List<string> group = refImages.Where(x => x.EndsWith(s)).ToList();
            int areEqualGroup = 0;
            Texture2D basicImage = GetTexture(group[0]);
            for (int i = 1; i < group.Count; i++)
            {
                Texture2D currentImage = GetTexture(group[i]);
                if (Compare(basicImage, currentImage))
                {
                    if (!newBaseImages.Contains(group[0])) newBaseImages.Add(group[0]);
                    excessImages.Add(group[i]);
                    areEqualGroup++;
                    areEqualTotal++;
                }
            }
            logMessageReceived(string.Format("{0} / {1} : {2}", areEqualGroup, group.Count, group[0]), LogType.Log, 0.5f);
        }
    }

    public void StrategySingleBaseMostCommonImage()
    {
        Texture2D basicImage;
        Texture2D currentImage;
        foreach (string s in refImagesDistinct)
        {
            List<string> group = refImages.Where(x => x.EndsWith(s)).ToList();
            int areEqualGroup = 0;

            int[] maxCompare = new int[group.Count];
            for (int i = 0; i < group.Count; i++)
            {
                maxCompare[i] = 0;
                basicImage = GetTexture(group[i]);
                for (int j = 0; j < group.Count; j++)
                {
                    if (j == i) continue;
                    currentImage = GetTexture(group[j]);
                    if (Compare(basicImage, currentImage))
                    {
                        maxCompare[i]++;
                    }
                }
            }

            int maxIndex = 0;
            for (int i = 1; i < group.Count; i++)
            {
                if (maxCompare[i] > maxCompare[maxIndex])
                    maxIndex = i;
            }
            basicImage = GetTexture(group[maxIndex]);
            for (int i = 0; i < group.Count; i++)
            {
                if (i == maxIndex)
                {
                    if (!newBaseImages.Contains(group[maxIndex])) newBaseImages.Add(group[maxIndex]);
                }
                else
                {
                    currentImage = GetTexture(group[i]);
                    if (Compare(basicImage, currentImage))
                    {
                        excessImages.Add(group[i]);
                        areEqualGroup++;
                        areEqualTotal++;
                    }
                }
            }
            logMessageReceived(string.Format("{0} / {1} : {2}", areEqualGroup, group.Count, group[0]), LogType.Log, 0.5f);
        }
    }
    private List<string> FindAssets(string path)
    {
        List<string> assets = new List<string>();
        guids = AssetDatabase.FindAssets("t:texture2D", new[] { path }).ToList();
        foreach (string guid in guids)
            assets.Add(AssetDatabase.GUIDToAssetPath(guid));
        return assets;
    }
    private void ReOptimization()
    {
        foreach (string s in baseImages)
        {
            string assetName = Path.GetFileName(s);
            List<string> group = refImages.Where(x => x.EndsWith(assetName)).ToList();
            Texture2D baseImage = GetTexture(s);
            for (int i = 0; i < group.Count; i++)
            {
                Texture2D currentImage = GetTexture(group[i]);
                if (Compare(baseImage, currentImage))
                {
                    excessImages.Add(group[i]);
                    if (refImages.Contains(group[i])) refImages = refImages.Where(e => e != group[i]).ToList();
                    logMessageReceived(string.Format("{0} found in ReferenceImages folder", assetName), LogType.Log, 0.4f);
                }
            }
        }
    }
    private void MoveImagesToBase()
    {
        foreach (string s in newBaseImages)
        {
            string newPath = $"{AssetsRefImageBasePath}/{Path.GetFileName(s)}";
            var moveResult = AssetDatabase.ValidateMoveAsset(s, newPath);
            if (moveResult == "")
                AssetDatabase.MoveAsset(s, newPath);
            else
            {
                logMessageReceived($"Couldn't move because {moveResult}", LogType.Error, 0.7f);
                excessImages = excessImages.Where(e => e != s).ToList();
            }
        }
        AssetDatabase.Refresh();
        logMessageReceived("Images added to the base folder", LogType.Log, 0.7f);

    }
    private void RemoveFiles()
    {
        if (!excessImages.Any()) return;
        logMessageReceived("Removing all excess images...", LogType.Log, 0.8f);
        foreach (string s in excessImages)
        {
            bool deleteResult = AssetDatabase.DeleteAsset(s);
            if (!deleteResult)
            {
                logMessageReceived($"Couldn't delete {s}", LogType.Error, 0.9f);
                deleteError = true;
            }
        }
        if (!deleteError) logMessageReceived("All excess images removed", LogType.Log, 0.9f);
        AssetDatabase.Refresh();
    }

    private void CleanUp()
    {
        newBaseImages = new List<string>();
        excessImages = new List<string>();
        refImages = new List<string>();
        baseImages = new List<string>();
    }

    private Texture2D GetTexture(string imagePath)
    {
        var image = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
        if (!image.isReadable) throw new ArgumentException("Image is not Assets-readable : " + imagePath);

        return image;
    }

    private bool Compare(Texture2D image1, Texture2D image2)
    {
        bool success = true;
        try
        {
            ImageAssert.AreEqual(image1, image2, saveFailedImage: false);
        }
        catch (AssertionException)
        {
            success = false;
        }

        return success;
    }
}
