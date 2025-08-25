#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in float TextureID;
in float LightValue;
in float FragDistance;

uniform vec3 lightDir;
uniform vec3 viewPos;
uniform sampler2D blockTexture;
uniform float fogStart;
uniform float fogEnd;
uniform vec3 fogColor;

void main() {
    vec4 texColor = texture(blockTexture, TexCoord);

    if (texColor.a < 0.1) {
        discard;
    }
    
    vec3 color = texColor.rgb;
   
    vec3 norm = normalize(Normal);
    vec3 lightDirection = normalize(-lightDir);
    float diff = max(dot(norm, lightDirection), 0.0);
    vec3 diffuse = diff * color;
    
    vec3 ambient = 0.3 * color;
    vec3 result = ambient + diffuse;

    result *= LightValue;

    float fogFactor = clamp((fogEnd - FragDistance) / (fogEnd - fogStart), 0.0, 1.0);
    vec3 finalColor = mix(fogColor, result, fogFactor);

    FragColor = vec4(finalColor, texColor.a);
}