# DepthSensorClient

Sends depth map from **Intel® RealSense™ SDK 2.0** depth sensor to the server.  
Contains simple Window Client to connect, receive, and send image to the server.  
[Download Sample Playback Files](https://github.com/IntelRealSense/librealsense/blob/master/doc/sample-data.md)

#### Dependencies
- *ZCU.TechnologyLab.Common*
- NuGet: *Intel.RealSenseWithNativeDll*
- NuGet: *Microsoft.AspNetCore.SignalR.Client*

#### Installation
```
git clone --recurse-submodules https://github.com/Cooble/depth-client
cd depth-client
curl -O https://librealsense.intel.com/rs-tests/TestData/stairs.bag
cd ./ZCU.TechnologyLab.DepthClientLib
cmake .
cd ..
start DepthClient.sln
```



