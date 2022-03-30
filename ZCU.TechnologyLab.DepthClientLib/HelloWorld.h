#pragma once
#define MODULE_API_EXPORTS
#ifdef __cplusplus
extern "C" {
#endif
#ifdef _WIN32
#  ifdef MODULE_API_EXPORTS
#    define MODULE_API __declspec(dllexport)
#  else
#    define MODULE_API __declspec(dllimport)
#  endif
#else
#  define MODULE_API
#endif
MODULE_API void Start(const char* filePath);
MODULE_API void Exit();
MODULE_API bool GetFrame(float** vertices,int** faces, int* itemCount, int* faceCount);
MODULE_API void DropFrame(float* items, int** indices);
//	MODULE_API void PrintHelloWorld();
#ifdef __cplusplus
}
#endif
