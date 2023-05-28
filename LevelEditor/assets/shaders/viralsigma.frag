uniform sampler2D texture;
uniform sampler2D paletteTexture;
uniform int palette;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);

	if (pixel.a == 0.0 || palette == 0) {
		gl_FragColor = pixel;
	} else {
		float savedAlpha = pixel.a;
		float rows = 3.0;
		float cols = 6.0;
		float firstRow = 0.5 / cols;
		float nextRow = (float(palette) + 0.5) / cols;
		vec4 targetColor = pixel;
		vec4 target;

		for (float i = 0.5; i <= rows; i += 1.0) {
			target = texture2D(paletteTexture, vec2(i / rows, firstRow));

			if (pixel.r == target.r && pixel.g == target.g && pixel.b == target.b) {
				targetColor = texture2D(paletteTexture, vec2(i / rows, nextRow));
				i = rows + 1.0;
			}
		}
		gl_FragColor = vec4(targetColor.r, targetColor.g, targetColor.b, savedAlpha);
	}
}