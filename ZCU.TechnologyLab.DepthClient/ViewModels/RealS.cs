using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    /// <summary>
    /// Class used to work with external C++ library controlling the connected camera
    /// </summary>
    public static class RealS
    {
        /// <summary> New Frames cannot be obtained </summary>
        private static bool NewFramesForbidden { get; set; } = true;

        /// <summary> Init was successfully called </summary>
        public static bool Started { get; set; }

        /// <summary> Name of last initialized .bag file </summary>
        public static string? BagFile { get; private set; }

        /// <summary> Number of allocated frames </summary>
        public static volatile int FramesAllocated;

        static class RealSenseWrapper
        {
            const string DLL_PATH = @"ZCU.TechnologyLab.DepthClientLib.dll";

            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool Start(string filePath);


            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            public static extern void Exit();

            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern unsafe bool GetFrame(
                out float* vertices,
                out int* faces,
                out float* uvs,
                out byte* colors,
                out byte* binaryPly,
                out int vertexCount,
                out int faceCount,
                out int uvCount,
                out int colorsCount,
                out int plyLength,
                out int width);


            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern unsafe bool GetFrameOnce(out ushort* depths, out byte* colors, out int width,
                out int height);

            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe void DropDepthFrame(ushort* depths);

            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe void DropFrame(float* items, int* faces, byte* ply);

            [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            public static extern unsafe void UpdateFilters(bool* filterss, float* filter_data);
        }

        /// <summary>
        /// Initializes depth stream from file or camera 
        /// If filePath is empty, camera stream is chosen
        /// </summary>
        /// <param name="filePath"> Path to file </param>
        /// <returns> return true if successfully opened stream </returns>
        public static bool Init(string filePath)
        {
            lock (syncLock)
            {
                //wait until nobody uses it
                if (Started && BagFile == filePath && BagFile == "")
                    return true; //no new camera connection if old one works
                if (Started)
                    RealSenseWrapper.Exit();


                Started = RealSenseWrapper.Start(filePath);
                if (Started)
                {
                    BagFile = filePath;
                    NewFramesForbidden = false;
                }

                return Started;
            }
        }

        private static readonly object syncLock = new object();

        /// <summary>
        /// Waits until all Frames are disposed and then shuts down the stream 
        /// </summary>
        public static void Exit()
        {
            lock (syncLock)
            {
                NewFramesForbidden = true;
                //while (FramesAllocated > 0)
                //    Thread.Sleep(10);


                if (Started)
                    RealSenseWrapper.Exit();
                Started = false;
            }
        }

        /// <summary>
        /// Update filters in C++ library
        /// 
        /// Order of filter parameters
        /// 1. Decimation filter - linear scale factor,
        /// 2. Threshold filter - min value, max value,
        /// 3. Spatial smoothing - number of iterations, alpha, delta, hole filling method,
        /// 4. Temporal smoothing - alpha, delta, persistency index
        /// 5. Hole filling - method
        /// 
        /// </summary>
        /// <param name="enables"> Which filters are enabled </param>
        /// <param name="filterData"> Parameters of filters </param>
        public static void updateFilters(bool[] enables, float[] filterData)
        {
            unsafe
            {
                fixed (bool* FirstResult = &enables[0])
                {
                    fixed (float* Data = &filterData[0])
                    {
                        RealSenseWrapper.UpdateFilters(FirstResult, Data);
                    }
                }
            }
        }

        /// <summary>
        /// Managed memory block that can be recycled 
        /// </summary>
        public class DepthFrameBuffer
        {
            public ushort[] Depths = Array.Empty<ushort>();
            public byte[] Colors = Array.Empty<byte>();
            public int Width, Height;
        }


        /// <summary>
        /// Frame read from unmanaged memory, after use Dispose 
        /// Depth Map + Colorized Depth Map
        /// </summary>
        public struct DepthFrame : IDisposable
        {
            public IntPtr Pointer;
            public DepthFrameBuffer Buffer;
            public ushort[] Depths => Buffer.Depths;
            public byte[] Colors => Buffer.Colors;
            public int Width => Buffer.Width;
            public int Height => Buffer.Height;


            public int Stride => Width * sizeof(ushort);
            public ushort[] Data => Depths;

            public byte[] ColorData => Colors;
            public int ColorStride => Width * sizeof(byte) * 3;

            public static DepthFrame? Obtain(DepthFrameBuffer buffer)
            {
                if (NewFramesForbidden)
                    return null;
                Interlocked.Increment(ref FramesAllocated);

                try
                {
                    unsafe
                    {
                        if (!RealSenseWrapper.GetFrameOnce(
                                out var depths,
                                out var colors,
                                out var width,
                                out var height))
                            throw new Exception();

                        var length = width * height;
                        var lengthColors = width * height * 3;

                        if (buffer.Depths.Length != length)
                            buffer.Depths = new ushort[length];

                        if (buffer.Colors.Length != lengthColors)
                            buffer.Colors = new byte[lengthColors];

                        buffer.Width = width;
                        buffer.Height = height;

                        //todo why on earth i cannot copy to ushort array
                        new Span<ushort>(depths, length)
                            .CopyTo(buffer.Depths);
                        new Span<byte>(colors, lengthColors)
                            .CopyTo(buffer.Colors);


                        return new DepthFrame
                        {
                            Buffer = buffer,
                            Pointer = (IntPtr)depths
                        };
                    }
                }
                catch
                {
                    Interlocked.Decrement(ref FramesAllocated);
                    return null;
                }
            }

            public void Dispose()
            {
                unsafe
                {
                    RealSenseWrapper.DropDepthFrame((ushort*)Pointer);
                }

                Interlocked.Decrement(ref FramesAllocated);
            }
        }

        /// <summary>
        /// Frame read from unmanaged memory 
        /// Mesh + Ply File as binary data
        /// </summary>
        public struct MeshFrame
        {
            public float[] Vertices;
            public float[] UVs;
            public int[] Faces;
            public int[] TempFaces;
            public byte[] Ply;
            public byte[] Colors;
            public int Width;

            public int Height => Colors.Length / 3 / Width;

            public static MeshFrame? Obtain()
            {
                if (NewFramesForbidden)
                    return null;

                Interlocked.Increment(ref FramesAllocated);

                MeshFrame meshFrame = new();
                try
                {
                    unsafe
                    {
                        if (!RealSenseWrapper.GetFrame(
                                out var vertices,
                                out var faces,
                                out var uvs,
                                out var colors,
                                out var binaryPly,
                                out var vertexCount,
                                out var faceCount,
                                out var uvCount,
                                out var colorsCount,
                                out var plyLength,
                                out var width))
                            throw new Exception("Cannot get frame from Realsense");

                        meshFrame.Width = width;
                        meshFrame.Vertices = new float[vertexCount];
                        meshFrame.Faces = new int[faceCount];
                        meshFrame.TempFaces = new int[faceCount];
                        meshFrame.Ply = new byte[plyLength];
                        meshFrame.UVs = new float[uvCount];
                        meshFrame.Colors = new byte[colorsCount];


                        Marshal.Copy((IntPtr)vertices, meshFrame.Vertices, 0, vertexCount);
                        Marshal.Copy((IntPtr)faces, meshFrame.Faces, 0, faceCount);
                        Marshal.Copy((IntPtr)binaryPly, meshFrame.Ply, 0, plyLength);
                        Marshal.Copy((IntPtr)uvs, meshFrame.UVs, 0, uvCount);
                        Marshal.Copy((IntPtr)colors, meshFrame.Colors, 0, colorsCount);

                        RealSenseWrapper.DropFrame(vertices, faces, binaryPly);
                        Interlocked.Decrement(ref FramesAllocated);
                    }

                    return meshFrame;
                }
                catch (Exception)
                {
                    Interlocked.Decrement(ref FramesAllocated);
                    return null;
                }
            }
        }
    }

}