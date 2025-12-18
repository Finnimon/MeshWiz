#version 430 core

layout(location = 0) in vec3 position;
layout(location = 1) in vec3 normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float depthOffset;


out vec3 fragNormal;
out vec3 fragPos;

void main()
{

    fragPos = vec3(model * vec4(position, 1.0));
    fragNormal = mat3(transpose(inverse(model))) * normal;
    gl_Position = projection * view * vec4(fragPos, 1.0);

    // Apply a small offset in clip-space depth
    gl_Position.z -= depthOffset * gl_Position.w;  // scale with w to maintain perspective

}
