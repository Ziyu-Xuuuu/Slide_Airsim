using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Semantic LiDAR Odometry and Mapping processor
    /// Interfaces with the SlideSLAM SLOAM backend via ROS
    /// </summary>
    public class SLOAMProcessor : MonoBehaviour
    {
        [Header("SLOAM Configuration")]
        public float mapResolution = 0.1f;
        public float maxMapRange = 100.0f;
        public int historySize = 100;
        
        private string droneName;
        private ROSCommunicator rosCommunicator;
        private Queue<LiDARData> lidarHistory;
        private SLAMPose currentPose;
        private List<Vector3> currentMap;
        
        private bool isInitialized = false;
        
        public void Initialize(string robotName, ROSCommunicator rosComm)
        {
            droneName = robotName;
            rosCommunicator = rosComm;
            
            lidarHistory = new Queue<LiDARData>();
            currentMap = new List<Vector3>();
            
            currentPose = new SLAMPose
            {
                timestamp = Time.time,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                isValid = false,
                confidence = 0.0f
            };
            
            InitializeROSCommunication();
            isInitialized = true;
            
            Debug.Log($"SLOAM Processor initialized for {droneName}");
        }
        
        void InitializeROSCommunication()
        {
            if (rosCommunicator != null)
            {
                // Subscribe to SLOAM output topics
                rosCommunicator.SubscribeToOdometry($"/{droneName}/sloam/optimized_odom", OnSLOAMPoseReceived);
                rosCommunicator.SubscribeToPointCloud($"/{droneName}/sloam/map_points", OnMapPointsReceived);
                
                // Advertise input topics for SLOAM
                rosCommunicator.AdvertisePointCloud($"/{droneName}/velodyne_points");
                rosCommunicator.AdvertiseOdometry($"/{droneName}/sync_odom");
            }
        }
        
        public void ProcessLiDARData(LiDARData lidarData)
        {
            if (!isInitialized || lidarData == null) return;
            
            // Add to history
            lidarHistory.Enqueue(lidarData);
            while (lidarHistory.Count > historySize)
            {
                lidarHistory.Dequeue();
            }
            
            // Convert Unity LiDAR data to ROS PointCloud2 format
            var rosPointCloud = ConvertToROSPointCloud(lidarData);
            
            // Publish to SLOAM
            rosCommunicator.PublishPointCloud($"/{droneName}/velodyne_points", rosPointCloud);
            
            // Also publish odometry data for SLOAM initialization
            var rosOdometry = ConvertToROSOdometry(lidarData);
            rosCommunicator.PublishOdometry($"/{droneName}/sync_odom", rosOdometry);
            
            // Update local SLAM estimate (simplified)
            UpdateLocalSLAM(lidarData);
        }
        
        ROSPointCloud ConvertToROSPointCloud(LiDARData lidarData)
        {
            var rosCloud = new ROSPointCloud
            {
                header = new ROSHeader
                {
                    stamp = TimeToROSTime(lidarData.timestamp),
                    frame_id = $"{droneName}/base_link/lidar"
                },
                points = new List<ROSPoint3D>()
            };
            
            foreach (var point in lidarData.points)
            {
                // Convert Unity coordinates to ROS coordinates (if needed)
                var rosPoint = UnityToROSCoordinates(point, lidarData.position, lidarData.rotation);
                rosCloud.points.Add(new ROSPoint3D
                {
                    x = rosPoint.x,
                    y = rosPoint.y,
                    z = rosPoint.z
                });
            }
            
            return rosCloud;
        }
        
        ROSOdometry ConvertToROSOdometry(LiDARData lidarData)
        {
            var rosOdom = new ROSOdometry
            {
                header = new ROSHeader
                {
                    stamp = TimeToROSTime(lidarData.timestamp),
                    frame_id = $"{droneName}/odom"
                },
                child_frame_id = $"{droneName}/base_link",
                pose = new ROSPoseWithCovariance
                {
                    pose = new ROSPose
                    {
                        position = UnityToROSPosition(lidarData.position),
                        orientation = UnityToROSQuaternion(lidarData.rotation)
                    }
                }
            };
            
            return rosOdom;
        }
        
        void UpdateLocalSLAM(LiDARData lidarData)
        {
            // Simplified SLAM update for local tracking
            // In real implementation, this would use actual SLAM algorithms
            
            currentPose.timestamp = lidarData.timestamp;
            currentPose.position = lidarData.position;
            currentPose.rotation = lidarData.rotation;
            currentPose.isValid = true;
            currentPose.confidence = CalculateConfidence(lidarData);
            
            // Update local map with new points
            foreach (var point in lidarData.points)
            {
                if (Vector3.Distance(point, lidarData.position) <= maxMapRange)
                {
                    currentMap.Add(point);
                }
            }
            
            // Limit map size for performance
            while (currentMap.Count > 10000)
            {
                currentMap.RemoveAt(0);
            }
        }
        
        float CalculateConfidence(LiDARData lidarData)
        {
            // Simple confidence calculation based on point density
            float baseConfidence = 0.5f;
            float pointDensityFactor = Mathf.Min(lidarData.points.Count / 100.0f, 1.0f);
            return baseConfidence + (0.5f * pointDensityFactor);
        }
        
        void OnSLOAMPoseReceived(ROSOdometry odom)
        {
            // Update pose from SLOAM backend
            currentPose.timestamp = ROSTimeToUnity(odom.header.stamp);
            currentPose.position = ROSToUnityPosition(odom.pose.pose.position);
            currentPose.rotation = ROSToUnityQuaternion(odom.pose.pose.orientation);
            currentPose.isValid = true;
            currentPose.confidence = 0.9f; // High confidence from SLOAM
            
            Debug.Log($"SLOAM pose updated for {droneName}: {currentPose.position}");
        }
        
        void OnMapPointsReceived(ROSPointCloud pointCloud)
        {
            // Update global map from SLOAM
            currentMap.Clear();
            foreach (var point in pointCloud.points)
            {
                currentMap.Add(ROSToUnityPosition(new ROSPosition 
                { 
                    x = point.x, 
                    y = point.y, 
                    z = point.z 
                }));
            }
            
            Debug.Log($"Map updated for {droneName}: {currentMap.Count} points");
        }
        
        public SLAMPose GetCurrentPose()
        {
            return currentPose;
        }
        
        public List<Vector3> GetCurrentMap()
        {
            return new List<Vector3>(currentMap);
        }
        
        public Queue<LiDARData> GetLiDARHistory()
        {
            return lidarHistory;
        }
        
        // Coordinate conversion utilities
        Vector3 UnityToROSCoordinates(Vector3 unityPoint, Vector3 origin, Quaternion rotation)
        {
            // Transform point to local coordinate system
            Vector3 localPoint = Quaternion.Inverse(rotation) * (unityPoint - origin);
            
            // Convert Unity (left-handed) to ROS (right-handed) coordinates
            return new Vector3(localPoint.z, -localPoint.x, localPoint.y);
        }
        
        Vector3 ROSToUnityPosition(ROSPosition rosPos)
        {
            // Convert ROS (right-handed) to Unity (left-handed) coordinates
            return new Vector3(-rosPos.y, rosPos.z, rosPos.x);
        }
        
        Vector3 UnityToROSPosition(Vector3 unityPos)
        {
            // Convert Unity (left-handed) to ROS (right-handed) coordinates
            return new Vector3(unityPos.z, -unityPos.x, unityPos.y);
        }
        
        Quaternion ROSToUnityQuaternion(ROSQuaternion rosQuat)
        {
            // Convert ROS to Unity quaternion
            return new Quaternion(-rosQuat.y, rosQuat.z, rosQuat.x, rosQuat.w);
        }
        
        ROSQuaternion UnityToROSQuaternion(Quaternion unityQuat)
        {
            // Convert Unity to ROS quaternion
            return new ROSQuaternion
            {
                x = unityQuat.z,
                y = -unityQuat.x,
                z = unityQuat.y,
                w = unityQuat.w
            };
        }
        
        float TimeToROSTime(float unityTime)
        {
            return unityTime;
        }
        
        float ROSTimeToUnity(float rosTime)
        {
            return rosTime;
        }
    }
}