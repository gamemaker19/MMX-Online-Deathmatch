uniform sampler2D texture;
uniform float fadeTime;

void main() {
	vec4 tint = vec4(1.0, 0.0, 1.0, 0.25);
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a > 0.0) {
		float fadeTime2 = (1.0 - fadeTime);
		gl_FragColor = vec4(pixel.r - fadeTime2 * 1.0, pixel.g - fadeTime2 * 1.0, pixel.b - fadeTime2 * 1.0, pixel.a - fadeTime2 * 1.0);
	} else {
		gl_FragColor = pixel;
	}
}