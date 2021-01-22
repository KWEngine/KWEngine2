#version 430
 
in		vec2 vTexture;

uniform sampler2D uTextureDiffuse;
uniform vec4 uTintColor;

out		vec4 color;
out     vec4 bloom;
 
void main()
{
    color = texture(uTextureDiffuse, vTexture) * vec4(uTintColor.xyz * uTintColor.w, 1.0);
    color.w = 1.0;
    bloom = vec4(0);
}