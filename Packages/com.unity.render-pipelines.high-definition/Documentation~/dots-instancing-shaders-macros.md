# DOTS Instancing shader macros reference

Unity provides the following access macros:

| **Access macro**                                             | **Description**                                              |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `UNITY_ACCESS_DOTS_INSTANCED_PROP(PropertyType, PropertyName)` | Returns the value loaded from `unity_DOTSInstanceData`. Refer to [Declare DOTS Instancing properties in a custom shader](dots-instancing-shaders-declare) for more information. Shaders that Unity provides use this version for DOTS Instanced built-in properties that don’t have a default value to fall back on. |
| `UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(PropertyType, PropertyName)` | Returns the same as `UNITY_ACCESS_DOTS_INSTANCED_PROP`, except if the most significant bit of the metadata value is zero, it returns a default value. The default value is the value of the regular material property with the same name as the DOTS Instanced property, which is why Shaders that Unity provides use the convention where DOTS Instanced properties have the same name as regular material properties. When using the default value, the access macro doesn't access `unity_DOTSInstanceData` at all. Shaders that Unity provides use this access macro for DOTS Instanced material properties, so the loads fall back to the value set on the material. |
| `UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_CUSTOM_DEFAULT(PropertyType, PropertyName, DefaultValue)` | Returns the same as `UNITY_ACCESS_DOTS_INSTANCED_PROP` unless the most significant bit of the metadata value is zero, in which case this macroreturns `DefaultValue` instead, and doesn't access `unity_DOTSInstanceData`. |
| `UNITY_DOTS_INSTANCED_METADATA_NAME(PropertyType, PropertyName)` | Returns the metadata value directly without accessing anything. This is useful for custom instance data loading schemes. |

