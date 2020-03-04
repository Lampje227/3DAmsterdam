﻿using cityJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ImportCitymodel: MonoBehaviour
{

    [MenuItem("GameObject/CityJSON/Import Citymodel",false,10)]
    static void importeer()
    {
        GameObject go = Selection.activeGameObject;
        CityJSONSettings settings = go.GetComponent<CityJSONSettings>();

        CityModel Citymodel = new CityModel(settings.filepath, settings.filename);
        List<Building> buildings = Citymodel.LoadBuildings(settings.LOD);
        Citymodel = null;
        //CreateGameObjects objectcreator = new CreateGameObjects();
        //Debug.Log(settings.filename);
        //objectcreator.useTextures = settings.useTextures;
        //Debug.Log(objectcreator.useTextures);
        //objectcreator.SingleMeshBuildings = settings.SingleMeshBuildings;
        //objectcreator.minimizeMeshes = settings.minimizeMeshes;
        //objectcreator.CreatePrefabs = settings.CreatePrefabs;

        //GameObject container = new GameObject(settings.modelname);

        CreateGameObjectsV2 objCreatorV2 = new CreateGameObjectsV2();

        objCreatorV2.CreateMeshesByIdentifier(buildings, "name");

        //objectcreator.CreateBuildings(buildings, settings.Origin, settings.Defaultmaterial, container);

    }

   

}
