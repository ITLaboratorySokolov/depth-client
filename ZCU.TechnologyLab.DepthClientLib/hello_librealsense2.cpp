// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2017 Intel Corporation. All Rights Reserved.
#include "hello_librealsense2.h"
#include <librealsense2/rs.hpp>
#include <librealsense2/hpp/rs_internal.hpp>
#include <iostream>
#include <map>

void inner();

void Start()
{
	//std::cout <<"Loding file: "  << "\n";
	return;

	try { inner(); }
	catch (std::exception& e) { std::cout << "Error " << e.what() << std::endl; }
}

void main(const char**)
{
	try { inner(); }
	catch (std::exception& e) { std::cout << "Error " << e.what() << std::endl; }
}

bool device_with_streams(std::vector<rs2_stream> stream_requests, std::string& out_serial)
{
	rs2::context ctx;
	auto devs = ctx.query_devices();
	std::vector<rs2_stream> unavailable_streams = stream_requests;
	for (auto dev : devs)
	{
		std::map<rs2_stream, bool> found_streams;
		for (auto& type : stream_requests)
		{
			found_streams[type] = false;
			for (auto& sensor : dev.query_sensors())
			{
				for (auto& profile : sensor.get_stream_profiles())
				{
					if (profile.stream_type() == type)
					{
						found_streams[type] = true;
						unavailable_streams.erase(std::remove(unavailable_streams.begin(), unavailable_streams.end(), type),
						                          unavailable_streams.end());
						if (dev.supports(RS2_CAMERA_INFO_SERIAL_NUMBER))
							out_serial = dev.get_info(RS2_CAMERA_INFO_SERIAL_NUMBER);
					}
				}
			}
		}
		// Check if all streams are found in current device
		bool found_all_streams = true;
		for (auto& stream : found_streams)
		{
			if (!stream.second)
			{
				found_all_streams = false;
				break;
			}
		}
		if (found_all_streams)
			return true;
	}
	// After scanning all devices, not all requested streams were found
	for (auto& type : unavailable_streams)
	{
		switch (type)
		{
		case RS2_STREAM_POSE:
		case RS2_STREAM_FISHEYE:
			std::cerr << "Connect T26X and rerun the demo" << std::endl;
			break;
		case RS2_STREAM_DEPTH:
			std::cerr << "The demo requires Realsense camera with DEPTH sensor" << std::endl;
			break;
		case RS2_STREAM_COLOR:
			std::cerr << "The demo requires Realsense camera with RGB sensor" << std::endl;
			break;
		default:
			throw std::runtime_error(
				"The requested stream: " + std::to_string(type) + ", for the demo is not supported by connected devices!");
		// stream type
		}
	}
	return false;
}

rs2::pointcloud pc;
// We want the points object to be persistent so we can display the last cloud when a frame drops
rs2::points points;

// Declare RealSense pipeline, encapsulating the actual device and sensors
rs2::pipeline pipe;

void inner()
{
	std::cout << "hello from librealsense - " << RS2_API_VERSION_STR << std::endl;
	// std::string serial;
	// if (!device_with_streams({ RS2_STREAM_COLOR,RS2_STREAM_DEPTH }, serial))
	//    return;
	// Start streaming with default recommended configuration
	rs2::config cfg;
	cfg.enable_device_from_file("d435i_walk_around.bag", true);
	//if (!serial.empty())
	//   cfg.enable_device(serial);
	cfg.enable_stream(RS2_STREAM_DEPTH);
	cfg.enable_stream(RS2_STREAM_COLOR);

	pipe.start(cfg);
}

bool GetFrame(float** hItems, int* itemCount)
{
	auto frames = pipe.wait_for_frames();

	auto color = frames.get_color_frame();

	// For cameras that don't have RGB sensor, we'll map the pointcloud to infrared instead of color
	if (!color)
		color = frames.get_infrared_frame();

	// Tell pointcloud object to map to this color frame
	pc.map_to(color);

	auto depth = frames.get_depth_frame();

	// Generate the pointcloud and texture mappings
	points = pc.calculate(depth);

	// Upload the color frame to OpenGL
	//app_state.tex.upload(color);

	// Draw the pointcloud
	//draw_pointcloud(app.width(), app.height(), app_state, points);

	auto length = points.size() * sizeof(rs2::vertex);
	auto memory = malloc(length);
	memcpy(memory, points.get_vertices(), length);
	*hItems = (float*)memory;
	*itemCount = points.size() * 3;
	return true;
}

void DropFrame(float* items) { free((void*)items); }
void Exit() { std::cout << "exitting"; }
void Hell()
{
	std::cout << "Lenny. Hell.o !\n"<<std::endl;
}
