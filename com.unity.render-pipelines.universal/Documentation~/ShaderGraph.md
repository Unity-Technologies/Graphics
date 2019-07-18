# Working with Shader Graph

## Creating a Lit Shader

1. Create a new Asset by selecting __Create > Shader > 2D Renderer > Lit Sprite Graph__.

   ![](images\image_49.png)

   

2. Double-click the new Asset to open the __Shader Graph__.

   ![](images\image_50.png)
   

3. Attach the Sample Texture 2D Nodes to Color, Mask, and Normal. Change the type on the sampler connected to normal from Default to Normal.

   ![](images\image_51.png)
   

4. In the Blackboard on the left side, add three new Texture2D items which can be named anything. However, it is required to name the reference for MainTex as _MainTex to render Sprites. It is also recommended to name the references for Mask as _MaskTex and Normal as _NormalMap to match the Shader inputs used in this package.

   ![](images\image_52.png)

   Lastly the mode on Normal should be set to Bump so that if a normal map is not supplied, an appropriate default Texture is still available.

5. Drag MainTex, Mask, and Normal from the Blackboard to the Shader Graph work area.![](images\image_53.png)
   

6. Connect the MainTex, Mask, and Normal nodes to their respective Sample Texture2D nodes.

7. Select 'Save Asset' to save the Shader.

   ![](images\image_54.png)

    You can now assign new materials to the newly built Shader.