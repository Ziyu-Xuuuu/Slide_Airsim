using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Simplified drone controller that works with existing Unity components
    /// Provides basic drone functionality without requiring AirSim dependencies
    /// </summary>
    public class SlideSLAMDroneController : MonoBehaviour
    {
        [Header("Drone Configuration")]
        public string droneName = "drone_0";
        public Transform lidarMount;
        public Transform cameraMount;
        
        [Header("Sensor Configuration")]
        public float lidarRange = 100.0f;
        public float lidarResolution = 0.1f;
        public int lidarRayCount = 32;
        public LayerMask lidarLayerMask = -1;
        
        [Header("Camera Configuration")]
        public Camera frontCamera;
        public Camera depthCamera;
        public int imageWidth = 640;
        public int imageHeight = 480;
        
        private SlideSLAMPerceptionManager manager;
        private SLOAMProcessor sloamProcessor;
        private ObjectDetectionProcessor objectDetector;
        private SemanticMappingProcessor semanticMapper;
        
        // Perception data
        private LiDARPointCloud currentLidarData;
        private CameraImageData currentCameraData;
        private SLAMPoseEstimate currentSLAMPose;
        
        // ROS communication
        private SlideSLAMROSInterface rosCommunicator;
        
        public bool IsInitialized { get; private set; }
        
        public void Initialize(string name, SlideSLAMPerceptionManager slamManager)
        {
            droneName = name;
            manager = slamManager;
            
            // Initialize ROS communication
            rosCommunicator = gameObject.AddComponent<SlideSLAMROSInterface>();
            rosCommunicator.Initialize(droneName, "http://localhost:11311");
            
            // Initialize perception components
            InitializePerceptionComponents();
            
            // Setup sensor mounts if not assigned
            SetupSensorMounts();
            
            IsInitialized = true;
            Debug.Log($"SlideSLAM Drone Controller initialized for {droneName}");
        }
        
        void InitializePerceptionComponents()
        {
            // Initialize SLOAM processor
            sloamProcessor = gameObject.AddComponent<SLOAMProcessor>();
            sloamProcessor.Initialize(droneName, rosCommunicator);
            
            // Initialize object detection
            objectDetector = gameObject.AddComponent<ObjectDetectionProcessor>();
            objectDetector.Initialize(droneName, rosCommunicator);
            
            // Initialize semantic mapping
            semanticMapper = gameObject.AddComponent<SemanticMappingProcessor>();
            semanticMapper.Initialize(droneName, rosCommunicator);
        }
        
        void SetupSensorMounts()
        {
            // Create LiDAR mount if not assigned
            if (lidarMount == null)
            {
                GameObject lidarObj = new GameObject("LiDAR_Mount");
                lidarObj.transform.SetParent(transform);
                lidarObj.transform.localPosition = Vector3.up * 0.1f; // Slightly above center
                lidarMount = lidarObj.transform;
            }
            
            // Create camera mount if not assigned
            if (cameraMount == null)
            {
                GameObject cameraObj = new GameObject("Camera_Mount");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = Vector3.forward * 0.2f; // In front of drone
                cameraMount = cameraObj.transform;
            }
            
            // Setup cameras if not assigned
            if (frontCamera == null)
            {
                frontCamera = CreateCamera("FrontCamera", false);
            }
            
            if (depthCamera == null)
            {
                depthCamera = CreateCamera("DepthCamera", true);
            }
        }
        
        Camera CreateCamera(string name, bool isDepth)
        {
            GameObject camObj = new GameObject(name);
            camObj.transform.SetParent(cameraMount);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            
            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 60.0f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000.0f;
            
            if (isDepth)
            {
                // Configure for depth rendering
                cam.depthTextureMode = DepthTextureMode.Depth;
            }
            
            return cam;
        }
        
        public void UpdatePerception()
        {
            if (!IsInitialized) return;
            
            // Capture LiDAR data
            CaptureLiDARData();
            
            // Capture camera data
            CaptureCameraData();
            
            // Process SLAM
            ProcessSLAM();
            
            // Update object detection
            ProcessObjectDetection();
            
            // Update semantic mapping
            ProcessSemanticMapping();
        }
        
        void CaptureLiDARData()
        {
            currentLidarData = new LiDARPointCloud
            {
                timestamp = Time.time,
                sensorPose = new SensorPose
                {
                    position = lidarMount.position,
                    rotation = lidarMount.rotation
                },
                points = new List<Vector3>()
            };
            
            // Simulate LiDAR scanning with raycasting
            float angleStep = 360.0f / lidarRayCount;
            for (int i = 0; i < lidarRayCount; i++)
            {
                float angle = i * angleStep;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                direction = lidarMount.rotation * direction;
                
                RaycastHit hit;
                if (Physics.Raycast(lidarMount.position, direction, out hit, lidarRange, lidarLayerMask))
                {
                    currentLidarData.points.Add(hit.point);
                }
            }
            
            // Send to SLOAM processor
            if (sloamProcessor != null)
            {
                sloamProcessor.ProcessLiDARData(currentLidarData);
            }
        }
        
        void CaptureCameraData()
        {
            if (frontCamera == null) return;
            
            // Capture RGB image
            RenderTexture rgbTexture = RenderTexture.GetTemporary(imageWidth, imageHeight, 24);
            frontCamera.targetTexture = rgbTexture;
            frontCamera.Render();
            
            // Capture depth image
            RenderTexture depthTexture = null;
            if (depthCamera != null)
            {
                depthTexture = RenderTexture.GetTemporary(imageWidth, imageHeight, 24, RenderTextureFormat.RFloat);
                depthCamera.targetTexture = depthTexture;
                depthCamera.Render();
            }
            
            currentCameraData = new CameraImageData
            {
                timestamp = Time.time,
                cameraName = frontCamera.name,
                pose = new SensorPose
                {
                    position = cameraMount.position,
                    rotation = cameraMount.rotation
                },
                imageData = ExtractImageData(rgbTexture),
                width = imageWidth,
                height = imageHeight
            };
            
            // Send to object detector
            if (objectDetector != null)
            {
                objectDetector.ProcessCameraData(currentCameraData);
            }
            
            // Cleanup
            RenderTexture.ReleaseTemporary(rgbTexture);
            if (depthTexture != null)
                RenderTexture.ReleaseTemporary(depthTexture);
        }
        
        List<byte> ExtractImageData(RenderTexture renderTexture)
        {
            RenderTexture.active = renderTexture;
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            
            byte[] data = tex.GetRawTextureData();
            List<byte> result = new List<byte>(data);
            
            Destroy(tex);
            RenderTexture.active = null;
            
            return result;
        }
        
        void ProcessSLAM()
        {
            if (currentLidarData != null && sloamProcessor != null)
            {
                currentSLAMPose = sloamProcessor.GetCurrentPose();
                
                // Update drone transform based on SLAM estimate
                if (currentSLAMPose != null && currentSLAMPose.isValid)
                {
                    // Optional: Apply SLAM correction to drone position
                    // This would replace default odometry
                    // transform.position = currentSLAMPose.position;
                    // transform.rotation = currentSLAMPose.rotation;
                }
            }
        }
        
        void ProcessObjectDetection()
        {
            if (currentCameraData != null && objectDetector != null)
            {
                var detections = objectDetector.GetLatestDetections();
                // Process object detections for coordination
            }
        }
        
        void ProcessSemanticMapping()
        {
            if (currentLidarData != null && currentCameraData != null && semanticMapper != null)
            {
                var detections = objectDetector?.GetLatestDetections() ?? new List<DetectedObject>();
                semanticMapper.UpdateMap(currentLidarData, detections);
            }
        }
        
        public SLAMPoseEstimate GetCurrentSLAMPose()
        {
            return currentSLAMPose;
        }
        
        public LiDARPointCloud GetCurrentLiDARData()
        {
            return currentLidarData;
        }
        
        public CameraImageData GetCurrentCameraData()
        {
            return currentCameraData;
        }
        
        public List<DetectedObject> GetCurrentDetections()
        {
            return objectDetector?.GetLatestDetections() ?? new List<DetectedObject>();
        }
    }
}