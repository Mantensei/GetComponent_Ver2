using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MantenseiLib.GetComponent
{
    /// <summary>
    /// プレハブに自動追加されるコンポーネント
    /// Awake時に自身のゲームオブジェクトに配置されている全てのMonoBehaviourに対してGetOrAddComponentを実行
    /// </summary>
    public class AutoGetComponentForPrefab : MonoBehaviour
    {
        [SerializeField] private bool _debugMode = false;

        private void Awake()
        {
            ProcessAllComponents();
        }

        private void ProcessAllComponents()
        {
            // 自身のゲームオブジェクトとその子オブジェクトの全てのMonoBehaviourを取得
            var monoBehaviours = GetComponentsInChildren<MonoBehaviour>(true);

            if (_debugMode)
            {
                Debug.Log($"[GetComponentAutoInitializer] Processing {monoBehaviours.Length} MonoBehaviours in {gameObject.name}");
            }

            foreach (var monoBehaviour in monoBehaviours)
            {
                // 自分自身は除外
                if (monoBehaviour == this) continue;

                // nullチェック（破壊されたコンポーネントを除外）
                if (monoBehaviour == null) continue;

                try
                {
                    GetComponentUtility.GetOrAddComponent(monoBehaviour);

                    if (_debugMode)
                    {
                        Debug.Log($"[GetComponentAutoInitializer] Processed {monoBehaviour.GetType().Name} on {monoBehaviour.name}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GetComponentAutoInitializer] Error processing {monoBehaviour.GetType().Name} on {monoBehaviour.name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 手動でコンポーネント処理を実行（デバッグ用）
        /// </summary>
        [ContextMenu("Process All Components")]
        public void ProcessAllComponentsManual()
        {
            ProcessAllComponents();
        }
    }
}