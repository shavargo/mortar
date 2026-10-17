#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler2D _MaskTex;
float _BloomThreshold;
float _GlowBoost;

static const float3 Luma = float3(0.299, 0.587, 0.114);
static const float Weights[3] = { 0.4, 0.2, 0.1 };

float3 Fetch(float2 uv)
{
    return tex2Dlod(_MainTex, float4(uv, 0.0, 0.0)).rgb;
}

float3 Bright(float2 uv)
{
    float3 color = Fetch(uv);
    float over = saturate((dot(color, Luma) - _BloomThreshold) / (1.0 - _BloomThreshold));
    float glow = tex2Dlod(_MaskTex, float4(uv, 0.0, 0.0)).g;
    return color * (over + glow * _GlowBoost);
}

fixed4 FragBloomAcross(v2f_img i) : SV_Target
{
    float2 step = float2(_MainTex_TexelSize.x, 0.0);
    float3 sum = Bright(i.uv) * Weights[0];
    for (int k = 1; k <= 2; k++)
    {
        sum += (Bright(i.uv + step * k) + Bright(i.uv - step * k)) * Weights[k];
    }
    return fixed4(sum, 1.0);
}

fixed4 FragBloomDown(v2f_img i) : SV_Target
{
    float2 step = float2(0.0, _MainTex_TexelSize.y);
    float3 sum = Fetch(i.uv) * Weights[0];
    for (int k = 1; k <= 2; k++)
    {
        sum += (Fetch(i.uv + step * k) + Fetch(i.uv - step * k)) * Weights[k];
    }
    return fixed4(sum, 1.0);
}
