#!/bin/bash

# AirSim-SlideSLAM Unity Integration Launch Script
# This script starts the complete integrated system with proper sequencing

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROS_WS_PATH="../Resource-Aware-Coordination-AirSim/AirSim/ros"
UNITY_PROJECT_PATH="$SCRIPT_DIR"
NUM_DRONES=3
ROS_MASTER_URI="http://localhost:11311"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check if ROS is installed
    if ! command -v roscore &> /dev/null; then
        print_error "ROS is not installed or not in PATH"
        exit 1
    fi
    
    # Check if ROS workspace exists
    if [ ! -d "$ROS_WS_PATH" ]; then
        print_error "ROS workspace not found at: $ROS_WS_PATH"
        exit 1
    fi
    
    # Check if Unity project exists
    if [ ! -f "$UNITY_PROJECT_PATH/Assets/SlideSLAM-AirSim/Scripts/SlideSLAMVehicleExtension.cs" ]; then
        print_error "Unity SlideSLAM integration scripts not found"
        exit 1
    fi
    
    print_success "Prerequisites check passed"
}

# Source ROS environment
setup_ros_environment() {
    print_status "Setting up ROS environment..."
    
    # Source ROS setup
    source /opt/ros/noetic/setup.bash 2>/dev/null || source /opt/ros/melodic/setup.bash 2>/dev/null || {
        print_error "Could not source ROS setup"
        exit 1
    }
    
    # Source workspace
    if [ -f "$ROS_WS_PATH/devel/setup.bash" ]; then
        source "$ROS_WS_PATH/devel/setup.bash"
        print_success "ROS workspace sourced"
    else
        print_warning "ROS workspace not built. Building now..."
        build_ros_workspace
    fi
    
    # Set ROS Master URI
    export ROS_MASTER_URI="$ROS_MASTER_URI"
    export ROS_IP=$(hostname -I | awk '{print $1}')
    
    print_success "ROS environment configured"
    print_status "ROS_MASTER_URI: $ROS_MASTER_URI"
    print_status "ROS_IP: $ROS_IP"
}

# Build ROS workspace if needed
build_ros_workspace() {
    print_status "Building ROS workspace..."
    
    cd "$ROS_WS_PATH"
    
    # Install dependencies
    print_status "Installing ROS dependencies..."
    rosdep install --from-paths src --ignore-src -r -y || print_warning "Some dependencies might be missing"
    
    # Build workspace
    print_status "Building catkin workspace..."
    catkin build -DCMAKE_C_COMPILER=gcc-8 -DCMAKE_CXX_COMPILER=g++-8 || {
        print_error "Failed to build ROS workspace"
        exit 1
    }
    
    # Source the built workspace
    source devel/setup.bash
    
    print_success "ROS workspace built successfully"
}

# Start ROS core
start_roscore() {
    print_status "Starting ROS core..."
    
    # Check if roscore is already running
    if pgrep -f roscore > /dev/null; then
        print_warning "ROS core is already running"
    else
        # Start roscore in background
        roscore &
        ROSCORE_PID=$!
        
        # Wait for roscore to start
        sleep 3
        
        if pgrep -f roscore > /dev/null; then
            print_success "ROS core started (PID: $ROSCORE_PID)"
        else
            print_error "Failed to start ROS core"
            exit 1
        fi
    fi
}

# Start SlideSLAM nodes
start_slideslam_nodes() {
    print_status "Starting SlideSLAM nodes for $NUM_DRONES drones..."
    
    cd "$ROS_WS_PATH"
    source devel/setup.bash
    
    # Start AirSim ROS nodes with SlideSLAM integration
    print_status "Launching AirSim-SlideSLAM integration..."
    roslaunch airsim_ros_pkgs airsim_slideslam_integration.launch num_drones:=$NUM_DRONES &
    AIRSIM_PID=$!
    
    sleep 5
    
    # Start coordination system
    print_status "Launching coordination system..."
    case $NUM_DRONES in
        3)
            roslaunch image_covering_coordination image_covering_3drones_RAG.launch &
            ;;
        15)
            roslaunch image_covering_coordination image_covering_15drones_RAG.launch &
            ;;
        45)
            roslaunch image_covering_coordination image_covering_45drones_RAG.launch &
            ;;
        *)
            print_warning "No predefined coordination launch for $NUM_DRONES drones, using generic launch"
            roslaunch image_covering_coordination connectivity_mesh_neighbors.launch num_drones:=$NUM_DRONES &
            ;;
    esac
    COORD_PID=$!
    
    sleep 3
    
    print_success "SlideSLAM nodes started"
    print_status "AirSim integration PID: $AIRSIM_PID"
    print_status "Coordination system PID: $COORD_PID"
}

# Start RViz for visualization
start_rviz() {
    print_status "Starting RViz for visualization..."
    
    cd "$ROS_WS_PATH"
    source devel/setup.bash
    
    # Check if RViz config exists
    RVIZ_CONFIG="$ROS_WS_PATH/src/scan2shape/rviz/faster-lio-sloam.rviz"
    if [ -f "$RVIZ_CONFIG" ]; then
        rosrun rviz rviz -d "$RVIZ_CONFIG" &
        RVIZ_PID=$!
        print_success "RViz started (PID: $RVIZ_PID)"
    else
        rosrun rviz rviz &
        RVIZ_PID=$!
        print_warning "RViz started with default config (PID: $RVIZ_PID)"
    fi
}

# Monitor system status
monitor_system() {
    print_status "Monitoring system status..."
    
    # Check if key nodes are running
    sleep 5
    
    # Check ROS topics
    print_status "Checking ROS topics..."
    rostopic list | grep -E "(velodyne_points|slam_pose|object_detections)" || {
        print_warning "Some expected topics are not available yet"
    }
    
    # Check node status
    print_status "Active ROS nodes:"
    rosnode list | grep -E "(airsim|sloam|coordination)" || {
        print_warning "Some expected nodes are not running yet"
    }
    
    print_success "System monitoring complete"
}

# Launch Unity (optional, requires Unity Editor)
launch_unity() {
    print_status "Unity project is ready to launch"
    print_status "Please open Unity Editor and load: $UNITY_PROJECT_PATH"
    print_status "Then open the SlideSLAM-AirSim-Demo scene and press Play"
    
    # If Unity command line tools are available, could automate this
    # unity -projectPath "$UNITY_PROJECT_PATH" -executeMethod BuildScript.LaunchScene
}

# Cleanup function
cleanup() {
    print_status "Cleaning up processes..."
    
    # Kill background processes
    if [ ! -z "$RVIZ_PID" ] && kill -0 $RVIZ_PID 2>/dev/null; then
        kill $RVIZ_PID
        print_status "Stopped RViz"
    fi
    
    if [ ! -z "$COORD_PID" ] && kill -0 $COORD_PID 2>/dev/null; then
        kill $COORD_PID
        print_status "Stopped coordination system"
    fi
    
    if [ ! -z "$AIRSIM_PID" ] && kill -0 $AIRSIM_PID 2>/dev/null; then
        kill $AIRSIM_PID
        print_status "Stopped AirSim integration"
    fi
    
    if [ ! -z "$ROSCORE_PID" ] && kill -0 $ROSCORE_PID 2>/dev/null; then
        kill $ROSCORE_PID
        print_status "Stopped ROS core"
    fi
    
    print_success "Cleanup complete"
}

# Trap CTRL+C
trap cleanup EXIT

# Main execution
main() {
    print_status "Starting AirSim-SlideSLAM Unity Integration"
    print_status "Configuration: $NUM_DRONES drones, ROS Master: $ROS_MASTER_URI"
    
    check_prerequisites
    setup_ros_environment
    start_roscore
    start_slideslam_nodes
    start_rviz
    monitor_system
    launch_unity
    
    print_success "System launched successfully!"
    print_status "System is running. Press Ctrl+C to stop all processes."
    print_status ""
    print_status "Next steps:"
    print_status "1. Open Unity Editor"
    print_status "2. Load project: $UNITY_PROJECT_PATH"
    print_status "3. Open SlideSLAM-AirSim-Demo scene"
    print_status "4. Configure ROS Master URI in SlideSLAMManager"
    print_status "5. Press Play to start simulation"
    print_status ""
    print_status "Monitoring logs... (Ctrl+C to exit)"
    
    # Keep script running
    while true; do
        sleep 10
        # Check if critical processes are still running
        if ! pgrep -f roscore > /dev/null; then
            print_error "ROS core has stopped unexpectedly"
            break
        fi
    done
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -n|--num-drones)
            NUM_DRONES="$2"
            shift 2
            ;;
        -u|--ros-uri)
            ROS_MASTER_URI="$2"
            shift 2
            ;;
        -w|--workspace)
            ROS_WS_PATH="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -n, --num-drones NUM    Number of drones (default: 3)"
            echo "  -u, --ros-uri URI       ROS Master URI (default: http://localhost:11311)"
            echo "  -w, --workspace PATH    ROS workspace path"
            echo "  -h, --help              Show this help message"
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Run main function
main