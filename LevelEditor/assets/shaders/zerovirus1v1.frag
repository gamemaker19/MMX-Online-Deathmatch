uniform sampler2D texture;
uniform sampler2D image;
uniform int imageW;
uniform int imageH;
uniform float t;

void main()
{
   vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
   if(pixel.a == 0.0)
   {
      gl_FragColor = pixel;
   }
   else
   {
      float factor = 0.0;
      float tMod = mod(t, 2.0);
      if (tMod < 0.25) factor = 0.0;
      else if (tMod >= 0.25 && tMod < 0.5) factor = 0.25;
      else if (tMod >= 0.5 && tMod < 0.75) factor = 0.5;
      else if (tMod >= 0.75 && tMod < 1.0) factor = 0.75;
      else if (tMod >= 1.0 && tMod < 1.25) factor = 1.0;
      else if (tMod >= 1.25 && tMod < 1.5) factor = 0.75;
      else if (tMod >= 1.5 && tMod < 1.75) factor = 0.5;
      else if (tMod >= 1.75 && tMod < 2.0) factor = 0.25;
      else if (tMod >= 2.0) factor = 0.0;
      gl_FragColor = vec4(pixel.r + (factor - 0.5)*(75.0/255.0), pixel.g - (factor)*(50.0/255.0), pixel.b - (factor - 0.5)*(50.0/255.0), pixel.a);
   }
}