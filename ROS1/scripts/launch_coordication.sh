#!/bin/bash
set -e

echo "🚀 Launching SlideSLAM-AirSim Full Coordination System (15 Drones)..."

# Define workspace path
WORKSPACE="/root/slideslam_ws"
ROS_WS="${WORKSPACE}/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"

# Check if workspace exists
if [ ! -d "${ROS_WS}" ]; then
    echo "❌ Error: ROS workspace not found at ${ROS_WS}"
    echo "Please ensure the container is properly set up and mounted."
    exit 1
fi

# Source ROS environment
echo "📦 Sourcing ROS environment..."
source /opt/ros/noetic/setup.bash

# Source workspace if it exists
if [ -f "${ROS_WS}/devel/setup.bash" ]; then
    echo "📦 Sourcing workspace environment..."
    source "${ROS_WS}/devel/setup.bash"
else
    echo "❌ Error: Workspace not built. Please run setup_container.sh first."
    exit 1
fi

# Change to ROS workspace
cd "${ROS_WS}"

# Check if ROS master is running
if ! rostopic list &>/dev/null; then
    echo "⚠️  Warning: ROS master not detected. Starting roscore..."
    roscore &
    sleep 3
fi

echo "🚁 Launching Full Coordination System..."
echo "This will start:"
echo "  - 15 drones with AirSim interface"
echo "  - Multi-robot SLAM (SLOAM)"
echo "  - faster-lio individual SLAM"
echo "  - Image covering coordination (RAG algorithm)"
echo "  - Object detection and modeling"
echo "  - Performance monitoring"

# Launch the full coordination system
roslaunch airsim_ros_pkgs airsim_slideslam_coordination.launch &
MAIN_PID=$!

# Wait for system to initialize
echo "⏳ Waiting for system initialization..."
sleep 15

echo "✅ Full coordination system launched successfully!"
echo ""
echo "📊 Monitor coordination with:"
echo "   rostopic list | grep -E '(coordination|score|fov|slam)'"
echo "   rostopic echo /total_score_logger/collective_fov_score"
echo "   rostopic hz /drone_0/slam_pose"
echo "   rviz"
echo ""
echo "🎯 Key topics for monitoring:"
echo "   /airsim_node/drone_X/odom_local_ned         - Drone odometry"
echo "   /airsim_node/drone_X/lidar/Lidar            - LiDAR data"
echo "   /drone_X/slam_pose                          - SLAM poses"
echo "   /coordination/task_allocation               - Task assignments"
echo "   /total_score_logger/collective_fov_score    - Performance score"
echo "   /image_covering_coordination/marginal_gain  - Coordination decisions"
echo "   /object_detection/objects                   - Detected objects"
echo ""
echo "🔍 Visualization:"
echo "   Launch RViz: roslaunch airsim_ros_pkgs rviz.launch"
echo "   Monitor area coverage: rostopic echo /area_coverage/visualization"
echo ""
echo "Press Ctrl+C to stop the system..."

# Keep script running
wait $MAIN_PID
