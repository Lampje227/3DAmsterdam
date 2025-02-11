﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Cameras
{
    public interface ICameraControls
    {
        float GetNormalizedCameraHeight();

        float GetCameraHeight();

        void SetNormalizedCameraHeight(float height);

        void MoveAndFocusOnLocation(Vector3 targetLocation, Quaternion rotation);

        Vector3 GetMousePositionInWorld(Vector3 optionalPositionOverride = default);

        void EnableKeyboardActionMap(bool enabled);

        void EnableMouseActionMap(bool enabled);

        bool UsesActionMap(InputActionMap actionMap);
    }
}