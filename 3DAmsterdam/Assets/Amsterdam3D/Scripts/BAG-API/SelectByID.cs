﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using LayerSystem;
using Amsterdam3D.CameraMotion;
using UnityEngine.EventSystems;
using System.Linq;
using Amsterdam3D.Interface;
using System;

public class SelectByID : MonoBehaviour
{
    public TileHandler tileHandler;

    private bool isWorkingOnSelection = false;
    private Ray ray;
    private string lastSelectedID = "";

    public static List<string> selectedIDs;

    private const string ApiUrl = "https://api.data.amsterdam.nl/bag/v1.1/pand/";
    private float mouseClickTime;
    private const float mouseDragDistance = 10.0f; //10 pixels results in a drag
    private Vector2 mousePosition;
    [SerializeField]
    private float clickTimer = 0.3f;

    [SerializeField]
    private LayerMask clickCheckLayerMask;
    private Layer containerLayer;

    private const string emptyID = "null";

    private bool doingMultiSelection = false;

	private string bagIdRequestServiceBoundingBoxUrl = "https://map.data.amsterdam.nl/maps/bag?REQUEST=GetFeature&SERVICE=wfs&version=2.0.0&typeName=bag:pand&propertyName=bag:id&outputFormat=csv&bbox=";
    private string bagIdRequestServicePolygonUrl = "https://map.data.amsterdam.nl/maps/bag?REQUEST=GetFeature&SERVICE=wfs&version=2.0.0&typeName=bag:pand&propertyName=bag:id&outputFormat=csv&Filter=";

    private void Awake()
	{
        selectedIDs = new List<string>();
        containerLayer = gameObject.GetComponent<Layer>();
    }

	void Update()
    {
        doingMultiSelection = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetMouseButtonDown(0))
        {
            mouseClickTime = Time.time;
            mousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0) && (Time.time - mouseClickTime) < clickTimer && Vector3.Distance(mousePosition, Input.mousePosition) < mouseDragDistance && !EventSystem.current.IsPointerOverGameObject() && CameraModeChanger.Instance.CameraMode == CameraMode.GodView)
        {
            //If we did a left mouse click without dragging, find a selected object
            FindSelectedID();
        }
        else if (Input.GetMouseButtonUp(1) && selectedIDs.Count <= 1){
            //Right mouse only does a selection when our list 1 or empty.
            doingMultiSelection = false;
            FindSelectedID();
        }
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                HideSelectedIDs();
            }
        }
    }

    /// <summary>
    /// Select a mesh ID underneath the pointer
    /// </summary>
    private void FindSelectedID()
    {
        //Clear selected ids if we are not adding to a multiselection
        if (!doingMultiSelection) selectedIDs.Clear();

        ray = CameraModeChanger.Instance.ActiveCamera.ScreenPointToRay(Input.mousePosition);
        //Try to find a selected mesh ID and highlight it
        StartCoroutine(GetSelectedMeshIDData(ray, (value) => { HighlightSelectedID(value); }));
    }

    /// <summary>
    /// Find selected ID's based on a area selection done by our selectiontools.
    /// We find BAG id's within an area using a WebRequest and an API.
    /// </summary>
    public void FindSelectedIDsInArea()
    {
        SelectionTools selectionTools = FindObjectOfType<SelectionTools>();
        var vertices = selectionTools.GetVertices();
        var bounds = selectionTools.GetBounds();
        containerLayer.AddMeshColliders();

        //Bounding box (does not support rotations)
        //StartCoroutine(GetAllIDsInBoundingBoxRange(vertices[0], vertices[2], HighlightSelectionRegionIDs));
        //Polygon selection
        StartCoroutine(GetAllIDsInPolygonRange(vertices.ToArray(), HighlightSelectionRegionIDs));
    }

    /// <summary>
    /// Add a single object to highlight selection. If we clicked an empty ID, clear the selection if we are not in multiselect
    /// </summary>
    /// <param name="id">The object ID</param>
    public void HighlightSelectedID(string id)
    {
        if (id == emptyID && !doingMultiSelection)
        {
            ClearSelection();  
        }
        else{
            HighlightSelectionRegionIDs(new List<string> { id });
            isWorkingOnSelection = false;
        }
    }

    /// <summary>
    /// Add list of ID's to our selected objects list
    /// </summary>
    /// <param name="ids">List of IDs to add to our selection</param>
    private void HighlightSelectionRegionIDs(List<string> ids)
    {
        selectedIDs.AddRange(ids);
        lastSelectedID = (selectedIDs.Count > 0) ? selectedIDs.Last() : emptyID;
        containerLayer.Highlight(selectedIDs);
    }

    /// <summary>
    /// Clear our list of selected objects, and update the highlights
    /// </summary>
    public void ClearSelection()
	{
		lastSelectedID = emptyID;
		selectedIDs.Clear();
        containerLayer.Highlight(selectedIDs);
    }

    /// <summary>
    /// Hides all objects that matches the list of ID's, and remove them from our selection list.
    /// </summary>
    public void HideSelectedIDs()
    {
        if (selectedIDs.Count > 0)
        {
            //Adds selected ID's to our hidding objects of our layer
            containerLayer.Hide(selectedIDs);
            selectedIDs.Clear();
        }
    }

    /// <summary>
    /// Shows all hidden objects by clearing our selection and hiding that empty list
    /// </summary>
    public void UnhideAll()
    {
        lastSelectedID = emptyID;
        selectedIDs.Clear();
        containerLayer.Hide(selectedIDs);
    }

    /// <summary>
    /// Method to allow other objects to display the information panel for the last ID we selected here.
    /// </summary>
    public void ShowBAGDataForSelectedID()
    {
        DisplayBAGData.Instance.PrepareUI();
        StartCoroutine(ImportBAG.Instance.CallAPI(ApiUrl, lastSelectedID, RetrieveType.Pand)); // laat het BAG UI element zien
    }

    IEnumerator GetSelectedMeshIDData(Ray ray, System.Action<string> callback)
    {
        isWorkingOnSelection = true;
        tileHandler.pauseLoading = true;
        containerLayer.AddMeshColliders();

        //Didn't hit anything
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10000, clickCheckLayerMask.value) == false)
        {
            isWorkingOnSelection = false;
            tileHandler.pauseLoading = false;
            callback(emptyID);
            yield break;
        }

        //Get the mesh we selected and check if it has an ID stored in the UV2 slot
        Mesh mesh = hit.collider.gameObject.GetComponent<MeshFilter>().mesh;
        int vertexIndex = hit.triangleIndex * 3;
        if (vertexIndex > mesh.uv2.Length) 
        {
            Debug.LogWarning("UV index out of bounds. This object/LOD level does not contain highlight/hidden uv2 slot");
            yield break;
        }
        var uv = mesh.uv2[vertexIndex];
        var gameObjectToHighlight = hit.collider.gameObject;
        var hitVisibleObject = true;
        var newHit = hit;
        var lastHit = hit;

        //Keep piercing forward with raycasts untill we find a visible UV
        if (uv.y == 0.2f)
        {
            hitVisibleObject = false;
            Vector3 lastPoint = hit.point;
            while (uv.y == 0.2f)
            {
                Vector3 hitPoint = newHit.point + (ray.direction * 0.01f);
                ray = new Ray(hitPoint, ray.direction);
                if (Physics.Raycast(ray, out newHit, 10000, clickCheckLayerMask.value))
                {
                    uv = mesh.uv2[newHit.triangleIndex * 3];
                    Debug.DrawLine(lastPoint, newHit.point, Color.blue, 1000);
                    lastPoint = newHit.point;
                    lastHit = newHit;
                    if (uv.y != 0.2f) 
                    {
                        hitVisibleObject = true;
                        Debug.Log("Hit visible");
                    }
                }
                else 
                {
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
        }
        hit = lastHit;

        //Not retrieve the selected BAG ID tied to the selected triangle
        tileHandler.GetIDData(gameObjectToHighlight, hit.triangleIndex * 3, HighlightSelectedID);
        //tileHandler.GetIDData(gameObjectToHighlight, hit.triangleIndex * 3, (value) => { PaintSelectionPixel(value, gameObjectToHighlight, mesh, hit); });
    }

    private void PaintSelectionPixel(string objectId, GameObject gameObjectToHighlight, Mesh mesh, RaycastHit hit)
    {
        print("Adding texture with colored pixel");
        int objectIndex = hit.triangleIndex * 3;

        var objectData = gameObjectToHighlight.GetComponent<ObjectData>();
        Vector2Int textureSize = ObjectIDMapping.GetTextureSize(objectData.ids.Count);

        //Reapply the mesh collider so it has the new proper UV's
        mesh.uv = objectData.uvs;
        gameObjectToHighlight.GetComponent<MeshCollider>().sharedMesh = mesh;

        //Create a texture, without mipmapping or filtering
        Vector2 uvCoordinate = ObjectIDMapping.GetUV(objectIndex, textureSize);

        //Create a main texture on click
        Texture2D colorTexture = (Texture2D)gameObjectToHighlight.GetComponent<MeshRenderer>().material.GetTexture("_BaseMap");        
        if (!colorTexture)
        {
            colorTexture = new Texture2D(textureSize.x / 2, textureSize.y / 2, TextureFormat.ARGB32,false);
            colorTexture.filterMode = FilterMode.Point;
        }

        colorTexture.SetPixel(Mathf.FloorToInt(uvCoordinate.x), Mathf.FloorToInt(uvCoordinate.y), new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f));
        colorTexture.Apply();

        //TODO: change shader

        gameObjectToHighlight.GetComponent<MeshRenderer>().material.SetTexture("_BaseMap",colorTexture);
    }

    IEnumerator GetAllIDsInBoundingBoxRange(Vector3 min, Vector3 max, System.Action<List<string>> callback = null)
    {
        var wgsMin = ConvertCoordinates.CoordConvert.UnitytoRD(min);
        var wgsMax = ConvertCoordinates.CoordConvert.UnitytoRD(max);

        List<string> ids = new List<string>();
        string url = bagIdRequestServiceBoundingBoxUrl;
        // construct url string
        url += wgsMin.x + "," + wgsMin.y + "," + wgsMax.x + "," + wgsMax.y;
        var hideRequest = UnityWebRequest.Get(url);
        print(url);
        yield return hideRequest.SendWebRequest();
        if (hideRequest.isNetworkError || hideRequest.isHttpError)
        {
            WarningDialogs.Instance.ShowNewDialog("Sorry, door een probleem met de BAG id server is een selectie maken tijdelijk niet mogelijk.");
        }
        else
        {
            //Filter out the list of ID's from our returned CSV           
            string dataString = hideRequest.downloadHandler.text;
            var csv = SplitCSV(dataString);
            int returnCounter = 0;
            
            for (int i = 3; i < csv.Count; i += 2)
            {
                var numberOnlyString = GetNumbers(csv[i]);
                ids.Add(numberOnlyString);
                returnCounter++;
                if (returnCounter > 100)
                {
                    yield return null;
                    returnCounter = 0;
                }
            }
        }

        callback?.Invoke(ids);
        yield return null;
    }

    public BagDataSelection bagDataSelection;
    IEnumerator GetAllIDsInPolygonRange(Vector3[] points, System.Action<List<string>> callback = null)
    {
        List<string> ids = new List<string>();

        //Create a string array of coordinates for our filter XML
        string coordinates = "";
		for (int i = 0; i < points.Length; i++)
		{
            //convert Unity to WGS84
            var coordinate = ConvertCoordinates.CoordConvert.UnitytoRD(points[i]);
            if (i != 0) coordinates += ",";
            coordinates += coordinate.x + " " + coordinate.y;
        }
        coordinates += "";

        //Our filter according to https://www.mapserver.org/ogc/filter_encoding.html , the type of WFS server used by the API.
        var filter = $"<Filter><Within><PropertyName>Geometry</PropertyName><gml:Polygon><gml:outerBoundaryIs><gml:LinearRing><gml:coordinates>{coordinates}</gml:coordinates></gml:LinearRing></gml:outerBoundaryIs></gml:Polygon></Within></Filter>";

        var requestUrl = bagIdRequestServicePolygonUrl + UnityWebRequest.EscapeURL(filter);
        var hideRequest = UnityWebRequest.Get(requestUrl);
        Debug.Log(requestUrl);
        yield return hideRequest.SendWebRequest();

        if (hideRequest.isNetworkError || hideRequest.isHttpError)
        {
            WarningDialogs.Instance.ShowNewDialog("Sorry, door een probleem met de BAG id server is een selectie maken tijdelijk niet mogelijk.");
        }
        else
        {
            //Filter out the list of ID's from our returned CSV
            string dataString = hideRequest.downloadHandler.text;
            var csv = SplitCSV(dataString);
            int returnCounter = 0;
            
            for (int i = 3; i < csv.Count; i += 2)
            {
                var numberOnlyString = GetNumbers(csv[i]);
                ids.Add(numberOnlyString);
                returnCounter++;
                if (returnCounter > 100)
                {
                    yield return null;
                    returnCounter = 0;
                }
            }
        }
        callback?.Invoke(ids);
        yield return null;
    }

    //Api bag data selection return format (selective)
    [System.Serializable]
    public class BagDataSelection
    {
        public BagDataObjects[] object_list;
        public int object_count;
        public int page_count;

        [System.Serializable]
        public class BagDataObjects
        {
            public string _id = "";
        }
    }
    public List<string> SplitCSV(string csv)
    {
        List<string> splitString = new List<string>();
        bool inBracket = false;
        int startIndex = 0;

        for (int i = 0; i < csv.Length; i++)
        {
            if (csv[i] == '"')
            {
                inBracket = !inBracket;
            }

            else if (!inBracket)
            {
                if (csv[i] == ',' || csv[i] == '\n')
                {
                    splitString.Add(csv.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }
        }

        return splitString;
    }

    private static string GetNumbers(string input)
    {
        return new string(input.Where(c => char.IsDigit(c)).ToArray());
    }
}
