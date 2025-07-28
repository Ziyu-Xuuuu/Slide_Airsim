#!/bin/bash
set -e

echo "🚀 Initializing SlideSLAM-AirSim Container..."

# Source ROS
source /opt/ros/noetic/setup.bash

WORKSPACE=/root/slideslam_ws

# Check if workspace is mounted
if [ ! -d "${WORKSPACE}/Source_Code" ]; then
    echo "❌ Error: Source code not found. Please ensure ROS1 folder is mounted to ${WORKSPACE}"
    exit 1
fi

cd ${WORKSPACE}/Source_Code

echo "📦 Building SlideSLAM dependencies..."

# Build Sophus
if [ -d "SlideSLAM/Sophus" ]; then
    cd SlideSLAM/Sophus
    rm -rf build
    mkdir build && cd build
    cmake -DCMAKE_BUILD_TYPE=Release ..
    make -j$(nproc)
    make install
    echo "✅ Sophus built and installed"
    cd ${WORKSPACE}/Source_Code
fi

# Build fmt
if [ -d "SlideSLAM/fmt" ]; then
    cd SlideSLAM/fmt
    rm -rf build
    mkdir build && cd build
    cmake -DCMAKE_BUILD_TYPE=Release ..
    make -j$(nproc)
    make install
    echo "✅ fmt built and installed"
    cd ${WORKSPACE}/Source_Code
fi

# Build faster-lio
if [ -d "SlideSLAM/faster-lio" ]; then
    cd SlideSLAM/faster-lio
    rm -rf build
    mkdir build && cd build
    cmake -DCMAKE_BUILD_TYPE=Release ..
    make -j$(nproc)
    echo "✅ faster-lio built"
    cd ${WORKSPACE}/Source_Code
fi

echo "🔧 Setting up ROS workspace..."

cd ~/slideslam_ws/Source_Code/Resource-Aware-Coordinat
ion-AirSim/AirSim/ros

# Install ROS dependencies
rosdep install --from-paths src --ignore-src -r -y || true

# Build ROS workspace
source /opt/ros/noetic/setup.bash
catkin build -DCMAKE_BUILD_TYPE=Release

# Source the workspace
source devel/setup.bash
echo "source ${WORKSPACE}/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros/devel/setup.bash" >> ~/.bashrc

echo "✅ Container setup complete!"
echo ""
echo "🎯 Available commands:"
echo "  start_airsim          - Start AirSim simulation"
echo "  launch_integration    - Launch basic integration (3 drones)"
echo "  launch_coordination   - Launch full coordination (15 drones)"
echo "  run_diagnostics       - Run system diagnostics"
echo "  open_rviz             - Open RViz visualization"
echo ""
echo "📁 Workspace: ${WORKSPACE}/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"
