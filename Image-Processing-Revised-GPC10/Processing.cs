using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Image_Processing_Revised_GPC10
{
    public class Processing
    {
        public static double MedianFiltering(Bitmap bitmap, int matrixSize)
        {
            List<byte> termsList = new List<byte>();
            byte[,] image = new byte[bitmap.Width,bitmap.Height];
            byte[,] original = new byte[bitmap.Width,bitmap.Height];

            float mseLoop = 0;
            
            // convert to Grayscale
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color color = bitmap.GetPixel(i, j);
                    int gray = (byte) (0.333 * color.R + 0.333 * color.G + 0.333 * color.B);
                    image[i, j] = (byte) gray;
                }
            }
            
            // applying median filter
            for (int i = 0; i < bitmap.Width - matrixSize; i++)
            {
                for (int j = 0; j < bitmap.Height - matrixSize; j++)
                {
                    for (int k = i; k <= i + (matrixSize - 1); k++)
                    {
                        for (int l = j; l <= j + (matrixSize - 1); l++)
                        {
                            termsList.Add(image[k, l]);
                        }
                    }

                    byte[] terms = termsList.ToArray();
                    termsList.Clear();
                    
                    Array.Sort(terms);
                    Array.Reverse(terms);
                    // index-4
                    byte color = terms[4];
                    bitmap.SetPixel(i + 1, j + 1, Color.FromArgb(color, color, color));
                    
                    image[i + 1, j + 1] = color;

                    mseLoop = image[i + 1, j + 1] - original[i + 1, j + 1];
                }
            }
            
            // MSE = Mean Square Error
            float mse = (mseLoop * mseLoop) * (bitmap.Height * bitmap.Width);
            
            // PSNR = Peak Signal-to-Noise Ratio
            double psnr = -10 * Math.Log10((255 * 255) / mse);
            
            return psnr;
        }

        public static Tuple<Bitmap, double> ConvolutionFilter(Bitmap bitmap, double[,] matrix)
        {
            Bitmap copyBitmap = bitmap;
            double factor = 1.0 / 9.0;

            double temp = 0;

            BitmapData sourceData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);
            bitmap.UnlockBits(sourceData);

            int filterWidth = matrix.GetLength(1);

            int filterOffset = (filterWidth - 1) / 2;

            for (int offsetY = filterOffset; offsetY < bitmap.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < bitmap.Width - filterOffset; offsetX++)
                {
                    double blue = 0;
                    double green = 0;
                    double red = 0;

                    var byteOffset = offsetY * sourceData.Stride + offsetX * 4;

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            var calculateOffset = byteOffset + (filterX * 4) + (filterY * sourceData.Stride);

                            blue += pixelBuffer[calculateOffset] *
                                    matrix[filterY + filterOffset, filterX + filterOffset];

                            green += (pixelBuffer[calculateOffset] + 1) *
                                     matrix[filterY + filterOffset, filterX + filterOffset];
                            
                            red += (pixelBuffer[calculateOffset] + 2) *
                                matrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    blue = factor * blue;
                    green = factor * green;
                    red = factor * red;

                    blue = (blue > 255 ? 255 : (blue < 0 ? 0 : blue));
                    green = (green > 255 ? 255 : (green < 0 ? 0 : green));
                    red = (red > 255 ? 255 : (red < 0 ? 0 : red));

                    pixelBuffer[byteOffset] = (byte) (blue);
                    pixelBuffer[byteOffset + 1] = (byte) (green);
                    pixelBuffer[byteOffset + 2] = (byte) (red);
                    pixelBuffer[byteOffset + 3] = 255;

                    Color colorOriginal = copyBitmap.GetPixel(offsetX, offsetY);
                    double redOriginal = colorOriginal.R;
                    double greenOriginal = colorOriginal.G;
                    double blueOriginal = colorOriginal.B;

                    double redOutput = red - redOriginal;
                    double greenOutput = green - greenOriginal;
                    double blueOutput = blue - blueOriginal;

                    temp = temp + redOutput + greenOutput + blueOutput;
                }
            }
            
            Bitmap resultBitmap = new Bitmap(bitmap.Width, bitmap.Height);

            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            
            Marshal.Copy(pixelBuffer, 0, resultData.Scan0, pixelBuffer.Length);
            resultBitmap.UnlockBits(resultData);

            // MSE = Mean Square Error
            double mse = (temp * temp) / (bitmap.Height * bitmap.Width);

            // PSNR = Peak Signal-to-Noise Ratio
            double psnr = -10 * Math.Log10((255 * 255) / mse);

            return Tuple.Create(resultBitmap, psnr);
        }
    }
}