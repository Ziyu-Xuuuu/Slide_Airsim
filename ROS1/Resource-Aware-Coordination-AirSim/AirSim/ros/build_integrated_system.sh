#!/bin/bash

# Build script for integrated AirSim + SlideSLAM + Resource-Aware Coordination system

echo "Building integrated AirSim-SlideSLAM-Coordination system..."

# Set up environment for ROS1
source /opt/ros/noetic/setup.bash

# Navigate to the workspace
cd "$(dirname "$0")"

# Install system dependencies
echo "Installing system dependencies..."
sudo apt-get update
sudo apt-get install -y \
    libgoogle-glog-dev \
    libeigen3-dev \
    libsuitesparse-dev \
    libgtsam-dev \
    libgtsam-unstable-dev \
    libdw-dev \
    ros-noetic-ros-numpy \
    libyaml-cpp-dev \
    libpcl-dev

# Install Python dependencies for SlideSLAM
echo "Installing Python dependencies..."
pip3 install -r << 'EOF'
numpy==1.22.3
scikit-learn
scipy
open3d
matplotlib
ultralytics==8.0.59
EOF

# Clean previous build
echo "Cleaning previous build..."
if [ -d "devel" ]; then
    rm -rf devel
fi
if [ -d "build" ]; then
    rm -rf build
fi

# Build the workspace
echo "Building catkin workspace..."
catkin build -DCMAKE_BUILD_TYPE=Release

# Source the workspace
echo "Sourcing workspace..."
source devel/setup.bash

echo "Build completed successfully!"
echo ""
echo "To use the integrated system:"
echo "1. Source the workspace: source $(pwd)/devel/setup.bash"
echo "2. Launch Unity/AirSim simulation"
echo "3. Run integration: roslaunch airsim_ros_pkgs airsim_slideslam_coordination.launch"
echo ""
echo "For conda environment compatibility:"
echo "1. conda activate ros2"
echo "2. export PYTHONPATH=\$PYTHONPATH:$(pwd)/devel/lib/python3/dist-packages"
echo "3. Follow steps above"