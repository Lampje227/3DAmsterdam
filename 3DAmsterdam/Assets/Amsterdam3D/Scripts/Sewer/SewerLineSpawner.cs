﻿using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Amsterdam3D.Sewerage
{
    public class SewerLineSpawner : MonoBehaviour
    {
        public GameObject sewerLinePrefab;

        /// <summary>
        /// Create SewerPipe-GameObject from template and location-information
        /// </summary>
        /// <param name="from">unity-coordinates of startpoint</param>
        /// <param name="to">unity-coordinates of endpoint</param>
        /// <param name="diameterMM">diameter in mm</param>
        /// <returns>GameObject with sewerpipe</returns>
        public GameObject CreateSewerLine(Vector3 from, Vector3 to, double diameterMM)
        {
            //get 2d-vectors for rotation-calculation
            Vector2 startpositionXZ = new Vector2(from.x, from.z);
            Vector2 endpositionXZ = new Vector2(to.x, to.z);
            float startHeight = from.y;
            float endHeight = to.y;

            //create new GameObject from sewerpipetemplate
            GameObject newSewerPipe = Instantiate(sewerLinePrefab, this.transform);
            Transform sewerPipe = newSewerPipe.transform;

            // rotate pipe in horizontally
            float angle = Vector2.SignedAngle((endpositionXZ - startpositionXZ), new Vector2(10, 0));
            sewerPipe.Rotate(new Vector3(0, 1, 0), angle);

            //rotate pipe vertically
            float distanceXY = (endpositionXZ - startpositionXZ).magnitude;
            float elevationDifference = endHeight - startHeight;
            angle = Vector2.Angle(new Vector2(distanceXY,elevationDifference), new Vector2(10, 0));
            sewerPipe.Rotate(new Vector3(0, 0, 1), angle);

            //scale pipe to correct length
            float requiredLength = (to - from).magnitude;
            Vector3 pipeScale = sewerPipe.transform.localScale;
            pipeScale.x *= requiredLength;
            sewerPipe.localScale = pipeScale;

            // scale pipe to correct diameter
            pipeScale = sewerPipe.transform.localScale;
            pipeScale.y *= (float)(diameterMM / 1000);
            pipeScale.z *= (float)(diameterMM / 1000);
            sewerPipe.localScale = pipeScale;

            /*determine vertical offset of pipe
             * input elevation of the pipe is BOB (Binnen-Onderkant Buis / Inside-Bottom of the Pipe)
             * thickness of the pipe-wall is unknown
             * we estimate that the thickness of the wall is 10% of the diameter of the pipe
             */
            float BOBoffset = (float)(0.4 * diameterMM / 1000);

            //move pipe to correct location
            sewerPipe.position = from + new Vector3(0, BOBoffset, 0);
            return newSewerPipe;
        }


        public GameObject CombineSewerLines()
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            Debug.Log(meshFilters.Length);
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);

                i++;
            }

            //destroy all children
            //Array to hold all child obj
            
            GameObject[] allChildren = new GameObject[transform.childCount];
            int j = 0;
            //Find all child obj and store to that array
            foreach (Transform child in transform)
            {
                allChildren[j] = child.gameObject;
                j++;
            }

           

            GameObject sewerlines = Instantiate(sewerLinePrefab, this.transform);
            sewerlines.SetActive(true);
            MeshFilter mf = sewerlines.GetComponentInChildren<MeshFilter>();    
            mf.mesh = new Mesh();
            mf.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mf.mesh.CombineMeshes(combine);
            mf.gameObject.transform.rotation = Quaternion.identity;
            mf.gameObject.transform.localScale = Vector3.one;
            mf.gameObject.transform.localPosition = Vector3.zero;
            //sewerlines.sewerlines

            //Now destroy them
            foreach (GameObject child in allChildren)
            {
                DestroyImmediate(child.gameObject);
            }
            return sewerlines;

        }
    }
}
