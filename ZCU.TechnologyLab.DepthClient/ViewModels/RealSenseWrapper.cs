using System.Runtime.InteropServices;

namespace ZCU.TechnologyLab.DepthClient.ViewModels
{
    public static class RealSenseWrapper
    {
        private const string DLL_PATH = @"C:\D\Uni\gymso\PROJECT\depth-client\ZCU.TechnologyLab.DepthClientLib\Release\";

        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Start(string filePath);

        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Exit();
        
        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe bool GetFrame(out float* vertices,out int* faces, out int vertexCount,out int faceCount);

        [DllImport(DLL_PATH, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe void DropFrame(float* items, int* faces);

    }
}