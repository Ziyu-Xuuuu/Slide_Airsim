#!/bin/bash
set -e

echo "📊 Starting RViz Visualization for SlideSLAM-AirSim..."

# Define workspace paths
WORKSPACE="/root/slideslam_ws"
ROS_WS="${WORKSPACE}/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"

# Source ROS environment
source /opt/ros/noetic/setup.bash

# Source workspace if available
if [ -f "${ROS_WS}/devel/setup.bash" ]; then
    source "${ROS_WS}/devel/setup.bash"
    echo "✅ ROS workspace sourced"
else
    echo "⚠️  Warning: ROS workspace not built. RViz may not find custom message types."
fi

# Check if ROS master is running
if ! rostopic list &>/dev/null; then
    echo "❌ Error: ROS master is not running"
    echo "   Please start roscore first or run one of the launch scripts"
    exit 1
fi

echo "🔍 Searching for appropriate RViz configuration..."

# Define possible RViz config locations and priorities
RVIZ_CONFIGS=(
    "${ROS_WS}/src/scan2shape/rviz/faster-lio-sloam.rviz"
    "${ROS_WS}/src/faster-lio/rviz_cfg/loam_livox.rviz"
    "${ROS_WS}/src/airsim_ros_pkgs/rviz/default.rviz"
    "${ROS_WS}/src/airsim_ros_pkgs/rviz/urbancity_6quadrotors.rviz"
    "${ROS_WS}/src/object_modeller/rviz/object_modeller.rviz"
    "${WORKSPACE}/Source_Code/SlideSLAM/faster-lio/rviz_cfg/loam_livox.rviz"
)

SELECTED_CONFIG=""
CONFIG_DESCRIPTION=""

for config in "${RVIZ_CONFIGS[@]}"; do
    if [ -f "$config" ]; then
        SELECTED_CONFIG="$config"
        case "$config" in
            *"faster-lio-sloam"*)
                CONFIG_DESCRIPTION="SlideSLAM integrated visualization"
                break
                ;;
            *"loam_livox"*)
                CONFIG_DESCRIPTION="Faster-LIO SLAM visualization"
                ;;
            *"urbancity_6quadrotors"*)
                CONFIG_DESCRIPTION="Multi-drone urban environment"
                ;;
            *"object_modeller"*)
                CONFIG_DESCRIPTION="Object detection and modelling"
                ;;
            *"default"*)
                CONFIG_DESCRIPTION="Default AirSim visualization"
                ;;
        esac
    fi
done

if [ -n "$SELECTED_CONFIG" ]; then
    echo "✅ Found RViz config: $CONFIG_DESCRIPTION"
    echo "📁 Config file: $SELECTED_CONFIG"
else
    echo "⚠️  No pre-configured RViz files found. Using default RViz."
    SELECTED_CONFIG=""
fi

# Check display environment
if [ -z "$DISPLAY" ]; then
    echo "❌ Error: DISPLAY environment variable not set"
    echo "   If running in Docker, ensure:"
    echo "   - X11 forwarding is enabled: -e DISPLAY=\$DISPLAY"
    echo "   - X11 socket is mounted: -v /tmp/.X11-unix:/tmp/.X11-unix:rw"
    echo "   - Host allows Docker GUI: xhost +local:docker"
    exit 1
fi

# Test GUI capability
echo "🖥️  Testing GUI capability..."
if ! timeout 5 xeyes &>/dev/null; then
    echo "⚠️  GUI test failed, but continuing anyway..."
else
    echo "✅ GUI capability confirmed"
fi

echo "🚀 Launching RViz..."

# Function to monitor ROS topics and provide feedback
monitor_topics() {
    sleep 5
    echo ""
    echo "📡 Available visualization topics:"
    echo "================================="
    
    # AirSim topics
    echo "🚁 AirSim topics:"
    rostopic list | grep airsim | head -5 || echo "   No AirSim topics found"
    
    # SLAM topics
    echo "🗺️  SLAM topics:"
    rostopic list | grep -E "(slam|pose|map)" | head -5 || echo "   No SLAM topics found"
    
    # Point cloud topics
    echo "☁️  Point cloud topics:"
    rostopic list | grep -E "(points|cloud|lidar)" | head -5 || echo "   No point cloud topics found"
    
    # TF topics
    echo "🔄 Transform topics:"
    rostopic list | grep -E "tf" || echo "   No TF topics found"
    
    echo ""
    echo "💡 RViz usage tips:"
    echo "=================="
    echo "  • Add displays using the 'Add' button"
    echo "  • Key topics to visualize:"
    echo "    - /airsim_node/drone_X/odom_local_ned (odometry)"
    echo "    - /airsim_node/drone_X/lidar/Lidar (point clouds)"
    echo "    - /tf (transforms)"
    echo "    - /drone_X/slam_pose (SLAM poses if available)"
    echo "    - /map (generated maps)"
    echo "  • Set Fixed Frame to 'map' or 'odom'"
    echo "  • Use different colors for multiple drones"
}

# Launch RViz with or without config
if [ -n "$SELECTED_CONFIG" ]; then
    echo "📋 Starting RViz with config: $CONFIG_DESCRIPTION"
    
    # Start topic monitoring in background
    monitor_topics &
    MONITOR_PID=$!
    
    # Launch RViz with config
    rosrun rviz rviz -d "$SELECTED_CONFIG"
    
    # Clean up background process
    kill $MONITOR_PID 2>/dev/null || true
    
else
    echo "📋 Starting RViz with default configuration"
    echo "   You'll need to manually add displays for:"
    echo "   - Robot models, point clouds, odometry, etc."
    
    # Start topic monitoring in background
    monitor_topics &
    MONITOR_PID=$!
    
    # Launch RViz without config
    rosrun rviz rviz
    
    # Clean up background process
    kill $MONITOR_PID 2>/dev/null || true
fi

echo "📊 RViz closed. Visualization session ended."
