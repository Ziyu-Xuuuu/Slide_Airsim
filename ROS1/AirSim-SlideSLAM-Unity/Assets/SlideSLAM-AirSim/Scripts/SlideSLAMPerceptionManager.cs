using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Manages perception processing coordination across multiple vehicles
    /// </summary>
    public class SlideSLAMPerceptionManager : MonoBehaviour
    {
        [Header("Perception Settings")]
        public bool enableGlobalMapping = true;
        public bool enableMapSharing = true;
        public float coordinationUpdateRate = 1.0f;
        
        private Dictionary<string, VehiclePerceptionData> vehicleData;
        private List<Vector3> globalMap;
        private Dictionary<string, SemanticObject> globalSemanticMap;
        
        private bool isInitialized = false;
        private float lastCoordinationUpdate = 0f;
        
        public void Initialize(bool sloamEnabled, bool objectDetectionEnabled, bool semanticMappingEnabled)
        {
            vehicleData = new Dictionary<string, VehiclePerceptionData>();
            globalMap = new List<Vector3>();
            globalSemanticMap = new Dictionary<string, SemanticObject>();
            
            isInitialized = true;
            Debug.Log("SlideSLAM Perception Manager initialized");
        }
        
        void Update()
        {
            if (!isInitialized) return;
            
            if (Time.time - lastCoordinationUpdate > (1.0f / coordinationUpdateRate))
            {
                UpdateGlobalPerception();
                lastCoordinationUpdate = Time.time;
            }
        }
        
        public void RegisterVehicle(string vehicleName, SlideSLAMVehicleExtension vehicle)
        {
            if (!vehicleData.ContainsKey(vehicleName))
            {
                vehicleData[vehicleName] = new VehiclePerceptionData
                {
                    vehicleName = vehicleName,
                    vehicle = vehicle,
                    lastUpdate = Time.time
                };
                
                Debug.Log($"Registered vehicle for perception: {vehicleName}");
            }
        }
        
        public void UnregisterVehicle(string vehicleName)
        {
            if (vehicleData.ContainsKey(vehicleName))
            {
                vehicleData.Remove(vehicleName);
                Debug.Log($"Unregistered vehicle from perception: {vehicleName}");
            }
        }
        
        public void UpdateMultiRobotCoordination(Dictionary<string, SlideSLAMVehicleExtension> controllers)
        {
            if (!isInitialized) return;
            
            // Update vehicle data from controllers
            foreach (var kvp in controllers)
            {
                string vehicleName = kvp.Key;
                var controller = kvp.Value;
                
                if (!vehicleData.ContainsKey(vehicleName))
                {
                    RegisterVehicle(vehicleName, controller);
                }
                
                var data = vehicleData[vehicleName];
                data.lastUpdate = Time.time;
                data.currentPose = controller.GetCurrentSLAMPose();
                data.lidarData = controller.GetCurrentLiDARData();
                data.detections = controller.GetCurrentDetections();
            }
            
            // Update global perception
            if (enableGlobalMapping)
            {
                UpdateGlobalPerception();
            }
        }
        
        void UpdateGlobalPerception()
        {
            // Merge maps from all vehicles
            if (enableGlobalMapping)
            {
                MergeGlobalMaps();
            }
            
            // Share semantic information
            if (enableMapSharing)
            {
                ShareSemanticInformation();
            }
        }
        
        void MergeGlobalMaps()
        {
            globalMap.Clear();
            
            foreach (var data in vehicleData.Values)
            {
                if (data.lidarData != null && data.lidarData.points != null)
                {
                    // Transform points to global coordinate system
                    foreach (var point in data.lidarData.points)
                    {
                        // Apply vehicle transform to get global position
                        Vector3 globalPoint = TransformToGlobal(point, data.currentPose);
                        
                        if (!globalMap.Contains(globalPoint))
                        {
                            globalMap.Add(globalPoint);
                        }
                    }
                }
            }
            
            // Limit global map size for performance
            while (globalMap.Count > 50000)
            {
                globalMap.RemoveAt(0);
            }
        }
        
        void ShareSemanticInformation()
        {
            globalSemanticMap.Clear();
            
            foreach (var data in vehicleData.Values)
            {
                if (data.detections != null)
                {
                    foreach (var detection in data.detections)
                    {
                        string globalId = $"{detection.className}_{detection.worldPosition.x:F1}_{detection.worldPosition.y:F1}_{detection.worldPosition.z:F1}";
                        
                        if (globalSemanticMap.ContainsKey(globalId))
                        {
                            // Update existing object with higher confidence
                            var existing = globalSemanticMap[globalId];
                            if (detection.confidence > existing.confidence)
                            {
                                existing.confidence = detection.confidence;
                                existing.worldPosition = detection.worldPosition;
                                existing.timestamp = detection.timestamp;
                            }
                        }
                        else
                        {
                            globalSemanticMap[globalId] = new SemanticObject
                            {
                                id = globalId,
                                className = detection.className,
                                position = detection.worldPosition,
                                confidence = detection.confidence,
                                lastSeen = detection.timestamp,
                                observationCount = 1
                            };
                        }
                    }
                }
            }
        }
        
        Vector3 TransformToGlobal(Vector3 localPoint, SLAMPoseEstimate vehiclePose)
        {
            if (vehiclePose == null || !vehiclePose.isValid)
            {
                return localPoint;
            }
            
            // Transform from vehicle local to global coordinates
            return vehiclePose.position + vehiclePose.rotation * localPoint;
        }
        
        public List<Vector3> GetGlobalMap()
        {
            return new List<Vector3>(globalMap);
        }
        
        public List<SemanticObject> GetGlobalSemanticObjects()
        {
            return new List<SemanticObject>(globalSemanticMap.Values);
        }
        
        public int GetActiveVehicleCount()
        {
            return vehicleData.Count;
        }
        
        public List<string> GetActiveVehicleNames()
        {
            return new List<string>(vehicleData.Keys);
        }
    }
    
    [Serializable]
    public class VehiclePerceptionData
    {
        public string vehicleName;
        public SlideSLAMVehicleExtension vehicle;
        public float lastUpdate;
        public SLAMPoseEstimate currentPose;
        public LiDARPointCloud lidarData;
        public List<DetectedObject> detections;
    }
}