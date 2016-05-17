using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VectorFieldGenerator : EditorWindow {

    GUIContent m_Title = new GUIContent("VectorFieldGen");
    [SerializeField]
    Texture2D m_Texture = null;

    [MenuItem("VFXEditor/Generate 3D Texture")]
    public static void OpenConverter()
    {
        EditorWindow.GetWindow<VectorFieldGenerator>();
    }

    void OnGUI()
    {
        titleContent = m_Title;
        using (new GUILayout.VerticalScope())
        {
            m_Texture = (Texture2D)EditorGUILayout.ObjectField(m_Texture, typeof(Texture2D),false);
            if (GUILayout.Button("Export To 3DTexture Asset"))
            {
                if (m_Texture.width == (m_Texture.height * m_Texture.height))
                {
                    int wsize = m_Texture.height;
                    Texture3D VectorField = new Texture3D(wsize,wsize,wsize,m_Texture.format, false);

                    Color[] data = new Color[wsize*wsize*wsize];
                    for (int i = 0; i < wsize; i++)
                        for (int j = 0; j < wsize; j++)
                        for (int k = 0; k < wsize; k++) {
                            data[i + (j*wsize) + (k*wsize*wsize)] = m_Texture.GetPixel(i + (k*wsize), j);
                        }
                    VectorField.SetPixels (data);
                    VectorField.Apply ();


                    string path = EditorUtility.SaveFilePanelInProject("Save Texture", "3DTexture.asset", "asset", "Save a Texture");
                    AssetDatabase.CreateAsset(VectorField, path);
                }
                else
                    Debug.LogError("Can only convert textures of NxNxN unwrapped on U axis : here texture is : " + m_Texture.width+"x" +m_Texture.height);


            }
        }
    }
}
