using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ThunderRoad;

namespace ModularFirearms
{
    public class TextureProcessor
    {
        /// <summary>
        /// original digits grid texture is 1024x512
        /// individual digits are tiles of 128x256
        /// returns: a new target texture of 256x256 with the specified digits
        /// </summary>
        //private Texture2D[] possibleOutputs;
        //private readonly int verticalBufferPx = 256;

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
            {
                y_offset = verticalBufferPx;
            }
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
            //int digit_size_x = 128;
            //int digit_size_y = 256;
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
                    {
                        outputTexture.SetPixel(x, y, pixelColorsTens[x, y]);
                    }
                    else
                    {
                        outputTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
                // Second half of target texture image (ones place digit)
                for (int x = digit_size_x; x < digit_size_x * 2; x++)
                {
                    if ((y <= pixelColorsOnes.GetUpperBound(1)) && (x - digit_size_x <= pixelColorsOnes.GetUpperBound(0)))
                    {
                        outputTexture.SetPixel(x, y, pixelColorsOnes[x - digit_size_x, y]);
                    }
                    else
                    {
                        outputTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
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

//Texture2D texture = new Texture2D(128, 128);
//GetComponent<Renderer>().material.mainTexture = texture;
//int tens = (digit / 10) % 10;
//Debug.Log("Tens: " + tens + " Ones: " + ones);
//private readonly int[,] digitBounds = { { 0, 127 }, { 128, 255 }, { 256, 383 }, { 384, 511 }, { 512, 639 }, { 640, 767 }, { 768, 895 }, { 896, 1023 }, { 0, 127 }, { 128, 255 } };
// Formula for digitBounds reads as (0+128*(i%8)) for lower bound, (128 + 128*(i%8) - 1) for upper bound

//void Start()
//{
//    Texture2D t = GetDigit(digit);
//    RenderToMesh(outputMesh, t);
//    lastDigit = digit;
//}

//void Update()
//{
//    if (digit != lastDigit)
//    {
//        lastDigit = digit;
//        Texture2D t = GetDigit(digit);
//        RenderToMesh(outputMesh, t);
//    }
//}

/*

        public Texture2D GetMergedDigit(int digit)
        {

            //Texture2D texture = new Texture2D(128, 128);
            //GetComponent<Renderer>().material.mainTexture = texture;
            int ones = digit % 10;
            int tens = (digit / 10) % 10;
            pixelColorsOnes = GetPixelColorArray(ones, digitsGrid, 128, 256);
            pixelColorsTens = GetPixelColorArray(tens, digitsGrid, 128, 256);
            //Debug.Log("Tens: " + tens + " Ones: " + ones);
            //int x_o = 0;
            //int y_o = 0;
            //int y_offset = 0;
            //if (ones > 7)
            //{
            //    y_offset = verticalBufferPx;
            //}
            //pixelColorsOnes = new Color[256, 128];
            //for (int y = 0; y < 256; y++)
            //{
            //    for (int x = (0 + 128 * (ones % 8)); x < (128 + 128 * (ones % 8) - 1); x++)
            //    {
            //        pixelColorsOnes[y_o, x_o] = digitsGrid.GetPixel(x, y + y_offset);
            //        x_o++;
            //    }
            //    x_o = 0;
            //    y_o++;
            //}

            //x_o = 0;
            //y_o = 0;
            //y_offset = 0;
            //if (tens > 7)
            //{
            //    y_offset = verticalBufferPx;
            //}
            //pixelColorsTens = new Color[256, 128];
            //for (int y = 0; y < 256; y++)
            //{
            //    for (int x = (0 + 128 * (tens % 8)); x < (128 + 128 * (tens % 8) - 1); x++)
            //    {
            //        pixelColorsTens[y_o, x_o] = digitsGrid.GetPixel(x, y + y_offset);
            //        x_o++;
            //    }
            //    x_o = 0;
            //    y_o++;
            //}

            outputTexture = new Texture2D(256, 256);
            for (int y = 0; y < outputTexture.height; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    if ((y <= pixelColorsTens.GetUpperBound(1)) && (x <= pixelColorsTens.GetUpperBound(0)))
                    {
                        outputTexture.SetPixel(x, y, pixelColorsTens[x, y]);
                    }
                    else
                    {
                        outputTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
                for (int x = 128; x < 256; x++)
                {
                    if ((y <= pixelColorsOnes.GetUpperBound(1)) && (x - 128 <= pixelColorsOnes.GetUpperBound(0)))
                    {
                        outputTexture.SetPixel(x, y, pixelColorsOnes[x - 128, y]);
                    }
                    else
                    {
                        outputTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            outputTexture.Apply();

            return outputTexture;
        } 
     
*/
