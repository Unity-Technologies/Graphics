# Metal Reflectance Node

## Description

Returns a **Metal Reflectance** value for a physically based material. The material to use can be selected with the **Material** dropdown parameter on the [Node](Node.md).

When using **Specular** **Workflow** on a [PBR Master Node](PBR-Master-Node.md) this value should be supplied to the **Specular** [Port](Port.md). When using **Metallic** **Workflow** this value should be supplied to the **Albedo** [Port](Port.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Vector 3 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Material | Dropdown | Iron, Silver, Aluminium, Gold, Copper, Chromium, Nickel, Titanium, Cobalt, Platform | Selects the material value to output. |

## Generated Code Example

The following example code represents one possible outcome of this node.

**Iron**
```
float3 _MetalReflectance_Out = float3(0.560, 0.570, 0.580);
```

**Silver**
```
float3 _MetalReflectance_Out = float3(0.972, 0.960, 0.915);
```

**Aluminium**
```
float3 _MetalReflectance_Out = float3(0.913, 0.921, 0.925);
```

**Gold**
```
float3 _MetalReflectance_Out = float3(1.000, 0.766, 0.336);
```

**Copper**
```
float3 _MetalReflectance_Out = float3(0.955, 0.637, 0.538);
```

**Chromium**
```
float3 _MetalReflectance_Out = float3(0.550, 0.556, 0.554);
```

**Nickel**
```
float3 _MetalReflectance_Out = float3(0.660, 0.609, 0.526);
```

**Titanium**
```
float3 _MetalReflectance_Out = float3(0.542, 0.497, 0.449);
```

**Cobalt**
```
float3 _MetalReflectance_Out = float3(0.662, 0.655, 0.634);
```

**Platinum**
```
float3 _MetalReflectance_Out = float3(0.672, 0.637, 0.585);
```

**Brass**
```
float3 _MetalReflectance_Out = float3(0.888, 0.745, 0.451);
```

**Lead**
```
float3 _MetalReflectance_Out = float3(0.491, 0.558, 0.591);
```

**Tin**
```
float3 _MetalReflectance_Out = float3(0.723, 0.584, 0.479);
```

**Steel**
```
float3 _MetalReflectance_Out = float3(0.61, 0.546, 0.509);
```

**Bronze**
```
float3 _MetalReflectance_Out = float3(0.88, 0.591, 0.558);
```

**Tungsten**
```
float3 _MetalReflectance_Out = float3(0.503, 0.491, 0.479);
```
