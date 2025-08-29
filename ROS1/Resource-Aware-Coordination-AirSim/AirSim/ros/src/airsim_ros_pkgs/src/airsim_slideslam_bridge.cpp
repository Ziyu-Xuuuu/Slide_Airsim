#include <ros/ros.h>
#include <sensor_msgs/PointCloud2.h>
#include <sensor_msgs/Image.h>
#include <sensor_msgs/CameraInfo.h>
#include <nav_msgs/Odometry.h>
#include <geometry_msgs/PoseWithCovarianceStamped.h>
#include <tf2_ros/transform_broadcaster.h>
#include <tf2_ros/transform_listener.h>
#include <tf2_geometry_msgs/tf2_geometry_msgs.h>
#include <tf/tf.h>

#include "sloam_msgs/ROSSyncOdom.h"
#include "sloam_msgs/ROSObservation.h"

class AirSimSlideSLAMBridge {
private:
    ros::NodeHandle nh_;
    ros::NodeHandle nh_private_;
    
    // Subscribers for AirSim data
    ros::Subscriber airsim_odom_sub_;
    ros::Subscriber airsim_lidar_sub_;
    ros::Subscriber airsim_camera_sub_;
    ros::Subscriber airsim_camera_info_sub_;
    
    // Publishers for SlideSLAM data
    ros::Publisher sloam_odom_pub_;
    ros::Publisher sloam_pointcloud_pub_;
    ros::Publisher sloam_observation_pub_;
    
    // Subscribers for SlideSLAM output
    ros::Subscriber sloam_pose_sub_;
    
    // Publishers for coordination system
    ros::Publisher coord_pose_pub_;
    
    // Camera forwarding publishers
    ros::Publisher camera_pub_;
    ros::Publisher camera_info_pub_;
    
    // TF
    tf2_ros::TransformBroadcaster tf_broadcaster_;
    tf2_ros::Buffer tf_buffer_;
    tf2_ros::TransformListener tf_listener_;
    
    // Parameters
    std::string robot_name_;
    std::string base_frame_;
    std::string odom_frame_;
    std::string map_frame_;
    
public:
    AirSimSlideSLAMBridge() : nh_(), nh_private_("~"), tf_listener_(tf_buffer_) {
        // Get parameters
        nh_private_.param<std::string>("robot_name", robot_name_, "drone_0");
        nh_private_.param<std::string>("base_frame", base_frame_, robot_name_ + "/base_link");
        nh_private_.param<std::string>("odom_frame", odom_frame_, robot_name_ + "/odom");
        nh_private_.param<std::string>("map_frame", map_frame_, "map");
        
        // Initialize subscribers for AirSim data
        airsim_odom_sub_ = nh_.subscribe("/" + robot_name_ + "/airsim_node/odom_local_ned", 10, 
                                        &AirSimSlideSLAMBridge::airsimOdomCallback, this);
        airsim_lidar_sub_ = nh_.subscribe("/" + robot_name_ + "/airsim_node/lidar/Lidar", 10,
                                         &AirSimSlideSLAMBridge::airsimLidarCallback, this);
        airsim_camera_sub_ = nh_.subscribe("/" + robot_name_ + "/airsim_node/camera_0/Scene", 10,
                                          &AirSimSlideSLAMBridge::airsimCameraCallback, this);
        airsim_camera_info_sub_ = nh_.subscribe("/" + robot_name_ + "/airsim_node/camera_0/Scene/camera_info", 10,
                                               &AirSimSlideSLAMBridge::airsimCameraInfoCallback, this);
        
        // Initialize publishers for SlideSLAM
        sloam_odom_pub_ = nh_.advertise<sloam_msgs::ROSSyncOdom>("/" + robot_name_ + "/sync_odom", 10);
        sloam_pointcloud_pub_ = nh_.advertise<sensor_msgs::PointCloud2>("/" + robot_name_ + "/velodyne_points", 10);
        sloam_observation_pub_ = nh_.advertise<sloam_msgs::ROSObservation>("/" + robot_name_ + "/semantic_observation", 10);
        
        // Subscribe to SlideSLAM output
        sloam_pose_sub_ = nh_.subscribe("/" + robot_name_ + "/sloam/optimized_odom", 10,
                                       &AirSimSlideSLAMBridge::sloamPoseCallback, this);
        
        // Publisher for coordination system
        coord_pose_pub_ = nh_.advertise<nav_msgs::Odometry>("/" + robot_name_ + "/slam_pose", 10);
        
        // Camera forwarding publishers
        camera_pub_ = nh_.advertise<sensor_msgs::Image>("/" + robot_name_ + "/camera/image_raw", 10);
        camera_info_pub_ = nh_.advertise<sensor_msgs::CameraInfo>("/" + robot_name_ + "/camera/camera_info", 10);
        
        ROS_INFO("AirSim-SlideSLAM Bridge initialized for robot: %s", robot_name_.c_str());
    }
    
    void airsimOdomCallback(const nav_msgs::Odometry::ConstPtr& msg) {
        // Convert AirSim odometry to SlideSLAM format
        sloam_msgs::ROSSyncOdom sloam_odom;
        sloam_odom.header = msg->header;
        sloam_odom.header.frame_id = base_frame_;
        
        // Copy pose and twist data
        // Map to the ROSSyncOdom structure
        sloam_odom.vio_odom = *msg;
        sloam_odom.sloam_odom = *msg;
        
        sloam_odom_pub_.publish(sloam_odom);
    }
    
    void airsimLidarCallback(const sensor_msgs::PointCloud2::ConstPtr& msg) {
        // Forward LiDAR data to SlideSLAM with proper frame
        sensor_msgs::PointCloud2 sloam_pc = *msg;
        sloam_pc.header.frame_id = base_frame_ + "/lidar";
        
        sloam_pointcloud_pub_.publish(sloam_pc);
    }
    
    void airsimCameraCallback(const sensor_msgs::Image::ConstPtr& msg) {
        // Forward camera data to object detection pipeline
        sensor_msgs::Image camera_msg = *msg;
        camera_msg.header.frame_id = base_frame_ + "/camera";
        
        camera_pub_.publish(camera_msg);
        
        ROS_DEBUG_THROTTLE(1.0, "Forwarded camera image for %s", robot_name_.c_str());
    }
    
    void airsimCameraInfoCallback(const sensor_msgs::CameraInfo::ConstPtr& msg) {
        // Forward camera calibration info to object detection pipeline
        sensor_msgs::CameraInfo camera_info = *msg;
        camera_info.header.frame_id = base_frame_ + "/camera";
        
        camera_info_pub_.publish(camera_info);
        
        ROS_DEBUG_THROTTLE(5.0, "Forwarded camera info for %s", robot_name_.c_str());
    }
    
    void sloamPoseCallback(const nav_msgs::Odometry::ConstPtr& msg) {
        // Receive optimized pose from SlideSLAM and forward to coordination system
        nav_msgs::Odometry coord_pose = *msg;
        coord_pose.header.frame_id = map_frame_;
        coord_pose.child_frame_id = base_frame_;
        
        coord_pose_pub_.publish(coord_pose);
        
        // Broadcast transform
        geometry_msgs::TransformStamped transform;
        transform.header = msg->header;
        transform.header.frame_id = map_frame_;
        transform.child_frame_id = base_frame_;
        transform.transform.translation.x = msg->pose.pose.position.x;
        transform.transform.translation.y = msg->pose.pose.position.y;
        transform.transform.translation.z = msg->pose.pose.position.z;
        transform.transform.rotation = msg->pose.pose.orientation;
        
        tf_broadcaster_.sendTransform(transform);
        
        ROS_DEBUG("Published SLAM pose for %s", robot_name_.c_str());
    }
};

int main(int argc, char** argv) {
    ros::init(argc, argv, "airsim_slideslam_bridge");
    
    AirSimSlideSLAMBridge bridge;
    
    ros::spin();
    
    return 0;
}