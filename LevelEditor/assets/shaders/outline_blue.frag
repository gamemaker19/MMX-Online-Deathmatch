uniform sampler2D texture;

void main() {
	vec4 tint = vec4(0.0, 0.0, 1.0, 0.5);
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a > 0.0) {
		gl_FragColor = tint;
	} else {
		gl_FragColor = vec4(0.0, 0.0, 0.0, 0.0);
	}
}