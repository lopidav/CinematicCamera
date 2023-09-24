using HarmonyLib;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CinematicCameraNS
{
    public class CinematicCamera : Mod
    {
		
		public static CinematicCamera? _instance;

		public void Awake()
		{
			_instance = this;		
			try
			{
				//HarmonyInstance = new Harmony("CinematicCamera");
				Harmony.PatchAll(typeof(CinematicCamera));
			}
			catch(Exception e)
			{
				Logger.Log("Patching failed: " + e.Message);
			}
		}

		override public void Ready()
		{
		}
		public static void Log(string s)
		{
			if (_instance != null) _instance.Logger.Log(s);
		}
		private void OnDestroy()
        {
            Harmony.UnpatchSelf();
        }

		public static ModalScreen CameraMenu;
		public static CustomButton disableGUIButton;
		private static bool _disableGUI = false;
		public static bool DisableGUI
		{
			get {return _disableGUI;}
			set
			{
				if (_disableGUI == value) return;
				_disableGUI = value;
				SetActiveGameScreenGUI(!_disableGUI);
			}
		}

		private static bool _disablePauseVolume = false;
		public static bool DisablePauseVolume
		{
			get {return _disablePauseVolume;}
			set
			{
				if (_disablePauseVolume == value) return;
				_disablePauseVolume = value;
				GameCamera.instance.PauseVolume.weight = _disablePauseVolume ? 0f : 1f;
				PerformanceHelper.SetActive(GameScreen.instance.PausedText.gameObject, !_disablePauseVolume);
			}
		}

		private static bool _EnforcePauseVolume = false;
		private static bool _EnforceFocusVolume = false;
		private static bool _LimitlessZoom = false;
		private static float targetFOV = 32f;
		private static bool _FreeMoveCamera = false;
		private static bool lockingOntoLast = false;
		private static GameCard? lockedOntoCard = null;
		private static bool _SlowerMovement = false;
		public static bool SlowerMovement
		{
			get {return _SlowerMovement;}
			set
			{
				if (_SlowerMovement == value) return;
				if (value)
				{
					GameCamera.instance.MoveSpeed = 0.1f;
				}
				else
				{
					GameCamera.instance.MoveSpeed = 1f;
				}
				_SlowerMovement = value;
			}
		}
		private static bool _SlowerZoom = false;
		public static bool SlowerZoom
		{
			get {return _SlowerZoom;}
			set
			{
				if (_SlowerZoom == value) return;
				if (value)
				{
					GameCamera.instance.ZoomSpeed = 0.1f;
				}
				else
				{
					GameCamera.instance.ZoomSpeed = 1f;
				}
				_SlowerZoom = value;
			}
		}
		public static Action onFreeMoveCameraEnd;
		public static bool FreeMoveCamera
		{
			get {return _FreeMoveCamera;}
			set
			{
				if (_FreeMoveCamera == value) return;
				if (value)
				{
					PreviosMousePosition = InputController.instance.ClampedMousePosition();
				}
				else if (onFreeMoveCameraEnd != null)
				{
					onFreeMoveCameraEnd.Invoke();
				}
				_FreeMoveCamera = value;
			}
		}
		private static Vector2 PreviosMousePosition = new Vector2();
		private static bool _FOVZoom = false;
		private static bool _DisableCursorVisuals = false;
		private static bool _DisableCameraBounds = false;
		private float targetRotateY = 0;
		private float targetRotateX = 0;

		public void Update()
		{
			if (FreeMoveCamera)
			{
				if (InputController.instance.GetLeftMouseEnded())
				{
					FreeMoveCamera = false;
				}
				else
				{
					Vector2 mouseVelocity = InputController.instance.ClampedMousePosition() - PreviosMousePosition;
					Log(mouseVelocity.ToString());
					targetRotateY += -mouseVelocity.y * 0.3f;
					targetRotateX += mouseVelocity.x * 0.3f;
					GameCamera.instance.transform.Rotate( Mathf.Lerp(0, targetRotateY, 0.1f), Mathf.Lerp(0, targetRotateX, 0.1f), 0);
					targetRotateY -= Mathf.Lerp(0, targetRotateY, 0.1f);
					targetRotateX -= Mathf.Lerp(0, targetRotateX, 0.1f);
					PreviosMousePosition = InputController.instance.ClampedMousePosition();
				}
			}
		}

		[HarmonyPatch(typeof(WorldManager), "Update")]
		[HarmonyPostfix]
		public static void WorldManager_Update_Postfix(WorldManager __instance)
		{

			if ((InputController.instance.GetKey(UnityEngine.InputSystem.Key.LeftShift)
					|| InputController.instance.GetKey(UnityEngine.InputSystem.Key.RightShift))
				&& InputController.instance.GetKey(UnityEngine.InputSystem.Key.X))
			{
				OpenMenu();
			}
			if (InputController.instance.GetKeyDown(UnityEngine.InputSystem.Key.F5))
			{
				DisableGUI = !DisableGUI;
			}
			// if (!Cursor.visible && _DisableCursorVisuals && (GameCanvas.instance.ModalIsOpen || WorldManager.instance.CurrentGameState != WorldManager.GameState.Playing))
			// {
			// 	Cursor.visible = true;
			// }
			// else 
			if (_DisableCursorVisuals && (!GameCanvas.instance.ModalIsOpen || !ModalScreen.instance.transform.Find("Modal").gameObject.activeSelf) && WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing && Cursor.visible)
			{
				Cursor.visible = false;
			}
		}
		
		public static void OpenMenu()
		{
			if(!WorldManager.instance.IsPlaying)
				return;
			if (GameCanvas.instance.ModalIsOpen)
			{
				return;
			}
			if (!CameraMenu)
			{
				InstantiateCameraMenu();
			}
			GameCanvas.instance.OpenModal();
		}
		public static void CloseMenu()
		{
			GameCanvas.instance.CloseModal();
		}
		public static void ApplyCameraSettings()
		{

		}
		public static void InstantiateCameraMenu()
		{
			ModalScreen.instance.Clear();
			CameraMenu = ModalScreen.instance;
			CameraMenu.SetTexts("Camera settings", "Options for cinematic camera mod:");
			disableGUIButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			disableGUIButton.transform.SetParent(CameraMenu.ButtonParent);
			disableGUIButton.transform.localPosition = Vector3.zero;
			disableGUIButton.transform.localScale = Vector3.one;
			disableGUIButton.transform.localRotation = Quaternion.identity;
			disableGUIButton.TextMeshPro.text = $"Click to {(DisableGUI ? "Anable" : "Disable")} GUI";
			disableGUIButton.Clicked += delegate
			{
				DisableGUI = !DisableGUI;
				disableGUIButton.TextMeshPro.text = $"Click to {(DisableGUI ? "Anable" : "Disable")} GUI";
			};

			CustomButton disablePauseVolumeButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			disablePauseVolumeButton.transform.SetParent(CameraMenu.ButtonParent);
			disablePauseVolumeButton.transform.localPosition = Vector3.zero;
			disablePauseVolumeButton.transform.localScale = Vector3.one;
			disablePauseVolumeButton.transform.localRotation = Quaternion.identity;
			disablePauseVolumeButton.TextMeshPro.text = $"Click to {(DisablePauseVolume ? "Anable" : "Disable")} pause overlay";
			disablePauseVolumeButton.Clicked += delegate
			{
				DisablePauseVolume = !DisablePauseVolume;
				disablePauseVolumeButton.TextMeshPro.text = $"Click to {(DisablePauseVolume ? "Anable" : "Disable")} pause overlay";
			};

			CustomButton disableCursorVisualsButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			disableCursorVisualsButton.transform.SetParent(CameraMenu.ButtonParent);
			disableCursorVisualsButton.transform.localPosition = Vector3.zero;
			disableCursorVisualsButton.transform.localScale = Vector3.one;
			disableCursorVisualsButton.transform.localRotation = Quaternion.identity;
				disableCursorVisualsButton.TextMeshPro.text = $"Click to {(_DisableCursorVisuals ? "Anable" : "Disable")} cursor visuals (after this window closed)";
			disableCursorVisualsButton.Clicked += delegate
			{
				_DisableCursorVisuals = !_DisableCursorVisuals;
				disableCursorVisualsButton.TextMeshPro.text = $"Click to {(_DisableCursorVisuals ? "Anable" : "Disable")} cursor visuals (after this window closed)";
			};

			CustomButton DisableBounds = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			DisableBounds.transform.SetParent(CameraMenu.ButtonParent);
			DisableBounds.transform.localPosition = Vector3.zero;
			DisableBounds.transform.localScale = Vector3.one;
			DisableBounds.transform.localRotation = Quaternion.identity;
			DisableBounds.TextMeshPro.text = $"Click to {(_DisableCameraBounds ? "Anable" : "Disable")} camera bounds";
			DisableBounds.Clicked += delegate
			{
				_DisableCameraBounds = !_DisableCameraBounds;//0.324
				DisableBounds.TextMeshPro.text = $"Click to {(_DisableCameraBounds ? "Anable" : "Disable")} camera bounds";
			};
			
			CustomButton LimitlessZoom = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			LimitlessZoom.transform.SetParent(CameraMenu.ButtonParent);
			LimitlessZoom.transform.localPosition = Vector3.zero;
			LimitlessZoom.transform.localScale = Vector3.one;
			LimitlessZoom.transform.localRotation = Quaternion.identity;
			LimitlessZoom.TextMeshPro.text = $"Click to {(_LimitlessZoom ? "Disable" : "Anable")} limitless zoom";// $"Switch to {(DisablePauseVolume ? "Y-position" : "FOV")} zoom";
			LimitlessZoom.Clicked += delegate
			{
				_LimitlessZoom = !_LimitlessZoom;//0.324
				LimitlessZoom.TextMeshPro.text = $"Click to {(_LimitlessZoom ? "Disable" : "Anable")} limitless zoom";
			};

			CustomButton FOVZoom = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			FOVZoom.transform.SetParent(CameraMenu.ButtonParent);
			FOVZoom.transform.localPosition = Vector3.zero;
			FOVZoom.transform.localScale = Vector3.one;
			FOVZoom.transform.localRotation = Quaternion.identity;
			FOVZoom.TextMeshPro.text = $"Switch to {(_FOVZoom ? "Y-position" : "FOV")} zoom";
			FOVZoom.Clicked += delegate
			{
				_FOVZoom = !_FOVZoom;//0.324
				FOVZoom.TextMeshPro.text = $"Switch to {(_FOVZoom ? "Y-position" : "FOV")} zoom";
			};
			CameraMenu.AddOption("Reset FOV", delegate {targetFOV = 32f;GameCamera.instance.gameObject.GetComponent<UnityEngine.Camera>().fieldOfView = 32f;});


			CustomButton FreeMoveCameraButton = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			FreeMoveCameraButton.transform.SetParent(CameraMenu.ButtonParent);
			FreeMoveCameraButton.transform.localPosition = Vector3.zero;
			FreeMoveCameraButton.transform.localScale = Vector3.one;
			FreeMoveCameraButton.transform.localRotation = Quaternion.identity;
			FreeMoveCameraButton.TextMeshPro.text = $"Change the camera angle [start dragging this button]";
			FreeMoveCameraButton.StartDragging += delegate
			{
				ModalScreen.instance.transform.Find("Modal").gameObject.SetActive(false);
				ModalScreen.instance.transform.Find("FadeImage").gameObject.SetActive(false);
				FreeMoveCamera = true;
				Log("started dragg");
			};
			onFreeMoveCameraEnd = delegate
			{
				ModalScreen.instance.transform.Find("Modal").gameObject.SetActive(true);
				ModalScreen.instance.transform.parent.GetChild(0).gameObject.SetActive(true);
				Log("end dragg");
			};
			CameraMenu.AddOption("Reset angle", delegate {GameCamera.instance.transform.rotation = Quaternion.Euler(80f, 0f ,0f);});

			CustomButton SlowerMovementBtn = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			SlowerMovementBtn.transform.SetParent(CameraMenu.ButtonParent);
			SlowerMovementBtn.transform.localPosition = Vector3.zero;
			SlowerMovementBtn.transform.localScale = Vector3.one;
			SlowerMovementBtn.transform.localRotation = Quaternion.identity;
			SlowerMovementBtn.TextMeshPro.text = $"Switch to {(!SlowerMovement ? "Slower" : "Faster")} move speed";
			SlowerMovementBtn.Clicked += delegate
			{
				SlowerMovement = !SlowerMovement;//0.324
				SlowerMovementBtn.TextMeshPro.text = $"Switch to {(!SlowerMovement ? "Slower" : "Faster")} move speed";
			};
			CustomButton SlowerZoomBtn = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			SlowerZoomBtn.transform.SetParent(CameraMenu.ButtonParent);
			SlowerZoomBtn.transform.localPosition = Vector3.zero;
			SlowerZoomBtn.transform.localScale = Vector3.one;
			SlowerZoomBtn.transform.localRotation = Quaternion.identity;
			SlowerZoomBtn.TextMeshPro.text = $"Switch to {(!SlowerZoom ? "Slower" : "Faster")} zoom speed";
			SlowerZoomBtn.Clicked += delegate
			{
				SlowerZoom = !SlowerZoom;//0.324
				SlowerZoomBtn.TextMeshPro.text = $"Switch to {(!SlowerZoom ? "Slower" : "Faster")} zoom speed";
			};

			CustomButton lockOnACard = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab);
			lockOnACard.transform.SetParent(CameraMenu.ButtonParent);
			lockOnACard.transform.localPosition = Vector3.zero;
			lockOnACard.transform.localScale = Vector3.one;
			lockOnACard.transform.localRotation = Quaternion.identity;
			lockOnACard.TextMeshPro.text = $"Lock on the last made card";
			lockOnACard.Clicked += delegate
			{
				lockingOntoLast = !lockingOntoLast;
				lockedOntoCard = lockingOntoLast ? WorldManager.instance.AllCards.LastOrDefault() : null;
				lockOnACard.TextMeshPro.text = !lockingOntoLast ? $"Lock on the last made card" : $"Unlock from a card";
			};

			

			//GameObject field =  UnityEngine.Object.Instantiate(DebugScreen.instance.CardRect.gameObject.GetComponentInChildren<TMPro.TMP_InputField>().gameObject, CameraMenu.ButtonParent);
			//field.GetComponent<RectTransform>().sizeDelta = new Vector2(field.GetComponent<RectTransform>().sizeDelta.x * 20f, field.GetComponent<RectTransform>().sizeDelta.y); 
			//CameraMenu.AddOption("Apply", delegate {ApplyCameraSettings();});
			CameraMenu.AddOption("Close menu", delegate {CloseMenu();});
			//CameraMenu.AddOption("Apply and close", delegate {ApplyCameraSettings();CloseMenu();});
		}
		
		[HarmonyPatch(typeof(GameCamera), "Update")]
		[HarmonyPostfix]
		public static void GameCamera_Update_Postfix(GameCamera __instance)
		{
			if (WorldManager.instance.IsPlaying && DisablePauseVolume)
			{
				GameCamera.instance.PauseVolume.weight = 0f;
			}
			if (WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing)
			{
				GameCamera.instance.MinZoom = _LimitlessZoom ? 0f : 3f;
				GameCamera.instance.MaxZoom = _LimitlessZoom ? float.MaxValue : 11f;
			}
		}
		private static bool wasDebugActive = false;
		public static void SetActiveGameScreenGUI(bool active)
		{
			GameScreen gameScreen = GameScreen.instance;
			wasDebugActive = gameScreen.SideTransform.gameObject.activeSelf ? gameScreen.DebugScreen.gameObject.activeSelf : wasDebugActive;
			PerformanceHelper.SetActive(gameScreen.DebugScreen.gameObject, active && wasDebugActive);
			PerformanceHelper.SetActive(gameScreen.InfoBox.gameObject, active);
			PerformanceHelper.SetActive(gameScreen.SideTransform.gameObject, active);
			PerformanceHelper.SetActive(gameScreen.QuestsParent.gameObject, active);
			PerformanceHelper.SetActive(gameScreen.FoodCardBox.gameObject, active);
			PerformanceHelper.SetActive(gameScreen.ShowInfoBoxTime.gameObject, active);
		}


		[HarmonyPatch(typeof(GameScreen), "Update")]
		[HarmonyPostfix]
		public static void GameScreen_Update_Postfix(GameScreen __instance)
		{
			if (DisableGUI)
			{
				SetActiveGameScreenGUI(false);
			}
			if (DisablePauseVolume)
			{
				PerformanceHelper.SetActive(GameScreen.instance.PausedText.gameObject, false);
			}
		}

		[HarmonyPatch(typeof(GameCamera), "Update")]
		[HarmonyPrefix]
		public static void GameCamera_Update_Prefix(GameCamera __instance, ref Vector3 ___cameraTargetPosition, out Vector3 __state)
		{
			__state = ___cameraTargetPosition;
		}
		[HarmonyPatch(typeof(GameCamera), "Update")]
		[HarmonyPostfix]
		public static void GameCamera_Update_Postfix(GameCamera __instance, ref Vector3 ___cameraTargetPosition, Vector3 __state)
		{
			if (!_FOVZoom) return;

			UnityEngine.Camera camera = __instance.gameObject.GetComponent<UnityEngine.Camera>();
			float zoomPower = ___cameraTargetPosition.y - __state.y;
			zoomPower *= Mathf.Min(32f/targetFOV, 32f*targetFOV);

			if (zoomPower > 0f)
			{
				targetFOV *= Mathf.Pow(10f/8f, zoomPower);
			}
			else if (zoomPower < 0f)
			{
				zoomPower *= -1f;
				targetFOV *= Mathf.Pow(8f/10f, zoomPower);
			}

			if (float.IsNaN(targetFOV) || targetFOV <= 0)
			{
				targetFOV = 32f;
			}
		
			Vector3 position = __instance.transform.position;
			position.y = __state.y;
			__instance.transform.position = position;
			___cameraTargetPosition.y = __state.y;

			Vector3 previousCursorPosition = __instance.ScreenPosToWorldPos(InputController.instance.ClampedMousePosition());
			camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, targetFOV, Time.deltaTime * 4f);
			Vector3 currentCursorPosition = __instance.ScreenPosToWorldPos(InputController.instance.ClampedMousePosition());
			
			
			if (!InputController.instance.CurrentSchemeIsController)
			{
				Vector3 cursorShift = previousCursorPosition - currentCursorPosition;
				cursorShift.y = 0f;
				___cameraTargetPosition = ___cameraTargetPosition + cursorShift;
				//__instance.transform.position = ___cameraTargetPosition;
			}
				
				

			
		}

		
		[HarmonyPatch(typeof(GameCamera), "ClampPos")]
		[HarmonyPrefix]
		private static bool GameCamera__ClampPos__Prefix(Vector3 p, ref Vector3 __result)
		{
			if (!_DisableCameraBounds)
				return true;

			__result = p;
			return false;
		}
		
		[HarmonyPatch(typeof(WorldManager), "Update")]
		[HarmonyPrefix]
		private static void WorldManager_Update_Prefix(out Vector3? __state)
		{
			if (lockingOntoLast) {
				if (lockedOntoCard == null || lockedOntoCard.Destroyed)
				{
					lockedOntoCard = WorldManager.instance.AllCards.LastOrDefault();
				}
				// else
				// {
				// 	var lastOne = WorldManager.instance.AllCards.LastOrDefault();
				// 	if (lastOne != null && Vector3.Distance(lastOne.Position, lockedOntoCard.Position) < 0.2f)
				// 	lockedOntoCard = lastOne;
				// }
				__state = lockedOntoCard.transform.position*1;
			} else __state = null;
		}
		[HarmonyPatch(typeof(WorldManager), "Update")]
		[HarmonyPostfix]
		private static void WorldManager_Update_Postfix(Vector3? __state)
		{
			if (lockingOntoLast) {
				if (lockedOntoCard == null || lockedOntoCard.Destroyed)
				{
					lockedOntoCard = WorldManager.instance.AllCards.LastOrDefault();
				}
				// else
				// {
				// 	var lastOne = WorldManager.instance.AllCards.LastOrDefault();
				// 	if (lastOne != null && Vector3.Distance(lastOne.Position, lockedOntoCard.Position) < 0.2f)
				// 	lockedOntoCard = lastOne;
				// }
				if (lockedOntoCard != null && __state != null)
				{
					GameCamera.instance.cameraTargetPosition += (Vector3)(lockedOntoCard.Position - __state);
					// CinematicCamera.Log((lockedOntoCard.Position - __state).ToString());
				}
				// if ()
			}
		}
		[HarmonyPatch(typeof(WorldManager), nameof(WorldManager.CreateCard)
		, new Type[]
		{
			typeof(Vector3)
			, typeof(CardData)
			, typeof(bool)
			, typeof(bool)
			, typeof(bool)
			, typeof(bool)
		})]
		private static void WorldManager_CreateCard_Postfix(Vector3 position, CardData __result)
		{
			if (lockingOntoLast && __result.MyGameCard != null)
			{
				if (lockedOntoCard != null && (Vector3.Distance(position, lockedOntoCard.Position) < 1f || Vector3.Distance(position, lockedOntoCard.GetRootCard().Position) < 1f))
				{
					lockedOntoCard = __result.MyGameCard;
				}

			}
		}
	}
	
}