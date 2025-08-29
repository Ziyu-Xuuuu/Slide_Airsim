#!/bin/bash
set -e

echo "🚀 Starting AirSim Three Drone System..."

# Define workspace path
WORKSPACE="/root/slideslam_ws"
ROS_WS="${WORKSPACE}/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"

# Check if workspace exists
if [ ! -d "${ROS_WS}" ]; then
    echo "❌ Error: ROS workspace not found at ${ROS_WS}"
    exit 1
fi

# Source ROS environment
echo "📦 Sourcing ROS environment..."
source /opt/ros/noetic/setup.bash
source "${ROS_WS}/devel/setup.bash"
cd "${ROS_WS}"

# Check if ROS master is running
if ! rostopic list &>/dev/null; then
    echo "⚠️  Starting roscore..."
    roscore &
    sleep 3
fi

echo "🚁 Launching three drone AirSim interface..."
echo "This will start:"
echo "  - Three drones with AirSim interface"
echo "  - Basic sensors (LiDAR, Camera, IMU) for each drone"
echo "  - Odometry publishing for each drone"

# Launch three drones
roslaunch airsim_ros_pkgs airsim_node.launch &
MAIN_PID=$!

echo ""
echo "⚠️  IMPORTANT: Make sure AirSim Unity application is running first!"
echo "   The Unity simulation environment must be active before launching this script."
echo ""

echo "⏳ Waiting for AirSim connection..."
sleep 5

echo "✅ Three drone system launched!"
echo ""
echo "📊 Available topics for Drone1:"
echo "   /drone1/airsim_node_drone1/odom_local_ned     - Drone1 odometry"
echo "   /drone1/airsim_node_drone1/lidar/Lidar1       - Drone1 LiDAR point cloud"
echo "   /drone1/airsim_node_drone1/front_center/image_raw - Drone1 camera image"
echo "   /drone1/airsim_node_drone1/imu/imu            - Drone1 IMU data"
echo ""
echo "📊 Available topics for Drone2:"
echo "   /drone2/airsim_node_drone2/odom_local_ned     - Drone2 odometry"
echo "   /drone2/airsim_node_drone2/lidar/Lidar1       - Drone2 LiDAR point cloud"
echo "   /drone2/airsim_node_drone2/front_center/image_raw - Drone2 camera image"
echo "   /drone2/airsim_node_drone2/imu/imu            - Drone2 IMU data"
echo ""
echo "📊 Available topics for Drone3:"
echo "   /drone3/airsim_node_drone3/odom_local_ned     - Drone3 odometry"
echo "   /drone3/airsim_node_drone3/lidar/Lidar1       - Drone3 LiDAR point cloud"
echo "   /drone3/airsim_node_drone3/front_center/image_raw - Drone3 camera image"
echo "   /drone3/airsim_node_drone3/imu/imu            - Drone3 IMU data"
echo ""
echo "🎮 Control the drones through:"
echo "   - AirSim Unity interface"
echo "   - ROS topics for position/velocity commands"
echo "   - Manual keyboard control in Unity"
echo ""
echo "📊 Monitor with:"
echo "   rostopic list"
echo "   rostopic hz /drone1/airsim_node_drone1/odom_local_ned"
echo "   rostopic hz /drone2/airsim_node_drone2/odom_local_ned"
echo "   rostopic hz /drone3/airsim_node_drone3/odom_local_ned"
echo "   rviz"
echo ""
echo "Press Ctrl+C to stop..."

wait $MAIN_PID
