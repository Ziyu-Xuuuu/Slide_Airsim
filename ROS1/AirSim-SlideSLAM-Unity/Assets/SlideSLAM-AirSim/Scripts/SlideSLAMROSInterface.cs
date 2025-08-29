using System;
using System.Collections.Generic;
using UnityEngine;
using AirSimUnity;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// ROS interface that bridges Unity AirSim with SlideSLAM ROS nodes
    /// Uses the existing AirSim ROS bridge architecture and extends it for SlideSLAM
    /// </summary>
    public class SlideSLAMROSInterface : MonoBehaviour
    {
        [Header("ROS Configuration")]
        public string rosMasterURI = "http://localhost:11311";
        public string nodeNamespace = "";
        public bool enableDebugLogging = true;
        
        private string fullNodeName;
        private bool isConnected = false;
        private bool isRecording = false;
        
        // Topic names following the existing ROS integration pattern
        private Dictionary<string, string> topicNames;
        
        // Message buffers for ROS communication
        private Queue<ROSLiDARMessage> lidarMessageQueue;
        private Queue<ROSImageMessage> imageMessageQueue;
        private Queue<ROSOdometryMessage> odometryMessageQueue;
        
        // Callbacks for received messages
        public event Action<SLAMPoseEstimate> OnSLAMPoseReceived;
        public event Action<List<DetectedObject>> OnObjectDetectionsReceived;
        public event Action<PointCloudData> OnGlobalMapReceived;
        
        public void Initialize(string robotNamespace, string masterURI)
        {
            nodeNamespace = robotNamespace;
            rosMasterURI = masterURI;
            fullNodeName = $"{robotNamespace}_airsim_slideslam_bridge";
            
            InitializeTopicNames();
            InitializeMessageQueues();
            ConnectToROS();
            
            Debug.Log($"SlideSLAM ROS Interface initialized for {robotNamespace}");
        }
        
        void InitializeTopicNames()
        {
            // Follow the existing airsim_ros_pkgs topic naming convention
            topicNames = new Dictionary<string, string>
            {
                // Input topics (Unity -> ROS/SlideSLAM)
                ["lidar_input"] = $"/{nodeNamespace}/airsim_node/lidar/Lidar",
                ["camera_rgb"] = $"/{nodeNamespace}/airsim_node/camera_0/Scene",
                ["camera_info"] = $"/{nodeNamespace}/airsim_node/camera_0/Scene/camera_info",
                ["odom_input"] = $"/{nodeNamespace}/airsim_node/odom_local_ned",
                
                // SlideSLAM bridge topics (matching existing bridge)
                ["velodyne_points"] = $"/{nodeNamespace}/velodyne_points",
                ["sync_odom"] = $"/{nodeNamespace}/sync_odom",
                ["semantic_observation"] = $"/{nodeNamespace}/semantic_observation",
                ["camera_image_raw"] = $"/{nodeNamespace}/camera/image_raw",
                ["camera_camera_info"] = $"/{nodeNamespace}/camera/camera_info",
                
                // SlideSLAM output topics
                ["sloam_pose"] = $"/{nodeNamespace}/sloam/optimized_odom",
                ["sloam_map"] = $"/{nodeNamespace}/sloam/map_points",
                ["object_detections"] = $"/{nodeNamespace}/object_detections",
                ["slam_pose_output"] = $"/{nodeNamespace}/slam_pose",
                
                // Multi-robot coordination (existing system)
                ["coordination_pose"] = $"/{nodeNamespace}/slam_pose",
                ["neighbors"] = $"/{nodeNamespace}/neighbors",
                ["image_covering"] = $"/{nodeNamespace}/image_covering"
            };
        }
        
        void InitializeMessageQueues()
        {
            lidarMessageQueue = new Queue<ROSLiDARMessage>();
            imageMessageQueue = new Queue<ROSImageMessage>();
            odometryMessageQueue = new Queue<ROSOdometryMessage>();
        }
        
        void ConnectToROS()
        {
            try
            {
                // In a real implementation, this would initialize ROS# or rosbridge
                // For now, simulate the connection
                LogDebug($"Connecting to ROS Master at {rosMasterURI}");
                LogDebug($"Node name: {fullNodeName}");
                
                // Simulate successful connection
                isConnected = true;
                
                // Setup publishers and subscribers
                SetupROSCommunication();
                
                LogDebug("Successfully connected to ROS");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to connect to ROS: {e.Message}");
                isConnected = false;
            }
        }
        
        void SetupROSCommunication()
        {
            // Setup publishers (Unity -> ROS)
            SetupPublishers();
            
            // Setup subscribers (ROS -> Unity)
            SetupSubscribers();
            
            LogDebug("ROS communication channels established");
        }
        
        void SetupPublishers()
        {
            // Publishers for sensor data going to SlideSLAM
            LogDebug($"Advertising LiDAR topic: {topicNames["velodyne_points"]}");
            LogDebug($"Advertising camera topic: {topicNames["camera_image_raw"]}");
            LogDebug($"Advertising odometry topic: {topicNames["sync_odom"]}");
            LogDebug($"Advertising semantic observation topic: {topicNames["semantic_observation"]}");
            
            // In real implementation, these would be actual ROS publishers
            // Example: lidarPublisher = rosNode.CreatePublisher<PointCloud2>(topicNames["velodyne_points"]);
        }
        
        void SetupSubscribers()
        {
            // Subscribers for SlideSLAM outputs
            LogDebug($"Subscribing to SLAM pose: {topicNames["sloam_pose"]}");
            LogDebug($"Subscribing to global map: {topicNames["sloam_map"]}");
            LogDebug($"Subscribing to object detections: {topicNames["object_detections"]}");
            
            // In real implementation, these would be actual ROS subscribers with callbacks
            // Example: slamPoseSubscriber = rosNode.CreateSubscriber<Odometry>(topicNames["sloam_pose"], OnSLAMPoseMessageReceived);
        }
        
        // Publishing methods (Unity -> ROS/SlideSLAM)
        public void PublishLiDARData(LiDARPointCloud lidarData)
        {
            if (!isConnected) return;
            
            var rosMessage = ConvertLiDARToROSMessage(lidarData);
            lidarMessageQueue.Enqueue(rosMessage);
            
            LogDebug($"Publishing LiDAR data: {lidarData.points.Count} points");
            
            // In real implementation: lidarPublisher.Publish(rosMessage);
        }
        
        public void PublishCameraData(CameraImageData cameraData)
        {
            if (!isConnected) return;
            
            var rosMessage = ConvertCameraToROSMessage(cameraData);
            imageMessageQueue.Enqueue(rosMessage);
            
            LogDebug($"Publishing camera data: {cameraData.cameraName}");
            
            // In real implementation: imagePublisher.Publish(rosMessage);
        }
        
        public void PublishOdometry(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (!isConnected) return;
            
            var rosMessage = CreateOdometryMessage(position, rotation, velocity);
            odometryMessageQueue.Enqueue(rosMessage);
            
            LogDebug($"Publishing odometry: {position}");
            
            // In real implementation: odometryPublisher.Publish(rosMessage);
        }
        
        public void PublishSemanticObservation(List<DetectedObject> detections, SensorPose cameraPose)
        {
            if (!isConnected) return;
            
            var rosMessage = CreateSemanticObservationMessage(detections, cameraPose);
            
            LogDebug($"Publishing semantic observation: {detections.Count} detections");
            
            // In real implementation: semanticObservationPublisher.Publish(rosMessage);
        }
        
        // Message conversion methods (Unity to ROS format)
        ROSLiDARMessage ConvertLiDARToROSMessage(LiDARPointCloud lidarData)
        {
            return new ROSLiDARMessage
            {
                header = CreateROSHeader(lidarData.timestamp, $"{nodeNamespace}/base_link/lidar"),
                points = ConvertPointsToROSFormat(lidarData.points, lidarData.sensorPose)
            };
        }
        
        ROSImageMessage ConvertCameraToROSMessage(CameraImageData cameraData)
        {
            return new ROSImageMessage
            {
                header = CreateROSHeader(cameraData.timestamp, $"{nodeNamespace}/base_link/camera"),
                width = (uint)cameraData.width,
                height = (uint)cameraData.height,
                encoding = "rgb8",
                data = cameraData.imageData.ToArray()
            };
        }
        
        ROSOdometryMessage CreateOdometryMessage(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            return new ROSOdometryMessage
            {
                header = CreateROSHeader(Time.time, $"{nodeNamespace}/odom"),
                child_frame_id = $"{nodeNamespace}/base_link",
                pose = new ROSPoseWithCovariance
                {
                    pose = new ROSPose
                    {
                        position = UnityToROSPosition(position),
                        orientation = UnityToROSQuaternion(rotation)
                    },
                    covariance = CreateDefaultCovariance()
                },
                twist = new ROSTwistWithCovariance
                {
                    twist = new ROSTwist
                    {
                        linear = UnityToROSPosition(velocity),
                        angular = UnityToROSPosition(Vector3.zero)
                    },
                    covariance = CreateDefaultCovariance()
                }
            };
        }
        
        ROSSemanticObservationMessage CreateSemanticObservationMessage(List<DetectedObject> detections, SensorPose cameraPose)
        {
            var rosDetections = new List<ROSDetection>();
            foreach (var detection in detections)
            {
                rosDetections.Add(new ROSDetection
                {
                    class_name = detection.className,
                    confidence = detection.confidence,
                    position = UnityToROSPosition(detection.worldPosition),
                    size = UnityToROSPosition(detection.size)
                });
            }
            
            return new ROSSemanticObservationMessage
            {
                header = CreateROSHeader(Time.time, $"{nodeNamespace}/base_link/camera"),
                robot_pose = new ROSPose
                {
                    position = UnityToROSPosition(cameraPose.position),
                    orientation = UnityToROSQuaternion(cameraPose.rotation)
                },
                detections = rosDetections
            };
        }
        
        // Callback methods for received ROS messages
        void OnSLAMPoseMessageReceived(ROSOdometryMessage odomMsg)
        {
            var slamPose = new SLAMPoseEstimate
            {
                timestamp = ROSTimeToUnity(odomMsg.header.stamp),
                position = ROSToUnityPosition(odomMsg.pose.pose.position),
                rotation = ROSToUnityQuaternion(odomMsg.pose.pose.orientation),
                isValid = true,
                confidence = 0.9f // High confidence from SLOAM
            };
            
            OnSLAMPoseReceived?.Invoke(slamPose);
            LogDebug($"Received SLAM pose: {slamPose.position}");
        }
        
        void OnObjectDetectionsMessageReceived(ROSDetectionArrayMessage detectionMsg)
        {
            var detections = new List<DetectedObject>();
            foreach (var rosDetection in detectionMsg.detections)
            {
                detections.Add(new DetectedObject
                {
                    timestamp = ROSTimeToUnity(detectionMsg.header.stamp),
                    className = rosDetection.class_name,
                    confidence = rosDetection.confidence,
                    worldPosition = ROSToUnityPosition(rosDetection.position),
                    size = ROSToUnityPosition(rosDetection.size)
                });
            }
            
            OnObjectDetectionsReceived?.Invoke(detections);
            LogDebug($"Received {detections.Count} object detections");
        }
        
        void OnGlobalMapMessageReceived(ROSPointCloudMessage mapMsg)
        {
            var pointCloudData = new PointCloudData
            {
                timestamp = ROSTimeToUnity(mapMsg.header.stamp),
                points = new List<Vector3>()
            };
            
            foreach (var point in mapMsg.points)
            {
                pointCloudData.points.Add(ROSToUnityPosition(new ROSPosition 
                { 
                    x = point.x, 
                    y = point.y, 
                    z = point.z 
                }));
            }
            
            OnGlobalMapReceived?.Invoke(pointCloudData);
            LogDebug($"Received global map: {pointCloudData.points.Count} points");
        }
        
        // Utility methods
        ROSHeader CreateROSHeader(float timestamp, string frameId)
        {
            return new ROSHeader
            {
                stamp = timestamp,
                frame_id = frameId
            };
        }
        
        List<ROSPoint3D> ConvertPointsToROSFormat(List<Vector3> points, SensorPose sensorPose)
        {
            var rosPoints = new List<ROSPoint3D>();
            foreach (var point in points)
            {
                // Transform point to sensor local coordinates
                Vector3 localPoint = Quaternion.Inverse(sensorPose.rotation) * (point - sensorPose.position);
                Vector3 rosPoint = UnityToROSPosition(localPoint);
                
                rosPoints.Add(new ROSPoint3D
                {
                    x = rosPoint.x,
                    y = rosPoint.y,
                    z = rosPoint.z,
                    intensity = 1.0f
                });
            }
            return rosPoints;
        }
        
        Vector3 UnityToROSPosition(Vector3 unityPos)
        {
            // Convert Unity (left-handed) to ROS (right-handed) coordinates
            return new Vector3(unityPos.z, -unityPos.x, unityPos.y);
        }
        
        Vector3 ROSToUnityPosition(ROSPosition rosPos)
        {
            // Convert ROS (right-handed) to Unity (left-handed) coordinates
            return new Vector3(-rosPos.y, rosPos.z, rosPos.x);
        }
        
        Quaternion UnityToROSQuaternion(Quaternion unityQuat)
        {
            // Convert Unity to ROS quaternion
            return new Quaternion(unityQuat.z, -unityQuat.x, unityQuat.y, unityQuat.w);
        }
        
        Quaternion ROSToUnityQuaternion(ROSQuaternion rosQuat)
        {
            // Convert ROS to Unity quaternion
            return new Quaternion(-rosQuat.y, rosQuat.z, rosQuat.x, rosQuat.w);
        }
        
        float[] CreateDefaultCovariance()
        {
            float[] covariance = new float[36];
            for (int i = 0; i < 6; i++)
            {
                covariance[i * 6 + i] = 0.1f; // Small diagonal values
            }
            return covariance;
        }
        
        float ROSTimeToUnity(float rosTime)
        {
            return rosTime;
        }
        
        void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[SlideSLAM-ROS] {message}");
            }
        }
        
        // Public interface
        public bool IsConnected()
        {
            return isConnected;
        }
        
        public void SetRecordingMode(bool recording)
        {
            isRecording = recording;
            LogDebug($"Recording mode: {(recording ? "ON" : "OFF")}");
        }
        
        public string GetNodeNamespace()
        {
            return nodeNamespace;
        }
        
        public Dictionary<string, string> GetTopicNames()
        {
            return new Dictionary<string, string>(topicNames);
        }
        
        public void Shutdown()
        {
            if (isConnected)
            {
                LogDebug("Shutting down ROS interface...");
                
                // Clean up ROS connections
                isConnected = false;
                
                // Clear message queues
                lidarMessageQueue?.Clear();
                imageMessageQueue?.Clear();
                odometryMessageQueue?.Clear();
                
                LogDebug("ROS interface shutdown complete");
            }
        }
        
        void OnDestroy()
        {
            Shutdown();
        }
    }
    
    // Additional data structures for ROS communication
    [Serializable]
    public class PointCloudData
    {
        public float timestamp;
        public List<Vector3> points;
    }
    
    // ROS message structures (matching the existing bridge format)
    [Serializable]
    public class ROSLiDARMessage
    {
        public ROSHeader header;
        public List<ROSPoint3D> points;
    }
    
    [Serializable]
    public class ROSImageMessage
    {
        public ROSHeader header;
        public uint width;
        public uint height;
        public string encoding;
        public byte[] data;
    }
    
    [Serializable]
    public class ROSOdometryMessage
    {
        public ROSHeader header;
        public string child_frame_id;
        public ROSPoseWithCovariance pose;
        public ROSTwistWithCovariance twist;
    }
    
    [Serializable]
    public class ROSSemanticObservationMessage
    {
        public ROSHeader header;
        public ROSPose robot_pose;
        public List<ROSDetection> detections;
    }
    
    [Serializable]
    public class ROSDetectionArrayMessage
    {
        public ROSHeader header;
        public List<ROSDetection> detections;
    }
    
    [Serializable]
    public class ROSPointCloudMessage
    {
        public ROSHeader header;
        public List<ROSPoint3D> points;
    }
}