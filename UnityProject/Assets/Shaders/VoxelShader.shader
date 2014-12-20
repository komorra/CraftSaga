Shader "Custom/VoxelShader" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" 
		_TopSkin ("Top Skin (RGB)", 2D) = "white" {}
		_SideSkin ("Side Skin (RGB)", 2D) = "white" {}
		_BottomSkin ("Bottom Skin (RGB)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_Multiplier("Color Multiplier",Range(1,10)) = 1.0
		_AO ("Ambient Occlusion", 3D) = "white" 
	}
	SubShader {
		
		AlphaTest Greater 0.5
		
		

		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout"}
		LOD 200
		
		CGPROGRAM
		
		#pragma surface surf Lambert alphatest:_Cutoff addshadow
		#pragma target 3.0

		fixed4 _Color;
		float _Multiplier;
		sampler2D _MainTex;
		sampler2D _TopSkin;
		sampler2D _SideSkin;
		sampler2D _BottomSkin;
		sampler3D _AO;

		struct Input {
			float2 uv_MainTex;
			float3 worldNormal;
			float3 worldPos;
			//LIGHTING_COORDS(4,5);			
		};

		float3 mod(float3 p)
		{
			return float3(p.x - floor(p.x), p.y - floor(p.y), p.z - floor(p.z));
		}

		float4 GetTexture(int tile, int tilemod, float3 uv, float3 nrm)
		{
			if(tile == 0)return (float4)0;

			float2 iuv = (float2)0;	
			nrm*=-1.0;

			if(nrm.x < -0.5)
			{
				iuv.x = 1-uv.z;
				iuv.y = 1-uv.y;
			}
			if(nrm.x > 0.5)
			{
				iuv.x = uv.z;
				iuv.y = 1-uv.y;
			}
			if(nrm.y < -0.5)
			{
				iuv.x = uv.x;
				iuv.y = uv.z;
			}
			if(nrm.y > 0.5)
			{
				iuv.x = 1-uv.x;
				iuv.y = uv.z;				
			}
			if(nrm.z < -0.5)
			{
				iuv.x = uv.x;
				iuv.y = 1-uv.y;
			}
			if(nrm.z > 0.5)
			{
				iuv.x = 1-uv.x;
				iuv.y = 1-uv.y;
			}			

			
			iuv.x /= 64.0;		
			iuv.y /= 64.0;			
			iuv.x += float(tile % 64) / 64.0;
			iuv.y += float(tile / 64) / 64.0;
			iuv = iuv-floor(iuv);						
			iuv.y = 1.0-iuv.y;

			if(abs(nrm.x) > 0.5)
			{
				return tex2D(_SideSkin, iuv);
			}
			if(abs(nrm.z) > 0.5)
			{
				return tex2D(_SideSkin, iuv);	 
			}
			if(nrm.y > 0.5)
			{
				return tex2D(_BottomSkin, iuv);
			}
			if(nrm.y < -0.5)
			{
				return tex2D(_TopSkin, iuv);
			}
			return float4(0,0,0,0);
		}

		void surf (Input IN, inout SurfaceOutput o) {
					
			float2 uvn = IN.uv_MainTex;			
			float4 col = tex2D(_MainTex, uvn);
			
			float4 ct = GetTexture((int)(col.r * 255) + (int)(col.g * 65280), (int)(col.b * 255) , mod(IN.worldPos), IN.worldNormal);
			//float4 ct = GetTexture(32*64, (int)(col.b * 255) , mod(IN.worldPos), IN.worldNormal);
			//if(ct.a < 0.5) ct.rgb = float3(1,0,0);	
			ct *=  _Color * _Multiplier;		
			
			float3 wp= IN.worldPos-0.001;
			if(wp.x < 0)wp.x -= 1.0;
			if(wp.y < 0)wp.y -= 1.0;
			if(wp.z < 0)wp.z -= 1.0;
			
			float ao = tex3D(_AO, mod(wp.xyz/16.0)).r;

			o.Albedo = ct.rgb *  pow(saturate(ao+0.05),6);
			o.Alpha = ct.a;
			//if(ct.a < 0.5)discard; saturate(0.5 + IN.worldPos.y * 0.06) *
		}
		ENDCG
	} 
	FallBack Off
}
