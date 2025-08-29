using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlideSLAMAirSim
{
    /// <summary>
    /// ROS message definitions for Unity-SlideSLAM communication
    /// Mirrors the ROS message structures used in the SlideSLAM pipeline
    /// </summary>
    
    [Serializable]
    public class ROSHeader
    {
        public float stamp;
        public string frame_id;
    }
    
    // Geometry Messages
    [Serializable]
    public class ROSPosition
    {
        public float x, y, z;
    }
    
    [Serializable]
    public class ROSQuaternion
    {
        public float x, y, z, w;
    }
    
    [Serializable]
    public class ROSPose
    {
        public ROSPosition position;
        public ROSQuaternion orientation;
    }
    
    [Serializable]
    public class ROSPoseWithCovariance
    {
        public ROSPose pose;
        public float[] covariance; // 6x6 matrix
    }
    
    [Serializable]
    public class ROSTwist
    {
        public ROSPosition linear;
        public ROSPosition angular;
    }
    
    [Serializable]
    public class ROSTwistWithCovariance
    {
        public ROSTwist twist;
        public float[] covariance; // 6x6 matrix
    }
    
    [Serializable]
    public class ROSTransform
    {
        public ROSHeader header;
        public string child_frame_id;
        public ROSPosition translation;
        public ROSQuaternion rotation;
    }
    
    // Navigation Messages
    [Serializable]
    public class ROSOdometry
    {
        public ROSHeader header;
        public string child_frame_id;
        public ROSPoseWithCovariance pose;
        public ROSTwistWithCovariance twist;
    }
    
    // Sensor Messages
    [Serializable]
    public class ROSPoint3D
    {
        public float x, y, z;
        public float intensity;
    }
    
    [Serializable]
    public class ROSPointCloud
    {
        public ROSHeader header;
        public List<ROSPoint3D> points;
    }
    
    [Serializable]
    public class ROSImage
    {
        public ROSHeader header;
        public uint height;
        public uint width;
        public string encoding;
        public byte is_bigendian;
        public uint step;
        public byte[] data;
    }
    
    [Serializable]
    public class ROSCameraInfo
    {
        public ROSHeader header;
        public uint height;
        public uint width;
        public string distortion_model;
        public float[] D; // distortion parameters
        public float[] K; // intrinsic camera matrix
        public float[] R; // rectification matrix
        public float[] P; // projection matrix
        public uint binning_x;
        public uint binning_y;
    }
    
    // SLOAM Messages (matching sloam_msgs package)
    [Serializable]
    public class ROSSyncOdom
    {
        public ROSHeader header;
        public string child_frame_id;
        public ROSPoseWithCovariance pose;
        public ROSTwistWithCovariance twist;
    }
    
    [Serializable]
    public class ROSDetection
    {
        public string class_name;
        public float confidence;
        public ROSPosition position;
        public ROSPosition size;
    }
    
    [Serializable]
    public class ROSDetectionArray
    {
        public ROSHeader header;
        public List<ROSDetection> detections;
    }
    
    [Serializable]
    public class ROSSemanticObservation
    {
        public ROSHeader header;
        public ROSPose robot_pose;
        public List<ROSDetection> detections;
        public ROSPointCloud associated_points;
    }
    
    [Serializable]
    public class ROSObjectModel
    {
        public ROSHeader header;
        public string object_id;
        public string class_name;
        public ROSPose pose;
        public ROSPosition size;
        public ROSPointCloud point_cloud;
        public float confidence;
    }
    
    // Multi-robot coordination messages
    [Serializable]
    public class ROSRobotState
    {
        public ROSHeader header;
        public string robot_id;
        public ROSPose pose;
        public ROSTwist velocity;
        public float battery_level;
        public string status;
    }
    
    [Serializable]
    public class ROSCoordinationCommand
    {
        public ROSHeader header;
        public string target_robot_id;
        public string command_type;
        public ROSPose target_pose;
        public string[] parameters;
    }
    
    [Serializable]
    public class ROSMapData
    {
        public ROSHeader header;
        public string robot_id;
        public ROSPointCloud global_map;
        public List<ROSObjectModel> detected_objects;
        public float map_confidence;
    }
    
    // Image Covering Coordination Messages (matching existing system)
    [Serializable]
    public class ROSImageCovering
    {
        public ROSHeader header;
        public string robot_id;
        public ROSPose current_pose;
        public float coverage_score;
        public List<ROSPosition> target_points;
    }
    
    [Serializable]
    public class ROSNeighbor
    {
        public string robot_id;
        public ROSPose pose;
        public float distance;
        public bool is_active;
    }
    
    [Serializable]
    public class ROSNeighborsArray
    {
        public ROSHeader header;
        public string robot_id;
        public List<ROSNeighbor> neighbors;
    }
    
    // SlideSLAM Data Structures for Unity
    [Serializable]
    public class LiDARData
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public List<Vector3> points;
        public float range;
        public int rayCount;
    }
    
    [Serializable]
    public class SLAMPose
    {
        public float timestamp;
        public Vector3 position;
        public Quaternion rotation;
        public bool isValid;
        public float confidence;
    }
    
    [Serializable]
    public class CameraData
    {
        public float timestamp;
        public string cameraName;
        public Vector3 position;
        public Quaternion rotation;
        public RenderTexture rgbTexture;
        public int width;
        public int height;
    }
    
    // Utility methods for message conversion
    public static class ROSMessageUtils
    {
        public static ROSHeader CreateHeader(string frameId)
        {
            return new ROSHeader
            {
                stamp = Time.time,
                frame_id = frameId
            };
        }
        
        public static ROSPosition Vector3ToROSPosition(Vector3 position)
        {
            return new ROSPosition
            {
                x = position.x,
                y = position.y,
                z = position.z
            };
        }
        
        public static Vector3 ROSPositionToVector3(ROSPosition position)
        {
            return new Vector3(position.x, position.y, position.z);
        }
        
        public static ROSQuaternion QuaternionToROSQuaternion(Quaternion rotation)
        {
            return new ROSQuaternion
            {
                x = rotation.x,
                y = rotation.y,
                z = rotation.z,
                w = rotation.w
            };
        }
        
        public static Quaternion ROSQuaternionToQuaternion(ROSQuaternion rotation)
        {
            return new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        }
        
        public static ROSPose TransformToROSPose(Transform transform)
        {
            return new ROSPose
            {
                position = Vector3ToROSPosition(transform.position),
                orientation = QuaternionToROSQuaternion(transform.rotation)
            };
        }
        
        public static void ROSPoseToTransform(ROSPose pose, Transform transform)
        {
            transform.position = ROSPositionToVector3(pose.position);
            transform.rotation = ROSQuaternionToQuaternion(pose.orientation);
        }
        
        public static float[] CreateIdentityCovariance()
        {
            float[] covariance = new float[36]; // 6x6 matrix
            for (int i = 0; i < 6; i++)
            {
                covariance[i * 6 + i] = 0.1f; // Small diagonal values
            }
            return covariance;
        }
        
        public static float[] CreateCameraIntrinsics(float focalLength, float width, float height)
        {
            float cx = width / 2.0f;
            float cy = height / 2.0f;
            
            return new float[]
            {
                focalLength, 0.0f, cx,
                0.0f, focalLength, cy,
                0.0f, 0.0f, 1.0f
            };
        }
    }
}