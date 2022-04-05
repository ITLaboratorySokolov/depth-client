// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2017 Intel Corporation. All Rights Reserved.
#include "HelloWorld.h"

#include <fstream>
#include <iostream>
#include <map>
#include <librealsense2/rs.hpp>
#include <librealsense2/hpp/rs_internal.hpp>
#include <librealsense2/hpp/rs_export.hpp>


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
	uint8_t* ply;
	int count;
	int faceCount;
	int plyCount;
	GetFrame(&f, &faces, &count,&faceCount,&ply,&plyCount);
	DropFrame(f, faces, ply);
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

std::array<uint8_t, 3> get_texcolor(const rs2::video_frame& texture, const uint8_t* texture_data, float u, float v)
{
	const int w = texture.get_width(), h = texture.get_height();
	int x = std::min(std::max(int(u * w + .5f), 0), w - 1);
	int y = std::min(std::max(int(v * h + .5f), 0), h - 1);
	int idx = x * texture.get_bytes_per_pixel() + y * texture.get_stride_in_bytes();
	return { texture_data[idx], texture_data[idx + 1], texture_data[idx + 2] };
}

std::string export_to_ply_string(rs2::points p, rs2::video_frame color)
{
	const bool use_texcoords = true;
	bool mesh = false;
	bool binary = true;
	bool use_normals =false;
	const auto verts = p.get_vertices();
	const auto texcoords = p.get_texture_coordinates();
	const uint8_t* texture_data;
	static const auto threshold = 0.05f;

	if (use_texcoords) // texture might be on the gpu, get pointer to data before for-loop to avoid repeated access
		texture_data = reinterpret_cast<const uint8_t*>(color.get_data());
	std::vector<rs2::vertex> new_verts;
	std::vector<rs2::vec3d> normals;
	std::vector<std::array<uint8_t, 3>> new_tex;
	std::map<size_t, size_t> idx_map;
	std::map<size_t, std::vector<rs2::vec3d>> index_to_normals;

	new_verts.reserve(p.size());
	if (use_texcoords) new_tex.reserve(p.size());

	static const auto min_distance = 1e-6;

	for (size_t i = 0; i < p.size(); ++i)
	{
		if (fabs(verts[i].x) >= min_distance || fabs(verts[i].y) >= min_distance ||
			fabs(verts[i].z) >= min_distance)
		{
			idx_map[int(i)] = int(new_verts.size());
			new_verts.push_back({ verts[i].x, -1 * verts[i].y, -1 * verts[i].z });
			if (use_texcoords)
			{
				auto rgb = get_texcolor(color, texture_data, texcoords[i].u, texcoords[i].v);
				new_tex.push_back(rgb);
			}
		}
	}

	auto profile = p.get_profile().as<rs2::video_stream_profile>();
	auto width = profile.width(), height = profile.height();
	std::vector<std::array<size_t, 3>> faces;
	if (mesh)
	{
		for (size_t x = 0; x < width - 1; ++x)
		{
			for (size_t y = 0; y < height - 1; ++y)
			{
				auto a = y * width + x, b = y * width + x + 1, c = (y + 1) * width + x, d = (y + 1) * width + x + 1;
				if (verts[a].z && verts[b].z && verts[c].z && verts[d].z
					&& fabs(verts[a].z - verts[b].z) < threshold && fabs(verts[a].z - verts[c].z) < threshold
					&& fabs(verts[b].z - verts[d].z) < threshold && fabs(verts[c].z - verts[d].z) < threshold)
				{
					if (idx_map.count(a) == 0 || idx_map.count(b) == 0 || idx_map.count(c) == 0 ||
						idx_map.count(d) == 0)
						continue;
					faces.push_back({ idx_map[a], idx_map[d], idx_map[b] });
					faces.push_back({ idx_map[d], idx_map[a], idx_map[c] });

					if (use_normals)
					{
						rs2::vec3d point_a = { verts[a].x, -1 * verts[a].y, -1 * verts[a].z };
						rs2::vec3d point_b = { verts[b].x, -1 * verts[b].y, -1 * verts[b].z };
						rs2::vec3d point_c = { verts[c].x, -1 * verts[c].y, -1 * verts[c].z };
						rs2::vec3d point_d = { verts[d].x, -1 * verts[d].y, -1 * verts[d].z };

						auto n1 = cross(point_d - point_a, point_b - point_a);
						auto n2 = cross(point_c - point_a, point_d - point_a);

						index_to_normals[idx_map[a]].push_back(n1);
						index_to_normals[idx_map[a]].push_back(n2);

						index_to_normals[idx_map[b]].push_back(n1);

						index_to_normals[idx_map[c]].push_back(n2);

						index_to_normals[idx_map[d]].push_back(n1);
						index_to_normals[idx_map[d]].push_back(n2);
					}
				}
			}
		}
	}

	if (mesh && use_normals)
	{
		for (size_t i = 0; i < new_verts.size(); ++i)
		{
			auto normals_vec = index_to_normals[i];
			rs2::vec3d sum = { 0, 0, 0 };
			for (auto& n : normals_vec)
				sum = sum + n;
			if (normals_vec.size() > 0)
				normals.push_back((sum.normalize()));
			else
				normals.push_back({ 0, 0, 0 });
		}
	}

	std::stringstream out(std::stringstream::out);
	out << "ply\n";
	if (binary)
		out << "format binary_little_endian 1.0\n";
	else
		out << "format ascii 1.0\n";
	out << "comment pointcloud saved from Realsense Viewer\n";
	out << "element vertex " << new_verts.size() << "\n";
	out << "property float" << sizeof(float) * 8 << " x\n";
	out << "property float" << sizeof(float) * 8 << " y\n";
	out << "property float" << sizeof(float) * 8 << " z\n";
	if (mesh && use_normals)
	{
		out << "property float" << sizeof(float) * 8 << " nx\n";
		out << "property float" << sizeof(float) * 8 << " ny\n";
		out << "property float" << sizeof(float) * 8 << " nz\n";
	}
	if (use_texcoords)
	{
		out << "property uchar red\n";
		out << "property uchar green\n";
		out << "property uchar blue\n";
	}
	if (mesh)
	{
		out << "element face " << faces.size() << "\n";
		out << "property list uchar int vertex_indices\n";
	}
	out << "end_header\n";

	if (binary)
	{
		std::stringstream newStream(std::stringstream::out | std::stringstream::binary);
		auto st = out.str();
		newStream.write(st.c_str(),st.length());

		for (size_t i = 0; i < new_verts.size(); ++i)
		{
			// we assume little endian architecture on your device
			newStream.write(reinterpret_cast<const char*>(&(new_verts[i].x)), sizeof(float));
			newStream.write(reinterpret_cast<const char*>(&(new_verts[i].y)), sizeof(float));
			newStream.write(reinterpret_cast<const char*>(&(new_verts[i].z)), sizeof(float));

			if (mesh && use_normals)
			{
				newStream.write(reinterpret_cast<const char*>(&(normals[i].x)), sizeof(float));
				newStream.write(reinterpret_cast<const char*>(&(normals[i].y)), sizeof(float));
				newStream.write(reinterpret_cast<const char*>(&(normals[i].z)), sizeof(float));
			}

			if (use_texcoords)
			{
				newStream.write(reinterpret_cast<const char*>(&(new_tex[i][0])), sizeof(uint8_t));
				newStream.write(reinterpret_cast<const char*>(&(new_tex[i][1])), sizeof(uint8_t));
				newStream.write(reinterpret_cast<const char*>(&(new_tex[i][2])), sizeof(uint8_t));
			}
		}
		if (mesh)
		{
			auto size = faces.size();
			for (size_t i = 0; i < size; ++i)
			{
				static const int three = 3;
				newStream.write(reinterpret_cast<const char*>(&three), sizeof(uint8_t));
				newStream.write(reinterpret_cast<const char*>(&(faces[i][0])), sizeof(int));
				newStream.write(reinterpret_cast<const char*>(&(faces[i][1])), sizeof(int));
				newStream.write(reinterpret_cast<const char*>(&(faces[i][2])), sizeof(int));
			}
		}
		return newStream.str();
	}
	else
	{
		for (size_t i = 0; i < new_verts.size(); ++i)
		{
			out << new_verts[i].x << " ";
			out << new_verts[i].y << " ";
			out << new_verts[i].z << " ";
			out << "\n";

			if (mesh && use_normals)
			{
				out << normals[i].x << " ";
				out << normals[i].y << " ";
				out << normals[i].z << " ";
				out << "\n";
			}

			if (use_texcoords)
			{
				out << unsigned(new_tex[i][0]) << " ";
				out << unsigned(new_tex[i][1]) << " ";
				out << unsigned(new_tex[i][2]) << " ";
				out << "\n";
			}
		}
		if (mesh)
		{
			auto size = faces.size();
			for (size_t i = 0; i < size; ++i)
			{
				int three = 3;
				out << three << " ";
				out << std::get<0>(faces[i]) << " ";
				out << std::get<1>(faces[i]) << " ";
				out << std::get<2>(faces[i]) << " ";
				out << "\n";
			}
		}

		return out.str();
	}
}


bool GetFrame(float** vertices, int** faces, int* itemCount, int* faceCount, uint8_t** binaryPly,int* plyLength)
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
	{
		std::ofstream myfile("my_ply_string.ply",std::_Iosb<int>::out| std::_Iosb<int>::binary);
		auto s = export_to_ply_string(points, color);

		*binaryPly = (uint8_t*)malloc(s.length());
		memcpy(*binaryPly, s.c_str(), s.length());
		*plyLength = s.length();
		myfile << s;
	}
	auto length = points.size() / 4 * sizeof(rs2::vertex);
	*vertices = (float*)malloc(length);


	fillVerticesWithDivider((float*)points.get_vertices(), *vertices, depth.get_width(), depth.get_height());


	*faces = (int*)malloc((depth.get_width() / 2 - 1) * (depth.get_height() / 2 - 1) * 2 * 3 * sizeof(int));

	generateFaces(*faces, *vertices, depth.get_width() / 2, depth.get_height() / 2, faceCount);

	//writeToObj("myObj.obj", *faces, *vertices, depth.get_width() * depth.get_height() / 4,*faceCount);

	*itemCount = points.size() / 4 * 3;

	return true;
}

void DropFrame(float* items, int* indices,uint8_t* ply)
{
	free(items);
	free(indices);
	free(ply);
}

void Exit()
{
	std::cout << "exitting";
	delete pipe;
}

void Hell() { std::cout << "Lenny. Hell.o !\n" << std::endl; }
