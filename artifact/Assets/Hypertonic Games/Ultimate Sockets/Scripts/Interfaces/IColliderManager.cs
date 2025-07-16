namespace Hypertonic.Modules.UltimateSockets.Interfaces
{
    public interface IColliderManager
    {
        public void RemoveColliders();
        public void AddSphereCollider();
        public void AddBoxCollider();
        public void AddMeshCollider();
        public int GetColliderTypeIndex();
    }
}
