﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateY : MonoBehaviour {

    private float rotSpeed = 15;

    void OnMouseDrag()
    {
         float rotY = Input.GetAxis("Mouse X") * rotSpeed + Input.GetAxis("Mouse Y") * -rotSpeed;

         transform.parent.parent.RotateAround(transform.parent.parent.GetComponent<Collider>().bounds.center, Vector3.up, rotY);
    }
}
