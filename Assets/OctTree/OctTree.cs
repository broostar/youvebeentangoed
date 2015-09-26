using UnityEngine;
using System.Collections;
using System.Threading;

namespace DDS.PointCloud {
    [ExecuteInEditMode]
    public class OctTree : MonoBehaviour {

        public OctTreeNode m_rootNode;
        public OctTreeNode m_nodePrefab;//we need this to be separate from OctTreeNode, otherwise we copy the mesh when instanciating. Known 'feature' apparently.
        private BoxCollider m_boxCollider;
        private Bounds m_bounds;

        private static OctTreeNode ms_nodePrefab;

        private Coroutine m_refreshCoroutine;
        private static Mutex m_nodeMutex = new Mutex();

        private int m_pointsCount;

        public void Start() {
            ms_nodePrefab = m_nodePrefab;
            m_boxCollider = GetComponent<BoxCollider>();
            m_bounds = m_boxCollider.bounds;
            m_rootNode.Bounds = m_bounds;
            m_refreshCoroutine = StartCoroutine( Refresh() );

            m_pointsCount = 0;
        }

        public void OnDestroy() {
            if( m_refreshCoroutine != null ) {
                StopCoroutine( m_refreshCoroutine );
            }
        }

        public void InsertPoint(Vector3 point) {
            m_rootNode.AddPoint( point );
        }

        public void InsertPoints( Vector3[] points ) {
            m_pointsCount += points.Length;
            if( m_pointsCount >= OctTreeNode.MAX_POINTS ) {
                Debug.Log( "MAX POINTS: (last points len: "+points.Length+")" );
                m_pointsCount = m_pointsCount - OctTreeNode.MAX_POINTS;
            }
            //if( !m_rootNode.IsRefreshing ) {
            m_nodeMutex.WaitOne();
            m_rootNode.AddPoints( points );
            m_nodeMutex.ReleaseMutex();
                //StartCoroutine( m_rootNode.RefreshMesh( true ) );
            //}
        }


        public static OctTreeNode InstanciateNode() {
            return Instantiate<OctTreeNode>( ms_nodePrefab );
        }

        public IEnumerator Refresh() {
            yield return 0;

            while( true ) {
                m_nodeMutex.WaitOne();
                m_rootNode.RefreshSubMesh();
                m_nodeMutex.ReleaseMutex();

                Debug.Log( "Refresh" );
                yield return new WaitForSeconds( 0.2f );
            }
        }
    }    
}
