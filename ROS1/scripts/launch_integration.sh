#!/bin/bash
set -e

echo "🚀 Launching SlideSLAM-AirSim Integration (3 Drones)..."

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

echo "🚁 Launching SlideSLAM Integration System..."
echo "This will start:"
echo "  - 3 drones with AirSim interface"
echo "  - faster-lio SLAM system"
echo "  - Object detection and modeling"
echo "  - Decentralized multi-robot coordination"

# Launch the integration system (3 drones)
roslaunch airsim_ros_pkgs airsim_slideslam_integration_3drones.launch &
MAIN_PID=$!

# Wait for system to initialize
echo "⏳ Waiting for system initialization..."
sleep 10

echo "✅ Integration system launched successfully!"
echo ""
echo "📊 Monitor the system with:"
echo "   rostopic list | grep -E '(slam|airsim|odom|lidar)'"
echo "   rostopic hz /drone_0/odom_local_ned"
echo "   rostopic echo /drone_0/lidar/Lidar1"
echo "   rviz"
echo ""
echo "🎯 Key topics (3 drones):"
echo "   /drone_0/odom_local_ned                - Drone 0 odometry"
echo "   /drone_1/odom_local_ned                - Drone 1 odometry"
echo "   /drone_2/odom_local_ned                - Drone 2 odometry"
echo "   /drone_X/lidar/Lidar1                  - LiDAR data"
echo "   /mapping/odometry                      - SLAM odometry"
echo "   /mapping/path                          - SLAM trajectory"
echo "   /object_detection/objects              - Detected objects"
echo ""
echo "Press Ctrl+C to stop the system..."

# Keep script running
wait $MAIN_PID
