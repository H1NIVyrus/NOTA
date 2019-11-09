﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlmostEngine.Screenshot
{
	[System.Serializable]
	public class ScreenshotConfig
	{
		public ScreenshotConfig ()
		{
			InitGameViewResolution ();
		}

		#region DESTINATION


		public ScreenshotNameParser.DestinationFolder m_DestinationFolder = ScreenshotNameParser.DestinationFolder.PICTURES_FOLDER;
		public string m_RelativePath = "Screenshots/";
		public string m_RootedPath = "";

		public string GetPath ()
		{
			string path = m_DestinationFolder == ScreenshotNameParser.DestinationFolder.CUSTOM_FOLDER ? m_RootedPath : m_RelativePath;
			return ScreenshotNameParser.ParsePath (m_DestinationFolder, path);
		}

		#endregion

		#region NAME

		public string m_FileName = "{width}x{height}-screenshot";

		[Tooltip ("Override files or increment automatically the filenames.")]
		public bool m_OverrideFiles = false;

		[Tooltip ("Use PNG to create screenshots with a transparent background.")]
		public TextureExporter.ImageFileFormat m_FileFormat;

		public float m_JPGQuality = 75f;

		#endregion

		#region CAPTURE MODES

		
		public ScreenshotTaker.CaptureMode m_CaptureMode = ScreenshotTaker.CaptureMode.FIXED_GAMEVIEW;

		public enum AntiAliasing
		{
			NONE = 0,
			TWO = 2,
			FOUR = 4,
			EIGHT = 8}

		;

		public AntiAliasing m_MultisamplingAntiAliasing = AntiAliasing.EIGHT;

		[Tooltip ("RGB is the default color format.\n" +
		"Use RGBA to create screenshots with an alpha layer, enabling transparent backgrounds.")]
		public ScreenshotTaker.ColorFormat m_ColorFormat;

		[Tooltip ("Force alpha layer to be recomputed. This is a costly process. " +
		"Use only if you have alpha problems in RGBA mode.")]
		public bool m_RecomputeAlphaLayer = false;

		public enum ShotMode
		{
			ONE_SHOT,
			BURST}
		;

		public ShotMode m_ShotMode;
		public int m_MaxBurstShotsNumber = 20;
		public float m_ShotTimeStep = 0.25f;

		#endregion

		#region CAMERAS

		public enum CamerasMode
		{
			GAME_VIEW,
			CUSTOM_CAMERAS}
		;

		[Tooltip ("GAME_VIEW will capture what you see on the screen.\n" +
		"CUSTOM_CAMERAS allows you to customize the cameras to be used, and their rendering properties.")]
		public CamerasMode m_CameraMode;


		[Tooltip ("When enabled, one screenshot is taken for each camera, to be used as independant layers for compositing.")]
		public bool m_ExportToDifferentLayers = false;

		public List<ScreenshotCamera> m_Cameras = new List<ScreenshotCamera> ();

		public List<ScreenshotCamera> GetActiveCameras ()
		{
			List<ScreenshotCamera> cameras = new List<ScreenshotCamera> ();

			if (m_CameraMode == ScreenshotConfig.CamerasMode.CUSTOM_CAMERAS) {
				foreach (ScreenshotCamera camera in m_Cameras) {

					// Ignore inactive ones
					if (camera.m_Active == false)
						continue;

					// Ignore invalid ones
					if (camera.m_Camera == null)
						continue;

					cameras.Add (camera);
				}
			}

			return cameras;
		}

		public void AlignToView ()
		{
			#if UNITY_EDITOR
			if (SceneView.lastActiveSceneView != null) {
				if (m_CameraMode == ScreenshotConfig.CamerasMode.CUSTOM_CAMERAS) {
					foreach (ScreenshotCamera camera in GetActiveCameras()) {
						Undo.RecordObject (camera.m_Camera.transform, "Changed Camera position");
						camera.m_Camera.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
						camera.m_Camera.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
					}
				} else {
					Undo.RecordObject (Camera.main.transform, "Changed Camera position");
					Camera.main.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
					Camera.main.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
				}
			}
			#endif
		}

		#endregion

		#region RESOLUTIONS

		public enum ResolutionMode
		{
			GAME_VIEW,
			CUSTOM_RESOLUTIONS}
		;

		[Tooltip ("GAME_VIEW will capture what you see on the screen.\n" +
		"CUSTOM_RESOLUTIONS allows you to customize the resolutions to be used.")]
		public ResolutionMode m_ResolutionCaptureMode = ResolutionMode.CUSTOM_RESOLUTIONS;

		public List<ScreenshotResolution> m_Resolutions = new List<ScreenshotResolution> ();

		public ScreenshotResolution m_GameViewResolution;

		public ScreenshotResolution GetFirstActiveResolution ()
		{
			var resolutions = GetActiveResolutions ();
			if (resolutions.Count > 0) {
				return resolutions [0];
			} 
			return m_GameViewResolution;
		}

		public List<ScreenshotResolution> GetActiveResolutions ()
		{
			List<ScreenshotResolution> resolutions = new List<ScreenshotResolution> ();

			if (m_CaptureMode != ScreenshotTaker.CaptureMode.FIXED_GAMEVIEW &&
			    m_ResolutionCaptureMode == ScreenshotConfig.ResolutionMode.CUSTOM_RESOLUTIONS) {
				foreach (ScreenshotResolution resolution in m_Resolutions) {

					// Ignore inactive ones
					if (resolution.m_Active == false)
						continue;

					// Ignore invalid ones
					if (!resolution.IsValid ())
						continue;

					resolutions.Add (resolution);
				}
			}

			if (m_ResolutionCaptureMode == ScreenshotConfig.ResolutionMode.GAME_VIEW
			    || m_CaptureMode == ScreenshotTaker.CaptureMode.FIXED_GAMEVIEW) {
				resolutions.Add (m_GameViewResolution);
			} 

			return resolutions;
		}

		public void UpdateGameviewResolution ()
		{
			// Update the gameview screenshot resolution
			Vector2 size = GameViewController.GetCurrentGameViewSize ();

			if (m_GameViewResolution == null) {
				InitGameViewResolution ();
			}

			m_GameViewResolution.m_Width = (int)size.x;
			m_GameViewResolution.m_Height = (int)size.y;
		}

		protected void InitGameViewResolution ()
		{
			m_GameViewResolution = new ScreenshotResolution ();
			m_GameViewResolution.m_Active = true;
			m_GameViewResolution.m_ResolutionName = "GameView";
			m_GameViewResolution.m_Width = Screen.width;
			m_GameViewResolution.m_Height = Screen.height;
			m_GameViewResolution.m_Scale = 1;
		}

		public void UpdateResolutionFilenames (List<ScreenshotResolution> resolutions)
		{
			foreach (ScreenshotResolution resolution in resolutions) {
				UpdateFileName (resolution);
			}
		}

		public string ParseFileName (ScreenshotResolution resolution)
		{
			string path = m_DestinationFolder == ScreenshotNameParser.DestinationFolder.CUSTOM_FOLDER ? m_RootedPath : m_RelativePath;
			return ScreenshotNameParser.ParseFileName (m_FileName, resolution, m_DestinationFolder, path, m_FileFormat, m_OverrideFiles);
		}

		public void UpdateFileName (ScreenshotResolution resolution)
		{
			resolution.m_FileName = ParseFileName (resolution);
		}

		public void UpdateRatios ()
		{
			foreach (ScreenshotResolution res in m_Resolutions) {
				res.UpdateRatio ();
			}
		}

		public void SetAllPortait ()
		{
			foreach (ScreenshotResolution res in m_Resolutions) {
				res.m_Orientation = ScreenshotResolution.Orientation.PORTRAIT;
			}
		}

		public void SetAllLandscape ()
		{
			foreach (ScreenshotResolution res in m_Resolutions) {
				res.m_Orientation = ScreenshotResolution.Orientation.LANDSCAPE;
			}
		}

		public void SelectAllResolutions ()
		{
			foreach (ScreenshotResolution res in m_Resolutions) {
				res.m_Active = true;
			}
		}

		public void ClearAllResolutions ()
		{
			foreach (ScreenshotResolution res in m_Resolutions) {
				res.m_Active = false;
			}
		}

		public void RemoveAllResolutions ()
		{
			m_Resolutions.Clear ();
		}

		#endregion

		#region OVERLAYS

		[Tooltip ("Capture or not the active UI Canvas.")]
		public bool m_CaptureActiveUICanvas = true;
		public List<ScreenshotOverlay> m_Overlays = new List<ScreenshotOverlay> ();

		#endregion


		#region COMPOSITION

		public enum CompositionMode
		{
			SIMPLE_CAPTURE,
			COMPOSITION}
		;

		public CompositionMode m_CompositionMode = CompositionMode.SIMPLE_CAPTURE;

		public List<ScreenshotComposer> m_Composers = new List<ScreenshotComposer> ();

		public List<ScreenshotComposer> GetActiveComposers ()
		{
			return m_Composers.Where (x => x.m_Active == true).ToList<ScreenshotComposer> ();
		}

		#endregion

		#region BATCHES

		public enum BatchMode
		{
			SIMPLE_CAPTURE,
			BATCHES}
		;

		public BatchMode m_BatchMode = BatchMode.SIMPLE_CAPTURE;

		public List<ScreenshotBatch> m_Batches = new List<ScreenshotBatch> ();

		public List<ScreenshotBatch> GetActiveBatches ()
		{
			return m_Batches.Where (x => x.m_Active == true).ToList<ScreenshotBatch> ();
		}


		#endregion


		#region PREVIEW

		public bool m_ShowGuidesInPreview = false;
		public Canvas m_GuideCanvas;
		public Color m_GuidesColor = Color.white;
		public bool m_ShowPreview = true;
		public float m_PreviewSize = 1f;
		
		[Tooltip ("If set to true, the camera and overlay settings will be applied when the application starts playing.")]
		public bool m_PreviewInGameViewWhilePlaying = false;

		#endregion

		#region UTILS

		public bool m_StopTimeOnCapture = true;
		public bool m_PlaySoundOnCapture = true;
		public AudioClip m_ShotSound;

		[System.NonSerialized]
		public float m_Time = 1f;

		public void SetTime (float time)
		{
			if (time != m_Time) {
				m_Time = time;
				Time.timeScale = time;
			}
		}

		public void TogglePause ()
		{
			if (m_Time == 0f) {
				SetTime (1f);
			} else {
				SetTime (0f);
			}
		}

		public void ClearCache ()
		{
			m_GameViewResolution.m_Texture = null;
			foreach (ScreenshotResolution res in m_Resolutions) {
				res.m_Texture = null;
			}
		}


		public void ExportAllToFiles ()
		{
			ExportToFiles (GetActiveResolutions ());
		}

		public void ExportToFiles (List<ScreenshotResolution> resolutions)
		{
			foreach (ScreenshotResolution resolution in resolutions) {
				UpdateFileName (resolution);
				if (TextureExporter.ExportToFile (resolution.m_Texture, resolution.m_FileName, m_FileFormat, (int)m_JPGQuality)) {
					Debug.Log ("Image exported : " + resolution.m_FileName);
				}
			}
		}

		#endregion

		#region HOTKEYS

		public HotKey m_CaptureHotkey = new HotKey (false, false, false, KeyCode.None);
		public HotKey m_UpdatePreviewHotkey = new HotKey (false, false, false, KeyCode.None);
		public HotKey m_AlignHotkey = new HotKey (false, false, false, KeyCode.None);
		public HotKey m_PauseHotkey = new HotKey (false, false, false, KeyCode.None);

		#endregion


		#region ScreenshotTaker

		public ScreenshotTaker.GameViewResizingWaitingMode m_GameViewResizingWaitingMode;
		public float m_ResizingWaitingTime = 1f;
		public int m_ResizingWaitingFrames = 2;

		#endregion


		#region UI

		public bool m_ShowDestination = true;
		public bool m_ShowName = true;
		public bool m_ShowCaptureMode = true;
		public bool m_ShowResolutions = true;
		public bool m_ShowCameras = true;
		public bool m_ShowCanvas = true;
		public bool m_ShowComposition = true;
		public bool m_ShowBatches = true;
		public bool m_ShowPreviewGUI = true;
		public bool m_ShowCapture = true;
		public bool m_ShowHotkeys = true;
		public bool m_ShowGallery = true;
		public bool m_ShowUtils = true;
		public bool m_ShowUsage = true;
		public bool m_ShowDelay = true;

		#endregion


	}
}