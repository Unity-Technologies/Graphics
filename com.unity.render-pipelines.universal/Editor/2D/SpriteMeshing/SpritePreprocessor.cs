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
        class MySpriteMetaData
        {


        }

        void SetPositions(NativeArray<Vector3> positions, float width, float height)
        {
            positions[0] = new Vector3(-width, -height);
            positions[1] = new Vector3(-width, height);
            positions[2] = new Vector3(0, height);
            positions[3] = new Vector3(0, -height);
            positions[4] = new Vector3(width, -height);
            positions[5] = new Vector3(width, height);
        }

        void SetUVs(NativeArray<Vector2> uvs)
        {
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, 1);
            uvs[2] = new Vector2(0.5f, 1);
            uvs[3] = new Vector2(0.5f, 0);
            uvs[4] = new Vector2(1, 0);
            uvs[5] = new Vector2(1, 1);
        }

        void SetIndices(NativeArray<ushort> indices)
        {
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 3;

            indices[3] = 3;
            indices[4] = 1;
            indices[5] = 2;

            indices[6] = 3;
            indices[7] = 2;
            indices[8] = 4;

            indices[9] = 4;
            indices[10] = 2;
            indices[11] = 5;
        }

        void DrawOutline(Vector2 pivot, List<Vector2[]> outlineList, float scale, Color color)
        {
            for (int outlineIndex = 0; outlineIndex < outlineList.Count; outlineIndex++)
            {
                Vector2[] outline = outlineList[outlineIndex];

                Vector2 prevPt = outline[outline.Length - 1];
                for (int i = 0; i < outline.Length; i++)
                {
                    Debug.DrawLine(pivot + (prevPt / scale), pivot + (outline[i] / scale), color, 120.0f);
                    prevPt = outline[i];
                }
            }
        }

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

        void CreateSplitSpriteMesh(Sprite sprite, Vector2 pivot, List<Vector2[]> outline, float pixelsPerUnit)
        {
            ShapeLibrary shapeLibrary = new ShapeLibrary();
            Texture2D texture = sprite.texture;

            RectInt rect = new RectInt(new Vector2Int((int)sprite.rect.position.x, (int)sprite.rect.position.y), new Vector2Int((int)sprite.rect.size.x, (int)sprite.rect.size.y));
            shapeLibrary.SetRegion(rect);

            GenerateMeshes.MakeShapes(shapeLibrary, texture, 1, 128); // We should do something with alpha and min area

            Debug.Log("Shapes generated: " + shapeLibrary.m_Shapes.Count);

            List<Vector3> allVertices = new List<Vector3>();
            List<Vector2> allUVs = new List<Vector2>();
            List<ushort> colorTriangles = new List<ushort>();
            List<ushort> depthTriangles = new List<ushort>();

            GenerateMeshes.TesselateShapes(shapeLibrary, (vertices, triangles, uvs, isOpaque) =>
            {
                int startingIndex = allVertices.Count;
                for (int i = 0; i < triangles.Length; i++)
                {
                    ushort index = (ushort)(triangles[i] + startingIndex);

                    if (isOpaque)
                        colorTriangles.Add(index);
                    //else
                    //    depthTriangles.Add(index);
                }

                Vector3 v3Pivot = new Vector3(pivot.x, pivot.y);
                if (isOpaque)
                {
                    for (int i = 0; i < vertices.Length; i++)
                        allVertices.Add(vertices[i] / pixelsPerUnit - v3Pivot);

                    for (int i = 0; i < uvs.Length; i++)
                        allUVs.Add(uvs[i]);
                }
            });


            NativeArray<Vector3> nativeVertices = ListToNativeArray<Vector3>(allVertices);
            NativeArray<Vector2> nativeUVs = ListToNativeArray<Vector2>(allUVs);
            NativeArray<ushort> nativeIndices = ListsToNativeArray<ushort>(colorTriangles, depthTriangles);

            sprite.SetVertexCount(allVertices.Count);
            sprite.SetVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position, nativeVertices);
            sprite.SetVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0, nativeUVs);
            sprite.SetIndices(nativeIndices);

            //int meshCount = sprite.GetSubmeshCount();
            //if (meshCount == 1)
            //    sprite.AddSubmesh();

            sprite.ModifySubmesh(0, allVertices.Count, 0, colorTriangles.Count, 0);
            //sprite.ModifySubmesh(0, allVertices.Count, colorTriangles.Count, depthTriangles.Count, 1);

            nativeVertices.Dispose();
            nativeUVs.Dispose();
            nativeIndices.Dispose();

        }

        void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            float width = (float)texture.width / (float)textureImporter.spritePixelsPerUnit;
            float height = (float)texture.height / (float)textureImporter.spritePixelsPerUnit;

            float halfWidth = 0.5f * width;
            float halfHeight = 0.5f * height;

            Vector2 spriteSize = new Vector2(width, height);

            if (textureImporter.spriteMeshType == SpriteMeshType.Tight && textureImporter.spriteGenerateDepthMesh)
            {
                if (textureImporter.spriteImportMode == SpriteImportMode.Single)
                {
                    Vector2 pivotOffset = textureImporter.spritePivot * spriteSize;  // This is probably wrong. Should use rect instead.
                    CreateSplitSpriteMesh(sprites[0], pivotOffset, textureImporter.spriteOutline, textureImporter.spritePixelsPerUnit);
                }
                else if (textureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    for (int i = 0; i < textureImporter.spritesheet.Length; i++)
                    {
                        Vector2 pivotOffset = textureImporter.spritesheet[i].pivot * spriteSize;   // This is probably wrong. Should use rect instead.
                        CreateSplitSpriteMesh(sprites[i], pivotOffset, textureImporter.spritesheet[i].outline, (float)textureImporter.spritePixelsPerUnit);
                    }
                }

                //for (int i = 0; i < sprites.Length; i++)
                //{
                //    NativeArray<Vector3> positions = new NativeArray<Vector3>(6, Allocator.Temp);
                //    NativeArray<Vector2> uv0 = new NativeArray<Vector2>(6, Allocator.Temp);
                //    NativeArray<ushort> indices = new NativeArray<ushort>(12, Allocator.Temp);

                //    SetPositions(positions, 0.5f * width, 0.5f * height);
                //    SetUVs(uv0);
                //    SetIndices(indices);

                //    sprites[i].SetVertexAttribute(UnityEngine.Rendering.VertexAttribute.Position, positions);
                //    sprites[i].SetVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0, uv0);
                //    sprites[i].SetIndices(indices);

                //    sprites[i].ModifySubmesh(0, 4, 0, 6, 0);

                //    int meshCount = sprites[i].GetSubmeshCount();
                //    if (meshCount == 1)
                //        sprites[i].AddSubmesh();
                //    sprites[i].ModifySubmesh(2, 4, 6, 6, 1);

                //    positions.Dispose();
                //    uv0.Dispose();
                //    indices.Dispose();
                //}
            }
        }
    }
}
