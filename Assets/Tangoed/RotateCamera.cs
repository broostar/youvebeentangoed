using UnityEngine;
using System.Collections;

public class RotateCamera : MonoBehaviour {

    private float m_angle = 0.0f;
    private float m_dist = 2.0f;

    void Update() {
        if( Input.touchCount == 1 ) {
            if( Input.touches[0].phase == TouchPhase.Moved ) {
                m_angle += Input.touches[0].deltaPosition.x*0.3f;
                //FIXME this could go massively big or small...
            }
        }
        Vector3 pos = new Vector3( Mathf.Cos( m_angle ), 2f, Mathf.Sin( m_angle ) );
        transform.position = pos * m_dist;
        transform.LookAt( Vector3.zero );
    }
}
