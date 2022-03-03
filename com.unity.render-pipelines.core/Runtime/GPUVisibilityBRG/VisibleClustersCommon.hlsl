#ifndef _VISIBLE_CLUSTERS_COMMON_H
#define _VISIBLE_CLUSTERS_COMMON_H

namespace VisibleClusters
{

uint PackIndexData(uint clusterOffset, int indexOffset)
{
    return (clusterOffset << 8) | (indexOffset & 0xFF);
}

void UnpackIndexData(uint indexData, out uint outClusterOffset, out uint outIndexOffset)
{
    outClusterOffset = indexData >> 8;
    outIndexOffset = indexData & 0xFF;
}

uint PackVisibleClusterInfo(uint instanceID, uint clusterIndex)
{
    return (instanceID & 0xFFFF) | (clusterIndex << 16);
}

void UnpackVisibleClusterInfo(uint visibleClusterInfo, out uint outInstanceID, out uint outClusterIndex)
{
    outClusterIndex = visibleClusterInfo >> 16;
    outInstanceID = visibleClusterInfo & 0xFFFF;
}

void LoadVisibleClusterInfo(
    ByteAddressBuffer visibleClusterBuffer,
    uint indexData,
    out uint outInstanceID,
    out uint outIndexOffset,
    out uint outClusterIndex)
{
    uint visibleClusterOffset;
    UnpackIndexData(indexData, visibleClusterOffset, outIndexOffset);
    uint clusterData = visibleClusterBuffer.Load(visibleClusterOffset << 2);
    UnpackVisibleClusterInfo(clusterData, outInstanceID, outClusterIndex);
}

}

#endif
