#!/bin/bash
set -e

echo "🔍 Running SlideSLAM-AirSim System Diagnostics..."

# Define workspace paths
WORKSPACE="/root/slideslam_ws"
ROS_WS="${WORKSPACE}/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros"

# Source ROS environment
source /opt/ros/noetic/setup.bash

# Source workspace if available
if [ -f "${ROS_WS}/devel/setup.bash" ]; then
    source "${ROS_WS}/devel/setup.bash"
else
    echo "⚠️  Warning: ROS workspace not built. Some diagnostics may fail."
fi

echo "📋 System Information:"
echo "===================="
echo "🐧 OS: $(lsb_release -d | cut -f2)"
echo "🤖 ROS Version: $(rosversion -d)"
echo "🐍 Python Version: $(python3 --version)"
echo "💾 Memory: $(free -h | grep Mem | awk '{print $2}' | sed 's/Gi/ GB/')"
echo "⚙️  CPU Cores: $(nproc)"
echo ""

echo "📁 Workspace Status:"
echo "==================="
echo "📍 Workspace Path: ${WORKSPACE}"
echo "📍 ROS Workspace: ${ROS_WS}"

if [ -d "${WORKSPACE}" ]; then
    echo "✅ Main workspace exists"
    echo "📊 Size: $(du -sh ${WORKSPACE} 2>/dev/null | cut -f1)"
else
    echo "❌ Main workspace missing"
fi

if [ -d "${ROS_WS}" ]; then
    echo "✅ ROS workspace exists"
    if [ -f "${ROS_WS}/devel/setup.bash" ]; then
        echo "✅ ROS workspace built"
    else
        echo "❌ ROS workspace not built"
    fi
else
    echo "❌ ROS workspace missing"
fi

echo ""

echo "🐍 Python Dependencies:"
echo "======================"
PYTHON_PACKAGES=("airsim" "numpy" "scipy" "opencv-python" "matplotlib" "scikit-learn")
for pkg in "${PYTHON_PACKAGES[@]}"; do
    if python3 -c "import ${pkg/opencv-python/cv2}" 2>/dev/null; then
        VERSION=$(python3 -c "import ${pkg/opencv-python/cv2}; print(${pkg/opencv-python/cv2}.__version__)" 2>/dev/null || echo "unknown")
        echo "✅ $pkg ($VERSION)"
    else
        echo "❌ $pkg missing"
    fi
done

echo ""

echo "🤖 ROS System Status:"
echo "===================="

# Check if ROS master is running
if rostopic list &>/dev/null; then
    echo "✅ ROS Master running"
    echo "📊 Active nodes: $(rosnode list 2>/dev/null | wc -l)"
    echo "📊 Active topics: $(rostopic list 2>/dev/null | wc -l)"
    echo "📊 Active services: $(rosservice list 2>/dev/null | wc -l)"
else
    echo "❌ ROS Master not running"
    echo "   Start with: roscore"
fi

echo ""

echo "📦 ROS Packages Status:"
echo "======================"
if [ -d "${ROS_WS}/src" ]; then
    cd "${ROS_WS}"
    PACKAGES_TO_CHECK=("airsim_ros_pkgs" "faster-lio" "sloam" "sloam_msgs" "image_covering_coordination" "object_modeller" "scan2shape" "multi_robot_utils_launch")
    
    for pkg in "${PACKAGES_TO_CHECK[@]}"; do
        if rospack find "$pkg" >/dev/null 2>&1; then
            PKG_PATH=$(rospack find "$pkg")
            echo "✅ $pkg"
            echo "   📍 Path: $PKG_PATH"
        else
            if [ -d "src/$pkg" ]; then
                echo "⚠️  $pkg (source exists but not built)"
            else
                echo "❌ $pkg (missing)"
            fi
        fi
    done
else
    echo "❌ ROS source directory not found"
fi

echo ""

echo "🔗 AirSim Connection Test:"
echo "=========================="
python3 -c "
import airsim
import sys
try:
    print('🔍 Testing AirSim connection...')
    client = airsim.MultirotorClient()
    client.confirmConnection()
    print('✅ AirSim connection successful!')
    
    # Get vehicle list
    vehicle_names = []
    for i in range(15):
        try:
            vehicle_name = f'drone_{i}'
            state = client.getMultirotorState(vehicle_name=vehicle_name)
            vehicle_names.append(vehicle_name)
        except:
            break
    
    if vehicle_names:
        print(f'🚁 Found {len(vehicle_names)} drones: {vehicle_names}')
    else:
        print('⚠️  No drones detected')
        
except Exception as e:
    print(f'❌ AirSim connection failed: {e}')
    print('   Make sure Unity AirSim is running and you clicked Play!')
"

echo ""

echo "📊 Active Topics Analysis:"
echo "========================="
if rostopic list &>/dev/null; then
    echo "🔍 AirSim topics:"
    rostopic list 2>/dev/null | grep airsim | head -10
    
    echo "🔍 SLAM topics:"
    rostopic list 2>/dev/null | grep -E "(slam|odom|pose)" | head -10
    
    echo "🔍 LiDAR topics:"
    rostopic list 2>/dev/null | grep -E "(lidar|points|cloud)" | head -5
    
    echo "🔍 Coordination topics:"
    rostopic list 2>/dev/null | grep -E "(coordination|score|fov)" | head -5
else
    echo "❌ Cannot analyze topics - ROS master not running"
fi

echo ""

echo "🎯 Topic Data Flow Test:"
echo "======================="
if rostopic list &>/dev/null; then
    # Test key topics for data flow
    TEST_TOPICS=("/airsim_node/drone_0/odom_local_ned" "/airsim_node/drone_0/lidar/Lidar" "/tf" "/tf_static")
    
    for topic in "${TEST_TOPICS[@]}"; do
        if rostopic list 2>/dev/null | grep -q "^${topic}$"; then
            echo -n "📡 Testing $topic... "
            if timeout 5 rostopic echo "$topic" -n 1 &>/dev/null; then
                RATE=$(timeout 10 rostopic hz "$topic" 2>/dev/null | grep "average rate" | awk '{print $3}' || echo "unknown")
                echo "✅ Active (${RATE} Hz)"
            else
                echo "❌ No data"
            fi
        else
            echo "⚠️  $topic not published"
        fi
    done
else
    echo "❌ Cannot test data flow - ROS master not running"
fi

echo ""

echo "🔧 Integration Status:"
echo "====================="
cd "${ROS_WS}" 2>/dev/null || true

# Check if integration diagnosis script exists
if [ -f "diagnose_integration.sh" ]; then
    echo "🔍 Running integration diagnosis..."
    ./diagnose_integration.sh
else
    echo "⚠️  Integration diagnosis script not found"
    echo "   Manual checks:"
    
    # Check launch files
    echo "📋 Checking launch files:"
    LAUNCH_FILES=("airsim_slideslam_integration.launch" "airsim_slideslam_coordination.launch")
    for launch in "${LAUNCH_FILES[@]}"; do
        if find . -name "$launch" -type f 2>/dev/null | head -1 | grep -q .; then
            echo "✅ $launch found"
        else
            echo "❌ $launch missing"
        fi
    done
fi

echo ""

echo "📈 System Resources:"
echo "=================="
echo "💾 Memory usage:"
free -h

echo ""
echo "💿 Disk usage:"
df -h / | tail -1

echo ""
echo "⚡ CPU usage:"
top -bn1 | grep "Cpu(s)" | awk '{print $2}' | sed 's/us,/% CPU usage/'

echo ""

echo "🎯 Recommendations:"
echo "=================="

# Provide recommendations based on findings
if ! rostopic list &>/dev/null; then
    echo "🔧 Start ROS master: roscore"
fi

if ! python3 -c "import airsim; airsim.MultirotorClient().confirmConnection()" 2>/dev/null; then
    echo "🔧 Start Unity AirSim and click Play button"
fi

if [ ! -f "${ROS_WS}/devel/setup.bash" ]; then
    echo "🔧 Build ROS workspace: run setup_container.sh"
fi

echo "🔧 To launch system:"
echo "   1. start_airsim (if Unity is running)"
echo "   2. launch_integration (basic 3-drone setup)"
echo "   3. launch_coordination (full 15-drone setup)"
echo "   4. open_rviz (for visualization)"

echo ""
echo "✅ Diagnostics complete!"
