#ifndef SIMPLE_HASH_INCLUDED
#define SIMPLE_HASH_INCLUDED

float hash(float2 p)
{
    float r;
    r = frac(p.x * 20.1234 + 1.0) * 10.403 + 1.0;
    r += frac(p.y * 13.503) * 8.4023;
    r *= frac(length((p + 10.0) + r + r * r)) + 10.0;
    return frac(r);
}

void Noise_float(float3 p, out float n)
{
    float r = hash(p.xy);

    r = r * 12.6023 + 1.0;
    r += frac(p.z + p.y * 12.504) * 12.5043;
    r += frac(p.z + p.x * 14.203) * 11.9950;
    r += frac(length(p.xy * 20.3423)) * p.z;

    n = frac(r);
}

#endif