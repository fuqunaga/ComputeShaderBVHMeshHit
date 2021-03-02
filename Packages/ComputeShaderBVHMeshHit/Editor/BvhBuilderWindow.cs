using UnityEditor;
using UnityEngine;


namespace ComputeShaderBvhMeshHit.Editor
{
    public class BvhBuilderWindow : EditorWindow
    {
        public GameObject meshObjectRoot;
        public int splitCount = 64;

        [MenuItem("Window/BvhBuilder")]
        static void Init()
        {
            var window = GetWindowWithRect<BvhBuilderWindow>(new Rect(0f, 0f, 400f, 130f));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Space(32f);

            meshObjectRoot = EditorGUILayout.ObjectField(nameof(meshObjectRoot), meshObjectRoot, typeof(GameObject), true) as GameObject;
            splitCount = EditorGUILayout.IntField(nameof(splitCount), splitCount);

            GUILayout.Space(32f);

            GUI.enabled = (meshObjectRoot != null) && (splitCount > 0);
            if (GUILayout.Button("Build"))
            {
                var path = EditorUtility.SaveFilePanel("Save Bvh asset", "Assets", "bvhAsset", "asset");
                path = "Assets" + path.Substring(Application.dataPath.Length);
                if (!string.IsNullOrEmpty(path))
                {

                    var (bvhDatas, triangles) = BvhBuilder.BuildBvh(meshObjectRoot, splitCount);

                    var bvhAsset = CreateInstance<BvhAsset>();
                    bvhAsset.bvhDatas = bvhDatas;
                    bvhAsset.triangles = triangles;

                    AssetDatabase.CreateAsset(bvhAsset, path);

                    EditorGUIUtility.PingObject(bvhAsset);
                }
            }
        }
    }
}