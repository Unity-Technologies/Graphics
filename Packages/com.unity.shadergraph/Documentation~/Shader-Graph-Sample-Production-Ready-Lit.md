# Lit Shaders
Both URP and HDRP come with code-based shaders. The most commonly used shader for each of the SRPs is called Lit. For projects that use it, it’s often applied to just about every mesh in the game. Both the HDRP and URP versions of the Lit shader are very full-featured.  However, sometimes users want to add additional features to get just the look they’re trying to achieve, or remove unused features to optimize performance. For users who aren’t familiar with shader code, this can be very difficult.

For that reason, we’ve included Shader Graph versions of the Lit shader for both URP and HDRP in this sample pack. Users will be able to make a copy of the appropriate Shader Graph Lit shader, and then change any material that’s currently referencing the code version of the Lit shader with the Shader Graph version. All of the material settings will correctly be applied and continue to work.  They’ll then be able to make changes to the Shader Graph version as needed.

Please note that most *but not all* of the features of the code-based shaders are duplicated in the Shader Graph versions. Some lesser-used features may be missing from the Shader Graph versions due to the differences in creating shader with Shader Graph vs creating them with code.

Also note - If you’re going to use the Lit shader *as is*, we recommend sticking with the code version.  Only swap out the shader for the Shader Graph version if you’re making changes.  We also recommend removing unused features from the Shader Graph version for better performance.  For example, if you’re not using Emissive or Detail Maps, you can remove those parts of the shader (both graph nodes and Blackboard parameters) for faster build times and better performance. The real power of Shader Graph is its flexibility and how easy it is to change, update, and improve shaders.

#### URP Lit
Just like the code version, this shader offers the Metallic workflow or the Specular workflow. Shaders can be either opaque or transparent, and there are options for Alpha Clipping, Cast Shadows, and Receive Shadows. For the main surface, users can apply a base map, metallic or specular map, normal map, height map, occlusion map, and emission map. Parameters are available to control the strength of the smoothness, height, normal, and occlusion and control the tiling and offset of the textures.

Users can also add base and normal detail maps and mask off where they appear using the mask map.

For more details on each of the parameters in the shader, refer to the [Lit Shader documentation for URP](http://UnityEditor.Rendering.Universal.ShaderGUI.LitShader).

##### Shader Variant Limit
In order to be able to use this shader, you’ll need to increase the Shader Variant Limit to at least 513.  This should be done on both the Shader Graph tab in Project Settings as well as the Shader Graph tab in the Preferences.

##### Custom Editor GUI
In order to create a more compact and user-friendly GUI in the material, this shader uses the same Custom Editor GUI that the code version of the Lit shader uses.  Open the Graph Inspector and look at the Graph Settings. At the bottom of the list, you’ll see the following under Custom Editor GUI:

        UnityEditor.Rendering.Universal.ShaderGUI.LitShader

This custom GUI script enables the small texture thumbnails and other features in the GUI. If you need to add or remove parameters in the Blackboard, we recommend removing the Custom Editor GUI and just using Shader Graph’s default material GUI instead.  The custom GUI depends on the existence of many of the Blackboard parameters and won’t function properly if they’re removed.

#### HDRP Lit
Just like the code version, this shader offers opaque and transparent options. It supports Pixel displacement (Parallax Occlusion mapping) and all of the parameters that go with it. (It does not support Material Types other than standard.) For the main surface, users can apply a base map, mask map, normal map, bent normal map, and height map. Options are also available to use a detail map and emissive map.

For more details on each of the parameters in the shader, refer to the [Lit Shader documentation for HDRP](http://UnityEditor.Rendering.Universal.ShaderGUI.LitShader).

##### Custom Editor GUI
In order to create a more compact and user-friendly GUI in the material, this shader uses the same Custom Editor GUI that the code version of the Lit shader uses.  Open the Graph Inspector and look at the Graph Settings. At the bottom of the list, you’ll see the following under Custom Editor GUI:

        Rendering.HighDefinition.LitGUI

This custom GUI script enables the small texture thumbnails and other features in the GUI. If you need to add or remove parameters in the Blackboard, we recommend removing the Custom Editor GUI and just using Shader Graph’s default material GUI instead.  The custom GUI depends on the existence of many of the Blackboard parameters and won’t function properly if they’re removed.