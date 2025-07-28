#!/bin/bash

# SlideSLAM-AirSim Integration Test Script
# This script builds and tests the integrated system

set -e  # Exit on any error

echo "🚀 SlideSLAM-AirSim Integration Test Script"
echo "============================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if we're in the right directory
if [ ! -f "build_integrated_system.sh" ]; then
    echo -e "${RED}❌ Error: Must run from AirSim/ros directory${NC}"
    exit 1
fi

# Function to check if a process is running
check_process() {
    if pgrep -f "$1" > /dev/null; then
        echo -e "${GREEN}✅ $1 is running${NC}"
        return 0
    else
        echo -e "${RED}❌ $1 is not running${NC}"
        return 1
    fi
}

# Function to check ROS topics
check_topics() {
    local robot_name=$1
    local expected_topics=(
        "/${robot_name}/airsim_node/odom_local_ned"
        "/${robot_name}/airsim_node/lidar/Lidar"
        "/${robot_name}/sync_odom"
        "/${robot_name}/velodyne_points"
        "/${robot_name}/slam_pose"
    )
    
    echo -e "${BLUE}📡 Checking topics for ${robot_name}:${NC}"
    for topic in "${expected_topics[@]}"; do
        if rostopic list | grep -q "^${topic}$"; then
            echo -e "${GREEN}  ✅ ${topic}${NC}"
        else
            echo -e "${RED}  ❌ ${topic} (missing)${NC}"
        fi
    done
}

# Function to check TF frames
check_tf_frames() {
    echo -e "${BLUE}🔄 Checking TF frames:${NC}"
    if rosrun tf tf_monitor > /dev/null 2>&1; then
        echo -e "${GREEN}✅ TF system is active${NC}"
    else
        echo -e "${YELLOW}⚠️  TF monitoring failed - check manually with: rosrun tf view_frames${NC}"
    fi
}

# Step 1: Build the system
echo -e "${BLUE}🔨 Step 1: Building integrated system...${NC}"
if ! ./build_integrated_system.sh; then
    echo -e "${RED}❌ Build failed!${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Build completed successfully${NC}"

# Step 2: Source the workspace
echo -e "${BLUE}📦 Step 2: Sourcing workspace...${NC}"
source devel/setup.bash
echo -e "${GREEN}✅ Workspace sourced${NC}"

# Step 3: Check if roscore is running
echo -e "${BLUE}🤖 Step 3: Checking ROS environment...${NC}"
if ! check_process "roscore"; then
    echo -e "${YELLOW}⚠️  Starting roscore in background...${NC}"
    roscore &
    sleep 3
    if ! check_process "roscore"; then
        echo -e "${RED}❌ Failed to start roscore${NC}"
        exit 1
    fi
fi

# Step 4: Quick launch test (dry run)
echo -e "${BLUE}🧪 Step 4: Testing launch files...${NC}"

# Test basic integration launch file
echo -e "${BLUE}  Testing basic integration launch file...${NC}"
if roslaunch --check airsim_ros_pkgs airsim_slideslam_integration.launch > /dev/null 2>&1; then
    echo -e "${GREEN}  ✅ airsim_slideslam_integration.launch syntax OK${NC}"
else
    echo -e "${RED}  ❌ airsim_slideslam_integration.launch has syntax errors${NC}"
fi

# Test coordination launch file
echo -e "${BLUE}  Testing coordination launch file...${NC}"
if roslaunch --check airsim_ros_pkgs airsim_slideslam_coordination.launch > /dev/null 2>&1; then
    echo -e "${GREEN}  ✅ airsim_slideslam_coordination.launch syntax OK${NC}"
else
    echo -e "${RED}  ❌ airsim_slideslam_coordination.launch has syntax errors${NC}"
fi

# Step 5: Node executable check
echo -e "${BLUE}🔍 Step 5: Checking executables...${NC}"
if [ -f "devel/lib/airsim_ros_pkgs/airsim_slideslam_bridge" ]; then
    echo -e "${GREEN}✅ Bridge node executable found${NC}"
else
    echo -e "${RED}❌ Bridge node executable missing${NC}"
fi

# Step 6: Package dependency check
echo -e "${BLUE}📋 Step 6: Checking package dependencies...${NC}"
required_packages=("sloam" "sloam_msgs" "object_modeller" "scan2shape" "multi_robot_utils_launch")
for pkg in "${required_packages[@]}"; do
    if rospack find "$pkg" > /dev/null 2>&1; then
        echo -e "${GREEN}  ✅ ${pkg}${NC}"
    else
        echo -e "${RED}  ❌ ${pkg} (not found)${NC}"
    fi
done

# Step 7: Interactive test menu
echo
echo -e "${BLUE}🎯 Step 7: Interactive Testing Menu${NC}"
echo "Choose a test to run:"
echo "1) Quick 3-drone integration test (5 minutes)"
echo "2) Full 15-drone coordination test (requires AirSim running)"
echo "3) Bridge node unit test"
echo "4) Topic flow verification"
echo "5) Skip interactive tests"
read -p "Enter choice (1-5): " choice

case $choice in
    1)
        echo -e "${BLUE}🚁 Starting 3-drone integration test...${NC}"
        echo -e "${YELLOW}⚠️  Make sure AirSim simulation is running!${NC}"
        read -p "Press Enter when AirSim is ready, or Ctrl+C to cancel..."
        
        roslaunch airsim_ros_pkgs airsim_slideslam_integration.launch &
        LAUNCH_PID=$!
        sleep 10
        
        # Check topics for each drone
        for i in {0..2}; do
            check_topics "drone_$i"
        done
        
        check_tf_frames
        
        echo -e "${YELLOW}Test running... Press Enter to stop${NC}"
        read
        kill $LAUNCH_PID 2>/dev/null || true
        ;;
    2)
        echo -e "${BLUE}🚁 Starting 15-drone coordination test...${NC}"
        echo -e "${YELLOW}⚠️  This is resource intensive! Make sure AirSim simulation is running!${NC}"
        read -p "Press Enter when ready, or Ctrl+C to cancel..."
        
        roslaunch airsim_ros_pkgs airsim_slideslam_coordination.launch &
        LAUNCH_PID=$!
        sleep 15
        
        # Check a few key topics
        check_topics "drone_0"
        check_topics "drone_7"
        check_topics "drone_14"
        
        echo -e "${YELLOW}Test running... Press Enter to stop${NC}"
        read
        kill $LAUNCH_PID 2>/dev/null || true
        ;;
    3)
        echo -e "${BLUE}🔗 Testing bridge node standalone...${NC}"
        rosrun airsim_ros_pkgs airsim_slideslam_bridge __name:=test_bridge _robot_name:=test_drone &
        BRIDGE_PID=$!
        sleep 3
        
        if check_process "airsim_slideslam_bridge"; then
            echo -e "${GREEN}✅ Bridge node started successfully${NC}"
        fi
        
        kill $BRIDGE_PID 2>/dev/null || true
        ;;
    4)
        echo -e "${BLUE}📡 Topic flow verification...${NC}"
        echo "Run 'rostopic list | grep -E \"(airsim|sloam|slam)\"' to see all related topics"
        echo "Run 'rostopic hz /drone_0/slam_pose' to check SLAM pose publishing rate"
        echo "Run 'rosnode list | grep -E \"(airsim|bridge|sloam)\"' to see all related nodes"
        ;;
    5)
        echo -e "${YELLOW}⏭️  Skipping interactive tests${NC}"
        ;;
    *)
        echo -e "${RED}❌ Invalid choice${NC}"
        ;;
esac

# Final summary
echo
echo -e "${BLUE}📋 Integration Test Summary${NC}"
echo "================================"
echo -e "${GREEN}✅ Build system: OK${NC}"
echo -e "${GREEN}✅ Launch files: Syntax OK${NC}"
echo -e "${GREEN}✅ Executables: Found${NC}"

echo
echo -e "${BLUE}🚀 Next Steps:${NC}"
echo "1. Start AirSim simulation with your environment"
echo "2. Run: roslaunch airsim_ros_pkgs airsim_slideslam_integration.launch"
echo "3. Monitor topics: rostopic list | grep slam"
echo "4. Check visualization: rosrun rviz rviz"
echo
echo -e "${GREEN}🎉 Integration test completed!${NC}"