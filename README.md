# DepthSensorClient

Sends depth map from **Intel® RealSense™ SDK 2.0** depth sensor to the server.  
Contains simple Window Client to connect and send images/meshes to the server,and save them locally.  
[Download Sample Playback Files](https://github.com/IntelRealSense/librealsense/blob/master/doc/sample-data.md)

![Main Window](view_1.PNG?raw=true "Client Window")

#### Dependencies
- *ZCU.TechnologyLab.Common*
- *Intel RealSense SDK 2.0* [Install](https://www.intelrealsense.com/sdk-2/)
- NuGet: *Microsoft.AspNetCore.SignalR.Client*

#### Installation
```
git clone --recurse-submodules https://github.com/ITLaboratorySokolov/depth-client
cd depth-client
curl -O https://librealsense.intel.com/rs-tests/TestData/stairs.bag
cd ./ZCU.TechnologyLab.DepthClientLib
cmake .
cd ..
start DepthClient.sln
```

