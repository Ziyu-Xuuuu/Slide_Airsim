# AirSim-SlideSLAM Unity Integration

This Unity project integrates AirSim with SlideSLAM perception capabilities, replacing AirSim's default perception with advanced SLAM and semantic understanding while maintaining full compatibility with the existing Resource-Aware Coordination system.

## Overview

The project extends existing AirSim Unity components to add SlideSLAM capabilities:
- **SLOAM**: Semantic LiDAR Odometry and Mapping
- **Object Detection**: Real-time object detection and classification  
- **Semantic Mapping**: Building semantic maps with detected objects
- **Multi-Robot Coordination**: Integration with existing RAG coordination system

## Key Features

### 🔧 Built on Existing AirSim Architecture
- Extends existing `Vehicle` and `Drone` components
- Uses existing `DataCaptureScript` for camera integration
- Maintains compatibility with existing `AirSimServer` and settings
- Preserves all existing AirSim functionality

### 🤖 SlideSLAM Integration
- **SlideSLAMVehicleExtension**: Adds perception to existing vehicles
- **SlideSLAMROSInterface**: Bridges Unity with SlideSLAM ROS nodes
- **Perception Processors**: SLOAM, object detection, semantic mapping
- **ROS Communication**: Compatible with existing airsim_ros_pkgs topics

### 🌐 Multi-Robot Coordination
- Compatible with existing Resource-Aware Coordination system
- Uses established ROS topic structure from airsim_ros_pkgs
- Supports existing image covering coordination algorithms
- Maintains connectivity mesh and neighbor detection

## Architecture

```
Unity AirSim-SlideSLAM Integration
├── Existing AirSim Components (preserved)
│   ├── AirSimGlobal
│   ├── AirSimServer  
│   ├── Vehicle/Drone classes
│   ├── DataCaptureScript
│   └── Coordination system
├── SlideSLAM Extensions (new)
│   ├── SlideSLAMVehicleExtension
│   ├── SlideSLAMROSInterface
│   ├── Perception Processors
│   └── ROS Message Handlers
└── ROS Integration
    ├── Existing airsim_ros_pkgs topics
    ├── SlideSLAM bridge topics
    └── Coordination topics
```

## Prerequisites

### Unity Setup
- Unity Editor 2022.3.13f1 (same as original project)
- Existing AirSim Unity assets from the source project
- SlideSLAM-AirSim integration scripts (this project)

### ROS Environment
- ROS Noetic (Ubuntu 20.04) or ROS Melodic (Ubuntu 18.04)
- SlideSLAM packages from the source project:
  - `sloam`
  - `faster-lio`
  - `object_modeller`
  - `scan2shape`
  - `image_covering_coordination`
- Existing `airsim_ros_pkgs`

### Network Configuration
- ROS Master running on accessible network
- Proper firewall configuration for ROS communication
- rosbridge or ROS# for Unity-ROS communication

## Installation

### 1. Copy AirSim Assets
```bash
# Copy existing AirSim Unity assets to this project
cp -r /path/to/Resource-Aware-Coordination-AirSim/AirSim/Unity/UnityDemo/Assets/AirSimAssets \
      ./Assets/
```

### 2. Configure ROS Bridge
Install ROS bridge for Unity-ROS communication:
```bash
# Option 1: Using rosbridge
sudo apt install ros-noetic-rosbridge-suite

# Option 2: Using ROS# (add to Unity project)
# Download ROS# from GitHub and import into Unity
```

### 3. Build SlideSLAM Packages
```bash
cd /path/to/Resource-Aware-Coordination-AirSim/AirSim/ros
catkin build -DCMAKE_C_COMPILER=gcc-8 -DCMAKE_CXX_COMPILER=g++-8
source devel/setup.bash
```

## Usage

### 1. Start ROS System
```bash
# Terminal 1: Start ROS core
roscore

# Terminal 2: Start SlideSLAM nodes (example for 3 drones)
roslaunch airsim_ros_pkgs airsim_slideslam_integration.launch num_drones:=3

# Terminal 3: Start coordination system
roslaunch image_covering_coordination image_covering_3drones_RAG.launch
```

### 2. Configure Unity Scene
1. Open the `SlideSLAM-AirSim-Demo` scene
2. Configure `SlideSLAMManager` with ROS Master URI
3. Set up vehicle namespaces (drone_0, drone_1, drone_2)
4. Verify camera and LiDAR configurations

### 3. Run Simulation
1. Press Play in Unity Editor
2. Wait for AirSim server initialization
3. Verify ROS connections in console
4. Monitor SlideSLAM perception in RViz:
```bash
rosrun rviz rviz -d /path/to/slideslam.rviz
```

## Configuration

### Vehicle Configuration
Each drone can be configured via the `SlideSLAMVehicleExtension` component:

```csharp
// Enable/disable SlideSLAM components
enableSLOAM = true;
enableObjectDetection = true; 
enableSemanticMapping = true;
replaceDefaultPerception = false; // Keep AirSim as backup

// ROS configuration
robotNamespace = "drone_0";
rosMasterURI = "http://localhost:11311";

// Sensor configuration  
lidarRange = 100.0f;
lidarRayCount = 32;
```

### ROS Topics
The integration follows existing airsim_ros_pkgs topic structure:

**Input Topics (Unity → ROS):**
- `/{namespace}/airsim_node/lidar/Lidar`
- `/{namespace}/airsim_node/camera_0/Scene`
- `/{namespace}/airsim_node/odom_local_ned`

**SlideSLAM Bridge Topics:**
- `/{namespace}/velodyne_points`
- `/{namespace}/sync_odom`
- `/{namespace}/semantic_observation`

**Output Topics (ROS → Unity):**
- `/{namespace}/sloam/optimized_odom`
- `/{namespace}/slam_pose`
- `/{namespace}/object_detections`

### Settings Integration
The system uses existing AirSim settings.json with SlideSLAM extensions:

```json
{
  "SettingsVersion": 1.2,
  "SimMode": "Multirotor",
  "Vehicles": {
    "Drone0": {
      "VehicleType": "SimpleFlight",
      "SlideSLAM": {
        "EnableSLOAM": true,
        "EnableObjectDetection": true,
        "ROSNamespace": "drone_0"
      }
    }
  }
}
```

## Extending the System

### Adding New Perception Components
1. Create new processor inheriting from base processor class
2. Register with `SlideSLAMVehicleExtension`
3. Configure ROS topics in `SlideSLAMROSInterface`

### Custom Object Detection
1. Extend `ObjectDetectionProcessor`
2. Implement custom classification logic
3. Add new ROS message types as needed

### Advanced SLAM Features
1. Extend `SLOAMProcessor` for custom SLAM algorithms
2. Add additional sensor modalities
3. Implement custom loop closure detection

## Troubleshooting

### Common Issues

**ROS Connection Failed:**
- Verify ROS Master URI is correct
- Check network connectivity and firewall settings
- Ensure rosbridge is running if using rosbridge

**Camera Data Not Publishing:**
- Verify CaptureCameras tag on camera parent object
- Check DataCaptureScript configuration
- Ensure cameras are child objects of vehicle

**SLAM Not Working:**
- Verify SlideSLAM ROS nodes are running
- Check topic names match configuration
- Monitor ROS topics with `rostopic echo`

**Performance Issues:**
- Reduce LiDAR ray count for better performance
- Lower camera resolution in AirSim settings
- Disable unused perception components

### Debug Tools

**Unity Console Logging:**
Enable debug logging in `SlideSLAMROSInterface`:
```csharp
enableDebugLogging = true;
```

**ROS Topic Monitoring:**
```bash
# List active topics
rostopic list

# Monitor specific topics
rostopic echo /drone_0/velodyne_points
rostopic echo /drone_0/sloam/optimized_odom
```

**RViz Visualization:**
```bash
rosrun rviz rviz -d scan2shape/rviz/faster-lio-sloam.rviz
```

## Contributing

This project extends the existing Resource-Aware Coordination AirSim system. When contributing:

1. Maintain compatibility with existing AirSim components
2. Follow the established ROS topic naming conventions
3. Preserve existing coordination system functionality
4. Test with multiple vehicles to ensure scalability

## License

This project builds upon the existing Resource-Aware Coordination AirSim system and should follow the same licensing terms as the original project.

## Citation

If using this integration in research, please cite both the original Resource-Aware Coordination work and any SlideSLAM-related publications:

```bibtex
@article{xu2025communication,
  title={Communication-and Computation-Efficient Distributed Submodular Optimization in Robot Mesh Networks},
  author={Xu, Zirui and Garimella, Sandilya Sai and Tzoumas, Vasileios},
  journal={IEEE Transactions on Robotics (TRO)},
  year={2025}
}
```