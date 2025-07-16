/*===============================================================================
Copyright (c) 2024 PTC Inc. and/or Its Subsidiary Companies. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.
===============================================================================*/

using UnityEngine;
using Vuforia;

namespace SampleResources.Scripts
{
    public class VuforiaPoseUtility : MonoBehaviour
    {
        public void ResetDevicePose()
        {
            VuforiaBehaviour.Instance.DevicePoseBehaviour.Reset();
        }
    }
}