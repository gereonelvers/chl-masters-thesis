using System.Collections.Generic;
using Hypertonic.Modules.UltimateSockets.Models;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets
{
    [CreateAssetMenu(fileName = "Socket Placement Profile", menuName = "Hypertonic/Ultimate Sockets/Socket Placement Profile")]
    public class SocketPlacementProfileScriptableObject : ScriptableObject
    {
        public SocketPlacementProfile SocketPlacementProfile;
    }
}
