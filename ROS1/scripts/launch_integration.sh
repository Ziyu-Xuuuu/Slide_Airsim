#!/bin/bash
source /root/slideslam_ws/Source_Code/Resource-Aware-Coordination-AirSim/AirSim/ros/devel/setup.bash
roslaunch airsim_ros_pkgs airsim_slideslam_integration.launch num_drones:=3
