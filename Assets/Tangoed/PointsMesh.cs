using UnityEngine;
using System.Collections;

[RequireComponent( typeof( MeshFilter ), typeof( MeshRenderer ) )]
public class PointsMesh : MonoBehaviour {

    private Mesh m_mesh;
    private Vector3[] m_points;// = new Vector3[MAX_POINTS];
    private int[] m_indices = new int[0];

    public void AddPoints( Vector3[] points ) {
        int count = points.Length;
        m_indices = new int[count];
        for( int i = 0; i < count; ++i ) {
            m_indices[i] = i;
        }
        m_points = points;
        StartCoroutine( UpdateMesh() );
    }

    public void Start() {
        m_mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = m_mesh;
    }

    private IEnumerator UpdateMesh() {
        yield return 0;
        m_mesh.Clear();
        m_mesh.vertices = m_points;
        m_mesh.SetIndices( m_indices, MeshTopology.Points, 0 );
        m_mesh.UploadMeshData( false );
    }
}
