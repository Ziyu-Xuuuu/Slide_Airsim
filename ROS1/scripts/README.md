# SlideSLAM-AirSim Scripts

This directory contains revised and fully functional scripts for the SlideSLAM-AirSim Unity integration project.

## 📋 Script Overview

### 🔧 `setup_container.sh`
**Purpose**: Complete container environment setup and build process
**Usage**: `./setup_container.sh` or `setup_container`
**What it does**:
- Builds all SlideSLAM dependencies (Sophus, fmt, faster-lio, qhull)
- Sets up and builds the ROS workspace
- Installs Python dependencies (airsim, numpy, opencv, etc.)
- Creates convenience aliases
- Verifies installation
- Configures AirSim settings

**When to use**: Run once after container startup or when dependencies change

---

### 🚁 `start_airsim.sh`
**Purpose**: Unity AirSim integration and connection management
**Usage**: `./start_airsim.sh` or `start_airsim`
**What it does**:
- Tests connection to Unity AirSim (localhost:41451)
- Waits for Unity to be running and Play button clicked
- Sets up AirSim environment and settings
- Initializes drone API control
- Monitors connection and provides troubleshooting

**When to use**: After Unity is running and you've clicked Play

---

### 🚀 `launch_integration.sh`
**Purpose**: Basic 3-drone SLAM integration system
**Usage**: `./launch_integration.sh` or `launch_integration`
**What it does**:
- Starts ROS master if not running
- Launches AirSim ROS interface
- Starts SLAM system (faster-lio)
- Launches object detection (if available)
- Provides system monitoring information

**When to use**: For basic testing with 3 drones

---

### 🎯 `launch_coordication.sh`
**Purpose**: Full 15-drone coordination system
**Usage**: `./launch_coordication.sh` or `launch_coordination`
**What it does**:
- Starts complete multi-drone AirSim setup
- Launches advanced SLAM (SLOAM)
- Starts coordination algorithms (RAG/SG)
- Enables performance monitoring
- Provides comprehensive system launch

**When to use**: For full-scale coordination research with 15 drones

---

### 🔍 `run_diagnostics.sh`
**Purpose**: Comprehensive system diagnostics and health check
**Usage**: `./run_diagnostics.sh` or `run_diagnostics`
**What it does**:
- System information (OS, ROS, Python, resources)
- Workspace status verification
- Python dependencies check
- ROS system status
- AirSim connection test
- Topic analysis and data flow verification
- Integration status check
- Resource usage monitoring
- Troubleshooting recommendations

**When to use**: When troubleshooting issues or verifying system health

---

### 📊 `open_rviz.sh`
**Purpose**: RViz visualization with automatic configuration detection
**Usage**: `./open_rviz.sh` or `open_rviz`
**What it does**:
- Finds and uses appropriate RViz configuration
- Tests GUI capability
- Provides topic monitoring and usage tips
- Launches RViz with optimal settings
- Supports multiple visualization scenarios

**When to use**: For monitoring and visualizing the running system

---

### 🔧 `fix_rpc_headers.sh`
**Purpose**: Fix missing RPC headers and build rpclib dependencies
**Usage**: `./fix_rpc_headers.sh`
**What it does**:
- Builds rpclib from source
- Creates minimal RPC headers if missing
- Fixes CMakeLists.txt include paths
- Installs RPC libraries system-wide
- Resolves `rpc/rpc_error.h` missing errors

**When to use**: When you see `fatal error: rpc/rpc_error.h: No such file or directory`

---

### 🔄 `fix_circular_deps.sh`
**Purpose**: Resolve circular dependency issues in ROS packages
**Usage**: `./fix_circular_deps.sh`
**What it does**:
- Cleans all previous builds
- Temporarily removes circular dependencies
- Builds packages in correct dependency order
- Restores dependencies after core packages built
- Handles `Could not find airsim_ros_pkgs` errors

**When to use**: When you see circular dependency or package not found errors

---

## 🚀 Quick Start Workflow

### First Time Setup:
```bash
# 1. Start container and run setup
docker exec -it slidehub_v2 bash
./setup_container.sh

# 2. Start Unity AirSim (click Play button)

# 3. Test AirSim connection
./start_airsim.sh
```

### Daily Usage:
```bash
# Terminal 1: Start AirSim interface
start_airsim

# Terminal 2: Launch system
launch_integration    # OR launch_coordination

# Terminal 3: Open visualization
open_rviz

# Terminal 4: Monitor system
run_diagnostics
```

## 🔧 Script Features

### ✅ All Scripts Include:
- **Error handling**: `set -e` for robust execution
- **Path validation**: Checks workspace and file existence
- **Environment setup**: Proper ROS sourcing
- **User feedback**: Clear status messages and emojis
- **Troubleshooting**: Helpful error messages and recommendations

### ✅ Enhanced Functionality:
- **Automatic fallbacks**: Alternative configurations when files missing
- **System checks**: Validates prerequisites before execution
- **Resource monitoring**: Displays system status and recommendations
- **GUI support**: X11 forwarding validation for RViz
- **Multi-configuration**: Supports various drone counts and setups

## 📁 File Structure Integration

The scripts work with this project structure:
```
/root/slideslam_ws/
└── Source_Code/
    ├── Resource-Aware-Coordination-AirSim/AirSim/ros/  # Main ROS workspace
    │   ├── src/                                        # ROS packages
    │   ├── devel/                                      # Built workspace
    │   └── settings_json/                              # AirSim configurations
    ├── SlideSLAM/                                      # SLAM dependencies
    │   ├── Sophus/, fmt/, faster-lio/, qhull-2020.2/
    └── scripts/                                        # These scripts
```

## 🎯 Usage Scenarios

### Scenario 1: Basic Testing (3 drones)
```bash
start_airsim &
launch_integration &
open_rviz
```

### Scenario 2: Full Coordination (15 drones)
```bash
start_airsim &
launch_coordination &
run_diagnostics
```

### Scenario 3: Development & Debugging
```bash
run_diagnostics        # Check system health
start_airsim           # Test AirSim connection
launch_integration     # Start with basic setup
open_rviz             # Monitor visualization
```

## 🐛 Troubleshooting

If scripts fail, check:
1. **Container setup**: Run `setup_container.sh` first
2. **Unity AirSim**: Ensure Unity is running with Play clicked
3. **ROS master**: Check if roscore is running
4. **Workspace**: Verify paths and file permissions
5. **Dependencies**: Check Python packages and ROS dependencies

Use `run_diagnostics.sh` for comprehensive system analysis and specific troubleshooting recommendations.

## 📝 Notes

- All scripts are designed to work in the containerized environment
- Scripts include automatic path detection and validation
- Syntax has been verified for all scripts
- Scripts provide clear feedback and error handling
- Convenience aliases are automatically created by setup_container.sh