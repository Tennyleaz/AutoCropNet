using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoCropNet
{
    internal class XBitmap
    {        
        public unsafe XBitmap(Bitmap bitmap)
        {
            if (bitmap == null || bitmap.PixelFormat != PixelFormat.Format24bppRgb)
                throw new ArgumentException("Wrong PixelFormat");

            ushort wBitBerPixel = 24;
            //int nColors = 1 << wBitBerPixel;  // 2^24色
            int dwBytesPerLine = (bitmap.Width * wBitBerPixel / 8 + 3) / 4 * 4;  // 除4乘4 因為BMP stride是4的倍數
            int nSize = dwBytesPerLine * bitmap.Height;
            nSize += Marshal.SizeOf(typeof(BITMAPINFOHEADER));

            // Create a new managed struct
            BITMAPINFOHEADER myHeader = new BITMAPINFOHEADER();

            // create memory
            //pInfo = (BITMAPINFO*) new  BYTE[nSize];            
            //pBit = ((BYTE*)pInfo) + sizeof(BITMAPINFOHEADER);
            PtrInfo = Marshal.AllocHGlobal(nSize);
            PtrBit = PtrInfo + Marshal.SizeOf(typeof(BITMAPINFOHEADER));

            // copy header
            //pInfo->bmiHeader.biSize = sizeof(BITMAPINFOHEADER);            
            //pInfo->bmiHeader.biWidth = dwWidth;
            //pInfo->bmiHeader.biHeight = dwHeight;
            //pInfo->bmiHeader.biPlanes = 1;
            //pInfo->bmiHeader.biBitCount = wBitBerPixel;
            //pInfo->bmiHeader.biCompression = 0;
            //pInfo->bmiHeader.biSizeImage = nSize;//dwBytesPerLine * dwHeight;
            //pInfo->bmiHeader.biXPelsPerMeter = 5906;
            //pInfo->bmiHeader.biYPelsPerMeter = 5906;
            //pInfo->bmiHeader.biClrUsed = 0;
            //pInfo->bmiHeader.biClrImportant = BI_RGB;
            myHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            myHeader.biWidth = bitmap.Width;
            myHeader.biHeight = bitmap.Height;
            myHeader.biPlanes = 1;
            myHeader.biBitCount = wBitBerPixel;
            myHeader.biCompression = 0;
            myHeader.biSizeImage = (uint)nSize;
            myHeader.biXPelsPerMeter = 5906;  // ~96dpi
            myHeader.biYPelsPerMeter = 5906;  // ~96dpi
            myHeader.biClrUsed = 0;
            myHeader.biClrImportant = 0;
            Marshal.StructureToPtr(myHeader, PtrInfo, false);

            // copy memory
            //memcpy(pBit, pdata, dataSize);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            int length = bitmapData.Stride * bitmapData.Height;            
            Buffer.MemoryCopy(bitmapData.Scan0.ToPointer(), PtrBit.ToPointer(), length, length);
            bitmap.UnlockBits(bitmapData);
            bitmap.Dispose();
        }

        public unsafe XBitmap(BITMAPPTR bmap)
        {
            //DWORD dwBytesPerLine = (bmap.pHeader->biWidth * bmap.pHeader->biBitCount / 8 + 3) / 4 * 4;
            BITMAPINFOHEADER newHeader = Marshal.PtrToStructure<BITMAPINFOHEADER>(bmap.pHeader);
            int dwBytesPerLine = (newHeader.biWidth * newHeader.biBitCount / 8 + 3) / 4 * 4;

            int nSize = dwBytesPerLine * newHeader.biHeight;
            //int biClrUsed = 0;
            //int nColors = 1 << wBitBerPixel;  // 2^24色
            nSize += Marshal.SizeOf(typeof(BITMAPINFOHEADER));

            // create memory
            //pInfo = (BITMAPINFO*) new  BYTE[nSize];            
            //pBit = ((BYTE*)pInfo) + sizeof(BITMAPINFOHEADER);
            PtrInfo = Marshal.AllocHGlobal(nSize);
            PtrBit = PtrInfo + Marshal.SizeOf(typeof(BITMAPINFOHEADER));            

            // copy data
            //pInfo->bmiHeader.biSizeImage = dwBytesPerLine * bmap.pHeader->biHeight;
            //memcpy(pBit, bmap.pBmp, pInfo->bmiHeader.biSizeImage);
            newHeader.biSizeImage = (uint)(dwBytesPerLine * newHeader.biHeight);
            byte[] imgData = new byte[newHeader.biSizeImage];
            Marshal.Copy(bmap.pBmp, imgData, 0, (int)newHeader.biSizeImage);
            Marshal.Copy(imgData, 0, PtrBit, (int)newHeader.biSizeImage);
            //Buffer.MemoryCopy(bmap.pBmp.ToPointer(), PtrBit.ToPointer(), newHeader.biSizeImage, newHeader.biSizeImage);

            // copy header values            
            newHeader.biCompression = 0;            
            newHeader.biXPelsPerMeter = 5906;
            newHeader.biYPelsPerMeter = 5906;
            newHeader.biClrUsed = 0;
            newHeader.biClrImportant = 0;
            Marshal.StructureToPtr(newHeader, PtrInfo, false);

            BITMAPFILEHEADER fileHeader = new BITMAPFILEHEADER
            {
                bfType = 0x4D42,
                bfOffBits = (uint)Marshal.SizeOf(typeof(BITMAPFILEHEADER)) + newHeader.biSize,
                bfSize = newHeader.biSizeImage,
                bfReserved1 = 0,
                bfReserved2 = 0
            };            

            //using (FileStream myFileStream = new FileStream("resultv2.bmp", FileMode.Create))
            //{
            //    byte[] data1 = DataToByteArray(newHeader);
            //    myFileStream.Write(data1, 0, data1.Length);

            //    byte[] data2 = DataToByteArray(fileHeader);
            //    myFileStream.Write(data2, 0, data2.Length);

            //    myFileStream.Write(imgData, 0, imgData.Length);
            //    myFileStream.Close();
            //}
        }

        /// <summary>
        /// Points to BITMAPINFOHEADER.
        /// </summary>
        public IntPtr PtrInfo { get; private set; }

        /// <summary>
        /// Points to RGB array.
        /// </summary>
        public IntPtr PtrBit { get; private set; }

        //public uint DwCurRow { get; private set; }

        public void SaveImage(string path)
        {
            BITMAPINFOHEADER infoHeader = Marshal.PtrToStructure<BITMAPINFOHEADER>(PtrInfo);

            //BITMAPFILEHEADER bmfh;
            //int nBitsOffset = sizeof(BITMAPFILEHEADER) + bmih.biSize;
            //LONG lImageSize = bmih.biSizeImage;
            //LONG lFileSize = nBitsOffset + lImageSize;
            //bmfh.bfType = 'B' + ('M' << 8);
            //bmfh.bfOffBits = nBitsOffset;
            //bmfh.bfSize = lFileSize;
            //bmfh.bfReserved1 = bmfh.bfReserved2 = 0;            
            BITMAPFILEHEADER fileHeader = new BITMAPFILEHEADER
            {
                bfType = 0x4D42,
                bfOffBits = (uint)Marshal.SizeOf(typeof(BITMAPFILEHEADER)) + infoHeader.biSize,
                bfSize = infoHeader.biSizeImage,
                bfReserved1 = 0,
                bfReserved2 = 0
            };            

            using (FileStream myFileStream = new FileStream(path, FileMode.Create))
            {
                byte[] data1 = DataToByteArray(infoHeader);
                byte[] data2 = DataToByteArray(fileHeader);
                byte[] data3 = new byte[infoHeader.biSizeImage];
                Marshal.Copy(PtrBit, data3, 0, (int)infoHeader.biSizeImage);
                using (BinaryWriter writer = new BinaryWriter(myFileStream))
                {
                    writer.Write(data1);
                    writer.Write(data2);
                    writer.Write(data3);
                }
                //byte[] data1 = DataToByteArray(infoHeader);
                //myFileStream.Write(data1, 0, data1.Length);

                //byte[] data2 = DataToByteArray(fileHeader);
                //myFileStream.Write(data2, 0, data2.Length);

                //byte[] data3 = new byte[infoHeader.biSizeImage];
                //Marshal.Copy(PtrBit, data3, 0, (int)infoHeader.biSizeImage);
                //myFileStream.Write(data3, 0, data3.Length);
                //myFileStream.Close();
            }
        }

        private byte[] DataToByteArray<T>(T data)
        {
            int length = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(length);
            byte[] myBuffer = new byte[length];
            Marshal.StructureToPtr<T>(data, ptr, true);
            Marshal.Copy(ptr, myBuffer, 0, length);
            Marshal.FreeHGlobal(ptr);
            return myBuffer;
        }
    }    

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        /// BITMAPINFOHEADER->tagBITMAPINFOHEADER
        public BITMAPINFOHEADER bmiHeader;

        /// RGBQUAD[1]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
        public RGBQUAD[] bmiColors;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        /// DWORD->unsigned int
        public uint biSize;

        /// LONG->int
        public int biWidth;

        /// LONG->int
        public int biHeight;

        /// WORD->unsigned short
        public ushort biPlanes;

        /// WORD->unsigned short
        public ushort biBitCount;

        /// DWORD->unsigned int
        public uint biCompression;

        /// DWORD->unsigned int
        public uint biSizeImage;

        /// LONG->int
        public int biXPelsPerMeter;

        /// LONG->int
        public int biYPelsPerMeter;

        /// DWORD->unsigned int
        public uint biClrUsed;

        /// DWORD->unsigned int
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPFILEHEADER
    {
        /// WORD->unsigned short
        public ushort bfType;

        /// DWORD->unsigned int
        public uint bfSize;

        /// WORD->unsigned short
        public ushort bfReserved1;

        /// WORD->unsigned short
        public ushort bfReserved2;

        /// DWORD->unsigned int
        public uint bfOffBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD
    {
        /// BYTE->unsigned char
        public byte rgbBlue;

        /// BYTE->unsigned char
        public byte rgbGreen;

        /// BYTE->unsigned char
        public byte rgbRed;

        /// BYTE->unsigned char
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPPTR
    {
        /// PBITMAPINFOHEADER->tagBITMAPINFOHEADER*
        public IntPtr pHeader;

        /// RGBQUAD*
        public IntPtr pQuad;

        /// PBYTE->BYTE*
        public IntPtr pBmp;
    }

    public class AutoCrop
    {
        /// Return Type: int
        ///param0: BITMAPINFOHEADER**
        ///param1: BITMAPINFOHEADER*
        ///param2: int
        [DllImport("atocropLIB.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int autocrop(ref IntPtr outputPtr, IntPtr inputHeader, int isDeskew);
    }    
}
