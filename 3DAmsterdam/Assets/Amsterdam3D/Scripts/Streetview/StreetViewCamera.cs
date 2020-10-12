﻿using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class StreetViewCamera : MonoBehaviour, ICameraControls
{
    private Vector2 rotation = new Vector2(0, 0);
    public float speed = 3;

    private bool inMenus = false;

    [SerializeField]
    private GameObject MainMenu;

    [SerializeField]
    private GameObject Layers;

    private Camera camera;

    private Ray ray;
    private RaycastHit hit;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        camera = GetComponent<Camera>();
        Layers.SetActive(false);
        MainMenu.SetActive(false);
        inMenus = false;
    }

    public void MoveAndFocusOnLocation(Vector3 targetLocation, Quaternion rotation) 
    {
        if (targetLocation.y < 1.8f) 
        {
            targetLocation.y = 1.81f;
        }
        
        transform.position = targetLocation;
        transform.rotation = rotation;
        Vector2 rotationEuler = rotation.eulerAngles;
        if (rotationEuler.x > 180) 
        {
            rotationEuler.x -= 360f;
        }

        if (rotationEuler.x < -180) 
        {
            rotationEuler.x += 360f;
        }
        this.rotation = rotationEuler;
    }

    void Update()
    {
        if (!inMenus)
        {
            rotation.y += Input.GetAxis("Mouse X") * speed;
            rotation.x += -Input.GetAxis("Mouse Y") * speed;
            rotation.x = ClampAngle(rotation.x, -90, 90);
            transform.eulerAngles = (Vector2)rotation;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                inMenus = true;
                Layers.SetActive(true);
                MainMenu.SetActive(true);
            }
        }
        else 
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) 
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                Layers.SetActive(false);
                MainMenu.SetActive(false);
                inMenus = false;
            }
        }
    }

    public float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    public float GetNormalizedCameraHeight()
    {
        return Mathf.InverseLerp(1.8f, 2500, transform.position.y);
    }

    public float GetCameraHeight()
    {
        return transform.position.y;
    }
    public void OnRotation(Quaternion rotation)
    {
        Vector2 rotationEuler = rotation.eulerAngles;
        if (rotationEuler.x > 180)
        {
            rotationEuler.x -= 360f;
        }

        if (rotationEuler.x < -180)
        {
            rotationEuler.x += 360f;
        }

        this.rotation = rotationEuler;
    }

    public Vector3 GetMousePositionInWorld()
    {
        ray = camera.ScreenPointToRay(Input.mousePosition);
        float distance = 99;
        if (Physics.Raycast(ray, out hit, distance))
        {
            return hit.point;
        }
        else 
        {
            // return end of mouse ray if nothing collides
            return ray.origin + ray.direction * (distance / 10);
        }
    }
}
