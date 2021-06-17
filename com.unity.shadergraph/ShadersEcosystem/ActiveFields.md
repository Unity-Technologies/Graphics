
# Active Fields

This isn't fleshed out yet, but more a placeholder for detailing deeper notes and thoughts on how active fields can be worked around.

Currently there's a lot of small intricacies in how this is built. For instance, some structs are built and filtered part way through assembling active fields. If these structs were built later then different parameters would exist on structs.

Currently the pass required fields are added after structs are baked, meaning the required fields aren't added to some structs (like SurfaceDescriptionInputs) unless they're actually used (such as uv1).

Additionally, active fields currently contain defines and a few other interesting fields. It's possible shader graph doesn't need any of the data from the target beyond what's in the blocks, and the linker can deduce the information from the blocks provided by shader graph / surface shaders.

Here's data from a quick run of URP to show how these fields are changed over time:

---
Pre SG: Data returned by `Target.GetFields()` that is filtered by SG

```
Name `"graphVertex"`; Type `null`; Define `"FEATURES_GRAPH_VERTEX"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"graphPixel"`; Type `null`; Define `"FEATURES_GRAPH_PIXEL"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalDropOffTS"`; Type `null`; Define `"_NORMAL_DROPOFF_TS 1"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `"_NORMALMAP 1"`	UnityEditor.ShaderGraph.FieldDescriptor

```

---
Post SG: Fields SG adds after visiting block fields and nodes
```
Name `"graphVertex"`; Type `null`; Define `"FEATURES_GRAPH_VERTEX"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"graphPixel"`; Type `null`; Define `"FEATURES_GRAPH_PIXEL"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalDropOffTS"`; Type `null`; Define `"_NORMAL_DROPOFF_TS 1"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `"_NORMALMAP 1"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Position"`; Type `null`; Define `"VERTEXDESCRIPTION_POSITION"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `"VERTEXDESCRIPTION_NORMAL"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Tangent"`; Type `null`; Define `"VERTEXDESCRIPTION_TANGENT"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"BaseColor"`; Type `null`; Define `"SURFACEDESCRIPTION_BASECOLOR"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalTS"`; Type `null`; Define `"SURFACEDESCRIPTION_NORMALTS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Emission"`; Type `null`; Define `"SURFACEDESCRIPTION_EMISSION"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Metallic"`; Type `null`; Define `"SURFACEDESCRIPTION_METALLIC"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Smoothness"`; Type `null`; Define `"SURFACEDESCRIPTION_SMOOTHNESS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Occlusion"`; Type `null`; Define `"SURFACEDESCRIPTION_OCCLUSION"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"TangentSpaceNormal"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"ObjectSpaceNormal"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"ObjectSpaceTangent"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"ObjectSpacePosition"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"WorldSpacePosition"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Position"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Tangent"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"BaseColor"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalTS"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Emission"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Metallic"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Smoothness"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Occlusion"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor

```
This is what's used to filter all structs

---
Full: Full data before running the rest of code gen, mostly just:
- `GenerationUtils.AddRequiredFields(pass.requiredFields, activeFields.baseInstance);`
```
Name `"graphVertex"`; Type `null`; Define `"FEATURES_GRAPH_VERTEX"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"graphPixel"`; Type `null`; Define `"FEATURES_GRAPH_PIXEL"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalDropOffTS"`; Type `null`; Define `"_NORMAL_DROPOFF_TS 1"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `"_NORMALMAP 1"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Position"`; Type `null`; Define `"VERTEXDESCRIPTION_POSITION"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `"VERTEXDESCRIPTION_NORMAL"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Tangent"`; Type `null`; Define `"VERTEXDESCRIPTION_TANGENT"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"BaseColor"`; Type `null`; Define `"SURFACEDESCRIPTION_BASECOLOR"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalTS"`; Type `null`; Define `"SURFACEDESCRIPTION_NORMALTS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Emission"`; Type `null`; Define `"SURFACEDESCRIPTION_EMISSION"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Metallic"`; Type `null`; Define `"SURFACEDESCRIPTION_METALLIC"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Smoothness"`; Type `null`; Define `"SURFACEDESCRIPTION_SMOOTHNESS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Occlusion"`; Type `null`; Define `"SURFACEDESCRIPTION_OCCLUSION"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"TangentSpaceNormal"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"ObjectSpaceNormal"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"ObjectSpaceTangent"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"ObjectSpacePosition"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"WorldSpacePosition"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Position"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Normal"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Tangent"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"BaseColor"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"NormalTS"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Emission"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Metallic"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Smoothness"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"Occlusion"`; Type `null`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"uv1"`; Type `"$precision4"`; Define `"ATTRIBUTES_NEED_TEXCOORD1"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"uv2"`; Type `"$precision4"`; Define `"ATTRIBUTES_NEED_TEXCOORD2"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"positionWS"`; Type `"$precision3"`; Define `"VARYINGS_NEED_POSITION_WS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"normalWS"`; Type `"$precision3"`; Define `"VARYINGS_NEED_NORMAL_WS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"tangentWS"`; Type `"$precision4"`; Define `"VARYINGS_NEED_TANGENT_WS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"viewDirectionWS"`; Type `"$precision3"`; Define `"VARYINGS_NEED_VIEWDIRECTION_WS"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"staticLightmapUV"`; Type `"$precision2"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"dynamicLightmapUV"`; Type `"$precision2"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"sh"`; Type `"$precision3"`; Define `""`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"fogFactorAndVertexLight"`; Type `"$precision4"`; Define `"VARYINGS_NEED_FOG_AND_VERTEX_LIGHT"`	UnityEditor.ShaderGraph.FieldDescriptor
Name `"shadowCoord"`; Type `"$precision4"`; Define `"VARYINGS_NEED_SHADOWCOORD"`	UnityEditor.ShaderGraph.FieldDescriptor

```
---

Currently this is very hard to deciper and doesn't work with the long-term architecture. Ordering changes things, could accidentally add other fields, etc...
Also most fields don't actually have a type...