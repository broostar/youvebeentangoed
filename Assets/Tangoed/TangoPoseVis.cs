using UnityEngine;
using System.Collections;
using Tango;

namespace DDS.Tango {
    public class TangoPoseVis : MonoBehaviour, ITangoPose {

        // Use this for initialization
        void Start() {
            TangoApplication app = FindObjectOfType<TangoApplication>();
            app.Register(this);
        }

        public void OnTangoPoseAvailable( TangoPoseData pose ) {
        // Get out of here if the pose is null
            if( pose == null || pose.status_code != TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID ) {
                Debug.Log( "TangoPoseData is null or invalid." );
                return;
            }

            TangoUtility.InitExtrinsics( TangoPoseRequest.IMU_TO_DEVICE );
            TangoUtility.SetPose( pose );

            Matrix4x4 uwTuc = TangoUtility.GetUnityWorldToUnityCamera();

            // Extract new local position
            transform.position = uwTuc.GetColumn( 3 );

            // Extract new local rotation
            transform.rotation = Quaternion.LookRotation( uwTuc.GetColumn( 2 ), uwTuc.GetColumn( 1 ) );
        }
    }
}