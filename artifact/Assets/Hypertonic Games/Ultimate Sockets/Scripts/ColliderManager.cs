using Hypertonic.Modules.UltimateSockets.Enums;
using Hypertonic.Modules.UltimateSockets.Interfaces;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets
{
    public class ColliderManager : MonoBehaviour, IColliderManager
    {
        public delegate void ColliderManagerEvent(Collider collider);

        public event ColliderManagerEvent OnTriggerEnterd;
        public event ColliderManagerEvent OnTriggerStaying;
        public event ColliderManagerEvent OnTriggerExited;


        public bool DisableOnStart { get => _disableOnStart; set { _disableOnStart = value; } }
        public bool IsTrigger { get; set; } = true;

        public Collider Collider => _collider;

        public Transform RootTransform { get => _rootTransform; set => _rootTransform = value; }



        [SerializeField]
        private Transform _rootTransform;

        [SerializeField]
        private Collider _collider;

        [SerializeField]
        private bool _disableOnStart = false;

        #region Unity Functions

        private void Awake()
        {
            if (_collider == null)
            {
                Debug.LogErrorFormat(this, "No collider assigned");
                return;
            }
        }

        private void Start()
        {
            if (_disableOnStart)
            {
                SetColliderActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            OnTriggerEnterd?.Invoke(other);
        }

        private void OnTriggerStay(Collider other)
        {
            OnTriggerStaying?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnTriggerExited?.Invoke(other);
        }

        #endregion Unity Functions

        public void AddSphereCollider()
        {
            _collider = gameObject.AddComponent<SphereCollider>();
            _collider.isTrigger = IsTrigger;
            ((SphereCollider)_collider).center = Vector3.zero;
            SizeColliderToFitObject();
        }

        public void AddBoxCollider()
        {
            _collider = gameObject.AddComponent<BoxCollider>();
            _collider.isTrigger = IsTrigger;
            ((BoxCollider)_collider).center = Vector3.zero;
            SizeColliderToFitObject();
        }

        public void AddMeshCollider()
        {
            _collider = gameObject.AddComponent<MeshCollider>();
            ((MeshCollider)_collider).convex = true;

            _collider.isTrigger = IsTrigger;

            MeshRenderer meshRendererOnParent = _rootTransform.GetComponentInChildren<MeshRenderer>();

            if (meshRendererOnParent != null)
            {
                MeshFilter meshFilter = meshRendererOnParent.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    ((MeshCollider)_collider).sharedMesh = meshFilter.sharedMesh;
                }
                else
                {
                    Debug.LogWarning("No valid mesh found on the parent object's MeshFilter.", this);
                }
            }
            else
            {
                Debug.LogWarning("No MeshRenderer found on the parent object.", this);
            }
        }

        public void RemoveColliders()
        {
            Collider[] colliders = GetComponents<Collider>();

            if (colliders.Length == 0)
                return;

            foreach (Collider collider in colliders)
            {
                DestroyImmediate(collider);
            }
        }

        public float GetRadius()
        {
            if (_collider is SphereCollider sphereCollider)
            {
                return sphereCollider.radius;
            }
            else if (_collider is BoxCollider boxCollider)
            {
                Vector3 halfExtents = boxCollider.size * 0.5f;
                float radius = Mathf.Max(halfExtents.x, halfExtents.y, halfExtents.z);
                return radius;
            }

            Debug.LogError("The GetRadius() function is not implemented for the collider type: " + _collider.GetType().Name);

            return 0f;
        }

        public void SetColliderRadius(float radius)
        {
            if (_collider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = radius;
            }
        }

        public void SetColliderActive(bool active)
        {
            _collider.enabled = active;
        }


        public bool TypeMatches(Collider collider)
        {
            return _collider.GetType() == collider.GetType();
        }

        public int GetColliderTypeIndex()
        {
            if (_collider is SphereCollider)
            {
                return (int)ColliderType.SPHERE;
            }
            else if (_collider is BoxCollider)
            {
                return (int)ColliderType.BOX;
            }
            else if (_collider is MeshCollider)
            {
                return (int)ColliderType.MESH;
            }
            else
            {
                Debug.LogError("Collider type not supported. Collider type: " + _collider.GetType().Name, this);
                return -1;
            }
        }

        private void SizeColliderToFitObject()
        {
            if (RootTransform == null)
            {
                Debug.LogWarningFormat(this, "No root transform assigned to this collider manager");
                return;
            }

            MeshFilter rootMeshFilter = RootTransform.GetComponent<MeshFilter>();

            if (rootMeshFilter == null)
            {
                return;
            }

            Mesh rootMesh = rootMeshFilter.sharedMesh;

            if (rootMesh == null)
            {
                return;
            }

            // Get the bounds of the parent mesh
            Bounds bounds = rootMesh.bounds;

            // Get the collider on this object
            Collider collider = GetComponent<Collider>();

            // Adjust the size of the collider to match the bounds
            if (collider is BoxCollider)
            {
                BoxCollider boxCollider = (BoxCollider)collider;
                boxCollider.size = bounds.size;
                boxCollider.center = bounds.center;
            }
            else if (collider is SphereCollider)
            {
                SphereCollider sphereCollider = (SphereCollider)collider;
                sphereCollider.radius = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / 2f;
                sphereCollider.center = bounds.center;
            }
        }
    }
}