#version 430 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;
layout(location = 2) in uint tcpOptions;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float depthOffset;
uniform vec4 infillColor;
uniform vec4 perimeterColor;

flat out vec4 color;
const uint perimeterMask = 32;
void main(){
    vec4 clipPos = projection * view * model * vec4(position, 1.0);

    // Apply a small offset in clip-space depth
    clipPos.z -= depthOffset * clipPos.w;  // scale with w to maintain perspective
    gl_Position = clipPos;
    
    color = (tcpOptions & perimeterMask) == perimeterMask 
        ? perimeterColor 
        : infillColor;
}
