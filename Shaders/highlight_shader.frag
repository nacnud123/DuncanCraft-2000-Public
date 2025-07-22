#version 330 core

out vec4 FragColor;

uniform vec3 highlightColor;

void main()
{
    FragColor = vec4(highlightColor, 0.8);
}