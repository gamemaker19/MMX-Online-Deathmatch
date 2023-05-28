uniform sampler2D texture;
uniform sampler2D image;
uniform int imageW;
uniform int imageH;
uniform float t;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	float savedAlpha = pixel.a;
	float rows = float(imageH);
	float cols = float(imageW);
	int palette = 1;
	if (t < 10.0) palette = 1;
	else if (t >= 10.0 && t < 20.0) palette = 2;
	else if (t >= 20.0 && t < 30.0) palette = 3;
	else if (t >= 30.0 && t < 40.0) palette = 4;
	else if (t >= 40.0 && t < 50.0) palette = 5;
	else if (t >= 50.0 && t < 60.0) palette = 6;
	else if (t >= 60.0 && t < 70.0) palette = 7;
	else if (t >= 70.0 && t < 80.0) palette = 8;
	else if (t >= 80.0 && t < 90.0) palette = 9;
	else if (t >= 90.0 && t < 100.0) palette = 10;
	else if (t >= 100.0) palette = 10;

	// Do not execute if pixel is transparent.
	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		float firstRow = 0.5 / rows;
		float nextRow = (float(palette) + 0.5) / rows;
		vec4 targetColor = pixel;
		vec4 target;

		for (float i = 0.5; i < cols; i += 1.0) {
			target = texture2D(image, vec2(i / cols, firstRow));

			if (pixel.r == target.r && pixel.g == target.g && pixel.b == target.b) {
				targetColor = texture2D(image, vec2(i / cols, nextRow));
				i = cols + 1.0;
			}
		}
		gl_FragColor = vec4(targetColor.r, targetColor.g, targetColor.b, savedAlpha);
	}
}