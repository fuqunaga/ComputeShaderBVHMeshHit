using System.IO;
using UnityEditor;
using UnityEngine;


namespace ComputeShaderBvhMeshHit.Editor
{
    public class BvhBuilderWindow : EditorWindow
    {
        public GameObject meshObjectRoot;
        public int splitCount = 64;

        string lastPath;

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
                var path = EditorUtility.SaveFilePanel("Save Bvh asset", Path.GetDirectoryName(lastPath) ?? "Assets", Path.GetFileName(lastPath) ?? "bvhAsset", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    lastPath = path;
                    var relativePath = "Assets" + path.Substring(Application.dataPath.Length);

                    var (bvhDatas, triangles) = BvhBuilder.BuildBvh(meshObjectRoot, splitCount);

                    var bvhAsset = AssetDatabase.LoadAssetAtPath<BvhAsset>(relativePath);
                    if (bvhAsset == null)
                    {
                        bvhAsset = CreateInstance<BvhAsset>();
                        AssetDatabase.CreateAsset(bvhAsset, relativePath);
                    }
                    bvhAsset.bvhDatas = bvhDatas;
                    bvhAsset.triangles = triangles;

                    AssetDatabase.Refresh();
                    EditorGUIUtility.PingObject(bvhAsset);
                }
            }
        }
    }
}