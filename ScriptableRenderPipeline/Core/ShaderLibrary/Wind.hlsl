// Globals
TEXTURE2D(WIND_SETTINGS_TexNoise);
SAMPLER(sampler_WIND_SETTINGS_TexNoise);
TEXTURE2D(WIND_SETTINGS_TexGust);
SAMPLER(sampler_WIND_SETTINGS_TexGust);

real4  WIND_SETTINGS_WorldDirectionAndSpeed;
real   WIND_SETTINGS_FlexNoiseScale;
real   WIND_SETTINGS_ShiverNoiseScale;
real   WIND_SETTINGS_Turbulence;
real   WIND_SETTINGS_GustSpeed;
real   WIND_SETTINGS_GustScale;
real   WIND_SETTINGS_GustWorldScale;

real AttenuateTrunk(real x, real s)
{
    real r = (x / s);
    return PositivePow(r,1/s);
}


real3 Rotate(real3 pivot, real3 position, real3 rotationAxis, real angle)
{
    rotationAxis = normalize(rotationAxis);
    real3 cpa = pivot + rotationAxis * dot(rotationAxis, position - pivot);
    return cpa + ((position - cpa) * cos(angle) + cross(rotationAxis, (position - cpa)) * sin(angle));
}

struct WindData
{
    real3 Direction;
    real Strength;
    real3 ShiverStrength;
    real3 ShiverDirection;
};


real3 texNoise(real3 worldPos, real LOD)
{
    return SAMPLE_TEXTURE2D_LOD(WIND_SETTINGS_TexNoise, sampler_WIND_SETTINGS_TexNoise, worldPos.xz, LOD).xyz -0.5;
}

real texGust(real3 worldPos, real LOD)
{
    return SAMPLE_TEXTURE2D_LOD(WIND_SETTINGS_TexGust, sampler_WIND_SETTINGS_TexGust, worldPos.xz, LOD).x;
}


WindData GetAnalyticalWind(real3 WorldPosition, real3 PivotPosition, real drag, real shiverDrag, real initialBend, real4 time)
{
    WindData result;
    real3 normalizedDir = normalize(WIND_SETTINGS_WorldDirectionAndSpeed.xyz);

    real3 worldOffset = normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w * time.y;
    real3 gustWorldOffset = normalizedDir * WIND_SETTINGS_GustSpeed * time.y;

    // Trunk noise is base wind + gusts + noise

    real3 trunk = real3(0,0,0);

    if(WIND_SETTINGS_WorldDirectionAndSpeed.w > 0.0 || WIND_SETTINGS_Turbulence > 0.0)
    {
        trunk = texNoise((PivotPosition - worldOffset)*WIND_SETTINGS_FlexNoiseScale,3);
    }

    real gust  = 0.0;

    if(WIND_SETTINGS_GustSpeed > 0.0)
    {
        gust = texGust((PivotPosition - gustWorldOffset)*WIND_SETTINGS_GustWorldScale,3);
        gust = pow(gust, 2) * WIND_SETTINGS_GustScale;
    }

    real3 trunkNoise =
        (
                (normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w)
                + (gust * normalizedDir * WIND_SETTINGS_GustSpeed)
                + (trunk * WIND_SETTINGS_Turbulence)
        ) * drag;

    // Shiver Noise
    real3 shiverNoise = texNoise((WorldPosition - worldOffset)*WIND_SETTINGS_ShiverNoiseScale,0) * shiverDrag * WIND_SETTINGS_Turbulence;

    real3 dir = trunkNoise;
    real flex = length(trunkNoise) + initialBend;
    real shiver = length(shiverNoise);

    result.Direction = dir;
    result.ShiverDirection = shiverNoise;
    result.Strength = flex;
    result.ShiverStrength = shiver + shiver * gust;

    return result;
}



void ApplyWindDisplacement( inout real3    positionWS,
                            real3          normalWS,
                            real3          rootWP,
                            real           stiffness,
                            real           drag,
                            real           shiverDrag,
                            real           shiverDirectionality,
                            real           initialBend,
                            real           shiverMask,
                            real4          time)
{
    WindData wind = GetAnalyticalWind(positionWS, rootWP, drag, shiverDrag, initialBend, time);

    if (wind.Strength > 0.0)
    {
        real att = AttenuateTrunk(distance(positionWS, rootWP), stiffness);
        real3 rotAxis = cross(real3(0, 1, 0), wind.Direction);

        positionWS = Rotate(rootWP, positionWS, rotAxis, (wind.Strength) * 0.001 * att);

        real3 shiverDirection = normalize(lerp(normalWS, normalize(wind.Direction + wind.ShiverDirection), shiverDirectionality));
        positionWS += wind.ShiverStrength * shiverDirection * shiverMask;
    }

}


