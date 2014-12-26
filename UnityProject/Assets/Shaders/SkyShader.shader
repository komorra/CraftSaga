Shader "Custom/SkyShader" {
	Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
		_HorizonColor ("Horizon Color", Color) = (0.1,0.2,0.6,1)
		_ZenithColor ("Zenith Color", Color) = (0.35,0.8,1,1)
    }
    SubShader {
		Fog{Mode Off}
        Pass {
			CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertexInput {
                float4 vertex : POSITION;
                float4 texcoord0 : TEXCOORD0;
            };

            struct fragmentInput{
                float4 position : SV_POSITION;
                float4 texcoord0 : TEXCOORD0;
				float3 wpos : TEXCOORD1;
            };

            fragmentInput vert(vertexInput i){
                fragmentInput o;
                o.position = mul (UNITY_MATRIX_MVP, i.vertex);
                o.texcoord0 = i.texcoord0;
				o.wpos = mul(_Object2World, i.vertex).xyz;
                return o;
            }
            
            uniform sampler2D _MainTex;
			float4 _HorizonColor;
			float4 _ZenithColor;

            float4 frag(fragmentInput i) : COLOR {
                //return tex2D(_MainTex, i.uv);
				float l = saturate(i.wpos.y*0.0005+0.5);
				return _ZenithColor * l + _HorizonColor * (1-l);
            }
            ENDCG
        }
    }
}
