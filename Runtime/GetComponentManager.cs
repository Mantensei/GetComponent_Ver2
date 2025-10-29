using UnityEngine;

namespace MantenseiLib.Internal
{
    public class GetComponentManager : MonoBehaviour
    {
        private static GetComponentManager _instance;
        public static GetComponentManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GetComponentManager>();
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            ProcessAllMonoBehaviours();
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void ProcessAllMonoBehaviours()
        {
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var monoBehaviour in allMonoBehaviours)
            {
                if (monoBehaviour == this) continue;

                GetComponentUtility.GetOrAddComponent(monoBehaviour);
            }
        }
    }
}