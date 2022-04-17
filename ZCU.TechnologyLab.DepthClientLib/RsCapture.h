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
MODULE_API bool Start(const char* filePath);
MODULE_API void UpdateFilters(bool* filters);

MODULE_API void Exit();
MODULE_API bool GetFrame(
	float** vertices,
	int** faces,
	float** uvs,
	uint8_t** colors,
	uint8_t** binaryPly,
	int* vertexCount,
	int* faceCount,
	int* uvsCount,
	int* colorsLength,
	int* plyLength,
	int* width);
MODULE_API bool GetFrameOnce(uint16_t** depths, uint8_t** colors, int* width, int* height);
MODULE_API void DropDepthFrame(uint16_t* depths);
MODULE_API void DropFrame(float* items, int* indices,uint8_t* ply);
//	MODULE_API void PrintHelloWorld();
#ifdef __cplusplus
}
#endif

