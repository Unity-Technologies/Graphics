#ifndef UNITY_DECALOUTPUTBUFFERS_INCLUDED
#define UNITY_DECALOUTPUTBUFFERS_INCLUDED

#define DBufferType0 float4

#define OUTPUT_DBUFFER(NAME)                            \
        out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0

#endif 