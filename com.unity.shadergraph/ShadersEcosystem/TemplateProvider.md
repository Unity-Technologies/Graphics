# Template Provider Config

----

Each target can currently be configured very independently. This is not currently a proposal, but documentation for what currently exists.

High level URP is a set of simple fields:
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget",
    "m_ObjectId": "8d2899bbbf094a08bbf69986aabed243",
    "m_ActiveSubTarget": {
        "m_Id": "e9e114bdd53046518d51ca1ceb7c9a6e"
    },
    "m_SurfaceType": 0,
    "m_AlphaMode": 0,
    "m_TwoSided": false,
    "m_AlphaClip": false,
    "m_CustomEditorGUI": ""
}
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.Universal.ShaderGraph.UniversalLitSubTarget",
    "m_ObjectId": "e9e114bdd53046518d51ca1ceb7c9a6e",
    "m_WorkflowMode": 1,
    "m_NormalDropOffSpace": 0,
    "m_ClearCoat": false
}
```

HDRP is much more complicated and is a collection of dynamically composable blocks:
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.HighDefinition.ShaderGraph.HDTarget",
    "m_ObjectId": "7fcc34c0f09b45d881a75fbd9a3eb3e1",
    "m_ActiveSubTarget": {
        "m_Id": "5dd1f3c0ac204ac5b516934a844323b4"
    },
    "m_Datas": [
        {
            "m_Id": "b9f58d18b5e14a87b4997389ff6cc782" // BuiltInData
        },
        {
            "m_Id": "44aad970e43a4998850b606645793716" // SystemData
        },
        {
            "m_Id": "076605a01a014a1ca58469e387d77dfb" // HDLitData
        },
        {
            "m_Id": "f031e6a63af3498b905e7b0d86dbe45a" // LightingData
        }
    ],
    "m_CustomEditorGUI": ""
}
```
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.HighDefinition.ShaderGraph.HDLitSubTarget",
    "m_ObjectId": "5dd1f3c0ac204ac5b516934a844323b4"
}
```
---
BuiltinData
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.HighDefinition.ShaderGraph.BuiltinData",
    "m_ObjectId": "b9f58d18b5e14a87b4997389ff6cc782",
    "m_Distortion": false,
    "m_DistortionMode": 0,
    "m_DistortionDepthTest": true,
    "m_AddPrecomputedVelocity": false,
    "m_TransparentWritesMotionVec": false,
    "m_AlphaToMask": false,
    "m_DepthOffset": false,
    "m_TransparencyFog": true,
    "m_AlphaTestShadow": false,
    "m_BackThenFrontRendering": false,
    "m_TransparentDepthPrepass": false,
    "m_TransparentDepthPostpass": false,
    "m_SupportLodCrossFade": false
}
```
---
SystemData
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.HighDefinition.ShaderGraph.SystemData",
    "m_ObjectId": "44aad970e43a4998850b606645793716",
    "m_MaterialNeedsUpdateHash": 529,
    "m_SurfaceType": 0,
    "m_RenderingPass": 1,
    "m_BlendMode": 0,
    "m_ZTest": 4,
    "m_ZWrite": false,
    "m_TransparentCullMode": 2,
    "m_OpaqueCullMode": 2,
    "m_SortPriority": 0,
    "m_AlphaTest": false,
    "m_TransparentDepthPrepass": false,
    "m_TransparentDepthPostpass": false,
    "m_SupportLodCrossFade": false,
    "m_DoubleSidedMode": 0,
    "m_DOTSInstancing": false,
    "m_Version": 0,
    "m_FirstTimeMigrationExecuted": true,
    "inspectorFoldoutMask": 0
}
```
---
HDLitData
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.HighDefinition.ShaderGraph.HDLitData",
    "m_ObjectId": "076605a01a014a1ca58469e387d77dfb",
    "m_RayTracing": false,
    "m_MaterialType": 0,
    "m_RefractionModel": 0,
    "m_SSSTransmission": true,
    "m_EnergyConservingSpecular": true,
    "m_ClearCoat": false
}
```
---
LightingData
```
{
    "m_SGVersion": 0,
    "m_Type": "UnityEditor.Rendering.HighDefinition.ShaderGraph.LightingData",
    "m_ObjectId": "f031e6a63af3498b905e7b0d86dbe45a",
    "m_NormalDropOffSpace": 0,
    "m_BlendPreserveSpecular": true,
    "m_ReceiveDecals": true,
    "m_ReceiveSSR": true,
    "m_ReceiveSSRTransparent": false,
    "m_SpecularAA": false,
    "m_SpecularOcclusionMode": 0,
    "m_OverrideBakedGI": false
}
```