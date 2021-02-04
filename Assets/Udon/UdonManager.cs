using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon.ClientBindings;
using VRC.Udon.ClientBindings.Interfaces;
using VRC.Udon.Common.Interfaces;
using Object = UnityEngine.Object;

namespace VRC.Udon
{
    [AddComponentMenu("")]
    public class UdonManager : MonoBehaviour, IUdonClientInterface
    {
        public UdonBehaviour currentlyExecuting;

        private static UdonManager _instance;
        private static readonly UpdateOrderComparer _udonBehaviourUpdateOrderComparer = new UpdateOrderComparer();

        private bool _isUdonEnabled = true;

        private readonly Dictionary<Scene, Dictionary<GameObject, HashSet<UdonBehaviour>>> _sceneUdonBehaviourDirectories =
            new Dictionary<Scene, Dictionary<GameObject, HashSet<UdonBehaviour>>>();

        private readonly SortedSet<UdonBehaviour> _updateUdonBehaviours = new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);
        private readonly SortedSet<UdonBehaviour> _lateUpdateUdonBehaviours = new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);
        private readonly SortedSet<UdonBehaviour> _fixedUpdateUdonBehaviours = new SortedSet<UdonBehaviour>(_udonBehaviourUpdateOrderComparer);

        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _updateUdonBehavioursRegistrationQueue = new Queue<(UdonBehaviour udonBehaviour, bool newState)>();
        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _lateUpdateUdonBehavioursRegistrationQueue = new Queue<(UdonBehaviour udonBehaviour, bool newState)>();
        private readonly Queue<(UdonBehaviour udonBehaviour, bool newState)> _fixedUpdateUdonBehavioursRegistrationQueue = new Queue<(UdonBehaviour udonBehaviour, bool newState)>();

        [PublicAPI]
        public static UdonManager Instance
        {
            get
            {
                #if !VRC_CLIENT
                if(_instance != null)
                {
                    return _instance;
                }

                GameObject udonManagerGameObject = new GameObject("UdonManager");
                DontDestroyOnLoad(udonManagerGameObject);
                _instance = udonManagerGameObject.AddComponent<UdonManager>();
                #endif

                return _instance;
            }
        }


        private IUdonClientInterface _udonClientInterface;

        private IUdonClientInterface UdonClientInterface
        {
            get
            {
                if(_udonClientInterface != null)
                {
                    return _udonClientInterface;
                }

                _udonClientInterface = new UdonClientInterface();

                return _udonClientInterface;
            }
        }

        #if !VRC_CLIENT
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            UdonManager udonManager = Instance;
            UdonBehaviour[] udonBehaviours = FindObjectsOfType<UdonBehaviour>();
            foreach(UdonBehaviour udonBehaviour in udonBehaviours)
            {
                udonManager.RegisterUdonBehaviour(udonBehaviour);
            }
        }

        #endif

        public void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
            }

            DebugLogging = Application.isEditor;

            if(Instance != this)
            {
                if(Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
                    DestroyImmediate(this);
                }

                return;
            }

            if(!Application.isPlaying)
            {
                return;
            }

            PrimitiveType[] primitiveTypes = (PrimitiveType[])Enum.GetValues(typeof(PrimitiveType));
            foreach(PrimitiveType primitiveType in primitiveTypes)
            {
                GameObject go = GameObject.CreatePrimitive(primitiveType);
                Mesh primitiveMesh = go.GetComponent<MeshFilter>().sharedMesh;
                Destroy(go);
                Blacklist(primitiveMesh);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Update()
        {
            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _updateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.ManagedUpdate();
            }

            while(_updateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _updateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _updateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _updateUdonBehaviours.Remove(udonBehaviour);
                }
            }

            if(anyNull)
            {
                _updateUdonBehaviours.RemoveWhere(o => o == null);
            }
        }

        private void LateUpdate()
        {
            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _lateUpdateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.ManagedLateUpdate();
            }

            while(_lateUpdateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _lateUpdateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _lateUpdateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _lateUpdateUdonBehaviours.Remove(udonBehaviour);
                }
            }

            if(anyNull)
            {
                _lateUpdateUdonBehaviours.RemoveWhere(o => o == null);
            }
        }

        private void FixedUpdate()
        {
            bool anyNull = false;
            foreach(UdonBehaviour udonBehaviour in _fixedUpdateUdonBehaviours)
            {
                if(udonBehaviour == null)
                {
                    anyNull = true;
                    continue;
                }

                udonBehaviour.ManagedFixedUpdate();
            }

            while(_fixedUpdateUdonBehavioursRegistrationQueue.Count > 0)
            {
                (UdonBehaviour udonBehaviour, bool newState) = _fixedUpdateUdonBehavioursRegistrationQueue.Dequeue();
                if(newState)
                {
                    _fixedUpdateUdonBehaviours.Add(udonBehaviour);
                }
                else
                {
                    _fixedUpdateUdonBehaviours.Remove(udonBehaviour);
                }
            }

            if(anyNull)
            {
                _fixedUpdateUdonBehaviours.RemoveWhere(o => o == null);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if(loadSceneMode == LoadSceneMode.Single)
            {
                _sceneUdonBehaviourDirectories.Clear();
            }

            Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory = new Dictionary<GameObject, HashSet<UdonBehaviour>>();
            List<Transform> transformsTempList = new List<Transform>();
            foreach(GameObject rootGameObject in scene.GetRootGameObjects())
            {
                rootGameObject.GetComponentsInChildren(true, transformsTempList);
                foreach(Transform currentTransform in transformsTempList)
                {
                    List<UdonBehaviour> currentGameObjectUdonBehaviours = new List<UdonBehaviour>();
                    foreach(UdonBehaviour udonBehaviour in currentGameObjectUdonBehaviours)
                    {
                        udonBehaviour.InitializeUdonContent();
                    }

                    GameObject currentGameObject = currentTransform.gameObject;
                    currentGameObject.GetComponents(currentGameObjectUdonBehaviours);

                    if(currentGameObjectUdonBehaviours.Count > 0)
                    {
                        sceneUdonBehaviourDirectory.Add(currentGameObject, new HashSet<UdonBehaviour>(currentGameObjectUdonBehaviours));
                    }
                }
            }

            if(!_isUdonEnabled)
            {
                VRC.Core.Logger.LogWarning("Udon is disabled globally, Udon components will be removed from the scene.");
                foreach(HashSet<UdonBehaviour> udonBehaviours in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviours)
                    {
                        Destroy(udonBehaviour);
                    }
                }

                return;
            }

            _sceneUdonBehaviourDirectories.Add(scene, sceneUdonBehaviourDirectory);

            // Initialize all UdonBehaviours in the scene so their Public Variables are populated.
            foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
            {
                foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                {
                    // All UdonBehaviours that exist in the scene get networking setup automatically.
                    udonBehaviour.IsNetworkingSupported = true;
                    udonBehaviour.InitializeUdonContent();
                }
            }
        }

        internal void RegisterUdonBehaviourUpdate(UdonBehaviour udonBehaviour)
        {
            _updateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));
        }

        internal void RegisterUdonBehaviourLateUpdate(UdonBehaviour udonBehaviour)
        {
            _lateUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));
        }

        internal void RegisterUdonBehaviourFixedUpdate(UdonBehaviour udonBehaviour)
        {
            _fixedUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, true));
        }

        internal void UnregisterUdonBehaviourUpdate(UdonBehaviour udonBehaviour)
        {
            _updateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));
        }

        internal void UnregisterUdonBehaviourLateUpdate(UdonBehaviour udonBehaviour)
        {
            _lateUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));
        }

        internal void UnregisterUdonBehaviourFixedUpdate(UdonBehaviour udonBehaviour)
        {
            _fixedUpdateUdonBehavioursRegistrationQueue.Enqueue((udonBehaviour, false));
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _sceneUdonBehaviourDirectories.Remove(scene);
        }

        [PublicAPI]
        public void SetUdonEnabled(bool isEnabled)
        {
            _isUdonEnabled = isEnabled;
        }

        public IUdonVM ConstructUdonVM()
        {
            return !_isUdonEnabled ? null : UdonClientInterface.ConstructUdonVM();
        }

        public void FilterBlacklisted<T>(ref T objectToFilter) where T : class
        {
            UdonClientInterface.FilterBlacklisted(ref objectToFilter);
        }


        public void Blacklist(UnityEngine.Object objectToBlacklist)
        {
            UdonClientInterface.Blacklist(objectToBlacklist);
        }

        public void Blacklist(IEnumerable<UnityEngine.Object> objectsToBlacklist)
        {
            UdonClientInterface.Blacklist(objectsToBlacklist);
        }

        public void FilterBlacklisted(ref UnityEngine.Object objectToFilter)
        {
            UdonClientInterface.FilterBlacklisted(ref objectToFilter);
        }

        public bool IsBlacklisted(Object objectToCheck)
        {
            return UdonClientInterface.IsBlacklisted(objectToCheck);
        }

        public void ClearBlacklist()
        {
            UdonClientInterface.ClearBlacklist();
        }

        public bool IsBlacklisted<T>(T objectToCheck)
        {
            return UdonClientInterface.IsBlacklisted(objectToCheck);
        }

        public IUdonWrapper GetWrapper()
        {
            return UdonClientInterface.GetWrapper();
        }

        [PublicAPI]
        public void RegisterUdonBehaviour(UdonBehaviour udonBehaviour)
        {
            GameObject udonBehaviourGameObject = udonBehaviour.gameObject;
            Scene udonBehaviourScene = udonBehaviourGameObject.scene;
            if(!_sceneUdonBehaviourDirectories.TryGetValue(udonBehaviourScene, out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                sceneUdonBehaviourDirectory = new Dictionary<GameObject, HashSet<UdonBehaviour>>();
                _sceneUdonBehaviourDirectories.Add(udonBehaviourScene, sceneUdonBehaviourDirectory);
            }

            if(sceneUdonBehaviourDirectory.TryGetValue(udonBehaviourGameObject, out HashSet<UdonBehaviour> gameObjectUdonBehaviours))
            {
                gameObjectUdonBehaviours.Add(udonBehaviour);
            }
            else
            {
                gameObjectUdonBehaviours = new HashSet<UdonBehaviour> {udonBehaviour};
                sceneUdonBehaviourDirectory.Add(udonBehaviourGameObject, gameObjectUdonBehaviours);
            }

            udonBehaviour.InitializeUdonContent();
        }

        //Run an udon event on all objects
        [PublicAPI]
        public void RunEvent(string eventName, params (string symbolName, object value)[] programVariables)
        {
            foreach(Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory in _sceneUdonBehaviourDirectories.Values)
            {
                foreach(HashSet<UdonBehaviour> udonBehaviourList in sceneUdonBehaviourDirectory.Values)
                {
                    foreach(UdonBehaviour udonBehaviour in udonBehaviourList)
                    {
                        if(udonBehaviour != null)
                        {
                            udonBehaviour.RunEvent(eventName, programVariables);
                        }
                    }
                }
            }
        }

        //Run an udon event on a specific gameObject
        [PublicAPI]
        public void RunEvent(GameObject eventReceiverObject, string eventName, params (string symbolName, object value)[] programVariables)
        {
            if(!_sceneUdonBehaviourDirectories.TryGetValue(eventReceiverObject.scene, out Dictionary<GameObject, HashSet<UdonBehaviour>> sceneUdonBehaviourDirectory))
            {
                return;
            }

            if(!sceneUdonBehaviourDirectory.TryGetValue(eventReceiverObject, out HashSet<UdonBehaviour> eventReceiverBehaviourList))
            {
                return;
            }

            foreach(UdonBehaviour udonBehaviour in eventReceiverBehaviourList)
            {
                udonBehaviour.RunEvent(eventName, programVariables);
            }
        }

        public bool DebugLogging
        {
            get => UdonClientInterface.DebugLogging;
            set => UdonClientInterface.DebugLogging = value;
        }

        private class UpdateOrderComparer : IComparer<UdonBehaviour>
        {
            public int Compare(UdonBehaviour x, UdonBehaviour y)
            {
                if(x == null)
                {
                    return y != null ? -1 : 0;
                }

                if(y == null)
                {
                    return 1;
                }

                int updateOrderComparison = x.UpdateOrder.CompareTo(y.UpdateOrder);
                if(updateOrderComparison != 0)
                {
                    return updateOrderComparison;
                }

                return x.GetInstanceID().CompareTo(y.GetInstanceID());
            }
        }
    }
}
