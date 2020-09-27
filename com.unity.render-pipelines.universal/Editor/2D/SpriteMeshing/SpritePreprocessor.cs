using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.U2D;
using Unity;
using Unity.Collections;
using UnityEditor.Experimental.AssetImporters;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public class SpritePreprocessor : AssetPostprocessor
    {
        NativeArray<T> ListToNativeArray<T>(List<T> list) where T : struct
        {
            NativeArray<T> nativeArray = new NativeArray<T>(list.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < list.Count; i++)
                nativeArray[i] = list[i];
            return nativeArray;
        }

        NativeArray<T> ListsToNativeArray<T>(List<T> list0, List<T> list1) where T : struct
        {
            NativeArray<T> nativeArray = new NativeArray<T>(list0.Count + list1.Count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < list0.Count; i++)
                nativeArray[i] = list0[i];

            for (int i = 0; i < list1.Count; i++)
                nativeArray[i + list0.Count] = list1[i];

            return nativeArray;
        }

        Vector2[][] TransformOutline(Vector2[][] customOutline, Vector2 offset, float scale)
        {
            if (customOutline != null && customOutline.Length > 0)
            {
                Vector2[][] retOutline = new Vector2[customOutline.Length][];

                for(int outlineIndex=0; outlineIndex < customOutline.Length; outlineIndex++)
                {
                    Vector2[] outline = customOutline[outlineIndex];
                    Vector2[] transformedOutline = new Vector2[outline.Length];

                    for (int i = 0; i < outline.Length; i++)
                        transformedOutline[i] = scale * outline[i] + offset;

                    retOutline[outlineIndex] = transformedOutline;
                }

                return retOutline;
            }

            return null;
       }

        void AddGeometry(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector2 pivot, Vector2 multiSpriteOffset, float pixelsPerUnit, bool isOpaque, List<ushort> colorTriangles, List<ushort> depthTriangles, List<Vector3> allVertices)
        {
            int startingIndex = allVertices.Count;
            for (int i = 0; i < triangles.Length; i++)
            {
                ushort index = (ushort)(triangles[i] + startingIndex);

                if (!isOpaque)
                    colorTriangles.Add(index);
                else
                    depthTriangles.Add(index);
            }


            Vector3 v3Pivot = new Vector3(pivot.x + multiSpriteOffset.x, pivot.y + multiSpriteOffset.y);
            for (int i = 0; i < vertices.Length; i++)
                allVertices.Add((vertices[i]  - v3Pivot) / pixelsPerUnit);
        }

        void CreateSplitSpriteMesh(Sprite sprite, Vector2 pivot, Vector2 multiSpriteOffset, Vector2[][] customOutline, float pixelsPerUnit)
        {
            ShapeLibrary shapeLibrary = new ShapeLibrary();
            Texture2D texture = sprite.texture;

            RectInt rect = new RectInt(new Vector2Int((int)sprite.rect.position.x, (int)sprite.rect.position.y), new Vector2Int((int)sprite.rect.size.x, (int)sprite.rect.size.y));
            shapeLibrary.SetRegion(rect);

            GenerateMeshes.MakeShapes(shapeLibrary, customOutline, texture, 1, 4096); // 2048 will give us a pretty good balance of verts to area. 4096 is the same or better area as previously and less than half the vertices.

            Debug.Log("Shapes generated: " + shapeLibrary.m_Shapes.Count);

            List<Vector3> allVertices = new List<Vector3>();
            List<ushort> colorTriangles = new List<ushort>();
            List<ushort> depthTriangles = new List<ushort>();

            // Tesselate shapes made with MakeShapes
            GenerateMeshes.TesselateShapes(shapeLibrary, (vertices, triangles, uvs, isOpaque) => AddGeometry(vertices, triangles, uvs, pivot, Vector2.zero, pixelsPerUnit, isOpaque, colorTriangles, depthTriangles, allVertices));

            if (customOutline != null && customOutline.Length > 0)
                GenerateMeshes.TesselateShapes(customOutline, rect, (vertices, triangles, uvs, isOpaque) => AddGeometry(vertices, triangles, uvs, pivot, multiSpriteOffset, pixelsPerUnit, false, colorTriangles, depthTriangles, allVertices));

            NativeArray<Vector3> nativeVertices = ListToNativeArray<Vector3>(allVertices);
            NativeArray<ushort> nativeIndices = ListsToNativeArray<ushort>(colorTriangles, depthTriangles);

            sprite.SetVertexCount(allVertices.Count);
            sprite.SetVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position, nativeVertices);
            sprite.SetIndices(nativeIndices);
            sprite.ModifySubmesh(0, allVertices.Count, 0, colorTriangles.Count, 0);

            if (depthTriangles.Count > 0)
            {
                int meshCount = sprite.GetSubmeshCount();
                if (meshCount == 1)
                    sprite.AddSubmesh();
                sprite.ModifySubmesh(0, allVertices.Count, colorTriangles.Count, depthTriangles.Count, 1);
            }

            nativeVertices.Dispose();
            nativeIndices.Dispose();
        }

        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            if (sprites.Length == 0 || !(assetImporter is TextureImporter))
                return;

            TextureImporter textureImporter = (TextureImporter)assetImporter;

            float correctedPPU = sprites[0].rect.width / sprites[0].bounds.size.x;
            float textureRescale = correctedPPU / textureImporter.spritePixelsPerUnit;
            Vector2 textureCenter = new Vector2(0.5f * (float)texture.width, 0.5f * (float)texture.height);


            if (textureImporter.spriteMeshType == SpriteMeshType.Tight && textureImporter.spriteGenerateDepthMesh)
            {
                if (textureImporter.spriteImportMode == SpriteImportMode.Single)
                {
                    float width = texture.width / correctedPPU;
                    float height = texture.height / correctedPPU;
                    Vector2 spriteSize = new Vector2(width, height);
                    Vector2 pivotOffset = textureImporter.spritePivot * spriteSize;

                    Vector2[][] spriteOutline = textureImporter.spriteOutline;
                    Vector2[][] transformedOutline = TransformOutline(spriteOutline, textureCenter, textureRescale);
                    CreateSplitSpriteMesh(sprites[0], pivotOffset, Vector2.zero, transformedOutline, correctedPPU);
                }
                else if (textureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    for (int i = 0; i < textureImporter.spritesheet.Length; i++)
                    {
                        float width = sprites[i].rect.width / correctedPPU;
                        float height = sprites[i].rect.height / correctedPPU;
                        Vector2 min = sprites[i].rect.min;
                        Vector2 spriteSize = new Vector2(width, height);
                        Vector2 spriteCenter = 0.5f * spriteSize;
                        
                        Vector2 pivotOffset = new Vector2(sprites[i].pivot.x / sprites[i].rect.width, sprites[i].pivot.y / sprites[i].rect.height) * spriteSize;

                        Debug.Log("CorrectedPPU: " + correctedPPU);

                        Vector2[][] transformedOutline = TransformOutline(textureImporter.spritesheet[i].outline, spriteCenter + min, textureRescale);
                        CreateSplitSpriteMesh(sprites[i], pivotOffset, min, transformedOutline, correctedPPU);
                    }
                }
            }
        }
    }
}
