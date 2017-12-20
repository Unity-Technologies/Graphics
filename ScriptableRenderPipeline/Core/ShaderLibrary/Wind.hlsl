// Globals
TEXTURE2D(WIND_SETTINGS_TexNoise);
SAMPLER(sampler_WIND_SETTINGS_TexNoise);
TEXTURE2D(WIND_SETTINGS_TexGust);
SAMPLER(sampler_WIND_SETTINGS_TexGust);

REAL4  WIND_SETTINGS_WorldDirectionAndSpeed;
REAL   WIND_SETTINGS_FlexNoiseScale;
REAL   WIND_SETTINGS_ShiverNoiseScale;
REAL   WIND_SETTINGS_Turbulence;
REAL   WIND_SETTINGS_GustSpeed;
REAL   WIND_SETTINGS_GustScale;
REAL   WIND_SETTINGS_GustWorldScale;

REAL AttenuateTrunk(REAL x, REAL s)
{
    REAL r = (x / s);
    return PositivePow(r,1/s);
}


REAL3 Rotate(REAL3 pivot, REAL3 position, REAL3 rotationAxis, REAL angle)
{
    rotationAxis = normalize(rotationAxis);
    REAL3 cpa = pivot + rotationAxis * dot(rotationAxis, position - pivot);
    return cpa + ((position - cpa) * cos(angle) + cross(rotationAxis, (position - cpa)) * sin(angle));
}

struct WindData
{
    REAL3 Direction;
    REAL Strength;
    REAL3 ShiverStrength;
    REAL3 ShiverDirection;
};


REAL3 texNoise(REAL3 worldPos, REAL LOD)
{
    return SAMPLE_TEXTURE2D_LOD(WIND_SETTINGS_TexNoise, sampler_WIND_SETTINGS_TexNoise, worldPos.xz, LOD).xyz -0.5;
}

REAL texGust(REAL3 worldPos, REAL LOD)
{
    return SAMPLE_TEXTURE2D_LOD(WIND_SETTINGS_TexGust, sampler_WIND_SETTINGS_TexGust, worldPos.xz, LOD).x;
}


WindData GetAnalyticalWind(REAL3 WorldPosition, REAL3 PivotPosition, REAL drag, REAL shiverDrag, REAL initialBend, REAL4 time)
{
    WindData result;
    REAL3 normalizedDir = normalize(WIND_SETTINGS_WorldDirectionAndSpeed.xyz);

    REAL3 worldOffset = normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w * time.y;
    REAL3 gustWorldOffset = normalizedDir * WIND_SETTINGS_GustSpeed * time.y;

    // Trunk noise is base wind + gusts + noise

    REAL3 trunk = REAL3(0,0,0);

    if(WIND_SETTINGS_WorldDirectionAndSpeed.w > 0.0 || WIND_SETTINGS_Turbulence > 0.0)
    {
        trunk = texNoise((PivotPosition - worldOffset)*WIND_SETTINGS_FlexNoiseScale,3);
    }

    REAL gust  = 0.0;

    if(WIND_SETTINGS_GustSpeed > 0.0)
    {
        gust = texGust((PivotPosition - gustWorldOffset)*WIND_SETTINGS_GustWorldScale,3);
        gust = pow(gust, 2) * WIND_SETTINGS_GustScale;
    }

    REAL3 trunkNoise =
        (
                (normalizedDir * WIND_SETTINGS_WorldDirectionAndSpeed.w)
                + (gust * normalizedDir * WIND_SETTINGS_GustSpeed)
                + (trunk * WIND_SETTINGS_Turbulence)
        ) * drag;

    // Shiver Noise
    REAL3 shiverNoise = texNoise((WorldPosition - worldOffset)*WIND_SETTINGS_ShiverNoiseScale,0) * shiverDrag * WIND_SETTINGS_Turbulence;

    REAL3 dir = trunkNoise;
    REAL flex = length(trunkNoise) + initialBend;
    REAL shiver = length(shiverNoise);

    result.Direction = dir;
    result.ShiverDirection = shiverNoise;
    result.Strength = flex;
    result.ShiverStrength = shiver + shiver * gust;

    return result;
}



void ApplyWindDisplacement( inout REAL3    positionWS,
                            REAL3          normalWS,
                            REAL3          rootWP,
                            REAL           stiffness,
                            REAL           drag,
                            REAL           shiverDrag,
                            REAL           shiverDirectionality,
                            REAL           initialBend,
                            REAL           shiverMask,
                            REAL4          time)
{
    WindData wind = GetAnalyticalWind(positionWS, rootWP, drag, shiverDrag, initialBend, time);

    if (wind.Strength > 0.0)
    {
        REAL att = AttenuateTrunk(distance(positionWS, rootWP), stiffness);
        REAL3 rotAxis = cross(REAL3(0, 1, 0), wind.Direction);

        positionWS = Rotate(rootWP, positionWS, rotAxis, (wind.Strength) * 0.001 * att);

        REAL3 shiverDirection = normalize(lerp(normalWS, normalize(wind.Direction + wind.ShiverDirection), shiverDirectionality));
        positionWS += wind.ShiverStrength * shiverDirection * shiverMask;
    }

}


