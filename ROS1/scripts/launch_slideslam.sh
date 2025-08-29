#!/bin/bash
set -e

echo "🚁 Launching SlideSLAM-AirSim System"
echo "===================================="

CONTAINER_NAME="slidehub_v2"
WORKSPACE_PATH="/root/slideslam_ws/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"

# Function to check if container is running
check_container() {
    if ! docker ps --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
        echo "❌ Error: Container '${CONTAINER_NAME}' is not running"
        echo "Please start the container: docker start ${CONTAINER_NAME}"
        exit 1
    fi
}

# Function to execute commands in container
exec_in_container() {
    docker exec ${CONTAINER_NAME} bash -c "cd ${WORKSPACE_PATH} && source /opt/ros/noetic/setup.bash && source devel/setup.bash && $1"
}

# Check container status
check_container

# Select launch mode
echo "Select launch mode:"
echo "1) Basic Integration (3 drones)"
echo "2) Full Coordination (15 drones)"
echo "3) Single drone demo"
echo "4) Custom launch"
read -p "Enter choice [1-4]: " choice

case $choice in
    1)
        echo "🚁 Launching Basic Integration (3 drones)..."
        exec_in_container "roslaunch airsim_ros_pkgs airsim_node_6quadrotors.launch &
        sleep 3
        roslaunch faster_lio mapping_velodyne.launch &
        echo 'Basic integration launched. Check topics with: rostopic list'"
        ;;
    2)
        echo "🚁 Launching Full Coordination (15 drones)..."
        exec_in_container "roslaunch airsim_ros_pkgs airsim_node_10quadrotors_imcov_SS.launch &
        sleep 5
        roslaunch faster_lio mapping_velodyne.launch &
        sleep 3
        echo 'Full coordination launched. Monitor with: rostopic list | grep coordination'"
        ;;
    3)
        echo "🚁 Launching Single Drone Demo..."
        exec_in_container "roslaunch airsim_ros_pkgs airsim_node.launch"
        ;;
    4)
        echo "Available launch files:"
        exec_in_container "find src/airsim_ros_pkgs/launch -name '*.launch' | head -10"
        read -p "Enter launch file name: " launch_file
        exec_in_container "roslaunch airsim_ros_pkgs ${launch_file}"
        ;;
    *)
        echo "Invalid choice. Exiting."
        exit 1
        ;;
esac

echo "✅ Launch completed!"
echo ""
echo "📊 Useful monitoring commands:"
echo "  rostopic list                    - List all topics"
echo "  rostopic hz /airsim_node/drone_0/odom_local_ned  - Check odometry rate"
echo "  rostopic echo /airsim_node/drone_0/lidar/Lidar   - Monitor LiDAR data"
echo "  rosnode list                     - List running nodes"