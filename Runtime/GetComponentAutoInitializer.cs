using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MantenseiLib
{
    /// <summary>
    /// �v���n�u�Ɏ����ǉ������R���|�[�l���g
    /// Awake���Ɏ��g�̃Q�[���I�u�W�F�N�g�ɔz�u����Ă���S�Ă�MonoBehaviour�ɑ΂���GetOrAddComponent�����s
    /// </summary>
    public class GetComponentAutoInitializer : MonoBehaviour
    {
        [SerializeField] private bool _debugMode = false;

        private void Awake()
        {
            ProcessAllComponents();
        }

        private void ProcessAllComponents()
        {
            // ���g�̃Q�[���I�u�W�F�N�g�Ƃ��̎q�I�u�W�F�N�g�̑S�Ă�MonoBehaviour���擾
            var monoBehaviours = GetComponentsInChildren<MonoBehaviour>(true);

            if (_debugMode)
            {
                Debug.Log($"[GetComponentAutoInitializer] Processing {monoBehaviours.Length} MonoBehaviours in {gameObject.name}");
            }

            foreach (var monoBehaviour in monoBehaviours)
            {
                // �������g�͏��O
                if (monoBehaviour == this) continue;

                // null�`�F�b�N�i�j�󂳂ꂽ�R���|�[�l���g�����O�j
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
        /// �蓮�ŃR���|�[�l���g���������s�i�f�o�b�O�p�j
        /// </summary>
        [ContextMenu("Process All Components")]
        public void ProcessAllComponentsManual()
        {
            ProcessAllComponents();
        }
    }
}