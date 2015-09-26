using UnityEngine;
using System.Collections;
using Tango;
using DDS.PointCloud;

namespace DDS.Tango {
    public class TangoOctTree : MonoBehaviour, ITangoDepth {

        public OctTree m_octTree;

        private TangoApplication m_tangoApplication;
        TangoPoseRequest m_request;

        // Use this for initialization
        void Start() {
            m_tangoApplication = FindObjectOfType<TangoApplication>();

            m_tangoApplication.RegisterPermissionsCallback( _OnTangoApplicationPermissionsEvent );
            m_tangoApplication.RequestNecessaryPermissionsAndConnect();

            m_tangoApplication.Register( this );

            m_request = TangoPoseRequest.IMU_TO_DEVICE | TangoPoseRequest.IMU_TO_CAMERA_DEPTH;
            
        }

        /// <summary>
        /// Callback that gets called when depth is available from the Tango Service.
        /// </summary>
        /// <param name="tangoDepth">Depth information from Tango.</param>
        public void OnTangoDepthAvailable( TangoUnityDepth tangoDepth ) {
            // Calculate the time since the last successful depth data
            // collection.
            //if( m_previousDepthDeltaTime == 0.0 ) {
            //    m_previousDepthDeltaTime = tangoDepth.m_timestamp;
            //} else {
            //    m_depthDeltaTime = (float)((tangoDepth.m_timestamp - m_previousDepthDeltaTime) * 1000.0);
            //    m_previousDepthDeltaTime = tangoDepth.m_timestamp;
            //}

            // Fill in the data to draw the point cloud.
            if( tangoDepth != null && tangoDepth.m_points != null ) {
                if( tangoDepth.m_pointCount > 0 ) {
                    TangoUtility.InitExtrinsics( m_request );

                    TangoUtility.GetPose( tangoDepth.m_timestamp );

                    // The transformation matrix that represents the pointcloud's pose. 
                    // Explanation: 
                    // The pointcloud which is in Depth camera's frame, is put in unity world's 
                    // coordinate system(wrt unity world).
                    // Then we are extracting the position and rotation from uwTuc matrix and applying it to 
                    // the PointCloud's transform.
                    Matrix4x4 unityWorldTDepthCamera = TangoUtility.GetUnityWorldToDepthCamera();// m_unityWorldTStartService * m_startServiceTDevice * Matrix4x4.Inverse( m_imuTDevice ) * m_imuTDepthCamera;

                    // Converting points array to world space.
                    int pointCount = tangoDepth.m_pointCount;
                    Vector3[] points = new Vector3[pointCount];
                    float[] from = tangoDepth.m_points;
                    Vector3 point = Vector3.zero;
                    int index = 0;
                    for( int i = 0; i < pointCount; ++i ) {
                        //float x = from[(i * 3) + 0];
                        //float y = from[(i * 3) + 1];
                        //float z = from[(i * 3) + 2];
                        index = i * 3;
                        point.Set( from[index + 0], from[index + 1], from[index + 2] ); 
                        points[i] = unityWorldTDepthCamera.MultiplyPoint( point );
                    }

                    m_octTree.InsertPoints( points );

                    // The color should be pose relative, we need to store enough info to go back to pose values.
                    //m_renderer.material.SetMatrix( "depthCameraTUnityWorld", unityWorldTDepthCamera.inverse );
                } 
            }
        }

        /// <summary>
        /// This callback function is called after user appoved or declined the permission to use Motion Tracking.
        /// </summary>
        /// <param name="permissionsGranted">If the permissions were granted.</param>
        private void _OnTangoApplicationPermissionsEvent( bool permissionsGranted ) {
            if( permissionsGranted ) {
                m_tangoApplication.InitApplication();
                m_tangoApplication.InitProviders( string.Empty );
                m_tangoApplication.ConnectToService();

                // Ask ARScreen to query the camera intrinsics from Tango Service.
                //_SetCameraIntrinsics();
                //_SetCameraExtrinsics();
                TangoUtility.InitExtrinsics( m_request );
            } else {
                AndroidHelper.ShowAndroidToastMessage( "Motion Tracking Permissions Needed", true );
            }
        }
    }
}