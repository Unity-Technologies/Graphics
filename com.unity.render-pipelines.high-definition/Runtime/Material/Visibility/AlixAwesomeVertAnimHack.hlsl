#ifndef _ALIX_VERT_HACK_H_
#define _ALIX_VERT_HACK_H_

#define ENABLE_HACK_VERTEX_ANIMATION 0

namespace AlixVertAnimHack
{

float sinAnimation_float(float time, float gradient, float speed, float amplitude, float period)
{
return sin((time * speed + gradient * period)*6.28)* amplitude * gradient;

}

// Graph Functions

        void Unity_Rotate_About_Axis_Radians_float(float3 In, float3 Axis, float Rotation, out float3 Out)
        {
            float s = sin(Rotation);
            float c = cos(Rotation);
            float one_minus_c = 1.0 - c;

            Axis = normalize(Axis);

            float3x3 rot_mat = { one_minus_c * Axis.x * Axis.x + c,            one_minus_c * Axis.x * Axis.y - Axis.z * s,     one_minus_c * Axis.z * Axis.x + Axis.y * s,
                                      one_minus_c * Axis.x * Axis.y + Axis.z * s,   one_minus_c * Axis.y * Axis.y + c,              one_minus_c * Axis.y * Axis.z - Axis.x * s,
                                      one_minus_c * Axis.z * Axis.x - Axis.y * s,   one_minus_c * Axis.y * Axis.z + Axis.x * s,     one_minus_c * Axis.z * Axis.z + c
                                    };

            Out = mul(rot_mat,  In);
        }

void animate_float(float2 _UV, float3 _VtxColor, float _Time, float3 _Position, float3 _Tangent, out float3 Out)
{
float3 up = {0,1,0};
float3 bellWiggle = sinAnimation_float(_Time, _Position.y, 0.5, 0.015, 0.5 ) * up;
float3 tentaclesWiggle = sinAnimation_float(_Time,_UV.x,0.1,1,1) * _Tangent;
float3 bigTentaclesDir = normalize(lerp(_Position,-up,(1-_UV.y)));
float3 bigTentaclesWiggle = sinAnimation_float(_Time,(1-_UV.y),0.5,0.1,3) * bigTentaclesDir;

float3 newPos = _Position + bellWiggle;
newPos = lerp(newPos, newPos + tentaclesWiggle, _VtxColor.y);
float rotationAngle = sinAnimation_float(_Time,(1-_UV.y),0.1,0.5,1);
float3 bigTentaclesNewPos;
Unity_Rotate_About_Axis_Radians_float(_Position + bigTentaclesWiggle,up,rotationAngle,bigTentaclesNewPos);

newPos = lerp (newPos, bigTentaclesNewPos, _VtxColor.x);


Out= newPos;

}

void ApplyLocalAnim(inout GeoPoolVertex vertexData)
{
    float3 newPos = vertexData.pos;
    AlixVertAnimHack::animate_float(vertexData.uv1, vertexData.C, _Time.z, vertexData.pos, vertexData.T, newPos);
    vertexData.pos = newPos;
}

}

#endif
