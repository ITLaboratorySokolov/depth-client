# License: Apache 2.0. See LICENSE file in root directory.
# Copyright(c) 2019 Intel Corporation. All Rights Reserved.
cmake_minimum_required(VERSION 3.1.0)

project(ZCU.TechnologyLab.DepthClientLib)

SET(RealSense2_ROOT_DIR "C:/Program Files (x86)/Intel RealSense SDK 2.0")

find_path(RealSense2_INCLUDE_DIRS NAMES librealsense2/rs.hpp PATHS ${RealSense2_ROOT_DIR}/include)

SET(CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/cmake_modules")
SET(RealSense2_FIND_REQUIRED true)

# Find librealsense2 installed package
find_package(realsense2 REQUIRED)

# Enable C++11
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED TRUE)

include_directories("${RealSense2_INCLUDE_DIRS}")

# Add the application sources to the target
add_library(${PROJECT_NAME} SHARED RsCapture.h RsCapture.cpp)

# Link librealsense2 to the target
target_link_libraries(${PROJECT_NAME} ${realsense2_LIBRARIES})

set_target_properties(${PROJECT_NAME}
    PROPERTIES
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/../ZCU.TechnologyLab.DepthClient/bin/$<CONFIG>/net6.0-windows/
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/../ZCU.TechnologyLab.DepthClient/bin/$<CONFIG>/net6.0-windows/
)

add_custom_command(
        TARGET ${PROJECT_NAME}  POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy 
                "${RealSense2_ROOT_DIR}/bin/x64/realsense2.dll" 
                "${CMAKE_BINARY_DIR}/../ZCU.TechnologyLab.DepthClient/bin/$<CONFIG>/net6.0-windows/realsense2.dll")
