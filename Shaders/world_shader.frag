#version 330 core
out vec4 FragColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in float TextureID;

uniform vec3 lightPos;
uniform vec3 viewPos;
uniform sampler2D blockTexture;

void main() {
    // Sample the texture using the texture coordinates (including alpha)
    vec4 texColor = texture(blockTexture, TexCoord);
    
    // Discard fragment if alpha is too low (alpha testing)
    if (texColor.a < 0.1) {
        discard;
    }
    
    vec3 color = texColor.rgb;
    
    // Simple lighting
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * color;
    
    vec3 ambient = 0.3 * color;
    vec3 result = ambient + diffuse;
    
    // Use the texture's alpha value
    FragColor = vec4(result, texColor.a);
}