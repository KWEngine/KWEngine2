#version 430
in vec4 gPosition;

uniform vec3 uLightPosition;
uniform float uFarPlane;

void main()
{
    // get distance between fragment and light source
    float lightDistance = length(gPosition.xyz - uLightPosition);
    
    // map to [0;1] range by dividing by far_plane
    lightDistance = lightDistance / uFarPlane;
    
    // write this as modified depth
    gl_FragDepth = lightDistance;
}  