#version 430 core

in vec3 position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 truePos = vec4(position, 1.0);
    gl_Position = projection * view * model * truePos;
}
