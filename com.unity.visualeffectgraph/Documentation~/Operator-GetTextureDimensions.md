# Get Texture Dimensions

Menu Path : **Operator > Sampling > Get Texture Dimensions**

The Get Texture Dimensions Operator allows you to get the dimensions of a given texture.

This Operator handles every supported texture type in the Visual Effect Graph. It infers the input type automatically when you attach a texture to the input property. You can also manually specify the input type in the [Operator configuration](#operator-configuration).

## Operator properties

| **Input** | **Type**                                 | **Description**                         |
| --------- | ---------------------------------------- | --------------------------------------- |
| **Tex**   | [Configurable](#operator-configuration). | The texture to get the dimensions from. |

| **Output** | **Type** | **Description**                                              |
| ---------- | -------- | ------------------------------------------------------------ |
| **width**  | uint     | The width of the texture.                                    |
| **height** | uint     | The height of the texture.                                   |
| **depth**  | uint     | The depth of the texture 3D (for Texture3D types only).      |
| **count**  | uint     | The layer count in the texture array (for Texture2DArray and TextureCubeArray only). |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. Use the drop-down to select the type for the **Tex** port. For the list of types this property supports, see [Available types](#available-types).

### Available types

All types of textures are available for your **Texture** ports:

- **Texture2D**
- **Texture3D**
- **Texture2DArray**
- **TextureCube**
- **TextureCubeArray**