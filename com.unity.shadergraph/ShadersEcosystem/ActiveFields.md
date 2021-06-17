
# Active Fields

This isn't fleshed out yet, but more a placeholder for detailing deeper notes and thoughts on how active fields can be worked around.

Currently there's a lot of small intricacies in how this is built. For instance, some structs are built and filtered part way through assembling active fields. If these structs were built later then different parameters would exist on structs.

Currently the pass required fields are added after structs are baked, meaning the required fields aren't added to some structs (like SurfaceDescriptionInputs) unless they're actually used (such as uv1).

Additionally, active fields currently contain defines and a few other interesting fields. It's possible shader graph doesn't need any of the data from the target beyond what's in the blocks, and the linker can deduce the information from the blocks provided by shader graph / surface shaders.

Here's data from a quick run of URP to show how these fields are changed over time:

---
Pre SG: Data returned by `Target.GetFields()` that is filtered by SG

```
Tag `"features"`; Name `"graphVertex"`; Type `null`; Define `"FEATURES_GRAPH_VERTEX"`
Tag `"features"`; Name `"graphPixel"`; Type `null`; Define `"FEATURES_GRAPH_PIXEL"`
Tag `""`; Name `"NormalDropOffTS"`; Type `null`; Define `"_NORMAL_DROPOFF_TS 1"`
Tag `""`; Name `"Normal"`; Type `null`; Define `"_NORMALMAP 1"`

```

---
Post SG: Fields SG adds after visiting block fields and nodes
```
Tag `"features"`; Name `"graphVertex"`; Type `null`; Define `"FEATURES_GRAPH_VERTEX"`
Tag `"features"`; Name `"graphPixel"`; Type `null`; Define `"FEATURES_GRAPH_PIXEL"`
Tag `""`; Name `"NormalDropOffTS"`; Type `null`; Define `"_NORMAL_DROPOFF_TS 1"`
Tag `""`; Name `"Normal"`; Type `null`; Define `"_NORMALMAP 1"`

Tag `"VertexDescription"`; Name `"Position"`; Type `null`; Define `"VERTEXDESCRIPTION_POSITION"`
Tag `"VertexDescription"`; Name `"Normal"`; Type `null`; Define `"VERTEXDESCRIPTION_NORMAL"`
Tag `"VertexDescription"`; Name `"Tangent"`; Type `null`; Define `"VERTEXDESCRIPTION_TANGENT"`
Tag `"SurfaceDescription"`; Name `"BaseColor"`; Type `null`; Define `"SURFACEDESCRIPTION_BASECOLOR"`
Tag `"SurfaceDescription"`; Name `"NormalTS"`; Type `null`; Define `"SURFACEDESCRIPTION_NORMALTS"`
Tag `"SurfaceDescription"`; Name `"Emission"`; Type `null`; Define `"SURFACEDESCRIPTION_EMISSION"`
Tag `"SurfaceDescription"`; Name `"Metallic"`; Type `null`; Define `"SURFACEDESCRIPTION_METALLIC"`
Tag `"SurfaceDescription"`; Name `"Smoothness"`; Type `null`; Define `"SURFACEDESCRIPTION_SMOOTHNESS"`
Tag `"SurfaceDescription"`; Name `"Occlusion"`; Type `null`; Define `"SURFACEDESCRIPTION_OCCLUSION"`
Tag `"SurfaceDescriptionInputs"`; Name `"TangentSpaceNormal"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"ObjectSpaceNormal"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"ObjectSpaceTangent"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"ObjectSpacePosition"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"WorldSpacePosition"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescription"`; Name `"Position"`; Type `null`; Define `""`
Tag `"VertexDescription"`; Name `"Normal"`; Type `null`; Define `""`
Tag `"VertexDescription"`; Name `"Tangent"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"BaseColor"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"NormalTS"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Emission"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Metallic"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Smoothness"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Occlusion"`; Type `null`; Define `""`
```
This is what's used to filter all structs

---
Full: Full data before running the rest of code gen, mostly just:
- `GenerationUtils.AddRequiredFields(pass.requiredFields, activeFields.baseInstance);`
```
Tag `"features"`; Name `"graphVertex"`; Type `null`; Define `"FEATURES_GRAPH_VERTEX"`
Tag `"features"`; Name `"graphPixel"`; Type `null`; Define `"FEATURES_GRAPH_PIXEL"`
Tag `""`; Name `"NormalDropOffTS"`; Type `null`; Define `"_NORMAL_DROPOFF_TS 1"`
Tag `""`; Name `"Normal"`; Type `null`; Define `"_NORMALMAP 1"`

Tag `"VertexDescription"`; Name `"Position"`; Type `null`; Define `"VERTEXDESCRIPTION_POSITION"`
Tag `"VertexDescription"`; Name `"Normal"`; Type `null`; Define `"VERTEXDESCRIPTION_NORMAL"`
Tag `"VertexDescription"`; Name `"Tangent"`; Type `null`; Define `"VERTEXDESCRIPTION_TANGENT"`
Tag `"SurfaceDescription"`; Name `"BaseColor"`; Type `null`; Define `"SURFACEDESCRIPTION_BASECOLOR"`
Tag `"SurfaceDescription"`; Name `"NormalTS"`; Type `null`; Define `"SURFACEDESCRIPTION_NORMALTS"`
Tag `"SurfaceDescription"`; Name `"Emission"`; Type `null`; Define `"SURFACEDESCRIPTION_EMISSION"`
Tag `"SurfaceDescription"`; Name `"Metallic"`; Type `null`; Define `"SURFACEDESCRIPTION_METALLIC"`
Tag `"SurfaceDescription"`; Name `"Smoothness"`; Type `null`; Define `"SURFACEDESCRIPTION_SMOOTHNESS"`
Tag `"SurfaceDescription"`; Name `"Occlusion"`; Type `null`; Define `"SURFACEDESCRIPTION_OCCLUSION"`
Tag `"SurfaceDescriptionInputs"`; Name `"TangentSpaceNormal"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"ObjectSpaceNormal"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"ObjectSpaceTangent"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"ObjectSpacePosition"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescriptionInputs"`; Name `"WorldSpacePosition"`; Type `"$precision3"`; Define `""`
Tag `"VertexDescription"`; Name `"Position"`; Type `null`; Define `""`
Tag `"VertexDescription"`; Name `"Normal"`; Type `null`; Define `""`
Tag `"VertexDescription"`; Name `"Tangent"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"BaseColor"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"NormalTS"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Emission"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Metallic"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Smoothness"`; Type `null`; Define `""`
Tag `"SurfaceDescription"`; Name `"Occlusion"`; Type `null`; Define `""`

Tag `"Attributes"`; Name `"uv1"`; Type `"$precision4"`; Define `"ATTRIBUTES_NEED_TEXCOORD1"`
Tag `"Attributes"`; Name `"uv2"`; Type `"$precision4"`; Define `"ATTRIBUTES_NEED_TEXCOORD2"`
Tag `"Varyings"`; Name `"positionWS"`; Type `"$precision3"`; Define `"VARYINGS_NEED_POSITION_WS"`
Tag `"Varyings"`; Name `"normalWS"`; Type `"$precision3"`; Define `"VARYINGS_NEED_NORMAL_WS"`
Tag `"Varyings"`; Name `"tangentWS"`; Type `"$precision4"`; Define `"VARYINGS_NEED_TANGENT_WS"`
Tag `"Varyings"`; Name `"viewDirectionWS"`; Type `"$precision3"`; Define `"VARYINGS_NEED_VIEWDIRECTION_WS"`
Tag `"Varyings"`; Name `"staticLightmapUV"`; Type `"$precision2"`; Define `""`
Tag `"Varyings"`; Name `"dynamicLightmapUV"`; Type `"$precision2"`; Define `""`
Tag `"Varyings"`; Name `"sh"`; Type `"$precision3"`; Define `""`
Tag `"Varyings"`; Name `"fogFactorAndVertexLight"`; Type `"$precision4"`; Define `"VARYINGS_NEED_FOG_AND_VERTEX_LIGHT"`
Tag `"Varyings"`; Name `"shadowCoord"`; Type `"$precision4"`; Define `"VARYINGS_NEED_SHADOWCOORD"`
```
---

Currently this is very hard to deciper and doesn't work with the long-term architecture. Ordering changes things, could accidentally add other fields, etc...
Also most fields don't actually have a type...