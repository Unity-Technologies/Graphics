#ifndef UNITY_CLUSTER_DISPLAY_INCLUDED
#define UNITY_CLUSTER_DISPLAY_INCLUDED

// _ClusterDisplayParams holds all cluster related data:
// row 0: normalized viewport subsection, (0, 0) is bottom-left, (1, 1) is top-right
// row 1: global screensize (xy) and its reciprocate (zw)
// row 2: grid size expressed in tiles (x:int, y:int, 0, 0)
// row 3: unused
float4x4 _ClusterDisplayParams;

float2 DeviceToClusterFullscreenUV(float2 xy)
{
    return _ClusterDisplayParams[0].xy + xy * _ClusterDisplayParams[0].zw;
}

float2 ClusterToDeviceFullscreenUV(float2 xy)
{
    return (xy - _ClusterDisplayParams[0].xy) / _ClusterDisplayParams[0].zw;
}

// from normalized-device-coordinates to normalized-cluster-coordinates
float2 NdcToNcc(float2 xy)
{
    // ndc to device-UV
    xy.y = -xy.y;
    xy = (xy + 1) * 0.5;
    xy = DeviceToClusterFullscreenUV(xy);
    // cluster-UV to ncc
    xy = xy * 2 - 1;
    xy.y = -xy.y;
    return xy;
}

// from normalized-cluster-coordinates to normalized-device-coordinates
float2 NccToNdc(float2 xy)
{
    // ncc to cluster-UV
    xy.y = -xy.y;
    xy = (xy + 1) * 0.5;
    xy = ClusterToDeviceFullscreenUV(xy);
    // device-UV to ndc
    xy = xy * 2 - 1;
    xy.y = -xy.y;
    return xy;
}

// USING_CLUSTER_DISPLAY keyword is enabled from Cluster Display Graphics Package
#if defined(USING_CLUSTER_DISPLAY)
    #define DEVICE_TO_CLUSTER_FULLSCREEN_UV(xy)          DeviceToClusterFullscreenUV(xy)
    #define CLUSTER_TO_DEVICE_FULLSCREEN_UV(xy)          ClusterToDeviceFullscreenUV(xy)
    #define DEVICE_TO_CLUSTER_NORMALIZED_COORDINATES(xy) NdcToNcc(xy)
    #define CLUSTER_TO_DEVICE_NORMALIZED_COORDINATES(xy) NccToNdc(xy)
    #define CLUSTER_GRID_SIZE                            _ClusterDisplayParams[2].xy
    #define CLUSTER_SCREEN_SIZE                          _ClusterDisplayParams[1]
#else
    #define DEVICE_TO_CLUSTER_FULLSCREEN_UV(xy)          xy
    #define CLUSTER_TO_DEVICE_FULLSCREEN_UV(xy)          xy
    #define DEVICE_TO_CLUSTER_NORMALIZED_COORDINATES(xy) xy
    #define CLUSTER_TO_DEVICE_NORMALIZED_COORDINATES(xy) xy
    #define CLUSTER_GRID_SIZE                            float2(1, 1)
    #define CLUSTER_SCREEN_SIZE                          _ScreenSize
#endif

#endif // UNITY_CLUSTER_DISPLAY_INCLUDED
