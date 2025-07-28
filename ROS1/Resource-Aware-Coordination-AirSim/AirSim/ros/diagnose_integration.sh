#!/bin/bash

# SlideSLAM-AirSim Integration Diagnostics Script
# This script helps identify and fix common integration issues

set -e

echo "🔍 SlideSLAM-AirSim Integration Diagnostics"
echo "=========================================="

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Function to check file existence
check_file() {
    if [ -f "$1" ]; then
        echo -e "${GREEN}✅ $1${NC}"
        return 0
    else
        echo -e "${RED}❌ $1 (missing)${NC}"
        return 1
    fi
}

# Function to check directory existence
check_dir() {
    if [ -d "$1" ]; then
        echo -e "${GREEN}✅ $1${NC}"
        return 0
    else
        echo -e "${RED}❌ $1 (missing)${NC}"
        return 1
    fi
}

echo -e "${BLUE}📁 Step 1: Checking file structure...${NC}"

# Key files that should exist
key_files=(
    "src/airsim_ros_pkgs/src/airsim_slideslam_bridge.cpp"
    "src/airsim_ros_pkgs/launch/airsim_slideslam_integration.launch"
    "src/airsim_ros_pkgs/launch/airsim_slideslam_coordination.launch"
    "src/airsim_ros_pkgs/config/slideslam_bridge_params.yaml"
    "build_integrated_system.sh"
)

for file in "${key_files[@]}"; do
    check_file "$file"
done

# Key directories that should exist
key_dirs=(
    "src/sloam"
    "src/sloam_msgs"
    "src/object_modeller"
    "src/scan2shape"
    "src/multi_robot_utils_launch"
)

for dir in "${key_dirs[@]}"; do
    check_dir "$dir"
done

echo
echo -e "${BLUE}🔧 Step 2: Checking build configuration...${NC}"

# Check CMakeLists.txt for required components
if grep -q "sloam_msgs" src/airsim_ros_pkgs/CMakeLists.txt; then
    echo -e "${GREEN}✅ sloam_msgs dependency found in CMakeLists.txt${NC}"
else
    echo -e "${RED}❌ sloam_msgs dependency missing in CMakeLists.txt${NC}"
    echo -e "${YELLOW}   Fix: Add 'sloam_msgs' to find_package() catkin REQUIRED COMPONENTS${NC}"
fi

if grep -q "airsim_slideslam_bridge" src/airsim_ros_pkgs/CMakeLists.txt; then
    echo -e "${GREEN}✅ Bridge node target found in CMakeLists.txt${NC}"
else
    echo -e "${RED}❌ Bridge node target missing in CMakeLists.txt${NC}"
    echo -e "${YELLOW}   Fix: Add bridge node executable definition${NC}"
fi

echo
echo -e "${BLUE}🐍 Step 3: Checking Python dependencies...${NC}"

# Check if key Python packages are available
python_packages=("numpy" "opencv-python" "scikit-learn" "scipy" "open3d")
for pkg in "${python_packages[@]}"; do
    if python3 -c "import ${pkg//-/_}" 2>/dev/null; then
        echo -e "${GREEN}✅ Python: ${pkg}${NC}"
    else
        echo -e "${RED}❌ Python: ${pkg} (missing)${NC}"
        echo -e "${YELLOW}   Fix: pip install ${pkg}${NC}"
    fi
done

echo
echo -e "${BLUE}📦 Step 4: Checking ROS package dependencies...${NC}"

# Source ROS if not already sourced
if [ -z "$ROS_DISTRO" ]; then
    echo -e "${YELLOW}⚠️  ROS not sourced, attempting to source...${NC}"
    source /opt/ros/noetic/setup.bash 2>/dev/null || echo -e "${RED}❌ Failed to source ROS${NC}"
fi

if [ -f "devel/setup.bash" ]; then
    source devel/setup.bash
fi

# Check ROS packages
ros_packages=("tf2_ros" "sensor_msgs" "nav_msgs" "geometry_msgs" "pcl_ros")
for pkg in "${ros_packages[@]}"; do
    if rospack find "$pkg" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ ROS: ${pkg}${NC}"
    else
        echo -e "${RED}❌ ROS: ${pkg} (missing)${NC}"
        echo -e "${YELLOW}   Fix: sudo apt install ros-noetic-${pkg//_/-}${NC}"
    fi
done

echo
echo -e "${BLUE}🔗 Step 5: Checking SlideSLAM dependencies...${NC}"

# Check for GTSAM
if pkg-config --exists gtsam; then
    echo -e "${GREEN}✅ GTSAM found${NC}"
else
    echo -e "${RED}❌ GTSAM missing${NC}"
    echo -e "${YELLOW}   Fix: Install GTSAM as per SlideSLAM README${NC}"
fi

# Check for Sophus
if [ -d "/usr/local/include/sophus" ] || [ -d "/usr/include/sophus" ]; then
    echo -e "${GREEN}✅ Sophus found${NC}"
else
    echo -e "${RED}❌ Sophus missing${NC}"
    echo -e "${YELLOW}   Fix: Install Sophus as per SlideSLAM README${NC}"
fi

# Check for qhull
if pkg-config --exists qhull; then
    echo -e "${GREEN}✅ QHull found${NC}"
else
    echo -e "${RED}❌ QHull missing${NC}"
    echo -e "${YELLOW}   Fix: Install qhull 8.0.2 as per SlideSLAM README${NC}"
fi

echo
echo -e "${BLUE}⚙️  Step 6: Configuration recommendations...${NC}"

echo -e "${BLUE}Topic remapping check:${NC}"
if grep -q "velodyne_points" src/airsim_ros_pkgs/src/airsim_slideslam_bridge.cpp; then
    echo -e "${GREEN}✅ LiDAR topic correctly mapped to velodyne_points${NC}"
else
    echo -e "${YELLOW}⚠️  Check LiDAR topic mapping${NC}"
fi

echo -e "${BLUE}Frame ID check:${NC}"
if grep -q "base_link" src/airsim_ros_pkgs/src/airsim_slideslam_bridge.cpp; then
    echo -e "${GREEN}✅ Frame IDs are configured${NC}"
else
    echo -e "${YELLOW}⚠️  Check coordinate frame configuration${NC}"
fi

echo
echo -e "${BLUE}🚀 Step 7: Quick fixes for common issues...${NC}"

echo -e "${YELLOW}If you encounter build errors:${NC}"
echo "1. cd src && git submodule update --init --recursive"
echo "2. rosdep install --from-paths . --ignore-src -r -y"
echo "3. ./build_integrated_system.sh"

echo -e "${YELLOW}If you encounter runtime errors:${NC}"
echo "1. Check AirSim is running and accessible"
echo "2. Verify robot names match between AirSim settings and launch files"
echo "3. Check topic names with: rostopic list"
echo "4. Monitor bridge node: rosnode info /drone_0/slideslam_bridge"

echo -e "${YELLOW}If SLAM is not working:${NC}"
echo "1. Check LiDAR data: rostopic echo /drone_0/velodyne_points"
echo "2. Verify odometry: rostopic echo /drone_0/sync_odom"
echo "3. Check SLOAM node: rosnode info /drone_0/sloam_node"

echo
echo -e "${GREEN}🎯 Diagnostics complete!${NC}"
echo
echo -e "${BLUE}📝 Summary of findings above. Address any ${RED}❌ missing${BLUE} items before proceeding.${NC}"