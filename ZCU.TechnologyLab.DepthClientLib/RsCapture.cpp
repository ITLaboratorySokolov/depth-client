// License: Apache 2.0. See LICENSE file in root directory.
// Copyright(c) 2017 Intel Corporation. All Rights Reserved.

#include "RsCapture.h"


#include <map>
#include <string>
#include <thread>
#include <atomic>
#include <iostream>
#include <fstream>
#include <librealsense2/rs.hpp>
#include <librealsense2/hpp/rs_internal.hpp>
#include <librealsense2/hpp/rs_export.hpp>

int main(const char** args)
{
	Start("d435i_walk_around.bag");
}


/**
Class to encapsulate a filter alongside its options
*/
class filter_options
{
public:
	filter_options(const std::string name, rs2::filter& filter);
	filter_options(filter_options&& other);
	std::string filter_name; //Friendly name of the filter
	rs2::filter& filter; //The filter in use
	std::atomic_bool is_enabled; //A boolean controlled by the user that determines whether to apply the filter or not
};

/**
Constructor for filter_options, takes a name and a filter.
*/
filter_options::filter_options(const std::string name, rs2::filter& flt) :
	filter_name(name),
	filter(flt),
	is_enabled(true)
{
	const std::array<rs2_option, 5> possible_filter_options = {
		RS2_OPTION_FILTER_MAGNITUDE,
		RS2_OPTION_FILTER_SMOOTH_ALPHA,
		RS2_OPTION_MIN_DISTANCE,
		RS2_OPTION_MAX_DISTANCE,
		RS2_OPTION_FILTER_SMOOTH_DELTA
	};

	//Go over each filter option and create a slider for it
	for (rs2_option opt : possible_filter_options)
	{
		if (flt.supports(opt))
		{
			rs2::option_range range = flt.get_option_range(opt);
			std::string opt_name = flt.get_option_name(opt);
			std::string prefix = "Filter ";
		}
	}
}

filter_options::filter_options(filter_options&& other) :
	filter_name(std::move(other.filter_name)),
	filter(other.filter),
	is_enabled(other.is_enabled.load())
{
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
						unavailable_streams.erase(
							std::remove(unavailable_streams.begin(), unavailable_streams.end(), type),
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
			std::cout << "Connect T26X and rerun the demo" << std::endl;
			break;
		case RS2_STREAM_DEPTH:
			std::cout << "The demo requires Realsense camera with DEPTH sensor" << std::endl;
			break;
		case RS2_STREAM_COLOR:
			std::cout << "The demo requires Realsense camera with RGB sensor" << std::endl;
			break;
		default:
			throw std::runtime_error(
				"The requested stream: " + std::to_string(type) +
				", for the demo is not supported by connected devices!");
		// stream type
		}
	}
	return false;
}

// Atomic boolean to allow thread safe way to stop the thread
std::atomic_bool stopped(false);
std::thread processing_thread;
rs2::frame_queue filteredQueue;
std::vector<filter_options> filters;

rs2::pipeline* pipe;

// Declare filters
rs2::decimation_filter dec_filter(2); // Decimation - reduces depth frame density
rs2::threshold_filter thr_filter; // Threshold  - removes values outside recommended range
rs2::spatial_filter spat_filter; // Spatial    - edge-preserving spatial smoothing
rs2::temporal_filter temp_filter; // Temporal   - reduces temporal noise

// Declare disparity transform from depth to disparity and vice versa
std::string disparity_filter_name = "Disparity";
rs2::disparity_transform depth_to_disparity(true);
rs2::disparity_transform disparity_to_depth(false);

void UpdateFilters(bool* filterss)
{
	for (int i = 0; i < filters.size(); ++i)
		filters[i].is_enabled = filterss[i];
}
rs2::align align_to_depth(RS2_STREAM_DEPTH);

bool Start(const char* filePath) try
{
	pipe = new rs2::pipeline;
	std::string file = filePath;

	std::cout << "LibRealSense - " << RS2_API_VERSION_STR << ": from " << (strlen(filePath) != 0
		                                                                       ? "file: " + file
		                                                                       : "camera") << std::endl;

	rs2::config cfg;
	cfg.enable_stream(RS2_STREAM_DEPTH);
	cfg.enable_stream(RS2_STREAM_COLOR);

	std::string serial;

	if (!file.empty()) { cfg.enable_device_from_file(file, true); }
	else
	{
		if (!device_with_streams({RS2_STREAM_COLOR, RS2_STREAM_DEPTH}, serial))
		{
			std::cout << "No device found" << std::endl;
			return false;
		}
	}

	if (!serial.empty())
		cfg.enable_device(serial);

	pipe->start(cfg);


	// Initialize a vector that holds filters and their options
	filters.clear();
	// The following order of emplacement will dictate the orders in which filters are applied
	filters.emplace_back("Decimate", dec_filter);
	filters.emplace_back("Threshold", thr_filter);
	filters.emplace_back(disparity_filter_name, depth_to_disparity);
	filters.emplace_back("Spatial", spat_filter);
	filters.emplace_back("Temporal", temp_filter);

	for (auto& filter_options : filters)
	{
		//if (filter_options.filter_name == "Decimate")
		filter_options.is_enabled = true;
	}

	// Declare depth colorizer for pretty visualization of depth data

	stopped = false;


	// Create a thread for getting frames from the device and process them
	// to prevent UI thread from blocking due to long computations.
	processing_thread = std::thread([]
	{
		while (!stopped) //While application is running
		{
			rs2::frameset data = pipe->wait_for_frames(); // Wait for next set of frames from the camera
			auto d2 = align_to_depth.process(data);

			rs2::frame depth_frame = d2.get_depth_frame(); //Take the depth frame from the frameset
			if (!depth_frame) // Should not happen but if the pipeline is configured differently
				return; //  it might not provide depth and we don't want to crash


			rs2::frame filtered = depth_frame; // Does not copy the frame, only adds a reference

			//The implemented flow of the filters pipeline is in the following order:
			/* Apply filters.
			1. apply decimation filter
			2. apply threshold filter
			3. transform the scene into disparity domain
			4. apply spatial filter
			5. apply temporal filter
			6. revert the results back (if step Disparity filter was applied
			to depth domain (each post processing block is optional and can be applied independantly).
			*/
			bool revert_disparity = false;
			for (auto&& filter : filters)
			{
				if (filter.is_enabled)
				{
					filtered = filter.filter.process(filtered);
					if (filter.filter_name == disparity_filter_name) { revert_disparity = true; }
				}
			}
			if (revert_disparity) { filtered = disparity_to_depth.process(filtered); }

			// Push filtered & original data to their respective queues
			// Note, pushing to two different queues might cause the application to display
			//  original and filtered pointclouds from different depth frames
			//  To make sure they are synchronized you need to push them together or add some
			//  synchronization mechanisms

			std::vector<rs2::frame> bundle;

			rs2::processing_block bundler([&](rs2::frame f, rs2::frame_source& src)
			{
				bundle.push_back(f);

				if (bundle.size() == 2)
				{
					auto fs = src.allocate_composite_frame(bundle);
					src.frame_ready(fs);
					bundle.clear();
				}
			});


			bundler.start(filteredQueue);

			//filteredQueue.enqueue(filtered);

			bundler.invoke(d2.get_color_frame());
			bundler.invoke(filtered);
		}
	});
	return true;


	// Signal the processing thread to stop, and join
	// (Not the safest way to join a thread, please wrap your threads in some RAII manner)
	//stopped = true;
	//processing_thread.join();
}
catch
(
	const rs2::error& e
)
{
	std::cout << "RealSense error calling " << e.get_failed_function() << "(" << e.get_failed_args() << "):\n    " << e.
		what() << std::endl;
	return false;
}
catch
(
	const std::exception& e
)
{
	std::cout << "Shit" << e.what() << std::endl;
	return false;
}

void Exit()
{
	if (pipe == nullptr)
		return;
	stopped = true;
	processing_thread.join();
	delete pipe;
	pipe = nullptr;
}

void DropDepthFrame(uint16_t* depths)
{
	//free(depths);
}

void DropFrame(float* items, int* indices, uint8_t* ply)
{
	free(items);
	free(indices);
	free(ply);
}

rs2::colorizer color_map;
rs2::frame colored_depth;

void update_data(rs2::frame_queue& data, rs2::frame& colorized_depth, rs2::points& points, rs2::pointcloud& pc,
                 rs2::colorizer& color_map)
{
	rs2::frameset f;
	if (data.poll_for_frame(&f)) // Try to take the depth and points from the queue
	{
		points = pc.calculate(f.get_depth_frame()); // Generate pointcloud from the depth data
		colorized_depth = color_map.process(f.get_depth_frame()); // Colorize the depth frame with a color map
		pc.map_to(colorized_depth); // Map the colored depth to the point cloud
	}
}

std::vector<uint16_t> depthBuffer;
std::vector<uint8_t> colorBuffer;
std::vector<uint8_t> textureBuffer;
std::vector<float> uvBuffer;

bool GetFrameOnce(uint16_t** depths, uint8_t** colors, int* width, int* height)try
{
	rs2::frameset f;


	if (f = filteredQueue.wait_for_frame()) // Try to take the depth and points from the queue
	{
		rs2::depth_frame df = f.get_depth_frame();

		//points = pc.calculate(f); // Generate pointcloud from the depth data
		colored_depth = color_map.process(df); // Colorize the depth frame with a color map
		rs2::video_frame video(colored_depth);

		//	*height = video.get_profile().format();
		//	pc.map_to(colorized_depth); // Map the colored depth to the point cloud

		*width = df.get_width();
		*height = df.get_height();
		if (depthBuffer.size() < df.get_data_size())
			depthBuffer.resize(df.get_data_size());
		if (colorBuffer.size() < video.get_data_size())
			colorBuffer.resize(video.get_data_size());

		memcpy(depthBuffer.data(), df.get_data(), df.get_data_size());
		memcpy(colorBuffer.data(), video.get_data(), video.get_data_size());
		*depths = depthBuffer.data();
		*colors = colorBuffer.data();
		return true;
	}
	return false;
}
catch (std::exception& e)
{
	std::cout << e.what();
	return false;
}

float dist2(const rs2_vector& a, const rs2_vector& b)
{
	auto x = a.x - b.x;
	auto y = a.y - b.y;
	auto z = a.z - b.z;
	return x * x + y * y + z * z;
}

bool isZero(rs2_vector& v)
{
	return v.x == 0 || v.y == 0 || v.z == 0;

}
void generateFaces(int* faces, float* vertices, int width, int height, int& facesCount)
{
	facesCount = 0;

	for (int i = 0; i < height - 1; ++i)
	{
		int lineStart = i * width;

		for (int j = 0; j < width - 1; ++j)
		{
			facesCount += 6;
			const float epsilon = 1;


			int s = lineStart + j;
			*faces++ = s;
			*faces++ = s + width;
			*faces++ = s + 1;

			 {
				//invalid triangle
				auto& v0 = (rs2_vector&)vertices[*(faces - 3) * 3];
				auto& v1 = (rs2_vector&)vertices[*(faces - 2) * 3];
				auto& v2 = (rs2_vector&)vertices[*(faces - 1) * 3];

				if (dist2(v0, v1) > epsilon
					|| dist2(v1, v2) > epsilon
					|| dist2(v2, v0) > epsilon
					|| isZero(v0)||isZero(v1)||isZero(v2))
				{
					faces -= 3;
					facesCount -= 3;
				}
			}

			*faces++ = s + width;
			*faces++ = s + width + 1;
			*faces++ = s + 1;
			 {
				//invalid triangle
				auto& v0 = (rs2_vector&)vertices[*(faces - 3) * 3];
				auto& v1 = (rs2_vector&)vertices[*(faces - 2) * 3];
				auto& v2 = (rs2_vector&)vertices[*(faces - 1) * 3];

				if (dist2(v0, v1) > epsilon
					|| dist2(v1, v2) > epsilon
					|| dist2(v2, v0) > epsilon
					|| isZero(v0) || isZero(v1) || isZero(v2))
				{
					faces -= 3;
					facesCount -= 3;
				}
			}
		}
	}
}

void writeToObj(const std::string& filePath, float* vertices, int* faces, int vertexCount, int facesCount)
{
	std::ofstream myfile;
	myfile.open(filePath);
	myfile << "#Vertices\n";
	auto oldVert = vertices;
	for (int i = 0; i < vertexCount; ++i)
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


std::array<uint8_t, 3> get_texcolor(const rs2::video_frame& texture, const uint8_t* texture_data, float u, float v)
{
	const int w = texture.get_width(), h = texture.get_height();
	int x = std::min(std::max(int(u * w + .5f), 0), w - 1);
	int y = std::min(std::max(int(v * h + .5f), 0), h - 1);
	int idx = x * texture.get_bytes_per_pixel() + y * texture.get_stride_in_bytes();
	return {texture_data[idx], texture_data[idx + 1], texture_data[idx + 2]};
}

rs2::pointcloud pc;

rs2::points points;

std::string export_to_ply_string(const rs2::points& p, const rs2::video_frame& color)
{
	const bool use_texcoords = true;
	bool mesh = false;
	bool binary = true;
	bool use_normals = false;
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
			new_verts.push_back({verts[i].x, -1 * verts[i].y, -1 * verts[i].z});
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
					faces.push_back({idx_map[a], idx_map[d], idx_map[b]});
					faces.push_back({idx_map[d], idx_map[a], idx_map[c]});

					if (use_normals)
					{
						rs2::vec3d point_a = {verts[a].x, -1 * verts[a].y, -1 * verts[a].z};
						rs2::vec3d point_b = {verts[b].x, -1 * verts[b].y, -1 * verts[b].z};
						rs2::vec3d point_c = {verts[c].x, -1 * verts[c].y, -1 * verts[c].z};
						rs2::vec3d point_d = {verts[d].x, -1 * verts[d].y, -1 * verts[d].z};

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
			rs2::vec3d sum = {0, 0, 0};
			for (auto& n : normals_vec)
				sum = sum + n;
			if (normals_vec.size() > 0)
				normals.push_back((sum.normalize()));
			else
				normals.push_back({0, 0, 0});
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
		newStream.write(st.c_str(), st.length());

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


bool GetFrame(
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
	int* width)
{
	rs2::frameset frameset = filteredQueue.wait_for_frame();

	auto color = frameset.get_color_frame();
	*width = color.get_width();

	// Tell pointcloud object to map to this color frame
	pc.map_to(color);

	auto depth = frameset.get_depth_frame();

	// Generate the pointcloud and texture mappings
	points = pc.calculate(depth);

	// Upload the color frame to OpenGL
	//app_state.tex.upload(color);

	// Draw the pointcloud
	//draw_pointcloud(app.width(), app.height(), app_state, points);

	//points.export_to_ply("my_ply.ply", color);

	//=================ply=====================
	{
		std::ofstream myfile("my_ply_string.ply", std::_Iosb<int>::out | std::_Iosb<int>::binary);
		auto s = export_to_ply_string(points, color);

		*binaryPly = (uint8_t*)malloc(s.length());
		memcpy(*binaryPly, s.c_str(), s.length());
		*plyLength = s.length();
		myfile << s;
	}

	//=================uv=====================
	auto uvLength = points.size() * 2;
	if (uvBuffer.size() < uvLength)
		uvBuffer.resize(uvLength);
	memcpy(uvBuffer.data(), points.get_texture_coordinates(), sizeof(float) * points.size() * 2);
	*uvs = uvBuffer.data();
	*uvsCount = points.size() * 2;

	//=================texture================
	if (textureBuffer.size() < color.get_data_size())
		textureBuffer.resize(color.get_data_size());
	memcpy(textureBuffer.data(), color.get_data(), color.get_data_size());
	*colors = textureBuffer.data();
	*colorsLength = color.get_data_size();

	//================vertices================
	auto vertexLength = points.size() * sizeof(rs2::vertex);
	*vertices = (float*)malloc(vertexLength);
	*vertexCount = points.size() * 3;
	memcpy(*vertices, points.get_vertices(), vertexLength);

	//================faces===================
	*faces = (int*)malloc((depth.get_width() - 1) * (depth.get_height() - 1) * 2 * 3 * sizeof(int));
	generateFaces(*faces, *vertices, depth.get_width(), depth.get_height(), *faceCount);


	//writeToObj("C:/Users/minek/Desktop/killme.obj", *vertices, *faces, *vertexCount, *faceCount);

	return true;
}
