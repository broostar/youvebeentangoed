using UnityEngine;
using System.Collections;
using Tango;

namespace DDS.Tango {
    public enum TangoPoseRequest {
        START_OF_SERVICE_TO_DEVICE = 0,
        IMU_TO_DEVICE = 1,
        IMU_TO_CAMERA_DEPTH = 2,
        IMU_TO_CAMERA_COLOR = 4,

    };

    public static class TangoUtility {

        // Matrix for Tango coordinate frame to Unity coordinate frame conversion.
        // Start of service frame with respect to Unity world frame.
        private static Matrix4x4 m_uwTss;//Unity World to Service Start

        // Unity camera frame with respect to color camera frame.
        private static Matrix4x4 m_ccTuc;//Colour Camera to Unity Camera

        // Device frame with respect to IMU frame.
        private static Matrix4x4 m_imuTd;//Inertial... to Device

        // Color camera frame with respect to IMU frame.
        private static Matrix4x4 m_imuTcc;//Inertial... to Colour Camera

        private static Matrix4x4 m_imuTdc;//Inertial... to Depth Camera

        // Unity camera frame with respect to IMU frame, this is composed by
        // Matrix4x4.Inverse(m_imuTd) * m_imuTc * m_cTuc;
        // We pre-compute this matrix to save some computation in update().
        private static Matrix4x4 m_dTuc;

        // Device frame relative to Service Start
        private static Matrix4x4 m_ssTd;//Service Start to Device

        public static void Init() {
            //init our static matrices
            m_uwTss = new Matrix4x4();
            m_uwTss.SetColumn( 0, new Vector4( 1.0f, 0.0f, 0.0f, 0.0f ) );
            m_uwTss.SetColumn( 1, new Vector4( 0.0f, 0.0f, 1.0f, 0.0f ) );
            m_uwTss.SetColumn( 2, new Vector4( 0.0f, 1.0f, 0.0f, 0.0f ) );
            m_uwTss.SetColumn( 3, new Vector4( 0.0f, 0.0f, 0.0f, 1.0f ) );

            // Constant matrix converting Unity world frame frame to device frame.
            m_ccTuc = new Matrix4x4();
            m_ccTuc.SetColumn( 0, new Vector4( 1.0f, 0.0f, 0.0f, 0.0f ) );
            m_ccTuc.SetColumn( 1, new Vector4( 0.0f, -1.0f, 0.0f, 0.0f ) );
            m_ccTuc.SetColumn( 2, new Vector4( 0.0f, 0.0f, 1.0f, 0.0f ) );
            m_ccTuc.SetColumn( 3, new Vector4( 0.0f, 0.0f, 0.0f, 1.0f ) );
        }

        public static void InitExtrinsics( TangoPoseRequest requests ) {
            Init();
            double timestamp = 0.0;
            TangoCoordinateFramePair pair;
            TangoPoseData poseData = new TangoPoseData();

            Vector3 one = Vector3.one;
            Vector3 position;
            Quaternion quat;

            //FIXME this could get called multiple times. Check for that shit.
            if( (requests & TangoPoseRequest.IMU_TO_DEVICE) == TangoPoseRequest.IMU_TO_DEVICE ) {
                // Query the extrinsics between IMU and device frame.
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
                PoseProvider.GetPoseAtTime( poseData, timestamp, pair );
                position = new Vector3( (float)poseData.translation[0],
                                               (float)poseData.translation[1],
                                               (float)poseData.translation[2] );
                quat = new Quaternion( (float)poseData.orientation[0],
                                                 (float)poseData.orientation[1],
                                                 (float)poseData.orientation[2],
                                                 (float)poseData.orientation[3] );
                m_imuTd = Matrix4x4.TRS( position, quat, one );


            }

            if( (requests & TangoPoseRequest.IMU_TO_CAMERA_DEPTH) == TangoPoseRequest.IMU_TO_CAMERA_DEPTH ) {
                // Query the extrinsics between IMU and color camera frame.
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH;
                PoseProvider.GetPoseAtTime( poseData, timestamp, pair );
                position = new Vector3( (float)poseData.translation[0],
                                       (float)poseData.translation[1],
                                       (float)poseData.translation[2] );
                quat = new Quaternion( (float)poseData.orientation[0],
                                      (float)poseData.orientation[1],
                                      (float)poseData.orientation[2],
                                      (float)poseData.orientation[3] );
                m_imuTdc = Matrix4x4.TRS( position, quat, one );
            }

            if( (requests & TangoPoseRequest.IMU_TO_CAMERA_COLOR) == TangoPoseRequest.IMU_TO_CAMERA_COLOR ) {
                // Query the extrinsics between IMU and color camera frame.
                pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
                pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_DEPTH;
                PoseProvider.GetPoseAtTime( poseData, timestamp, pair );
                position = new Vector3( (float)poseData.translation[0],
                                       (float)poseData.translation[1],
                                       (float)poseData.translation[2] );
                quat = new Quaternion( (float)poseData.orientation[0],
                                      (float)poseData.orientation[1],
                                      (float)poseData.orientation[2],
                                      (float)poseData.orientation[3] );
                m_imuTdc = Matrix4x4.TRS( position, quat, one );
            }


            //m_dTuc = Matrix4x4.Inverse( m_imuTd ) * m_imuTcc * m_ccTuc;
        }

        public static TangoEnums.TangoPoseStatusType GetPose( double timestamp ) {
            TangoCoordinateFramePair pair;
            TangoPoseData poseData = new TangoPoseData();

            pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE;
            pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
            PoseProvider.GetPoseAtTime( poseData, timestamp, pair );

            if( poseData.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID ) {
                return poseData.status_code;
            }

            Vector3 position = new Vector3( (float)poseData.translation[0],
                                           (float)poseData.translation[1],
                                           (float)poseData.translation[2] );
            Quaternion quat = new Quaternion( (float)poseData.orientation[0],
                                             (float)poseData.orientation[1],
                                             (float)poseData.orientation[2],
                                             (float)poseData.orientation[3] );
            m_ssTd = Matrix4x4.TRS( position, quat, Vector3.one );

            return TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID;
        }

        public static Matrix4x4 GetUnityWorldToDepthCamera() {
            // The transformation matrix that represents the pointcloud's pose. 
            // Explanation: 
            // The pointcloud which is in Depth camera's frame, is put in unity world's 
            // coordinate system(wrt unity world).
            // Then we are extracting the position and rotation from uwTuc matrix and applying it to 
            // the PointCloud's transform.
            return m_uwTss * m_ssTd * Matrix4x4.Inverse( m_imuTd ) * m_imuTdc;
        }

        public static Matrix4x4 GetUnityWorldToUnityCamera() {
            // The returned Matrix is:
            //  position = GetColumn(3);
            //  rotation = Quaternion.LookRotation(GetColumn(2), GetColumn(1));
            return m_uwTss * m_ssTd * m_dTuc;
        }

        public static void SetPose( TangoPoseData pose ) {
            if( pose.framePair.baseFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
                pose.framePair.targetFrame == TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE ) {

                // Update the stats for the pose for the debug text
                if( pose.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID ) {
                    // Create new Quaternion and Vec3 from the pose data received in the event.
                    Vector3 position = new Vector3( (float)pose.translation[0],
                                                  (float)pose.translation[1],
                                                  (float)pose.translation[2] );

                    Quaternion quat = new Quaternion( (float)pose.orientation[0],
                                                     (float)pose.orientation[1],
                                                     (float)pose.orientation[2],
                                                     (float)pose.orientation[3] );
                    m_ssTd = Matrix4x4.TRS( position, quat, Vector3.one );
                }
            }
        }
    }
}