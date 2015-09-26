using UnityEngine;
using System.Collections;
using Tango;
using UnityEngine.UI;

public class ShowTangoSensors : MonoBehaviour {

    public RawImage lumaTexture;
    public RawImage chromaBlueTexture;
    public RawImage chromaRedTexture;

    private TangoApplication m_tangoApplication;
    private YUVTexture m_textures;

    // Matrix for Tango coordinate frame to Unity coordinate frame conversion.
    // Start of service frame with respect to Unity world frame.
    private Matrix4x4 m_uwTss;

    // Unity camera frame with respect to color camera frame.
    private Matrix4x4 m_cTuc;

    // Device frame with respect to IMU frame.
    private Matrix4x4 m_imuTd;

    // Color camera frame with respect to IMU frame.
    private Matrix4x4 m_imuTc;

    // Unity camera frame with respect to IMU frame, this is composed by
    // Matrix4x4.Inverse(m_imuTd) * m_imuTc * m_cTuc;
    // We pre-compute this matrix to save some computation in update().
    private Matrix4x4 m_dTuc;

    /// <summary>
    /// Initialize the AR Screen.
    /// </summary>
    private void Start() {
        // Constant matrix converting start of service frame to Unity world frame.
        m_uwTss = new Matrix4x4();
        m_uwTss.SetColumn( 0, new Vector4( 1.0f, 0.0f, 0.0f, 0.0f ) );
        m_uwTss.SetColumn( 1, new Vector4( 0.0f, 0.0f, 1.0f, 0.0f ) );
        m_uwTss.SetColumn( 2, new Vector4( 0.0f, 1.0f, 0.0f, 0.0f ) );
        m_uwTss.SetColumn( 3, new Vector4( 0.0f, 0.0f, 0.0f, 1.0f ) );

        // Constant matrix converting Unity world frame frame to device frame.
        m_cTuc.SetColumn( 0, new Vector4( 1.0f, 0.0f, 0.0f, 0.0f ) );
        m_cTuc.SetColumn( 1, new Vector4( 0.0f, -1.0f, 0.0f, 0.0f ) );
        m_cTuc.SetColumn( 2, new Vector4( 0.0f, 0.0f, 1.0f, 0.0f ) );
        m_cTuc.SetColumn( 3, new Vector4( 0.0f, 0.0f, 0.0f, 1.0f ) );

	    //register for data callbacks
        m_tangoApplication = FindObjectOfType<TangoApplication>();

        if( m_tangoApplication != null ) {
            if( AndroidHelper.IsTangoCorePresent() ) {
                // Request Tango permissions
                m_tangoApplication.RegisterPermissionsCallback( _OnTangoApplicationPermissionsEvent );
                m_tangoApplication.RequestNecessaryPermissionsAndConnect();
                m_tangoApplication.Register( this );
            } else {
                // If no Tango Core is present let's tell the user to install it.
                Debug.Log( "Tango Core is outdated." );
            }
        } else {
            Debug.Log( "No Tango Manager found in scene." );
        }
        if( m_tangoApplication != null ) {
            m_textures = m_tangoApplication.GetVideoOverlayTextureYUV();

            lumaTexture.texture = m_textures.m_videoOverlayTextureY;
            chromaBlueTexture.texture = m_textures.m_videoOverlayTextureCb;
            chromaRedTexture.texture = m_textures.m_videoOverlayTextureCr;

            // Pass YUV textures to shader for process.
            //m_screenMaterial.SetTexture( "_YTex", m_textures.m_videoOverlayTextureY );
            //m_screenMaterial.SetTexture( "_UTex", m_textures.m_videoOverlayTextureCb );
            //m_screenMaterial.SetTexture( "_VTex", m_textures.m_videoOverlayTextureCr );
        }

        m_tangoApplication.Register( this );
	}
	
	// Update is called once per frame
	void Update () {
        if( Input.GetKeyDown( KeyCode.Escape ) ) {
            if( m_tangoApplication != null ) {
                m_tangoApplication.Shutdown();
            }

            // This is a temporary fix for a lifecycle issue where calling
            // Application.Quit() here, and restarting the application immediately,
            // results in a hard crash.
            AndroidHelper.AndroidQuit();
        }
        double timestamp = VideoOverlayProvider.RenderLatestFrame( TangoEnums.TangoCameraId.TANGO_CAMERA_COLOR );
        GL.InvalidateState();//?
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
            AndroidHelper.ShowAndroidToastMessage( "OK. Let's fucking rock!", false );
        } else {
            AndroidHelper.ShowAndroidToastMessage( "Motion Tracking Permissions Needed", true );
        }
    }

    /// <summary>
    /// The function is for querying the camera extrinsic, for example: the transformation between
    /// IMU and device frame. These extrinsics is used to transform the pose from the color camera frame
    /// to the device frame. Because the extrinsic is being queried using the GetPoseAtTime()
    /// with a desired frame pair, it can only be queried after the ConnectToService() is called.
    ///
    /// The device with respect to IMU frame is not directly queryable from API, so we use the IMU
    /// frame as a temporary value to get the device frame with respect to IMU frame.
    /// </summary>
    private void _SetCameraExtrinsics() {
        double timestamp = 0.0;
        TangoCoordinateFramePair pair;
        TangoPoseData poseData = new TangoPoseData();

        // Getting the transformation of device frame with respect to IMU frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_DEVICE;
        PoseProvider.GetPoseAtTime( poseData, timestamp, pair );
        Vector3 position = new Vector3( (float)poseData.translation[0],
                                       (float)poseData.translation[1],
                                       (float)poseData.translation[2] );
        Quaternion quat = new Quaternion( (float)poseData.orientation[0],
                                         (float)poseData.orientation[1],
                                         (float)poseData.orientation[2],
                                         (float)poseData.orientation[3] );
        m_imuTd = Matrix4x4.TRS( position, quat, new Vector3( 1.0f, 1.0f, 1.0f ) );

        // Getting the transformation of IMU frame with respect to color camera frame.
        pair.baseFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_IMU;
        pair.targetFrame = TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_CAMERA_COLOR;
        PoseProvider.GetPoseAtTime( poseData, timestamp, pair );
        position = new Vector3( (float)poseData.translation[0],
                               (float)poseData.translation[1],
                               (float)poseData.translation[2] );
        quat = new Quaternion( (float)poseData.orientation[0],
                              (float)poseData.orientation[1],
                              (float)poseData.orientation[2],
                              (float)poseData.orientation[3] );
        m_imuTc = Matrix4x4.TRS( position, quat, new Vector3( 1.0f, 1.0f, 1.0f ) );
        m_dTuc = Matrix4x4.Inverse( m_imuTd ) * m_imuTc * m_cTuc;
    }
}
