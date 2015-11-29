using UnityEngine;
using System.Collections;
using Tango;
using DDS.PointCloud;
using System;

namespace DDS.Tango {
    public class TangoOctTree : MonoBehaviour, ITangoDepth, ITangoVideoOverlay {

        public OctTree m_octTree;
        public GameObject m_pointsMeshPrefab;

        private TangoApplication m_tangoApplication;
        private TangoPoseRequest m_request;

        private YUVTexture m_yuvTexture;
        public Material m_screenMaterial;

        public Texture2D m_colourBuffer;
        private Matrix4x4 m_cameraProjection;

        private bool m_readyToDraw;

        // Use this for initialization
        void Start() {
            m_tangoApplication = FindObjectOfType<TangoApplication>();

            m_tangoApplication.RegisterPermissionsCallback( _OnTangoApplicationPermissionsEvent );
            m_tangoApplication.RequestNecessaryPermissionsAndConnect();

            m_tangoApplication.Register( this );

            m_request = TangoPoseRequest.IMU_TO_DEVICE | TangoPoseRequest.IMU_TO_CAMERA_DEPTH | TangoPoseRequest.IMU_TO_CAMERA_COLOR;

            m_yuvTexture = m_tangoApplication.GetVideoOverlayTextureYUV();

            //// Pass YUV textures to shader for process.
            //m_screenMaterial.SetTexture( "_YTex", m_yuvTexture.m_videoOverlayTextureY );
            //m_screenMaterial.SetTexture( "_UTex", m_yuvTexture.m_videoOverlayTextureCb );
            //m_screenMaterial.SetTexture( "_VTex", m_yuvTexture.m_videoOverlayTextureCr );

            //create colour buffer texture based on camera intrinsics
            TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
            VideoOverlayProvider.GetIntrinsics( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics );

            m_cameraProjection = TangoUtility.ProjectionMatrixForCameraIntrinsics(  (float)intrinsics.width,
                                                                                    (float)intrinsics.height,
                                                                                    (float)intrinsics.fx,
                                                                                    (float)intrinsics.fy,
                                                                                    (float)intrinsics.cx,
                                                                                    (float)intrinsics.cy,
                                                                                    0.1f, 1000.0f );

            m_colourBuffer = new Texture2D( (int)intrinsics.width, (int)intrinsics.height );
            
            

            if( m_tangoApplication.m_useExperimentalVideoOverlay ) {
                VideoOverlayProvider.ExperimentalConnectTexture( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, m_yuvTexture, OnExperimentalFrameAvailable );
            } else {
                VideoOverlayProvider.ConnectTexture( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, m_colourBuffer.GetNativeTextureID() );
            }

            TangoUtility.Init();
            
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

                    //double timestamp = VideoOverlayProvider.RenderLatestFrame( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR );
                    //GL.InvalidateState();

                    // Converting points array to world space.
                    int pointCount = tangoDepth.m_pointCount;
                    Vector3[] points = new Vector3[pointCount];
                    float[] from = tangoDepth.m_points;
                    Vector3 point = Vector3.zero;
                    Vector3 camSpacePoint;
                    Matrix4x4 dcTcc = m_cameraProjection * TangoUtility.GetDepthToColourCamera()  ;
                    int index = 0;
                    for( int i = 0; i < pointCount; ++i ) {
                        //float x = from[(i * 3) + 0];
                        //float y = from[(i * 3) + 1];
                        //float z = from[(i * 3) + 2];
                        index = i * 3;
                        point.Set( from[index + 0], from[index + 1], from[index + 2] ); 
                        points[i] = unityWorldTDepthCamera.MultiplyPoint( point );

                        //we need the point in camera space to look up the colour...
                        camSpacePoint = dcTcc.MultiplyPoint( point );
                        //Debug.Log( "CamSpacePoint: " + camSpacePoint );
                    }

                    ///
                    /// The Following will only work when the API provides the data
                    ///
                    //int count = tangoDepth.m_ij.Length;
                    //int colCount = tangoDepth.m_ijColumns;
                    //int rowCount = tangoDepth.m_ijRows;
                    //int[] all = tangoDepth.m_ij;
                    //Debug.Log( "Points: " + count+"; cols: "+colCount+"; rows:"+rowCount );
                    //int total = 0;
                    //for( int y = 0; y < rowCount; y++ ) {
                    //    for( int x = 0; x < colCount; x++ ) {
                    //        index = all[y*colCount+x];
                    //        if( index < 0 ) {
                    //            continue;
                    //        }
                    //        point.Set( all[index + 0], all[index + 1], all[index + 2] );
                    //        points[total++] = unityWorldTDepthCamera.MultiplyPoint( point );
                    //    }
                    //}

                    //m_octTree.InsertPoints( points );
                    GameObject obj = Instantiate<GameObject>( m_pointsMeshPrefab );
                    obj.GetComponent<PointsMesh>().AddPoints( points );

                    // The color should be pose relative, we need to store enough info to go back to pose values.
                    //m_renderer.material.SetMatrix( "depthCameraTUnityWorld", unityWorldTDepthCamera.inverse );
                } 
            }
        }

        public void OnExperimentalFrameAvailable(IntPtr callbackContext, TangoEnums.TangoCameraId cameraId) {
            Debug.Log( "IMAGE FRAME AVAILABLE: " + cameraId  );
        }

        public void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId, TangoUnityImageData imageBuffer){
            Debug.Log( "IMAGE DATA: " + cameraId + "; " + imageBuffer.format );
        }

        void OnPreRender() {
            if( m_readyToDraw ) {
                VideoOverlayProvider.RenderLatestFrame( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR );
                GL.InvalidateState();

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

                m_readyToDraw = true;

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