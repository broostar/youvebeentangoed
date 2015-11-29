using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tango;
using DDS.PointCloud;
using System;
using System.IO;
using System.Threading;

namespace DDS.Tango {
    public class TangoPointsMesh : MonoBehaviour, ITangoDepth, ITangoVideoOverlay {

        public GameObject m_pointsMeshPrefab;
        private BoxCollider m_scanBounds;

        private TangoApplication m_tangoApplication;
        private TangoPoseRequest m_request;

        private YUVTexture m_yuvTexture;
        //public Material m_screenMaterial;

        public Texture2D m_colourBuffer;
        private Matrix4x4 m_cameraProjection;

        private bool m_readyToDraw;
        private bool m_isExporting;
        private AndroidJavaObject m_unityActivity;
        private AndroidJavaObject m_progressDlg;

        private Stack<MeshFilter> m_meshes;
        private char[] m_trimmers = new char[]{ '(', ')' };

        private Thread m_exportThread;
        private Mutex m_exportLock;
        private string m_exportPath;
        private Mutex m_progressLock;
        private int m_progress;

        private List<Vector3> m_vertices;

        private void ExportMeshes() {
            float current = 0f;
            float total = (float)m_meshes.Count;
            using( StreamWriter writeStream = new StreamWriter( m_exportPath, false, System.Text.Encoding.ASCII ) ) {
                writeStream.WriteLine( "X, Y, Z" );
                foreach( MeshFilter filter in m_meshes ) {
                    foreach( Vector3 vert in filter.mesh.vertices ) {
                        writeStream.WriteLine( vert.ToString( "F15" ).Trim( m_trimmers ) );
                    }
                    m_progressLock.WaitOne();
                    m_progress = (int)(++current / total * 100.0);
                    m_progressLock.ReleaseMutex();
                }
                writeStream.Close();
            }
        }

        private void ExportVertices() {
            int current = 0;
            float total = (float)m_vertices.Count;
            using( StreamWriter writeStream = new StreamWriter( m_exportPath, false, System.Text.Encoding.ASCII ) ) {
                writeStream.WriteLine( "X, Y, Z" );
                foreach( Vector3 vector in m_vertices ) {
                    writeStream.WriteLine( vector.ToString( "F15" ).Trim( m_trimmers ) );
                    if( ++current % 400 == 0 ) {
                        m_progressLock.WaitOne();
                        m_progress = (int)(++current / total * 100.0);
                        m_progressLock.ReleaseMutex();
                    }
                }
                writeStream.Close();
            }
            
        }

        public void ExportPointCloud() {
            //First check to see if we're already exporting...
            m_exportLock.WaitOne();
            if( m_isExporting ) {
                AndroidHelper.ShowAndroidToastMessage( "Already Exporting.", false );
                m_exportLock.ReleaseMutex();
                return;
            }
            //AndroidHelper.ShowAndroidToastMessage( "Exporting...", false );
            m_isExporting = true;
            m_exportLock.ReleaseMutex();

            //
            //Get place to store data
            //
            //string path = "/sdcard/DDS_Apps/";// 
            string path = Application.persistentDataPath;
            if( path == null || path.Length <= 0 ) {
                Debug.Log( "Cannot find persistent path to save data!" );
                AndroidHelper.ShowAndroidToastMessage( "ERROR- Cannot find data path.", false );
                return;
            }
            int fileCount = Directory.GetFiles( path, "*.txt", SearchOption.TopDirectoryOnly ).Length;
            m_exportPath = Path.Combine( path, string.Format( "PointCloudExport_{0}.txt", fileCount ) );

            
            ////Is it quicker to get all vertices first, then loop through the verts, or, loop through mesh/verts each time?        
            //AndroidHelper.ShowAndroidToastMessage( "Collating vertices...", false );
            //m_vertices = new List<Vector3>();
            //foreach( MeshFilter filter in m_meshes ) {
            //    m_vertices.AddRange( filter.mesh.vertices );
            //}
            ////-------------------------------------------------------------------------------------------------------------


            //ExportMeshes();
            //AndroidHelper.ShowAndroidToastMessage( "Exporting Complete.", false );
            //return;

            //AndroidHelper.ShowAndroidToastMessage( "Starting Export Thread.", false );
            //You, /burp/ you, you gotta do this shit behind the scenes broh, quiet like...
            //m_exportThread = new Thread( ExportMeshes );
            //m_exportThread.Start();

            AndroidHelper.ShowAndroidToastMessage( "Running UI stuff...", false );
            using( AndroidJavaClass unityPlayer = new AndroidJavaClass( "com.unity3d.player.UnityPlayer" ) ) {
                m_unityActivity = unityPlayer.GetStatic<AndroidJavaObject>( "currentActivity" );
                m_unityActivity.Call( "runOnUiThread", new AndroidJavaRunnable( showExportProgress ) );
            }

            //Debug.Log( "Persistent data path: " + path +" ("+m_meshes.Count+" meshes)" );
            //int fileCount = Directory.GetFiles( path, "*.txt", SearchOption.TopDirectoryOnly ).Length;
            //Debug.Log( "Existing files: " + fileCount );
            //path = Path.Combine( path, string.Format( "PointCloudExport_{0}.txt", fileCount ) );
            //Debug.Log( "New File: " + path );
            //string exportTxt = " (" + m_meshes.Count + " meshes)";
            //using( AndroidJavaClass progressDlg = new AndroidJavaClass( "android.app.ProgressDialog" ) ) {
            //    m_progressDlg = progressDlg.CallStatic<AndroidJavaObject>( "show", m_unityActivity, "Exporting...", exportTxt );
            //    if( m_progressDlg != null ) {
            //        m_progressDlg.Call( "setMax", m_meshes.Count );
            //        m_progressDlg.Call( "setProgress", 0 );
            //        //StartCoroutine( ExportMeshes( path ) );
            //        ExportMeshes( path );
            //    }
            //}

            //using( StreamWriter writeStream = new StreamWriter( path, false, System.Text.Encoding.ASCII ) ) {
            //    writeStream.WriteLine( "X, Y, Z" );
            //    foreach( MeshFilter filter in m_meshes ) {
            //        WriteMesh( filter.mesh, writeStream );
            //    }
            //    writeStream.Close();
            //}

            
        }

        public void showExportProgress() {
            string exportTxt = " (" + m_meshes.Count + " meshes)";
            jvalue[] args = new jvalue[1];
            using( AndroidJavaClass progressDlg = new AndroidJavaClass( "android.app.ProgressDialog" ) ) {
                m_progressDlg = progressDlg.CallStatic<AndroidJavaObject>( "show", m_unityActivity, "Exporting...", exportTxt );
                //args[0].i = 1;
                //m_progressDlg.Call( "setProgressStyle", 1 );
                //if( m_progressDlg != null ) {
                //    args[0].i = m_meshes.Count;
                //    m_progressDlg.Call( "setMax", m_meshes.Count );
                //    args[0].i = 0;
                //    m_progressDlg.Call( "setProgress", 0 );
                //}
                //m_progressDlg.Call( "show" );
            }
            //args[0].i = 0;
            int progress = 0;
            m_exportLock.WaitOne();
            while( m_isExporting ) {
                m_exportLock.ReleaseMutex();

                m_progressLock.WaitOne();
                progress = m_progress;
                m_progressLock.ReleaseMutex();
                //m_progressDlg.Call( "setProgress", args[0].i );//FIX THIS THIS THIS SHITE
                m_progressDlg.Call( "setMessage", (progress + " %") );

                Thread.Sleep( 500 );
                m_exportLock.WaitOne();
            }
            m_exportLock.ReleaseMutex();
            m_progressDlg.Call( "dismiss" );
            m_progressDlg.Dispose();//necessary
        }

        public void Reset() {
            if( m_meshes != null ) {
                while( m_meshes.Count > 0 ) {
                    Destroy( m_meshes.Pop().gameObject );
                }
            }
        }

        private void RunExport() {
            //ExportMeshes( m_exportPath );
            AndroidHelper.ShowAndroidToastMessage( "Exporting Complete.", false );
        }

        // Use this for initialization
        void Start() {
            m_tangoApplication = FindObjectOfType<TangoApplication>();

            m_scanBounds = GetComponent<BoxCollider>();

            m_exportLock = new Mutex();
            m_progressLock = new Mutex();

            m_tangoApplication.RegisterPermissionsCallback( _OnTangoApplicationPermissionsEvent );
            m_tangoApplication.RequestNecessaryPermissionsAndConnect();

            //m_tangoApplication.Register( this );

            m_request = TangoPoseRequest.IMU_TO_DEVICE | TangoPoseRequest.IMU_TO_CAMERA_DEPTH | TangoPoseRequest.IMU_TO_CAMERA_COLOR;

            m_yuvTexture = m_tangoApplication.GetVideoOverlayTextureYUV();

            //// Pass YUV textures to shader for process.
            //m_screenMaterial.SetTexture( "_YTex", m_yuvTexture.m_videoOverlayTextureY );
            //m_screenMaterial.SetTexture( "_UTex", m_yuvTexture.m_videoOverlayTextureCb );
            //m_screenMaterial.SetTexture( "_VTex", m_yuvTexture.m_videoOverlayTextureCr );

            m_meshes = new Stack<MeshFilter>();
            m_isExporting = false;

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

                        point = unityWorldTDepthCamera.MultiplyPoint( point );
                        if( m_scanBounds.bounds.Contains( point ) ) {
                            points[i] = point;

                            //we need the point in camera space to look up the colour...
                            camSpacePoint = dcTcc.MultiplyPoint( point );
                            Debug.Log( "CamSpacePoint: " + camSpacePoint );
                        }
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
                    m_meshes.Push( obj.GetComponent<MeshFilter>() );

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
                double timestamp = VideoOverlayProvider.RenderLatestFrame( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR );
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

                //m_readyToDraw = true;

                //create colour buffer texture based on camera intrinsics
                TangoCameraIntrinsics intrinsics = new TangoCameraIntrinsics();
                VideoOverlayProvider.GetIntrinsics( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR, intrinsics );

                m_cameraProjection = TangoUtility.ProjectionMatrixForCameraIntrinsics( (float)intrinsics.width,
                                                                                        (float)intrinsics.height,
                                                                                        (float)intrinsics.fx,
                                                                                        (float)intrinsics.fy,
                                                                                        (float)intrinsics.cx,
                                                                                        (float)intrinsics.cy,
                                                                                        0.1f, 1000.0f );

                m_colourBuffer = new Texture2D( (int)intrinsics.width, (int)intrinsics.height );
            
                TangoUtility.InitExtrinsics( m_request );

            } else {
                AndroidHelper.ShowAndroidToastMessage( "Motion Tracking Permissions Needed", true );
            }
        }
    }
}