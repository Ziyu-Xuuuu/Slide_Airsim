#!/bin/bash

# SlideSLAM-AirSim Integration Dependencies Setup Script
# This script installs all required dependencies for the integrated system

set -e

echo "📦 SlideSLAM-AirSim Dependencies Setup"
echo "====================================="

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check if running as root for apt installs
check_sudo() {
    if ! sudo -n true 2>/dev/null; then
        echo -e "${YELLOW}⚠️  This script requires sudo access for system packages${NC}"
        echo "Please enter your password when prompted."
    fi
}

install_system_deps() {
    echo -e "${BLUE}🔧 Step 1: Installing system dependencies...${NC}"
    
    sudo apt update
    
    # Core ROS packages
    echo -e "${BLUE}Installing ROS packages...${NC}"
    sudo apt install -y \
        ros-noetic-tf2-ros \
        ros-noetic-sensor-msgs \
        ros-noetic-nav-msgs \
        ros-noetic-geometry-msgs \
        ros-noetic-pcl-ros \
        ros-noetic-pcl-conversions \
        ros-noetic-vision-opencv \
        ros-noetic-image-transport \
        ros-noetic-cv-bridge \
        ros-noetic-message-generation \
        ros-noetic-message-runtime \
        ros-noetic-std-srvs \
        ros-noetic-actionlib \
        ros-noetic-actionlib-msgs \
        ros-noetic-dynamic-reconfigure
    
    # Additional dependencies for SlideSLAM
    echo -e "${BLUE}Installing SlideSLAM system dependencies...${NC}"
    sudo apt install -y \
        libgoogle-glog-dev \
        libdw-dev \
        libeigen3-dev \
        libboost-all-dev \
        libpcl-dev \
        python3-pip \
        python3-dev \
        python3-numpy \
        python3-scipy \
        python3-matplotlib \
        python3-sklearn \
        libtbb-dev
    
    echo -e "${GREEN}✅ System dependencies installed${NC}"
}

install_gtsam() {
    echo -e "${BLUE}🔧 Step 2: Installing GTSAM...${NC}"
    
    if pkg-config --exists gtsam; then
        echo -e "${GREEN}✅ GTSAM already installed${NC}"
        return 0
    fi
    
    echo -e "${BLUE}Installing GTSAM from PPA...${NC}"
    sudo add-apt-repository -y ppa:borglab/gtsam-release-4.0
    sudo apt update
    sudo apt install -y libgtsam-dev libgtsam-unstable-dev
    
    echo -e "${GREEN}✅ GTSAM installed${NC}"
}

install_python_deps() {
    echo -e "${BLUE}🐍 Step 3: Installing Python dependencies...${NC}"
    
    # Install Python packages
    pip3 install --user \
        numpy==1.22.3 \
        scikit-learn \
        scipy \
        open3d \
        opencv-python \
        matplotlib \
        ultralytics==8.0.59
    
    # Install pypcd if needed
    pip3 install --user git+https://github.com/dimatura/pypcd.git
    
    echo -e "${GREEN}✅ Python dependencies installed${NC}"
}

install_ros_numpy() {
    echo -e "${BLUE}📦 Step 4: Installing ros_numpy...${NC}"
    
    if rospack find ros_numpy > /dev/null 2>&1; then
        echo -e "${GREEN}✅ ros_numpy already installed${NC}"
        return 0
    fi
    
    sudo apt install -y ros-noetic-ros-numpy
    
    echo -e "${GREEN}✅ ros_numpy installed${NC}"
}

setup_workspace() {
    echo -e "${BLUE}🔧 Step 5: Setting up workspace dependencies...${NC}"
    
    # Source ROS
    source /opt/ros/noetic/setup.bash
    
    # Install rosdep dependencies
    cd src
    rosdep update
    rosdep install --from-paths . --ignore-src -r -y
    cd ..
    
    echo -e "${GREEN}✅ Workspace dependencies resolved${NC}"
}

install_optional_deps() {
    echo -e "${BLUE}⚙️  Step 6: Installing optional dependencies...${NC}"
    
    echo "Would you like to install optional components?"
    echo "1) GPU acceleration (CUDA) - for faster processing"
    echo "2) Additional visualization tools"
    echo "3) Development tools (debugging, profiling)"
    echo "4) Skip optional components"
    
    read -p "Enter choice (1-4): " choice
    
    case $choice in
        1)
            echo -e "${BLUE}Installing CUDA support (if available)...${NC}"
            sudo apt install -y nvidia-cuda-toolkit || echo -e "${YELLOW}⚠️  CUDA not available on this system${NC}"
            ;;
        2)
            echo -e "${BLUE}Installing visualization tools...${NC}"
            sudo apt install -y \
                ros-noetic-rviz \
                ros-noetic-rqt \
                ros-noetic-rqt-common-plugins \
                meshlab \
                paraview
            ;;
        3)
            echo -e "${BLUE}Installing development tools...${NC}"
            sudo apt install -y \
                gdb \
                valgrind \
                htop \
                tmux \
                tree
            ;;
        4)
            echo -e "${YELLOW}⏭️  Skipping optional components${NC}"
            ;;
    esac
}

verify_installation() {
    echo -e "${BLUE}🔍 Step 7: Verifying installation...${NC}"
    
    # Check critical packages
    critical_packages=("gtsam" "ros-noetic-tf2-ros" "python3-numpy")
    all_good=true
    
    for pkg in "${critical_packages[@]}"; do
        if dpkg -l | grep -q "$pkg"; then
            echo -e "${GREEN}✅ $pkg${NC}"
        else
            echo -e "${RED}❌ $pkg${NC}"
            all_good=false
        fi
    done
    
    # Check Python packages
    python_packages=("numpy" "scipy" "sklearn" "cv2")
    for pkg in "${python_packages[@]}"; do
        if python3 -c "import $pkg" 2>/dev/null; then
            echo -e "${GREEN}✅ Python: $pkg${NC}"
        else
            echo -e "${RED}❌ Python: $pkg${NC}"
            all_good=false
        fi
    done
    
    if $all_good; then
        echo -e "${GREEN}🎉 All dependencies verified successfully!${NC}"
    else
        echo -e "${YELLOW}⚠️  Some dependencies may need manual installation${NC}"
    fi
}

# Main execution
main() {
    echo -e "${BLUE}Starting dependency installation...${NC}"
    echo "This may take 10-20 minutes depending on your system."
    echo
    
    check_sudo
    
    install_system_deps
    install_gtsam
    install_python_deps
    install_ros_numpy
    
    if [ -d "src" ]; then
        setup_workspace
    else
        echo -e "${YELLOW}⚠️  Run this script from the ROS workspace root to setup workspace dependencies${NC}"
    fi
    
    install_optional_deps
    verify_installation
    
    echo
    echo -e "${GREEN}🎯 Dependencies setup complete!${NC}"
    echo
    echo -e "${BLUE}Next steps:${NC}"
    echo "1. ./build_integrated_system.sh"
    echo "2. ./test_slideslam_integration.sh"
    echo "3. Start AirSim simulation"
    echo "4. roslaunch airsim_ros_pkgs airsim_slideslam_integration.launch"
}

# Check if script is being sourced or executed
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi