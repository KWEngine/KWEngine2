#version 430
 
in		vec3 aPosition;
in		vec2 aTexture;

out		vec2 vTexture;

uniform mat4 uMVP;
uniform vec2 uTextureTransform;
uniform vec2 uTextureOffset;
uniform vec2 uTextureClip;
 
void main()
{
	float u = aTexture.x * uTextureClip.x + uTextureOffset.x;
	float v = (1.0 - aTexture.y) * uTextureClip.y - uTextureOffset.y;
	vTexture = vec2(u,v) * uTextureTransform; 
	gl_Position = (uMVP * vec4(aPosition, 1.0)).xyww; 
}