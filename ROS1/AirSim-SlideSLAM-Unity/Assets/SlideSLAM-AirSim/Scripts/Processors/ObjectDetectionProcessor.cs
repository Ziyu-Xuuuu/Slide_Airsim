using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Object detection processor for semantic perception
    /// Integrates with SlideSLAM's object modeling pipeline
    /// </summary>
    public class ObjectDetectionProcessor : MonoBehaviour
    {
        [Header("Detection Configuration")]
        public float detectionThreshold = 0.5f;
        public int maxDetectionsPerFrame = 20;
        public List<string> targetClasses = new List<string> { "person", "car", "building", "tree" };
        
        private string droneName;
        private ROSCommunicator rosCommunicator;
        private List<ObjectDetection> currentDetections;
        private Queue<CameraData> cameraHistory;
        
        private bool isInitialized = false;
        
        public void Initialize(string robotName, ROSCommunicator rosComm)
        {
            droneName = robotName;
            rosCommunicator = rosComm;
            
            currentDetections = new List<ObjectDetection>();
            cameraHistory = new Queue<CameraData>();
            
            InitializeROSCommunication();
            isInitialized = true;
            
            Debug.Log($"Object Detection Processor initialized for {droneName}");
        }
        
        void InitializeROSCommunication()
        {
            if (rosCommunicator != null)
            {
                // Subscribe to object detection results
                rosCommunicator.SubscribeToDetections($"/{droneName}/object_detections", OnDetectionsReceived);
                
                // Advertise camera topics for object detection pipeline
                rosCommunicator.AdvertiseImage($"/{droneName}/camera/image_raw");
                rosCommunicator.AdvertiseCameraInfo($"/{droneName}/camera/camera_info");
                
                // Advertise semantic observation topic
                rosCommunicator.AdvertiseSemanticObservation($"/{droneName}/semantic_observation");
            }
        }
        
        public void ProcessCameraData(CameraData cameraData)
        {
            if (!isInitialized || cameraData == null) return;
            
            // Add to history
            cameraHistory.Enqueue(cameraData);
            while (cameraHistory.Count > 10) // Keep last 10 frames
            {
                cameraHistory.Dequeue();
            }
            
            // Convert and publish camera data for object detection
            var rosImage = ConvertToROSImage(cameraData);
            rosCommunicator.PublishImage($"/{droneName}/camera/image_raw", rosImage);
            
            var rosCameraInfo = ConvertToROSCameraInfo(cameraData);
            rosCommunicator.PublishCameraInfo($"/{droneName}/camera/camera_info", rosCameraInfo);
            
            // Perform local object detection (simplified)
            PerformLocalDetection(cameraData);
            
            // Create semantic observation
            CreateSemanticObservation(cameraData);
        }
        
        ROSImage ConvertToROSImage(CameraData cameraData)
        {
            // Convert Unity RenderTexture to ROS Image message
            var texture = cameraData.rgbTexture;
            var rosImage = new ROSImage
            {
                header = new ROSHeader
                {
                    stamp = TimeToROSTime(cameraData.timestamp),
                    frame_id = $"{droneName}/base_link/camera"
                },
                width = (uint)texture.width,
                height = (uint)texture.height,
                encoding = "rgb8",
                step = (uint)(texture.width * 3),
                data = ExtractImageData(texture)
            };
            
            return rosImage;
        }
        
        ROSCameraInfo ConvertToROSCameraInfo(CameraData cameraData)
        {
            // Create camera info from Unity camera parameters
            var rosCameraInfo = new ROSCameraInfo
            {
                header = new ROSHeader
                {
                    stamp = TimeToROSTime(cameraData.timestamp),
                    frame_id = $"{droneName}/base_link/camera"
                },
                width = 640,
                height = 480,
                distortion_model = "plumb_bob",
                K = new float[] { 525.0f, 0.0f, 320.0f, 0.0f, 525.0f, 240.0f, 0.0f, 0.0f, 1.0f },
                D = new float[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f }
            };
            
            return rosCameraInfo;
        }
        
        byte[] ExtractImageData(RenderTexture renderTexture)
        {
            // Extract pixel data from RenderTexture
            RenderTexture.active = renderTexture;
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            
            byte[] data = tex.GetRawTextureData();
            
            Destroy(tex);
            RenderTexture.active = null;
            
            return data;
        }
        
        void PerformLocalDetection(CameraData cameraData)
        {
            // Simplified object detection using Unity's built-in methods
            // In a real implementation, this would interface with ML models
            
            currentDetections.Clear();
            
            // Simulate object detection with raycasting from camera
            Camera camera = Camera.main; // Get actual camera component
            if (camera == null) return;
            
            // Cast rays in a grid pattern to detect objects
            int gridSize = 10;
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    float screenX = (x / (float)gridSize) * Screen.width;
                    float screenY = (y / (float)gridSize) * Screen.height;
                    
                    Ray ray = camera.ScreenPointToRay(new Vector3(screenX, screenY, 0));
                    RaycastHit hit;
                    
                    if (Physics.Raycast(ray, out hit, 100.0f))
                    {
                        GameObject hitObject = hit.collider.gameObject;
                        string objectClass = ClassifyObject(hitObject);
                        
                        if (targetClasses.Contains(objectClass))
                        {
                            var detection = new ObjectDetection
                            {
                                timestamp = cameraData.timestamp,
                                className = objectClass,
                                confidence = UnityEngine.Random.Range(0.7f, 0.95f), // Simulated confidence
                                boundingBox = CalculateBoundingBox(hitObject, camera),
                                worldPosition = hit.point,
                                distance = hit.distance
                            };
                            
                            currentDetections.Add(detection);
                        }
                    }
                }
            }
            
            // Limit detections per frame
            if (currentDetections.Count > maxDetectionsPerFrame)
            {
                currentDetections = currentDetections.GetRange(0, maxDetectionsPerFrame);
            }
        }
        
        string ClassifyObject(GameObject obj)
        {
            // Simple object classification based on name and tags
            string name = obj.name.ToLower();
            string tag = obj.tag.ToLower();
            
            if (name.Contains("person") || tag.Contains("person")) return "person";
            if (name.Contains("car") || name.Contains("vehicle") || tag.Contains("vehicle")) return "car";
            if (name.Contains("building") || tag.Contains("building")) return "building";
            if (name.Contains("tree") || tag.Contains("tree")) return "tree";
            
            return "unknown";
        }
        
        BoundingBox2D CalculateBoundingBox(GameObject obj, Camera camera)
        {
            // Calculate 2D bounding box in image coordinates
            Bounds bounds = obj.GetComponent<Renderer>()?.bounds ?? new Bounds(obj.transform.position, Vector3.one);
            
            Vector3[] corners = new Vector3[8];
            corners[0] = bounds.min;
            corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            corners[3] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            corners[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            corners[7] = bounds.max;
            
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            
            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = camera.WorldToScreenPoint(corner);
                if (screenPoint.z > 0) // In front of camera
                {
                    min = Vector2.Min(min, screenPoint);
                    max = Vector2.Max(max, screenPoint);
                }
            }
            
            return new BoundingBox2D
            {
                x = (int)min.x,
                y = (int)min.y,
                width = (int)(max.x - min.x),
                height = (int)(max.y - min.y)
            };
        }
        
        void CreateSemanticObservation(CameraData cameraData)
        {
            if (currentDetections.Count == 0) return;
            
            var semanticObs = new ROSSemanticObservation
            {
                header = new ROSHeader
                {
                    stamp = TimeToROSTime(cameraData.timestamp),
                    frame_id = $"{droneName}/base_link/camera"
                },
                robot_pose = new ROSPose
                {
                    position = UnityToROSPosition(cameraData.position),
                    orientation = UnityToROSQuaternion(cameraData.rotation)
                },
                detections = new List<ROSDetection>()
            };
            
            foreach (var detection in currentDetections)
            {
                semanticObs.detections.Add(new ROSDetection
                {
                    class_name = detection.className,
                    confidence = detection.confidence,
                    position = UnityToROSPosition(detection.worldPosition)
                });
            }
            
            rosCommunicator.PublishSemanticObservation($"/{droneName}/semantic_observation", semanticObs);
        }
        
        void OnDetectionsReceived(ROSDetectionArray detectionArray)
        {
            // Update detections from external object detection pipeline
            currentDetections.Clear();
            
            foreach (var rosDetection in detectionArray.detections)
            {
                var detection = new ObjectDetection
                {
                    timestamp = ROSTimeToUnity(detectionArray.header.stamp),
                    className = rosDetection.class_name,
                    confidence = rosDetection.confidence,
                    worldPosition = ROSToUnityPosition(rosDetection.position)
                };
                
                currentDetections.Add(detection);
            }
            
            Debug.Log($"Received {currentDetections.Count} detections for {droneName}");
        }
        
        public List<ObjectDetection> GetLatestDetections()
        {
            return new List<ObjectDetection>(currentDetections);
        }
        
        // Utility methods for coordinate conversion
        Vector3 UnityToROSPosition(Vector3 unityPos)
        {
            return new Vector3(unityPos.z, -unityPos.x, unityPos.y);
        }
        
        Vector3 ROSToUnityPosition(ROSPosition rosPos)
        {
            return new Vector3(-rosPos.y, rosPos.z, rosPos.x);
        }
        
        ROSQuaternion UnityToROSQuaternion(Quaternion unityQuat)
        {
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
    
    // Data structures for object detection
    [Serializable]
    public class ObjectDetection
    {
        public float timestamp;
        public string className;
        public float confidence;
        public BoundingBox2D boundingBox;
        public Vector3 worldPosition;
        public float distance;
    }
    
    [Serializable]
    public class BoundingBox2D
    {
        public int x, y, width, height;
    }
}