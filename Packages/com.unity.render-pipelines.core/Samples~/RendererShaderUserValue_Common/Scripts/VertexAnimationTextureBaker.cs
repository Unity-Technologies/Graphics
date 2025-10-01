using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/*
 * In the Rig Import settings on the sceneObject
 * "Optimize Game Objects" parameter needs to be disable for the baking to work
 * 
 */
public class VertexAnimationTextureBaker : EditorWindow
{
    public GameObject sceneObject;             
    public List<AnimationClip> clipsToBake = new List<AnimationClip>();
    public int sampleRate = 30;

    [MenuItem("Tools/Vertex Animation Texture Baker")]
    public static void ShowWindow()
    {
        GetWindow<VertexAnimationTextureBaker>("Vertex Animation Texture Baker");
    }

    void OnGUI()
    {
        GUILayout.Label("Vertex Animation Texture Baker", EditorStyles.boldLabel);
        sceneObject = (GameObject)EditorGUILayout.ObjectField("Scene Object", sceneObject, typeof(GameObject), true);
        sampleRate = EditorGUILayout.IntField("Sample Rate (FPS)", sampleRate);

        // Draw the list of clips
        SerializedObject so = new SerializedObject(this);
        SerializedProperty prop = so.FindProperty("clipsToBake");
        EditorGUILayout.PropertyField(prop, true);
        so.ApplyModifiedProperties();

        if (GUILayout.Button("Bake VAT Array"))
        {
            if (sceneObject == null || clipsToBake.Count == 0)
            {
                Debug.LogError("Assign a scene object and at least one AnimationClip.");
                return;
            }
            BakeVATArray(sceneObject, clipsToBake, sampleRate);
        }
    }

    void BakeVATArray(GameObject target, List<AnimationClip> clips, int fps)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        SkinnedMeshRenderer smr = target.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("No SkinnedMeshRenderer found!");
            return;
        }

        Mesh mesh = new Mesh();
        smr.BakeMesh(mesh);
        int vertexCount = mesh.vertexCount;

        // Find maximum frame count across all clips
        int maxFrames = 0;
        List<int> frameCounts = new List<int>();
        foreach (var clip in clips)
        {
            int fc = Mathf.CeilToInt(clip.length * fps);
            frameCounts.Add(fc);
            if (fc > maxFrames) maxFrames = fc;
        }

        // Create Texture2DArray in RGBAHalf
        Texture2DArray vatArray = new Texture2DArray(vertexCount, maxFrames, clips.Count, TextureFormat.RGBAHalf, false);

        for (int c = 0; c < clips.Count; c++)
        {
            AnimationClip clip = clips[c];
            int frameCount = frameCounts[c];
            Color[] pixels = new Color[vertexCount * maxFrames];

            for (int f = 0; f < frameCount; f++)
            {
                float time = (f / (float)frameCount) * clip.length;
                clip.SampleAnimation(target, time);
                smr.BakeMesh(mesh);

                for (int v = 0; v < vertexCount; v++)
                {
                    Vector3 pos = mesh.vertices[v];
                    pixels[f * vertexCount + v] = new Color(pos.x, pos.y, pos.z, 1f);
                }
            }

            // Fill unused frames with last frame
            if (frameCount < maxFrames)
            {
                for (int f = frameCount; f < maxFrames; f++)
                {
                    for (int v = 0; v < vertexCount; v++)
                    {
                        pixels[f * vertexCount + v] = pixels[(frameCount - 1) * vertexCount + v];
                    }
                }
            }

            // Encode frameCount into alpha of first pixel
            pixels[0].a = frameCount;

            // Apply to slice
            Texture2D tempTex = new Texture2D(vertexCount, maxFrames, TextureFormat.RGBAHalf, false);
            tempTex.SetPixels(pixels);
            tempTex.Apply();
            Graphics.CopyTexture(tempTex, 0, 0, vatArray, c, 0);
            Object.DestroyImmediate(tempTex);
        }

        // Save VAT array
        string path = "Assets/VertexAnimationTextureArray.asset";
        AssetDatabase.CreateAsset(vatArray, path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"VAT Array (RGBAHalf) baked: {path} with {clips.Count} clips. Max frames = {maxFrames}");
    }
}