using System.Runtime.InteropServices;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public static class RealSenseWrapper
    {
        private const string DLL_PATH = @"ZCU.TechnologyLab.DepthClientLib.dll";

        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Start(string filePath);

        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Exit();
        
        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetFrame(out float* vertices,out int* faces, out int vertexCount,out int faceCount,out byte* binaryPly,out int plyLength);

        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void DropFrame(float* items, int* faces,byte* ply);

    }
}