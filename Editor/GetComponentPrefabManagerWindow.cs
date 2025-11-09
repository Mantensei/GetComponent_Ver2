using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MantenseiLib.GetComponent.Editor
{
    public class GetComponentPrefabManagerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<PrefabInfo> _prefabs = new List<PrefabInfo>();

        [MenuItem("Tools/Mantensei/GetComponent Prefab Manager #%&G")]
        public static void ShowWindow()
        {
            var window = GetWindow<GetComponentPrefabManagerWindow>("GetComponent Prefab Manager");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            ScanAllPrefabs();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("GetComponent Prefab Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("再スキャン", GUILayout.Height(30)))
            {
                ScanAllPrefabs();
            }

            EditorGUILayout.Space();

            int needsAdd = _prefabs.Count(p => p.status == PrefabStatus.NeedsAdd);
            int needsRemove = _prefabs.Count(p => p.status == PrefabStatus.NeedsRemove);

            EditorGUILayout.LabelField($"全対象プレハブ: {_prefabs.Count}件");
            EditorGUILayout.LabelField($"統計: 追加 {needsAdd}件 / 削除 {needsRemove}件");

            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (var prefab in _prefabs)
            {
                DrawPrefabInfo(prefab);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            GUI.enabled = needsAdd > 0 || needsRemove > 0;
            if (GUILayout.Button("正規化を実行", GUILayout.Height(40)))
            {
                ExecuteNormalization();
            }
            GUI.enabled = true;
        }

        private void DrawPrefabInfo(PrefabInfo prefab)
        {
            EditorGUILayout.BeginHorizontal("box");

            string statusIcon = prefab.status switch
            {
                PrefabStatus.OK => "✓",
                PrefabStatus.NeedsAdd => "⚠️",
                PrefabStatus.NeedsRemove => "⚠️",
                _ => ""
            };

            string statusText = prefab.status switch
            {
                PrefabStatus.OK => "OK",
                PrefabStatus.NeedsAdd => "要追加",
                PrefabStatus.NeedsRemove => "要削除",
                _ => ""
            };

            EditorGUILayout.LabelField(prefab.name, GUILayout.Width(200));
            EditorGUILayout.LabelField($"{statusIcon} {statusText}", GUILayout.Width(100));

            if (GUILayout.Button("選択", GUILayout.Width(60)))
            {
                Selection.activeObject = prefab.prefab;
                EditorGUIUtility.PingObject(prefab.prefab);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ScanAllPrefabs()
        {
            _prefabs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                bool usesGetComponent = CheckUsesGetComponentAttributes(prefab);
                bool hasAutoInitializer = prefab.GetComponent<AutoGetComponentForPrefab>() != null;

                // GetComponent属性を使っているか、AutoInitializerがあれば対象
                if (!usesGetComponent && !hasAutoInitializer) continue;

                PrefabStatus status = (usesGetComponent, hasAutoInitializer) switch
                {
                    (true, true) => PrefabStatus.OK,
                    (true, false) => PrefabStatus.NeedsAdd,
                    (false, true) => PrefabStatus.NeedsRemove,
                    _ => PrefabStatus.OK
                };

                _prefabs.Add(new PrefabInfo
                {
                    name = prefab.name,
                    prefab = prefab,
                    status = status
                });
            }

            _prefabs = _prefabs.OrderBy(p => p.status).ThenBy(p => p.name).ToList();
        }

        private bool CheckUsesGetComponentAttributes(GameObject prefab)
        {
            var allMonoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (var monoBehaviour in allMonoBehaviours)
            {
                if (monoBehaviour == null) continue;

                var type = monoBehaviour.GetType();
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    if (HasGetComponentAttribute(field)) return true;
                }

                foreach (var property in properties)
                {
                    if (HasGetComponentAttribute(property)) return true;
                }
            }

            return false;
        }

        private bool HasGetComponentAttribute(MemberInfo member)
        {
            return member.GetCustomAttribute<GetComponentAttribute>() != null;
        }

        private void ExecuteNormalization()
        {
            int addCount = _prefabs.Count(p => p.status == PrefabStatus.NeedsAdd);
            int removeCount = _prefabs.Count(p => p.status == PrefabStatus.NeedsRemove);

            if (!EditorUtility.DisplayDialog(
                "正規化の確認",
                $"以下の操作を実行します:\n\n追加: {addCount}件\n削除: {removeCount}件\n\nよろしいですか？",
                "実行",
                "キャンセル"))
            {
                return;
            }

            int successCount = 0;

            foreach (var prefabInfo in _prefabs)
            {
                if (prefabInfo.status == PrefabStatus.NeedsAdd)
                {
                    if (AddAutoInitializer(prefabInfo.prefab))
                    {
                        successCount++;
                    }
                }
                else if (prefabInfo.status == PrefabStatus.NeedsRemove)
                {
                    if (RemoveAutoInitializer(prefabInfo.prefab))
                    {
                        successCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完了", $"{successCount}件のプレハブを正規化しました。", "OK");

            ScanAllPrefabs();
        }

        private bool AddAutoInitializer(GameObject prefab)
        {
            try
            {
                string path = AssetDatabase.GetAssetPath(prefab);
                GameObject instance = PrefabUtility.LoadPrefabContents(path);

                if (instance.GetComponent<AutoGetComponentForPrefab>() == null)
                {
                    instance.AddComponent<AutoGetComponentForPrefab>();
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }

                PrefabUtility.UnloadPrefabContents(instance);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to add AutoGetComponentForPrefab to {prefab.name}: {ex.Message}");
                return false;
            }
        }

        private bool RemoveAutoInitializer(GameObject prefab)
        {
            try
            {
                string path = AssetDatabase.GetAssetPath(prefab);
                GameObject instance = PrefabUtility.LoadPrefabContents(path);

                var component = instance.GetComponent<AutoGetComponentForPrefab>();
                if (component != null)
                {
                    DestroyImmediate(component);
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }

                PrefabUtility.UnloadPrefabContents(instance);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to remove AutoGetComponentForPrefab from {prefab.name}: {ex.Message}");
                return false;
            }
        }

        private class PrefabInfo
        {
            public string name;
            public GameObject prefab;
            public PrefabStatus status;
        }

        private enum PrefabStatus
        {
            OK,
            NeedsAdd,
            NeedsRemove
        }
    }
}