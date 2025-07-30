#version 430 core
layout(location = 0) in vec3 position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float depthOffset;

void main()
{
    vec4 clipPos = projection * view * model * vec4(position, 1.0);

    // Apply a small offset in clip-space depth
    clipPos.z -= depthOffset * clipPos.w;  // scale with w to maintain perspective

    gl_Position = clipPos;
}
