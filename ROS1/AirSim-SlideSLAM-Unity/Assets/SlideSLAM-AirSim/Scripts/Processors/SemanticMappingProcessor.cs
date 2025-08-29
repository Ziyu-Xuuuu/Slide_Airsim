using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Semantic mapping processor that builds object-aware maps
    /// </summary>
    public class SemanticMappingProcessor : MonoBehaviour
    {
        [Header("Mapping Configuration")]
        public float mapResolution = 0.5f;
        public float maxMapSize = 200.0f;
        public int maxObjects = 100;
        
        private string droneName;
        private SlideSLAMROSInterface rosInterface;
        private Dictionary<string, SemanticObject> semanticMap;
        private List<Vector3> occupancyGrid;
        
        private bool isInitialized = false;
        
        public void Initialize(string robotName, SlideSLAMROSInterface rosComm)
        {
            droneName = robotName;
            rosInterface = rosComm;
            
            semanticMap = new Dictionary<string, SemanticObject>();
            occupancyGrid = new List<Vector3>();
            
            isInitialized = true;
            Debug.Log($"Semantic Mapping Processor initialized for {droneName}");
        }
        
        public void UpdateMap(LiDARPointCloud lidarData, List<DetectedObject> detections)
        {
            if (!isInitialized) return;
            
            // Update occupancy grid with LiDAR points
            if (lidarData != null)
            {
                UpdateOccupancyGrid(lidarData.points);
            }
            
            // Update semantic objects
            if (detections != null)
            {
                UpdateSemanticObjects(detections);
            }
        }
        
        void UpdateOccupancyGrid(List<Vector3> points)
        {
            foreach (var point in points)
            {
                // Discretize point to grid
                Vector3 gridPoint = DiscretizeToGrid(point);
                
                // Add to occupancy grid if not already present
                if (!occupancyGrid.Contains(gridPoint))
                {
                    occupancyGrid.Add(gridPoint);
                }
            }
            
            // Limit grid size for performance
            while (occupancyGrid.Count > 10000)
            {
                occupancyGrid.RemoveAt(0);
            }
        }
        
        void UpdateSemanticObjects(List<DetectedObject> detections)
        {
            foreach (var detection in detections)
            {
                string objectId = GenerateObjectId(detection);
                
                if (semanticMap.ContainsKey(objectId))
                {
                    // Update existing object
                    UpdateExistingObject(objectId, detection);
                }
                else
                {
                    // Add new object
                    AddNewObject(objectId, detection);
                }
            }
        }
        
        string GenerateObjectId(DetectedObject detection)
        {
            // Generate unique ID based on position and class
            Vector3 discretePos = DiscretizeToGrid(detection.worldPosition);
            return $"{detection.className}_{discretePos.x}_{discretePos.y}_{discretePos.z}";
        }
        
        void UpdateExistingObject(string objectId, DetectedObject detection)
        {
            var existingObject = semanticMap[objectId];
            
            // Update confidence and position
            existingObject.confidence = Mathf.Max(existingObject.confidence, detection.confidence);
            existingObject.lastSeen = detection.timestamp;
            existingObject.observationCount++;
            
            // Update position with weighted average
            float weight = detection.confidence;
            existingObject.position = Vector3.Lerp(existingObject.position, detection.worldPosition, weight);
        }
        
        void AddNewObject(string objectId, DetectedObject detection)
        {
            if (semanticMap.Count >= maxObjects)
            {
                // Remove oldest object
                RemoveOldestObject();
            }
            
            var semanticObject = new SemanticObject
            {
                id = objectId,
                className = detection.className,
                position = detection.worldPosition,
                size = detection.size,
                confidence = detection.confidence,
                firstSeen = detection.timestamp,
                lastSeen = detection.timestamp,
                observationCount = 1
            };
            
            semanticMap[objectId] = semanticObject;
        }
        
        void RemoveOldestObject()
        {
            string oldestId = "";
            float oldestTime = float.MaxValue;
            
            foreach (var kvp in semanticMap)
            {
                if (kvp.Value.lastSeen < oldestTime)
                {
                    oldestTime = kvp.Value.lastSeen;
                    oldestId = kvp.Key;
                }
            }
            
            if (!string.IsNullOrEmpty(oldestId))
            {
                semanticMap.Remove(oldestId);
            }
        }
        
        Vector3 DiscretizeToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / mapResolution) * mapResolution,
                Mathf.Round(position.y / mapResolution) * mapResolution,
                Mathf.Round(position.z / mapResolution) * mapResolution
            );
        }
        
        public List<SemanticObject> GetSemanticObjects()
        {
            return new List<SemanticObject>(semanticMap.Values);
        }
        
        public List<Vector3> GetOccupancyGrid()
        {
            return new List<Vector3>(occupancyGrid);
        }
    }
    
    [Serializable]
    public class SemanticObject
    {
        public string id;
        public string className;
        public Vector3 position;
        public Vector3 size;
        public float confidence;
        public float firstSeen;
        public float lastSeen;
        public int observationCount;
    }
}