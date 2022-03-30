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
	MODULE_API void Start();
	MODULE_API void Hell();

	MODULE_API bool GetFrame(float** items, int* itemCount);
	MODULE_API void DropFrame(float* items);


	MODULE_API void Exit();

#ifdef __cplusplus
}

#endif