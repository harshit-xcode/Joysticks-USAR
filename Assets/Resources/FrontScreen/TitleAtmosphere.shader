Shader "Joystrix/TitleAtmosphere"
{
    Properties { _MenuTime ("Unscaled animation time", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _MenuTime;
            struct input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct output { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            output vert(input v) { output o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float fbm(float2 p)
            {
                float n=0,amp=.5;
                [unroll] for(int i=0;i<4;i++) { n+=noise(p)*amp;p=p*2.03+17.3;amp*=.5; }
                return n;
            }
            fixed4 frag(output i) : SV_Target
            {
                float2 drift=float2(_MenuTime*.002,-_MenuTime*.00055);
                float clouds=fbm(i.uv*float2(5,3)-drift);
                float mist=smoothstep(.34,.76,clouds)*.15*(1-smoothstep(.45,1,i.uv.y));
                float2 grid=i.uv*float2(160,90), cell=floor(grid);
                float chance=hash(cell+13.4);
                float2 starPosition=float2(hash(cell+7.1),hash(cell+23.6))*.7+.15;
                float radius=length(frac(grid)-starPosition);
                float star=(1-smoothstep(.012,.085,radius))*step(.992,chance);
                float pulse=.25+.65*pow(.5+.5*sin(_MenuTime*(.35+hash(cell)*.5)+chance*100),2);
                float starAlpha=star*pulse;
                return fixed4(lerp(float3(.23,.31,.37),float3(.7,.84,.93),saturate(starAlpha*5)),max(mist,starAlpha));
            }
            ENDCG
        }
    }
    Fallback Off
}
