using UnityEngine;
using UnityEditor;

namespace DDS.PointCloud {
    public class OctTreeFactory : ScriptableObject {
        [MenuItem( "Tools/Create OctTree" )]
        public static void CreateOctTree() {
            //We need an OctTree, then recursively create as realtime version?
            OctTree tree;
            if( (tree = Selection.activeGameObject.GetComponent<OctTree>()) == null ) {
                Debug.Log( "Please select OctTree" );
                return;
            }

            Transform treeTransform = tree.transform;


        }
    }
}