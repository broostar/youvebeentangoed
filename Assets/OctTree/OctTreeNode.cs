using UnityEngine;
using System.Collections;
using System.Threading;

namespace DDS.PointCloud {
    /// <summary>
    /// Each node is effectively the mesh, containing up to 65000 points
    /// </summary>
    [RequireComponent( typeof( MeshFilter ), typeof( MeshRenderer ) )]
    public class OctTreeNode : MonoBehaviour {

        //public enum CHILDREN { TL,TR,BL,BR };

        public static readonly int MAX_POINTS = 65000;//will this ever change?
        public static readonly int MAX_DEPTH = 2;

        private string[] m_childNames = new string[]{
            "OctTreeNode (1) ",
            "OctTreeNode (2) ",
            "OctTreeNode (3) ",
            "OctTreeNode (4) ",
            "OctTreeNode (5) ",
            "OctTreeNode (6) ",
            "OctTreeNode (7) ",
            "OctTreeNode (8) "
        };

        //public OctTreeNode m_nodePrefab;

        private int m_depth = 0;
        public int Depth {
            get { return m_depth; }
            set { m_depth = value; }
        }

        private bool m_isLeaf = true;//always start as a leaf
        private OctTreeNode[] m_children = new OctTreeNode[8];//always eight children!

        private Mesh m_mesh;
        private MeshRenderer m_meshRenderer;

        private Bounds m_bounds;
        public Bounds Bounds {
            get { return m_bounds; }
            set { m_bounds = value; }
        }

        private int m_pointsCount = 0;
        private Vector3[] m_points = new Vector3[MAX_POINTS];
        private int[] m_indices = new int[0];


        private bool m_isRefreshing;
        public bool IsRefreshing {
            get { return m_isRefreshing; }
        }

        public bool Contains( Vector3 point ) {
            return m_bounds.Contains( point );
        }

        public void Start() {
            m_mesh = GetComponent<MeshFilter>().mesh;//we want the actual mesh, not the shared one
            m_mesh.MarkDynamic();
            //Debug.Log( "VertexCount: " + m_mesh.vertexCount );
            m_meshRenderer = GetComponent<MeshRenderer>();
            m_pointsCount = 0;
            m_isLeaf = true;
            m_isRefreshing = false;
        }

        /// <summary>
        /// This should only be called on the root node. And from the main thread? Initiates refresh of the whole tree.
        /// 
        /// </summary>
        /// <param name="refreshIndices"></param>
        /// <returns></returns>
        public IEnumerator RefreshMesh(bool refreshIndices=false) {
            if( m_isRefreshing ) {
                yield break;
            }
            
            if( m_isLeaf ) {
                // Need to update indicies too!
                if( refreshIndices ) {
                    m_indices = new int[m_pointsCount];
                    for( int i = 0; i < m_pointsCount; ++i ) {
                        m_indices[i] = i;
                    }
                }

                m_mesh.Clear();
                m_mesh.vertices = m_points;
                m_mesh.SetIndices( m_indices, MeshTopology.Points, 0 );
                yield break;
            } else {
                m_isRefreshing = true;// we need to wait till the next frame in case any new meshes have been created
                yield return 0;
                foreach( OctTreeNode node in m_children ) {//this could be unrolled?
                    node.RefreshSubMesh();
                }
                m_isRefreshing = false;
            }
        }

        /// <summary>
        /// Recursive method to parse the children of a root node
        ///
        /// TODO: Mark meshes as dirty or something.
        /// </summary>
        public void RefreshSubMesh() {
            if( m_isLeaf ) {
                if( m_mesh == null ){
                    throw new UnityException("Cannot refresh Mesh: null!");
                }
                if( m_pointsCount == m_mesh.vertexCount ) {
                    return;
                }
                // Need to update indicies too!
                m_indices = new int[m_pointsCount];
                for( int i = 0; i < m_pointsCount; ++i ) {
                    m_indices[i] = i;
                }
                //Debug.Log( "REFRESH THREAD: " + Thread.CurrentThread.ManagedThreadId );

                m_mesh.Clear();
                m_mesh.vertices = m_points;
                m_mesh.SetIndices( m_indices, MeshTopology.Points, 0 );
            } else {
                //m_meshRenderer.enabled = m_isLeaf;
                foreach( OctTreeNode node in m_children ) {//this could be unrolled?
                    node.RefreshSubMesh();
                }
            }
        }

        /// <summary>
        /// Add a single point into the node. Very slow.
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint( Vector3 point ) {
            //Debug.Log( "Adding point: " + point );
            if( m_isLeaf ) {
                if( m_pointsCount < MAX_POINTS ) {
                    m_points[m_pointsCount] = point;
                    ++m_pointsCount;

                    //int[] indices = new int[m_pointsCount];
                    //m_indices.CopyTo( indices, 0 );
                    //indices[m_pointsCount - 1] = m_pointsCount - 1;
                    //m_indices = indices;
                    //refreshMesh();
                    return;
                } else if (m_depth < MAX_DEPTH) {
                    //Debug.Log( "Cannot add Point. Mesh full!" );
                    //Debug.Log( "-- -- -- splitting? "+m_depth+" -- -- --" );
                    splitNode();
                    //Debug.Log( "-------------- SPLITTING YO! --------------" );
                    ////AddPoint( point );
                }
            } else {
                foreach( OctTreeNode node in m_children ) {//this could be unrolled?
                    if( node.Contains( point ) ) {
                        node.AddPoint( point );
                        break;//it can't be in two children...
                    }
                }
            }
        }

        /// <summary>
        /// Add an array of points to this node, if the number exeeds the max, split into eight...
        ///
        /// It might be better to use a smaller number than the max, if it's too slow splitting?
        /// </summary>
        /// <param name="points"></param>
        public void AddPoints( Vector3[] points ) {
            if( m_isLeaf ) {
                int diff = MAX_POINTS - (points.Length + m_pointsCount);
                if( diff >= 0 ) {
                    //we fit in the current mesh. sweet.
                    points.CopyTo( m_points, m_pointsCount );
                    int oldCount = m_pointsCount;
                    m_pointsCount += points.Length;

                    int[] indices = new int[m_pointsCount];
                    m_indices.CopyTo( indices, 0 );
                    for( int i = oldCount; i < m_pointsCount; ++i ) {
                        indices[i] = i;
                    }
                    m_indices = indices;
                    //RefreshMesh();

                    return;//we don't need to do any testing

                } else {
                    
                    //sheeeit, we gotta split.
                    splitNode();//this adds the current points
                    //AddPoints( points ); //add the new points as well!
                    Debug.Log( "-------------- SPLITTING YO! --------------" );

                    return;//remove this when uncommenting above!!
                }
            } 
            //C'EST NE PAS UN LEAF!
            //add each point, one by fucking one
            foreach( Vector3 point in points ) {
                foreach( OctTreeNode node in m_children ) {//this could be unrolled?
                    if( node.Contains( point ) ) {
                        node.AddPoint( point );
                        //Debug.Log( "Adding point: " + point );
                        break;//next point
                    }
                }
            }

            
        }

        public void OnDrawGizmosSelected() {
            Gizmos.DrawWireCube( m_bounds.center, m_bounds.size );
            //Debug.Log( "NODE: " + m_bounds );
        }


        private void splitNode() {
            if( m_depth >= MAX_DEPTH ) {
                return;
            }
            Vector3 extents = m_bounds.extents;//half the size, i.e. the size of our new nodes!
            Vector3 offsetX = new Vector3( extents.x, 0f, 0f );
            Vector3 offsetY = new Vector3( 0f, extents.y, 0f );
            //Vector3 offsetZ = new Vector3( 0f, 0f, extents.z );

            StartCoroutine( clearNode() );

            OctTreeNode temp = OctTree.InstanciateNode();
            temp.name = m_childNames[0] + m_depth;
            Bounds tempBounds;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.min, m_bounds.center );
            temp.Bounds = tempBounds;
            m_children[0] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[1] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.min + offsetX, m_bounds.center + offsetX );
            temp.Bounds = tempBounds;
            m_children[1] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[2] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.min + offsetY, m_bounds.center + offsetY );
            temp.Bounds = tempBounds;
            m_children[2] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[3] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.min + offsetX + offsetY, m_bounds.center + offsetX + offsetY );
            temp.Bounds = tempBounds;
            m_children[3] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[4] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.center, m_bounds.max );
            temp.Bounds = tempBounds;
            m_children[4] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[5] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.center - offsetX, m_bounds.max - offsetX );
            temp.Bounds = tempBounds;
            m_children[5] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[6] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.center - offsetY, m_bounds.max - offsetY );
            temp.Bounds = tempBounds;
            m_children[6] = temp;

            temp = OctTree.InstanciateNode();
            temp.name = m_childNames[7] + m_depth;
            temp.transform.parent = transform;
            temp.Depth = m_depth + 1;
            tempBounds = new Bounds();
            tempBounds.SetMinMax( m_bounds.center - offsetX - offsetY, m_bounds.max - offsetX - offsetY );
            temp.Bounds = tempBounds;
            m_children[7] = temp;

            m_isLeaf = false;

            //AddPoints( m_points );
        }

        private IEnumerator clearNode() {
            yield return 0;
            m_mesh.Clear( false );
            m_meshRenderer.enabled = false;
        }

        //private Vector3 findCenter( Bounds bounds, CHILDREN dir ) {
        //    switch( dir ) {
        //        case CHILDREN.TL:
        //            bounds.m
        //            break;
        //    }
        //}
    }
}