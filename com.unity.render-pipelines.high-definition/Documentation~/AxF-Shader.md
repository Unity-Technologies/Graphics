# AxF Shader

The AxF Shader allows you to render X-Rite AxF materials in the High Definition Render Pipeline (HDRP). AxF is a standardized format that allows for the exchange of material appearance data. The production of an AxF file typically involves an authoring suite that includes real-world material measurements from which it can produce various textures and model properties.

![](Images/AxFShader1.png)

To translate AxF file data into Material properties and data that HDRP's AxF Shader can understand and render, Unity uses the **AxF Importer** package. You are not required to use the importer and can instead use the Inspector to assign values yourself. However, the AxF Shader is specifically designed to work with data the AxF Importer translates from AxF files. Unity currently does not provide a method to author certain Assets that AxF Materials rely on to accurately portray the real-world material they represent. This means that, if you create the AxF Material manually, you may not be able to reproduce certain results available from an imported AxF file.

## Importing and Creating an AxF Material

Although it is possible to create an AxF Material from scratch in Unity, you should instead use an external authoring tool, such as X-Rite’s Total Appearance Capture (TAC™) Ecosystem, to create an AxF file and then import the result into Unity. If you install the AxF Importer package, Unity automatically imports AxF files as AxF Materials.

The AxF importer is available in Unity Enterprise for Product Lifecycle. For more information, contact your Unity sales representative. When you download the AxF Importer package, use the Package Manager to install it locally. For information on how to install local packages, see [Installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html).

### Using the AxF Importer package

When you import an AxF file, you cannot modify any of its properties. If you want to edit your AxF Material, and have installed the AxF Importer package, you can create an editable copy of the imported AxF Material. To do this:

1. Select an AxF file in your Unity Project and view it in the Inspector. 
2. Right click on the **Imported Object** header area and select **Create AxF Material From This** from the context menu.

This process does not duplicate the Textures and other resources that the original AxF file uses. Instead, the duplicate Material references the original file's Textures and resources, but every value in its Inspector is editable.

**Note**: If you manually reference a new Texture, be sure that it uses the same encoding as the Texture it replaces. Otherwise, inconsistent input values produce unpredictable or flawed rendering.

### Creating AxF Materials from scratch

New Materials in HDRP use the [Lit Shader](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Lit-Shader.html) by default. To create an AxF Material from scratch, create a Material and then make it use the AxF Shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.
2. Right-click the Asset Window and select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Click the **Shader** drop-down at the top of the Material Inspector, and select **HDRP > AxF**.

It is possible to copy AxF Material properties from an imported AxF Material to your newly created AxF Material. To do this:

1. Select an AxF file in your Unity Project and view it in the Inspector. 
2. In the header area, right click on the **Shader** drop-down and select **Copy AxF Material Properties** from the context menu.
3. Select your new AxF Material and view it in the Inspector.
4. In the header area, right click on the **Shader** drop-down and select **Paste AxF Material Properties** from the context menu.

## Properties

### Surface Options

**Surface Options** control the overall look of your Material's surface and how Unity renders the Material on screen.

Note: The AxF Importer imports every Texture as half float, linear, sRGB gamut (when representing color).

| **Property**         | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Surface Type**     | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.html). |
| **- Rendering Pass** | Use the drop-down to set the rendering pass that HDRP processes this Material in. For more information on this property, see the [Surface Type documentation](Surface-Type.html). |
| **Double-Sided**     | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |
| **Receive Decals**   | Enable the checkbox to allow HDRP to draw decals on this Material’s surface. |
| **Receive SSR**      | Enable the checkbox to make HDRP include this Material when it processes the screen space reflection pass. |

### Surface Inputs

| **Property**          | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| **Material Tiling U** | Sets the tile rate along the x-axis for every Texture in the **Surface Inputs** section. HDRP uses this value to tile the Textures along the x-axis of the Material’s surface, in object space. |
| **Material Tiling V** | Sets the tile rate along the y-axis for every Texture in the **Surface Inputs** section. HDRP uses this value to tile the Textures along the y-axis of the Material’s surface, in object space. |
| **BRDF Type**         | Controls the main AxF Material representation.<br/>&#8226; **SVBRDF**: For information on the properties Unity makes visible when you select this option, see [BRDF Type - SVBRDF](https://docs.google.com/document/d/1_Oq2hsx3J7h8GHKoQM_8qf6Ip5VlHv_31K7dYYVOEmU/edit#heading=h.f1msh9g44mev).<br/>&#8226;**CAR_PAINT**: For information on the properties Unity makes visible when you select this option, see [BRDF Type - CAR_PAINT](https://docs.google.com/document/d/1_Oq2hsx3J7h8GHKoQM_8qf6Ip5VlHv_31K7dYYVOEmU/edit#heading=h.eorkre6buegg). |

#### BRDF Type - SVBRDF

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Diffuse Type**        | The **SVBRDF BRDF Type** representation supports two diffuse reflectance models which are:<br/>&#8226; **LAMBERT**: A simple diffuse model where reflectance is constant in every view direction. This model uses a single albedo color property.<br/>&#8226; **OREN_NAYAR**: This diffuse reflectance model better represents rough diffuse surfaces. This model uses a roughness property and an albedo color property. |
| **Specular Type**       | The **SVBRDF BRDF Type** supports multiple specular reflectance models which are:<br/>&#8226; **WARD**: A microfacet-derived model that describes the surface microscopically by a Gaussian distribution of microfacets orientation. This option has multiple variants and a configurable Fresnel effect. For information on these properties, see **Ward Variants** and **Fresnel Variant** respectively. <br/>&#8226; **BLINN_PHONG**: An anisotropic version of the classic Blinn-Phong empirical reflectance model. The specular falloff is proportional to the cosine of the median angle between view and light directions (instead of a Gaussian). This option has multiple variants, but does not include a Fresnel effect. For information on the variants, see **Blinn Variant**. <br/>&#8226; **COOK_TORRANCE**: A microfacet-derived model that is contrary to **WARD**. This model is isotropic and the microfacet distribution is Gaussian.<br/>&#8226; **GGX**: Like HDRP’s [Lit Material](Lit-Shader.html), a model with a slow specular falloff that creates highlights that decrease slower, over a larger area, than Gaussian or cosine models. |
| **Fresnel Variant**     | The **WARD Specular Type** allows you to specify how HDRP calculates the Fresnel effect for the Material:<br/>&#8226; **NO_FRESNEL**: Removes the Fresnel effect from the Material.<br/>&#8226; **FRESNEL**: Uses an accurate equation to calculate the Fresnel.<br/>&#8226; **SCHLICK**: Uses an approximation to calculate the Fresnel. This is a less resource intensive calculation to process than the **FRESNEL** option.<br/><br/>This property is only visible if you set **Specular Type** to **WARD**. |
| **Ward Variant**        | The **WARD Specular Type** supports multiple sub-variants, mainly based on a change to a normalization term:<br/>&#8226; **WARD**: Based on the original 1992 Ward et al. paper.<br/>&#8226; **DUER**: Based on a modification by Duer et al. 2004 paper.<br/>&#8226; **GEISLERMORODER**: Based on a modification by Geisler and Moroder 2010. Mainly developed for the Radiance predictive rendering software, this model is the most advanced form of Ward and is more realistic especially at grazing angles.<br/><br/>This property is only visible if you set **Specular Type** to **WARD**. |
| **Blinn Variant**       | The **BLINN Specular Type** supports multiple variants:<br/>&#8226; **BLINN**: This is the classic Blinn-Phong model and anisotropy has no effect on it.<br/>&#8226; **ASHIKHMIN_SHIRLEY**: An anisotropic version of the model based on Ashikhmin and Shirley.<br/><br/>This property is only visible if you set **Specular Type** to **BLINN_PHONG**. |
| **Diffuse Color**       | Specifies an RGB Texture that controls the color of the Material. To assign a Texture to this field, click the radio button and, in the **Select Texture** window, select the Texture. |
| **Specular Color**      | Specifies an RGB Texture that acts as a multiplier for the color of the specular reflectance of the Material. Note: This property does not represent what is commonly termed "specular color" (aka "F0", Fresnel reflectance at a perpendicular direction to the surface) in the usual PBR workflow, that is, it does not represent the same thing as the **Specular Color** [Material Type](Material-Type.html) does. Instead this represents the color tint that HDRP multiplies with the result of the specular BRDF evaluation. |
| **Specular Lobe**       | Specifies a Texture that encodes the roughness of the Material surface. Depending on which **Specular Type** you select, HDRP uses different color channels:<br/>&#8226; **Isotropic specular models**: HDRP uses the red color channel.<br/>&#8226; **Anisotropic specular models**: HDRP uses the red and green color channels.<br/><br/>HDRP interprets the values in the Texture as between **0** and **1**. When compared to the Lit Shader's GGX, a comparable AxF roughness (when **Specular Model** is set to **GGX**) is as follows:<br/>`axf_roughness = (1 - hdrp_lit_user_smoothness)^2` |
| **Specular Lobe Scale** | A multiplier for the **Specular Lobe** roughness value. Lower values make your Material smoother overall and less anisotropic.<br/>Note: AxF files do not include this value. Therefore, if you want your Material to be consistent with the appearance intended from the AxF file, you should set this value to **1**. |
| **Fresnel**             | Specifies a Texture (red channel only) that defines the reflectance of the specular model of the surface at **0** from the normal. |
| **Normal**              | Specifies an RGB Texture that defines a normal map, in tangent space. HDRP uses values in the red, green, and blue channels of this Texture as a three component normalized vector value. This is instead of the usual RG or AG that only describes two components of the vector. |
| **Alpha**               | Specifies a Texture (red channel only) that defines the transparency of the Material surface. The Material uses the red channel of this Texture as the transparency value. |
| **Is Anisotropic**      | Indicates that the Material supports anisotropy.             |
| **- Anisotropy Angle**  | Specifies a Texture (red channel only) that defines a tangent frame rotation map. This map determines the x and y-axis orientation of the tangent frame against which HDRP aligns the anisotropic roughness properties.<br/>HDRP maps the red color channel from its original range (**0** to **1**) to between **-pi / 2** and **pi / 2**.<br/>Note: HDRP determines the original tangent frame for the x-axis by the interpolated vertex tangent and for the z-axis by the interpolated vertex normal, both coming from the Mesh geometry. |
| **Enable Clearcoat**    | Indicates whether there is a clear coat on the Material surface or not. |
| **- Clearcoat Color**   | Specifies an RGB Texture that adjusts the light that enters and leaves through the coat. This acts as a tint for the clear coat. |
| **- Clearcoat Normal**  | Specifies an RGB Texture that defines a normal map, in tangent space, for the coat. HDRP uses values in the red, green, and blue channels of this Texture as a three component normalized vector value. This is instead of the usual RG or AG that only describes two components of the vector. |
| **- Enable Refraction** | Indicates whether the clear coat is refractive. If you enable this checkbox, HDRP uses angles refracted by the clear coat to evaluate the undercoat of the Material surface. |
| **- - Clearcoat IOR**   | Specifies a Texture (red channel only) that implicitly defines the index of refraction (IOR) for the clear coat by encoding it to a monochromatic (single value) F0 (aka as specular color or Fresnel reflectance at 0 degree incidence. This also assumes the coat interfaces with air). As such, the value is in the range of **0** to **1** and HDRP calculates the final IOR as:<br/>`IOR =  (1.0 + squareRoot(R) ) / (1.0 - squareRoot(R))`<br/>Where **R** is the normalized value in the red color channel of this Texture.  Note: HDRP uses this IOR for both coat refraction and, if enabled, transmission and reflectance calculations through and on the coat. Therefore, you must always assign a Texture to this property when you enable clear coat. |

#### BRDF Type - CAR_PAINT

| **Property**                              | **Description**                                              |
| ----------------------------------------- | ------------------------------------------------------------ |
| **BRDF Color**                            | Specifies an RGB Texture that captures angular-dependent variations of the car paint tint, similar to modeling iridescent paint colors. Each of the UV axes represent the following:The u-axis represents the variations depending on the angle between the median vector (the vector halfway between the view direction and light direction) and the surface normal.The v-axis represents the angle between the view or the light and this median (aka as the “difference” angle). The property space starts at the top-left, where for **U** equals **1** (the maximum range), the angle corresponds to **pi / 2**. |
| **BRDF Color Scale**                      | A multiplier for the **BRDF Color** color value. Note: AxF files do not include this value. Therefore, if you want your Material to be consistent with the appearance intended from the AxF file, you should set this value to **1**. |
| **BRDF Color Table Diagonal Clamping**    | Indicates whether Unity should clamp accesses to the **BRDF Color** map. Some BRDF Color tables have a zero (all black) lower half below a central diagonal region. Usually, on correctly measured Materials, the combination of index of refraction (IOR) maps and refractive options prevents the Shader from accessing zero values. The Unity AxF Importer populates values (see the restriction below) that HDRP uses to force the Shader to never access the zero regions. To do this, HDRP linearly remaps the angular domain in non-black regions of the Texture. |
| **- BRDF Color Map UV Scale Restriction** | Specifies the scale of the restriction HDRP applies when it clamps the BRDF Color table. If you use lower values, this prevents the Shader from reaching higher angular ranges (nearer to **pi / 2**). If you set both of these values to **1**, this is equivalent to disabling the diagonal clamping feature. |
| **BTF Flake Color Texture2DArray**        | Specifies the measured angular slices data for the flakes. This set of slices represents a sort of a Bidirectional Texture Function (BTF). You should not assign this property manually. Instead allow the AxF Importer to assign this property. |
| **BTF Flake Scale**                       | A multiplier for the flake reflectance intensity. Note: AxF files do not include this value. Therefore, if you want your Material to be consistent with the appearance intended from the AxF file, you should set this value to **1**. |
| **Flakes Tiling**                         | A multiplier that applies specific tiling for the Flakes texture slices. |
| **ThetaFI Slice LUT**                     | Specifies the look-up table that converts angular ranges to slices. You should not assign this property manually. Instead allow the AxF Importer to assign this property. |
| **Diffuse coeff**                         | The diffuse lobe coefficient. The **CAR_PAINT** AxF model uses a hybrid multi-lobe model. A summary of this model is:<br/>`BRDFColor * (Lambert + Cook-Torrance) + Flakes BTF`<br/>This controls the diffuse albedo for the Lambert diffuse reflectance part of the model. |
| **CT Lobes F0s**                          | The Fresnel reflectance at 0 degrees of incidence for each Cook-Torrance lobe. |
| **CT Lobes coeff**                        | The reflectance multiplier coefficients for the Cook-Torrance lobes specular response. |
| **CT Lobes spreads**                      | The roughness of the Cook-Torrance lobes.                    |
| **Enable Clearcoat**                      | Indicates whether there is a clear coat on the Material surface or not. |
| **- Clearcoat Normal**                    | Specifies an RGB Texture that defines a normal map, in tangent space, for the coat. HDRP uses values in the red, green, and blue channels of this Texture as a three component normalized vector value. This is instead of the usual RG or AG that only describes two components of the vector.<br/>Use this property to simulate the car paint **orange peel** effect.<br/>Note that the surface under the coat never has its own normal map: it instead uses the vertex interpolated normal from the Mesh geometry. |
| **- Clearcoat IOR**                       | Controls the IOR for the clear coat. Unlike **SVBRDF**, this is a single scalar value, not a Texture. Note: HDRP uses this IOR for both coat refraction and, if enabled, transmission and reflectance calculations through and on the coat. |
| **- Enable Refraction**                   | Indicates whether the clear coat is refractive. If you enable this checkbox, HDRP uses angles refracted by the clear coat to evaluate the undercoat of the Material surface. |

### Advanced options

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Enable GPU instancing**    | Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP cannot render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you can not[ static-batch](https://docs.unity3d.com/Manual/DrawCallBatching.html) GameObjects that have an animation based on the object pivot, but the GPU can instance them. |
| **Add Precomputed Velocity** | Enable the checkbox to use precomputed velocity information stored in an Alembic file. |