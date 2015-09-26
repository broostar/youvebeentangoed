using UnityEngine;
using System.Collections;

namespace DDS.PointCloud {
    public class OctTreeGenerator : MonoBehaviour {

        public OctTree m_octTree;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            Vector3[] points = new Vector3[1600];
            int len = points.Length;
            Vector3 center = Random.insideUnitCircle;
            for( int i=0;i<len;i++){
               points[i] = Random.onUnitSphere;
            }
            m_octTree.InsertPoints(points);
        }
    }
}