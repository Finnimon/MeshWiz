#version 430 core
layout(location = 0) in vec3 position;
layout(location = 1) in int transfromIndex;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float depthOffset;
layout(std430, binding = 2) buffer transforms{
    mat4 matrices[];
};
        
void main()
{
    mat4 transform=matrices[transfromIndex];
    vec4 clipPos = projection * view * model * transform * vec4(position, 1.0);

    // Apply a small offset in clip-space depth
    clipPos.z -= depthOffset * clipPos.w;  // scale with w to maintain perspective

    gl_Position = clipPos;
}