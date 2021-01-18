#version 430
 
in		vec3 vTexture;

uniform samplerCube uTextureDiffuse;
uniform vec4 uTintColor;

out vec4 color;
out vec4 bloom;
 
void main()
{
	color = vec4(uTintColor.xyz * uTintColor.w, 1.0) * texture(uTextureDiffuse, vTexture);
	bloom = vec4(0);
}