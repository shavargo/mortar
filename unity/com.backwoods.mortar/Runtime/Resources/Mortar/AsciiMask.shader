Shader "Hidden/Mortar/AsciiMask"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="MortarStroke" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragStroke
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="MortarWater" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragWater
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="MortarGlow" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragGlow
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="MortarFigure" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragFigure
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Cull Off ZWrite Off Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment FragOccluder
            #pragma target 2.0
            #include "Mask.cginc"
            ENDCG
        }
    }
    Fallback Off
}
