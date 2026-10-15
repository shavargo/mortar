#include "UnitySprites.cginc"

fixed4 Mask(v2f i, fixed4 value)
{
    clip(SampleSpriteTexture(i.texcoord).a * i.color.a - 0.5);
    return value;
}

fixed4 FragStroke(v2f i) : SV_Target { return Mask(i, fixed4(1.0, 0.0, 0.0, 1.0)); }

fixed4 FragGlow(v2f i) : SV_Target { return Mask(i, fixed4(0.0, 1.0, 0.0, 1.0)); }

fixed4 FragFigure(v2f i) : SV_Target { return Mask(i, fixed4(0.0, 0.0, 1.0, 1.0)); }

fixed4 FragOccluder(v2f i) : SV_Target { return Mask(i, fixed4(0.0, 0.0, 0.0, 1.0)); }
