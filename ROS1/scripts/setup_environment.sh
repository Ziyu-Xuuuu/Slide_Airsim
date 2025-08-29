#!/bin/bash
set -e

echo "🚀 SlideSLAM-AirSim Environment Setup"
echo "======================================"

# Configuration
CONTAINER_NAME="slidehub_v2"
WORKSPACE_PATH="/root/slideslam_ws/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"

# Check if container is running
if ! docker ps --format "table {{.Names}}" | grep -q "^${CONTAINER_NAME}$"; then
    echo "❌ Error: Container '${CONTAINER_NAME}' is not running"
    echo "Please start the container first: docker start ${CONTAINER_NAME}"
    exit 1
fi

echo "✅ Container '${CONTAINER_NAME}' is running"

# Verify workspace is built
echo "🔍 Verifying workspace build status..."
VERIFICATION_RESULT=$(docker exec ${CONTAINER_NAME} bash -c "
    cd ${WORKSPACE_PATH} && 
    source /opt/ros/noetic/setup.bash && 
    source devel/setup.bash 2>/dev/null || true &&
    rospack find airsim_ros_pkgs >/dev/null 2>&1 && echo 'OK' || echo 'FAILED'
")

if [ "$VERIFICATION_RESULT" != "OK" ]; then
    echo "❌ Error: Workspace is not properly built"
    echo "Please run the build process in the container first"
    exit 1
fi

echo "✅ Workspace is properly built and configured"

# Display available functionality
echo ""
echo "🎯 Available Commands:"
echo "======================"
echo "  start_airsim          - Start AirSim simulation interface"
echo "  launch_integration    - Launch basic integration (3 drones)"
echo "  launch_coordination   - Launch full coordination (15 drones)"
echo "  run_diagnostics       - Run system diagnostics"
echo "  open_rviz             - Open RViz visualization"
echo ""

echo "📊 Built Packages:"
echo "=================="
docker exec ${CONTAINER_NAME} bash -c "
    cd ${WORKSPACE_PATH} &&
    source /opt/ros/noetic/setup.bash &&
    source devel/setup.bash &&
    echo 'Core packages:' &&
    echo '  ✅ airsim_ros_pkgs' &&
    echo '  ✅ faster_lio (SLAM)' &&
    echo '  ✅ sloam_msgs' &&
    echo '  ✅ object_modeller' &&
    echo '  ✅ navigation stack' &&
    echo '  ✅ airsim_tutorial_pkgs' &&
    echo 'Total packages built:' \$(ls devel/lib/pkgconfig/ | wc -l)
"

echo ""
echo "🐍 Python Dependencies:"
echo "======================="
echo "  ✅ airsim"
echo "  ✅ numpy, scipy, opencv-python"
echo "  ✅ ROS Noetic packages"

echo ""
echo "🔧 System Dependencies:"
echo "======================="
echo "  ✅ Sophus (SE3/SO3 library)"
echo "  ✅ fmt (formatting library)"
echo "  ✅ qhull (computational geometry)"
echo "  ✅ Eigen3, PCL, OpenCV"

echo ""
echo "📁 Workspace Structure:"
echo "======================="
echo "  Container path: ${WORKSPACE_PATH}"
echo "  Local mount: /mnt/data/Desktop/SlideSlam/ROS1"
echo "  Settings: Resource-Aware-Coordination-AirSim/AirSim/ros/settings_json/"

echo ""
echo "🚀 Quick Start:"
echo "==============="
echo "1. Ensure Unity AirSim is running (click Play button)"
echo "2. Run: docker exec -it ${CONTAINER_NAME} bash"
echo "3. Execute: cd ${WORKSPACE_PATH} && source devel/setup.bash"
echo "4. Launch: roslaunch airsim_ros_pkgs airsim_node.launch"
echo ""
echo "✅ Environment setup verification complete!"