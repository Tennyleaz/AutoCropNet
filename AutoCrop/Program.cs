using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace AutoCropNet
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Bitmap bmp = new Bitmap("mypic01.bmp");
                XBitmap xBitmap = new XBitmap(bmp);

                BITMAPPTR outBitmapPtr = new BITMAPPTR();                
                int nCropRtn = AutoCrop.autocrop(ref outBitmapPtr.pHeader, xBitmap.PtrInfo, 1);
                Console.WriteLine("autocrop return=" + nCropRtn);
                if (nCropRtn > 0 && outBitmapPtr.pHeader != IntPtr.Zero)
                {
                    //int BitCount = bitmapptrout.pHeader->biBitCount;
                    //int nColorData = (BitCount <= 8) ? 1 << BitCount : 0;
                    //bitmapptrout.pQuad = (RGBQUAD*)(bitmapptrout.pHeader + 1);
                    //bitmapptrout.pBmp = (PBYTE)(bitmapptrout.pQuad + nColorData);
                    //bmpImage.CopyFrom(bitmapptrout);
                    //bmpfree(&bitmapptrout.pHeader);
                    BITMAPINFOHEADER newHeader = Marshal.PtrToStructure<BITMAPINFOHEADER>(outBitmapPtr.pHeader);
                    int BitCount = newHeader.biBitCount;
                    int nColorData = (BitCount <= 8) ? 1 << BitCount : 0;
                    outBitmapPtr.pQuad = outBitmapPtr.pHeader + Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                    outBitmapPtr.pBmp = outBitmapPtr.pQuad + nColorData;

                    //int dwBytesPerLine = (newHeader.biWidth * 24 / 8 + 3) / 4 * 4;
                    int stride = ((((newHeader.biWidth * newHeader.biBitCount) + 31) & ~31) >> 3);
                    Bitmap outputBmp = new Bitmap(newHeader.biWidth, newHeader.biHeight, stride, PixelFormat.Format24bppRgb, outBitmapPtr.pBmp);
                    outputBmp.Save("result.bmp", ImageFormat.Bmp);
                }                

                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadLine();
        }
    }
}
