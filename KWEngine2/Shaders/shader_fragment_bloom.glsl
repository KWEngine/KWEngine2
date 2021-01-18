#version 430

in		vec2 vTexture;

uniform sampler2D uTextureBloom;
uniform int uHorizontal;
uniform vec2 uResolution;
uniform float uBloomRadius;

out		vec4 color;

float weight[5] = float[] (0.15, 0.4, 0.27, 0.18, 0.09);
//float weight[11] = float[] (0.4, 0.36, 0.32, 0.27, 0.24, 0.20, 0.16, 0.11, 0.06, 0.02, 0.008);
//float weight[4] = float[] (0.4, 0.3, 0.2, 0.1);


void main()
{
	vec3 result = texture(uTextureBloom, vTexture).rgb * weight[0];


	if(uHorizontal > 0)
    {
	
        for(int i = 1; i < 5; i++)
        {
            result += texture(uTextureBloom, vTexture + vec2(uResolution.x * float(i) * uBloomRadius, 0.0)).rgb * weight[i];
            result += texture(uTextureBloom, vTexture - vec2(uResolution.x * float(i) * uBloomRadius, 0.0)).rgb * weight[i];
        }
		
	}
    else
    {
	
        for(int i = 1; i < 5; i++)
        {
            result += texture(uTextureBloom, vTexture + vec2(0.0, (uResolution.y * float(i) * uBloomRadius))).rgb * weight[i];
            result += texture(uTextureBloom, vTexture - vec2(0.0, (uResolution.y * float(i) * uBloomRadius))).rgb * weight[i];
        }	
	}

    color.x = result.x;
	color.y = result.y;
	color.z = result.z;
	color.w = 1.0;
}