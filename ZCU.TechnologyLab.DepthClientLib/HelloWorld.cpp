// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2017 Intel Corporation. All Rights Reserved.
#include "HelloWorld.h"

#include <fstream>
#include <iostream>
#include <map>
#include <librealsense2/rs.hpp>
#include <librealsense2/hpp/rs_internal.hpp>


rs2::pointcloud pc;
// We want the points object to be persistent so we can display the last cloud when a frame drops
rs2::points points;

// Declare RealSense pipeline, encapsulating the actual device and sensors
rs2::pipeline* pipe;

std::string file;


void main()
{
	Start("d435i_walk_around.bag");
	float* f;
	int* faces;
	int count;
	int faceCount;
	GetFrame(&f, &faces, &count,&faceCount);
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


void Start(const char* filePath)
{
	pipe = new rs2::pipeline;
	file = filePath;
	try
	{
		std::cout << "LibRealSense - " << RS2_API_VERSION_STR << " from file: " << file << std::endl;

		rs2::config cfg;
		cfg.enable_stream(RS2_STREAM_DEPTH);
		cfg.enable_stream(RS2_STREAM_COLOR);

		std::string serial;

		if (!file.empty()) { cfg.enable_device_from_file(file, true); }
		else
		{
			if (!device_with_streams({RS2_STREAM_COLOR, RS2_STREAM_DEPTH}, serial))
			{
				std::cout << "No device found";
				return;
			}
		}

		if (!serial.empty())
			cfg.enable_device(serial);

		pipe->start(cfg);
	}
	catch (std::exception& e) { std::cout << "Error " << e.what() << std::endl; }
}

void generateFaces(int* faces, float* vertices, int width, int height, int* facesCount)
{
	*facesCount = 0;

	for (int i = 0; i < height - 1; ++i)
	{
		int lineStart = i * width;

		for (int j = 0; j < width - 1; ++j)
		{
			(*facesCount) += 2;
			const float epsilon = 0.001;


			int s = lineStart + j;
			*faces++ = s;
			*faces++ = s + width;
			*faces++ = s + 1;

			//invalid triangle
			if (vertices[*(faces - 3) * 3 + 2] < epsilon ||
				vertices[*(faces - 2) * 3 + 2] < epsilon ||
				vertices[*(faces - 1) * 3 + 2] < epsilon)
			{
				faces -= 3;
				(*facesCount) -= 1;
			}


			*faces++ = s + width;
			*faces++ = s + width + 1;
			*faces++ = s + 1;

			//invalid triangle
			if (vertices[*(faces - 3) * 3 + 2] < epsilon ||
				vertices[*(faces - 2) * 3 + 2] < epsilon ||
				vertices[*(faces - 1) * 3 + 2] < epsilon)
			{
				faces -= 3;
				(*facesCount) -= 1;
			}
		}
	}
}


void writeToObj(const std::string& filePath, int* faces, float* vertices, int count, int facesCount)
{
	std::ofstream myfile;
	myfile.open(filePath);
	myfile << "#Vertices\n";
	auto oldVert = vertices;
	for (int i = 0; i < count; ++i)
	{
		myfile << "v ";
		myfile << *vertices++;
		myfile << " ";
		myfile << *vertices++;
		myfile << " ";
		myfile << *vertices++;
		myfile << "\n";
	}
	myfile << "#Faces\n";
	for (int i = 0; i < facesCount; ++i)
	{
		int f[3];
		f[0] = (*faces++);
		f[1] = (*faces++);
		f[2] = (*faces++);

		myfile << "f ";
		myfile << (f[0] + 1);
		myfile << " ";
		myfile << (f[1] + 1);
		myfile << " ";
		myfile << (f[2] + 1);
		myfile << "\n";
	}

	myfile.close();
}

void fillVerticesWithDivider(float* src, float* target, int width, int height)
{
	for (int y = 0; y < height / 2; ++y)
	{
		for (int x = 0; x < width / 2; ++x)
		{
			*target++ = *src++;
			*target++ = *src++;
			*target++ = *src++;

			src += 3;
		}
		src += width * 3;
	}
}

bool GetFrame(float** vertices, int** faces, int* itemCount, int* faceCount)
{
	auto frames = pipe->wait_for_frames();

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

	//points.export_to_ply("my_ply.ply", color);

	auto length = points.size() / 4 * sizeof(rs2::vertex);
	*vertices = (float*)malloc(length);


	fillVerticesWithDivider((float*)points.get_vertices(), *vertices, depth.get_width(), depth.get_height());


	*faces = (int*)malloc((depth.get_width() / 2 - 1) * (depth.get_height() / 2 - 1) * 2 * 3 * sizeof(int));

	generateFaces(*faces, *vertices, depth.get_width() / 2, depth.get_height() / 2, faceCount);

	//writeToObj("myObj.obj", *faces, *vertices, depth.get_width() * depth.get_height() / 4,*faceCount);

	*itemCount = points.size() / 4 * 3;

	return true;
}

void DropFrame(float* items, int** indices)
{
	free(items);
	free(indices);
}

void Exit()
{
	std::cout << "exitting";
	delete pipe;
}

void Hell() { std::cout << "Lenny. Hell.o !\n" << std::endl; }
