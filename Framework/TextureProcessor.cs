using UnityEngine;

namespace ModularFirearms
{
    /// <summary>
    /// original digits grid texture is 1024x512
    /// individual digits are tiles of 128x256
    /// returns: a new target texture of 256x256 with the specified digits
    /// </summary>
    public class TextureProcessor
    {
        public Renderer outputMesh;
        public Texture2D baseTexture;
        private Texture2D outputTexture;
        private Color[,] pixelColorsOnes;
        private Color[,] pixelColorsTens;

        private Color[,] GetPixelColorArray(int digit, Texture2D baseTexture, int x_size, int y_size, int overflowIndex = 7, int verticalBufferPx = 256)
        {
            Color[,] pixelColors;
            digit = digit % 10;
            int x_o = 0;
            int y_o = 0;
            int y_offset = 0;
            if (digit > overflowIndex)
                y_offset = verticalBufferPx;
            pixelColors = new Color[x_size, y_size];
            for (int y = 0; y < 256; y++)
            {
                for (int x = (0 + 128 * (digit % (overflowIndex + 1))); x < (128 + 128 * (digit % (overflowIndex + 1)) - 1); x++)
                {
                    pixelColors[x_o, y_o] = baseTexture.GetPixel(x, y + y_offset);
                    x_o++;
                }
                x_o = 0;
                y_o++;
            }
            return pixelColors;
        }

        public void SetTargetRenderer(Renderer newRenderer) { outputMesh = newRenderer; }

        public void SetGridTexture(Texture2D newDigitGrid) { baseTexture = newDigitGrid; }

        public void DisplayUpdate(int displayValue)
        {
            RenderToMesh(outputMesh, GetNumberTexture(displayValue));
        }

        protected Texture2D GetNumberTexture(int numberValue, int digit_size_x = 128, int digit_size_y = 256)
        {
            // From number value, calculate the ones and tens place positions
            int ones = numberValue % 10;
            int tens = (numberValue / 10) % 10;
            // Using the refernce grid of digits, extract the pixel values for given digits
            pixelColorsOnes = GetPixelColorArray(ones, baseTexture, digit_size_x, digit_size_y);
            pixelColorsTens = GetPixelColorArray(tens, baseTexture, digit_size_x, digit_size_y);
            // Build target texture from pixel color arrays of tens and ones (Processed left-to-right, respectively)
            outputTexture = new Texture2D(digit_size_x * 2, digit_size_y);
            for (int y = 0; y < outputTexture.height; y++)
            {
                // First half of target texture image (tens place digit)
                for (int x = 0; x < digit_size_x; x++)
                {
                    if ((y <= pixelColorsTens.GetUpperBound(1)) && (x <= pixelColorsTens.GetUpperBound(0)))
                        outputTexture.SetPixel(x, y, pixelColorsTens[x, y]);
                    else
                        outputTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
                // Second half of target texture image (ones place digit)
                for (int x = digit_size_x; x < digit_size_x * 2; x++)
                {
                    if ((y <= pixelColorsOnes.GetUpperBound(1)) && (x - digit_size_x <= pixelColorsOnes.GetUpperBound(0)))
                        outputTexture.SetPixel(x, y, pixelColorsOnes[x - digit_size_x, y]);
                    else
                        outputTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            // Apply changes and return a reference to this texture
            outputTexture.Apply();
            return outputTexture;
        }

        protected void RenderToMesh(Renderer r, Texture2D t)
        {
            r.material.mainTexture = t;
        }

        protected Texture2D CurrentDigitTexture()
        {
            return outputTexture;
        }
    }
}
