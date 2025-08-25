#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in float aTextureID;
layout (location = 4) in float aLightValue;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 viewPos;

out vec3 FragPos;
out vec3 Normal;
out vec2 TexCoord;
out float TextureID;
out float LightValue;
out float FragDistance;

void main() {
    FragPos = vec3(model * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(model))) * aNormal;
    TexCoord = aTexCoord;
    TextureID = aTextureID;
    LightValue = aLightValue;
    
    FragDistance = length(FragPos - viewPos);
    
    gl_Position = projection * view * vec4(FragPos, 1.0);
}