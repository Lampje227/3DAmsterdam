﻿using Netherlands3D.Cameras;
using Netherlands3D.InputHandler;
using Netherlands3D.ObjectInteraction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Netherlands3D.Interface.SidePanel;
using System.Linq;
using UnityEngine.UI;

namespace Netherlands3D.Interface
{
	public class GridSelection : Interactable
	{
		[SerializeField]
		private Material gridMaterial;

		[SerializeField]
		private GameObject gridSelectionBlock;

		[SerializeField]
		private float gridSize = 100.0f; //Meter

		[SerializeField]
		private float gridPlaneSize = 10000.0f;

		private Vector3 gridBlockPosition;
		private Vector3Int mouseGridPosition;

		private IAction toggleAction;
		private IAction drawAction;
		private IAction clearAction;

		private bool drawing = false;
		private bool add = true;

		private GameObject scaleBlock;
		private Dictionary<Vector3Int, GameObject> voxels;
		private int maxVoxels = 200;

		[SerializeField]
		private bool freePaint = false;
		private Vector3Int startGridPosition;

		private string selectedExportFormat = "";
		[SerializeField]
		List<LayerSystem.Layer> selectableLayers;
		private bool[] exportLayerToggles = new bool[4] { true, true, true, true };


		private void Awake()
		{
			ActionMap = ActionHandler.actions.GridSelection;

			toggleAction = ActionHandler.instance.GetAction(ActionHandler.actions.GridSelection.ToggleVoxel);
			toggleAction.SubscribePerformed(Toggle);

			drawAction = ActionHandler.instance.GetAction(ActionHandler.actions.GridSelection.DrawVoxels);
			drawAction.SubscribePerformed(Drawing);
			drawAction.SubscribeCancelled(Drawing);

			if (freePaint)
			{
				clearAction = ActionHandler.instance.GetAction(ActionHandler.actions.GridSelection.EraseVoxels);
				clearAction.SubscribePerformed(Clear);
				clearAction.SubscribeCancelled(Clear);
			}

			voxels = new Dictionary<Vector3Int, GameObject>();
		}

		void Start()
		{
			this.transform.position = new Vector3(0, Config.activeConfiguration.zeroGroundLevelY, 0);
			SetGridSize();
		}
		private void Toggle(IAction action)
		{
			if (action.Performed)
			{

				if (Selector.Instance.HoveringInterface()) return;

				if (freePaint)
				{
					DrawVoxelsUnderMouse(true);
				}
				else
				{
					startGridPosition = GetGridPosition(CameraModeChanger.Instance.CurrentCameraControls.GetMousePositionInWorld());
					ScaleSingleVoxelUnderMouse(false);
				}

				FinishSelection();
			}
		}

		private void Drawing(IAction action)
		{
			if (action.Cancelled)
			{
				drawing = false;
				FinishSelection();
			}
			else if (!Selector.Instance.HoveringInterface() && action.Performed)
			{
				drawing = true;
				add = true;
				if (!freePaint)
				{
					startGridPosition = GetGridPosition(CameraModeChanger.Instance.CurrentCameraControls.GetMousePositionInWorld());
				}
			}
		}
		private void Clear(IAction action)
		{
			if (action.Cancelled)
			{
				drawing = false;
			}
			else if (action.Performed)
			{
				drawing = true;
				add = false;
			}
		}

		private void OnEnable()
		{
			TakeInteractionPriority();
		}
		private void OnDisable()
		{
			StopInteraction();
		}

		private void Update()
		{
			if (Selector.Instance.HoveringInterface())
			{
				gridSelectionBlock.SetActive(false);
			}
			else
			{
				gridSelectionBlock.SetActive(true);
				MoveSelectionBlock();
				if (drawing)
				{
					if (freePaint)
					{
						DrawVoxelsUnderMouse();
					}
					else
					{
						ScaleSingleVoxelUnderMouse();
					}				
				}
			}
		}

		private void DrawVoxelsUnderMouse(bool toggled = false)
		{
			if (Selector.Instance.HoveringInterface()) return;
			
			mouseGridPosition = GetGridPosition(CameraModeChanger.Instance.CurrentCameraControls.GetMousePositionInWorld());
			if (!voxels.ContainsKey(mouseGridPosition) && add && voxels.Count < maxVoxels)
			{
				voxels.Add(mouseGridPosition, Instantiate(gridSelectionBlock, new Vector3(mouseGridPosition.x, mouseGridPosition.y, mouseGridPosition.z), Quaternion.identity, gridSelectionBlock.transform.parent));
			}
			else if ((toggled || !add) && voxels.ContainsKey(mouseGridPosition))
			{
				Destroy(voxels[mouseGridPosition]);
				voxels.Remove(mouseGridPosition);
			}
		}

		private void ScaleSingleVoxelUnderMouse(bool calculateScale = true)
		{
			if (Selector.Instance.HoveringInterface()) return;

			//Just make sure there is one voxel that we can scale
			if (!scaleBlock)
			{
				voxels.Clear();
				voxels.Add(mouseGridPosition, Instantiate(gridSelectionBlock, new Vector3(mouseGridPosition.x, mouseGridPosition.y, mouseGridPosition.z), Quaternion.identity, gridSelectionBlock.transform.parent));
				scaleBlock = voxels.First().Value;
			}
			
			mouseGridPosition = GetGridPosition(CameraModeChanger.Instance.CurrentCameraControls.GetMousePositionInWorld());		
			scaleBlock.transform.position = mouseGridPosition;

			if (calculateScale)
			{
				var xDifference = (mouseGridPosition.x - startGridPosition.x);
				var zDifference = (mouseGridPosition.z - startGridPosition.z);

				scaleBlock.transform.position = startGridPosition;
				scaleBlock.transform.Translate(xDifference / 2.0f, 0, zDifference / 2.0f);
				scaleBlock.transform.localScale = new Vector3(
						(mouseGridPosition.x - startGridPosition.x) + ((xDifference < 0 ) ? -gridSize : gridSize),
						gridSize,
						(mouseGridPosition.z - startGridPosition.z) + ((zDifference < 0) ? -gridSize : gridSize)
				);
			}
			else{
				//Just make sure it is default size
				scaleBlock.transform.localScale = Vector3.one * gridSize;
			}
		}

		private Vector3Int GetGridPosition(Vector3 samplePosition)
		{
			samplePosition.x += (gridSize * 0.5f);
			samplePosition.z += (gridSize * 0.5f);

			samplePosition.x = (Mathf.Round(samplePosition.x / gridSize) * gridSize) - (gridSize * 0.5f);
			samplePosition.z = (Mathf.Round(samplePosition.z / gridSize) * gridSize) - (gridSize * 0.5f);

			Vector3Int roundedPosition = new Vector3Int
			{
				x = Mathf.RoundToInt(samplePosition.x),
				y = Mathf.RoundToInt(Config.activeConfiguration.zeroGroundLevelY + (gridSize * 0.5f)),
				z = Mathf.RoundToInt(samplePosition.z)
			};

			return roundedPosition;
		}

		private void SetGridSize()
		{
			gridMaterial.SetTextureScale("_MainTex", Vector2.one * (gridPlaneSize / (gridSize * 0.1f)));
			gridSelectionBlock.transform.localScale = new Vector3(gridSize, gridSize, gridSize);
		}

		private void MoveSelectionBlock()
		{
			gridBlockPosition = CameraModeChanger.Instance.CurrentCameraControls.GetMousePositionInWorld();
			//Offset to make up for grid object origin (centered)
			gridBlockPosition.x += (gridSize * 0.5f);
			gridBlockPosition.z += (gridSize * 0.5f);

			//Snap block to grid
			gridBlockPosition.x = (Mathf.Round(gridBlockPosition.x / gridSize) * gridSize) - (gridSize * 0.5f);
			gridBlockPosition.z = (Mathf.Round(gridBlockPosition.z / gridSize) * gridSize) - (gridSize * 0.5f);

			gridSelectionBlock.transform.position = gridBlockPosition;
			gridSelectionBlock.transform.Translate(Vector3.up * (gridSize * 0.5f));
		}

		private void FinishSelection()
		{
			
			//TODO: send this boundingbox to the mesh selection logic, and draw the sidepanel
			PropertiesPanel.Instance.OpenObjectInformation("Grid selectie", true, 10);

			//Lets render a ortographic thumbnail for a proper grid topdown view
			gridSelectionBlock.SetActive(false);
			PropertiesPanel.Instance.RenderThumbnailContaining(
				scaleBlock.GetComponent<MeshRenderer>().bounds, 
				PropertiesPanel.ThumbnailRenderMethod.ORTOGRAPHIC, 
				scaleBlock.GetComponent<MeshRenderer>().bounds.center + Vector3.up * 150.0f
			);
			gridSelectionBlock.SetActive(true);

			PropertiesPanel.Instance.AddTitle("Lagen");
			PropertiesPanel.Instance.AddActionCheckbox("Gebouwen", Convert.ToBoolean(PlayerPrefs.GetInt("exportLayer0Toggle",1)), (action) =>
			{
				exportLayerToggles[0] = action;
				PlayerPrefs.SetInt("exportLayer0Toggle", Convert.ToInt32(exportLayerToggles[0]));
			});
			PropertiesPanel.Instance.AddActionCheckbox("Bomen", Convert.ToBoolean(PlayerPrefs.GetInt("exportLayer1Toggle", 1)), (action) =>
			{
				exportLayerToggles[1] = action;
				PlayerPrefs.SetInt("exportLayer1Toggle", Convert.ToInt32(exportLayerToggles[1]));
			});
			PropertiesPanel.Instance.AddActionCheckbox("Maaiveld", Convert.ToBoolean(PlayerPrefs.GetInt("exportLayer2Toggle", 1)), (action) =>
			{
				exportLayerToggles[2] = action;
				PlayerPrefs.SetInt("exportLayer2Toggle", Convert.ToInt32(exportLayerToggles[2]));
			});
			PropertiesPanel.Instance.AddActionCheckbox("Ondergrond", Convert.ToBoolean(PlayerPrefs.GetInt("exportLayer3Toggle", 1)), (action) =>
			{
				exportLayerToggles[3] = action;
				PlayerPrefs.SetInt("exportLayer3Toggle", Convert.ToInt32(exportLayerToggles[3]));
			});

			var exportFormats = new string[] { "AutoCAD DXF (.dxf)", "Collada DAE (.dae)" };
			selectedExportFormat = PlayerPrefs.GetString("exportFormat", exportFormats[0]);
			PropertiesPanel.Instance.AddActionDropdown(exportFormats, (action) =>
			{
				selectedExportFormat = action;
				PlayerPrefs.SetString("exportFormat", action);

			}, PlayerPrefs.GetString("exportFormat", exportFormats[0]));

			PropertiesPanel.Instance.AddLabel("Pas Op! bij een selectie van meer dan 16 tegels is het mogelijk dat uw browser niet genoeg geheugen heeft en crasht");

			PropertiesPanel.Instance.AddActionButtonBig("Downloaden", (action) =>
			{
				List<LayerSystem.Layer> selectedLayers = new List<LayerSystem.Layer>();
				for (int i = 0; i < selectableLayers.Count; i++)
				{
					if (exportLayerToggles[i])
					{
						selectedLayers.Add(selectableLayers[i]);
					}
				}
				print(selectedExportFormat);
				switch (selectedExportFormat)
                {
					case "AutoCAD DXF (.dxf)":
						Debug.Log("Start building DXF");
						GetComponent<DXFCreation>().CreateDXF(scaleBlock.GetComponent<MeshRenderer>().bounds, selectedLayers);
						break;
					case "Collada DAE (.dae)":
						Debug.Log("Start building collada");
						GetComponent<ColladaCreation>().CreateCollada(scaleBlock.GetComponent<MeshRenderer>().bounds,selectedLayers);
						break;
					default:
						WarningDialogs.Instance.ShowNewDialog("Exporteer " + selectedExportFormat + " nog niet geactiveerd.");
                        break;
                }
			});
		}

		public void OnValidate()
		{
			SetGridSize();
		}

	}
}