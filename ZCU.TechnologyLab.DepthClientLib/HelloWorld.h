#pragma once
#define MODULE_API_EXPORTS
#include <map>
#include <librealsense2/hpp/rs_export.hpp>
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
MODULE_API bool GetFrame(float** vertices, int** faces, int* itemCount, int* faceCount,uint8_t** binaryPly,int* plyLength);
MODULE_API void DropFrame(float* items, int* indices,uint8_t* ply);
//	MODULE_API void PrintHelloWorld();
#ifdef __cplusplus
}
#endif

