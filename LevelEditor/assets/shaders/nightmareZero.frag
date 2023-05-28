uniform sampler2D texture;
uniform sampler2D paletteTexture;
uniform int palette;
uniform float cols;
uniform float rows;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	float savedAlpha = pixel.a;

	// Do not execute if pixel is transparent.
	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		float firstRow = 0.5 / rows;
		float nextRow = (float(palette) + 0.5) / rows;
		vec4 targetColor = pixel;
		vec4 target;
		int found = 0;

		for (float i = 0.5; i < cols; i += 1.0) {
			target = texture2D(paletteTexture, vec2(i / cols, firstRow));

			if (pixel.r == target.r && pixel.g == target.g && pixel.b == target.b) {
				targetColor = texture2D(paletteTexture, vec2(i / cols, nextRow));
				i = cols + 1.0;
				found = 1;
				break;
			}
		}

		if (found == 0) {
			// This value contols greyscale intensity.
			// 1.0 = full grey, 0.0 = full color.
			float colorFactor = 0.8;

			// Greyscale conversion.
			float colorFactorR = 1.0 - colorFactor;
			float cLinear = pixel.r * 0.2126 + pixel.g * 0.7152 + pixel.b * 0.0722;

			targetColor = vec4(
				pixel.r * colorFactorR + cLinear * colorFactor,
				pixel.g * colorFactorR + cLinear * colorFactor,
				pixel.b * colorFactorR + cLinear * colorFactor,
				pixel.a
			);
		}

		gl_FragColor = vec4(targetColor.r, targetColor.g, targetColor.b, savedAlpha);
	}
}