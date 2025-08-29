using System;
using System.Collections.Generic;
using UnityEngine;
using AirSimUnity;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// Main controller for SlideSLAM perception integration with AirSim
    /// Manages the coordination between AirSim sensors and SlideSLAM processing
    /// </summary>
    public class SlideSLAMManager : MonoBehaviour
    {
        [Header("ROS Integration")]
        public string rosNodeName = "airsim_slideslam_node";
        public string rosMasterURI = "http://localhost:11311";
        
        [Header("SlideSLAM Components")]
        public bool enableSLOAM = true;
        public bool enableObjectDetection = true;
        public bool enableSemanticMapping = true;
        public bool enableMultiRobotCoordination = true;
        
        [Header("Perception Settings")]
        public float slamUpdateRate = 10.0f;
        public float perceptionRange = 100.0f;
        public LayerMask detectionLayers = -1;
        
        [Header("Drone Configuration")]
        public List<GameObject> drones = new List<GameObject>();
        
        private Dictionary<string, SlideSLAMVehicleExtension> droneControllers;
        private SlideSLAMROSInterface rosCommunicator;
        private SlideSLAMPerceptionManager perceptionManager;
        private bool isInitialized = false;
        
        public static SlideSLAMManager Instance { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            InitializeSlideSLAM();
        }
        
        void InitializeSlideSLAM()
        {
            Debug.Log("Initializing SlideSLAM-AirSim Integration...");
            
            // Initialize ROS communication
            rosCommunicator = gameObject.AddComponent<SlideSLAMROSInterface>();
            rosCommunicator.Initialize(rosNodeName, rosMasterURI);
            
            // Initialize perception manager
            perceptionManager = gameObject.AddComponent<SlideSLAMPerceptionManager>();
            perceptionManager.Initialize(enableSLOAM, enableObjectDetection, enableSemanticMapping);
            
            // Initialize drone controllers
            droneControllers = new Dictionary<string, SlideSLAMVehicleExtension>();
            foreach (GameObject drone in drones)
            {
                if (drone != null)
                {
                    var controller = drone.GetComponent<SlideSLAMVehicleExtension>();
                    if (controller == null)
                    {
                        controller = drone.AddComponent<SlideSLAMVehicleExtension>();
                    }
                    
                    string droneName = drone.name;
                    // SlideSLAMVehicleExtension initializes itself in Start()
                    droneControllers[droneName] = controller;
                }
            }
            
            isInitialized = true;
            Debug.Log($"SlideSLAM-AirSim Integration initialized with {droneControllers.Count} drones");
        }
        
        void Update()
        {
            if (!isInitialized) return;
            
            // Update perception processing at specified rate
            if (Time.time % (1.0f / slamUpdateRate) < Time.deltaTime)
            {
                UpdatePerception();
            }
        }
        
        void UpdatePerception()
        {
            // Perception is updated automatically by SlideSLAMVehicleExtension components
            
            if (enableMultiRobotCoordination)
            {
                perceptionManager.UpdateMultiRobotCoordination(droneControllers);
            }
        }
        
        public void RegisterDrone(GameObject drone)
        {
            if (!drones.Contains(drone))
            {
                drones.Add(drone);
                
                if (isInitialized)
                {
                    var controller = drone.GetComponent<SlideSLAMVehicleExtension>();
                    if (controller == null)
                    {
                        controller = drone.AddComponent<SlideSLAMVehicleExtension>();
                    }
                    
                    string droneName = drone.name;
                    // SlideSLAMVehicleExtension initializes itself in Start()
                    droneControllers[droneName] = controller;
                    
                    Debug.Log($"Registered new drone: {droneName}");
                }
            }
        }
        
        public void UnregisterDrone(GameObject drone)
        {
            if (drones.Contains(drone))
            {
                drones.Remove(drone);
                
                if (droneControllers.ContainsKey(drone.name))
                {
                    droneControllers.Remove(drone.name);
                    Debug.Log($"Unregistered drone: {drone.name}");
                }
            }
        }
        
        public SlideSLAMROSInterface GetROSCommunicator()
        {
            return rosCommunicator;
        }
        
        public SlideSLAMPerceptionManager GetPerceptionManager()
        {
            return perceptionManager;
        }
        
        void OnDestroy()
        {
            if (rosCommunicator != null)
            {
                rosCommunicator.Shutdown();
            }
        }
    }
}