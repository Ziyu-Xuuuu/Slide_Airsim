using System;
using System.Collections.Generic;
using UnityEngine;
using AirSimUnity;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Extension component for existing AirSim Vehicle to add SlideSLAM perception
    /// Attaches to existing Drone/Car components and extends them with SlideSLAM capabilities
    /// </summary>
    [RequireComponent(typeof(Vehicle))]
    public class SlideSLAMVehicleExtension : MonoBehaviour
    {
        [Header("SlideSLAM Configuration")]
        public bool enableSLOAM = true;
        public bool enableObjectDetection = true;
        public bool enableSemanticMapping = true;
        public bool replaceDefaultPerception = true;
        
        [Header("ROS Configuration")]
        public string robotNamespace = "";
        public string rosMasterURI = "http://localhost:11311";
        
        [Header("Sensor Configuration")]
        public Transform lidarMount;
        public float lidarRange = 100.0f;
        public int lidarRayCount = 32;
        public LayerMask lidarLayerMask = -1;
        
        // References to existing AirSim components
        private Vehicle airsimVehicle;
        private List<DataCaptureScript> captureCameras;
        
        // SlideSLAM components
        private SlideSLAMROSInterface rosInterface;
        private ROSCommunicator rosCommunicator;
        private SLOAMProcessor sloamProcessor;
        private ObjectDetectionProcessor objectDetector;
        private SemanticMappingProcessor semanticMapper;
        
        // Data storage
        private LiDARPointCloud currentLidarPointCloud;
        private SLAMPoseEstimate currentSLAMPose;
        private List<DetectedObject> currentDetections;
        
        private bool isInitialized = false;
        private string vehicleName;
        
        void Start()
        {
            InitializeSlideSLAMExtension();
        }
        
        void InitializeSlideSLAMExtension()
        {
            // Get existing AirSim vehicle component
            airsimVehicle = GetComponent<Vehicle>();
            if (airsimVehicle == null)
            {
                Debug.LogError("SlideSLAMVehicleExtension requires an AirSim Vehicle component");
                return;
            }
            
            // Get vehicle name and set namespace
            vehicleName = gameObject.name;
            if (string.IsNullOrEmpty(robotNamespace))
            {
                robotNamespace = vehicleName.ToLower();
            }
            
            // Get existing capture cameras from AirSim
            captureCameras = GetExistingCaptureCameras();
            
            // Initialize ROS interface
            InitializeROSInterface();
            
            // Initialize SlideSLAM processors
            InitializeSlideSLAMProcessors();
            
            // Setup LiDAR mount if not assigned
            SetupLiDARMount();
            
            isInitialized = true;
            Debug.Log($"SlideSLAM extension initialized for {vehicleName}");
        }
        
        List<DataCaptureScript> GetExistingCaptureCameras()
        {
            // Find capture cameras from existing AirSim setup
            GameObject camerasParent = GameObject.FindGameObjectWithTag("CaptureCameras");
            List<DataCaptureScript> cameras = new List<DataCaptureScript>();
            
            if (camerasParent != null)
            {
                DataCaptureScript[] allCameras = camerasParent.GetComponentsInChildren<DataCaptureScript>();
                foreach (var camera in allCameras)
                {
                    // Check if camera belongs to this vehicle
                    if (camera.transform.IsChildOf(transform))
                    {
                        cameras.Add(camera);
                    }
                }
            }
            
            return cameras;
        }
        
        void InitializeROSInterface()
        {
            rosInterface = gameObject.AddComponent<SlideSLAMROSInterface>();
            rosInterface.Initialize(robotNamespace, rosMasterURI);
            
            rosCommunicator = gameObject.AddComponent<ROSCommunicator>();
            rosCommunicator.Initialize(rosMasterURI, $"{robotNamespace}_unity_node");
        }
        
        void InitializeSlideSLAMProcessors()
        {
            if (enableSLOAM)
            {
                sloamProcessor = gameObject.AddComponent<SLOAMProcessor>();
                sloamProcessor.Initialize(robotNamespace, rosCommunicator);
            }
            
            if (enableObjectDetection)
            {
                objectDetector = gameObject.AddComponent<ObjectDetectionProcessor>();
                objectDetector.Initialize(robotNamespace, rosCommunicator);
            }
            
            if (enableSemanticMapping)
            {
                semanticMapper = gameObject.AddComponent<SemanticMappingProcessor>();
                semanticMapper.Initialize(robotNamespace, rosInterface);
            }
        }
        
        void SetupLiDARMount()
        {
            if (lidarMount == null)
            {
                GameObject lidarObj = new GameObject("LiDAR_Mount");
                lidarObj.transform.SetParent(transform);
                lidarObj.transform.localPosition = Vector3.up * 0.1f;
                lidarObj.transform.localRotation = Quaternion.identity;
                lidarMount = lidarObj.transform;
            }
        }
        
        void Update()
        {
            if (!isInitialized) return;
            
            // Process SlideSLAM perception
            ProcessSlideSLAMPerception();
            
            // Override AirSim perception if enabled
            if (replaceDefaultPerception)
            {
                OverrideAirSimPerception();
            }
        }
        
        void ProcessSlideSLAMPerception()
        {
            // Capture LiDAR data
            if (enableSLOAM && sloamProcessor != null)
            {
                var lidarPointCloud = CaptureLiDARData();
                var lidarData = ConvertToLiDARData(lidarPointCloud);
                sloamProcessor.ProcessLiDARData(lidarData);
                var currentPose = sloamProcessor.GetCurrentPose();
                currentSLAMPose = ConvertToSLAMPoseEstimate(currentPose);
            }
            
            // Process camera data for object detection
            if (enableObjectDetection && objectDetector != null && captureCameras.Count > 0)
            {
                foreach (var camera in captureCameras)
                {
                    var cameraImageData = ExtractCameraData(camera);
                    var cameraData = ConvertToCameraData(cameraImageData);
                    objectDetector.ProcessCameraData(cameraData);
                }
                var detections = objectDetector.GetLatestDetections();
                currentDetections = ConvertToDetectedObjects(detections);
            }
            
            // Update semantic mapping
            if (enableSemanticMapping && semanticMapper != null)
            {
                semanticMapper.UpdateMap(currentLidarPointCloud, currentDetections);
            }
        }
        
        LiDARPointCloud CaptureLiDARData()
        {
            var pointCloud = new LiDARPointCloud
            {
                timestamp = Time.time,
                sensorPose = new SensorPose
                {
                    position = lidarMount.position,
                    rotation = lidarMount.rotation
                },
                points = new List<Vector3>()
            };
            
            // Perform raycast-based LiDAR simulation
            float angleStep = 360.0f / lidarRayCount;
            
            for (int i = 0; i < lidarRayCount; i++)
            {
                float angle = i * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                direction = lidarMount.rotation * direction;
                
                RaycastHit hit;
                if (Physics.Raycast(lidarMount.position, direction, out hit, lidarRange, lidarLayerMask))
                {
                    pointCloud.points.Add(hit.point);
                }
            }
            
            return pointCloud;
        }
        
        CameraImageData ExtractCameraData(DataCaptureScript camera)
        {
            // Create image request for RGB data using AirSim ImageRequest struct
            var imageRequest = new ImageRequest(camera.GetCameraName(), ImageType.Scene, false, false);
            
            // Get image from existing AirSim camera
            var imageResponse = camera.GetImageBasedOnRequest(imageRequest);
            
            return new CameraImageData
            {
                timestamp = Time.time,
                cameraName = camera.GetCameraName(),
                pose = new SensorPose
                {
                    position = camera.transform.position,
                    rotation = camera.transform.rotation
                },
                imageData = new List<byte>(imageResponse.image_data_uint8),
                width = imageResponse.width,
                height = imageResponse.height
            };
        }
        
        void OverrideAirSimPerception()
        {
            // Override vehicle pose with SLAM estimate if available
            if (currentSLAMPose != null && currentSLAMPose.isValid)
            {
                // Apply SLAM pose correction to vehicle
                // Note: This modifies the vehicle's perception of its own pose
                var correctedPose = new AirSimPose(
                    DataManager.ToAirSimVector(currentSLAMPose.position),
                    DataManager.ToAirSimQuaternion(currentSLAMPose.rotation)
                );
                
                // You could override the vehicle's pose here if needed
                // airsimVehicle.SetPose(correctedPose, false);
            }
        }
        
        // Public API for accessing SlideSLAM data
        public SLAMPoseEstimate GetCurrentSLAMPose()
        {
            return currentSLAMPose;
        }
        
        public LiDARPointCloud GetCurrentLiDARData()
        {
            return currentLidarPointCloud;
        }
        
        public List<DetectedObject> GetCurrentDetections()
        {
            return currentDetections ?? new List<DetectedObject>();
        }
        
        public bool IsSlideSLAMActive()
        {
            return isInitialized && rosInterface != null && rosInterface.IsConnected() && 
                   rosCommunicator != null && rosCommunicator.IsConnected();
        }
        
        // Integration with existing AirSim recording system
        public void OnRecordingToggle(bool isRecording)
        {
            if (rosInterface != null)
            {
                rosInterface.SetRecordingMode(isRecording);
            }
        }
        
        void OnDestroy()
        {
            if (rosInterface != null)
            {
                rosInterface.Shutdown();
            }
            if (rosCommunicator != null)
            {
                rosCommunicator.Shutdown();
            }
        }
        
        // Conversion methods between data structures
        private LiDARData ConvertToLiDARData(LiDARPointCloud pointCloud)
        {
            return new LiDARData
            {
                timestamp = pointCloud.timestamp,
                position = pointCloud.sensorPose.position,
                rotation = pointCloud.sensorPose.rotation,
                points = pointCloud.points,
                range = lidarRange,
                rayCount = lidarRayCount
            };
        }
        
        private SLAMPoseEstimate ConvertToSLAMPoseEstimate(SLAMPose pose)
        {
            return new SLAMPoseEstimate
            {
                timestamp = pose.timestamp,
                position = pose.position,
                rotation = pose.rotation,
                isValid = pose.isValid,
                confidence = pose.confidence,
                covariance = Matrix4x4.identity
            };
        }
        
        private CameraData ConvertToCameraData(CameraImageData imageData)
        {
            // Convert image data to RenderTexture
            RenderTexture renderTexture = new RenderTexture(imageData.width, imageData.height, 24);
            // In a real implementation, you would create the texture from the image bytes
            
            return new CameraData
            {
                timestamp = imageData.timestamp,
                cameraName = imageData.cameraName,
                position = imageData.pose.position,
                rotation = imageData.pose.rotation,
                rgbTexture = renderTexture,
                width = imageData.width,
                height = imageData.height
            };
        }
        
        private List<DetectedObject> ConvertToDetectedObjects(List<ObjectDetection> detections)
        {
            var convertedObjects = new List<DetectedObject>();
            
            foreach (var detection in detections)
            {
                convertedObjects.Add(new DetectedObject
                {
                    timestamp = detection.timestamp,
                    className = detection.className,
                    confidence = detection.confidence,
                    worldPosition = detection.worldPosition,
                    size = Vector3.one, // Default size
                    boundingBox = new Bounds(detection.worldPosition, Vector3.one)
                });
            }
            
            return convertedObjects;
        }
    }
    
    // Data structures specific to SlideSLAM integration
    [Serializable]
    public class LiDARPointCloud
    {
        public float timestamp;
        public SensorPose sensorPose;
        public List<Vector3> points;
    }
    
    [Serializable]
    public class CameraImageData
    {
        public float timestamp;
        public string cameraName;
        public SensorPose pose;
        public List<byte> imageData;
        public int width;
        public int height;
    }
    
    [Serializable]
    public class SensorPose
    {
        public Vector3 position;
        public Quaternion rotation;
    }
    
    [Serializable]
    public class SLAMPoseEstimate
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public bool isValid;
        public float confidence;
        public Matrix4x4 covariance;
    }
    
    [Serializable]
    public class DetectedObject
    {
        public float timestamp;
        public string className;
        public float confidence;
        public Vector3 worldPosition;
        public Vector3 size;
        public Bounds boundingBox;
    }
}