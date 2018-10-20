﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime;
using KSP;
using KSP.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace scatterer
{
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]
	public class Core: MonoBehaviour
	{	
		private static Core instance;
		
		private Core()
		{
			if (instance == null)
			{
				instance = this;
				Debug.Log("[Scatterer] Core instance created");
			}
			else
			{
				//destroy any duplicate instances that may be created by a duplicate install
				Debug.Log("[Scatterer] Destroying duplicate instance, check your install for duplicate mod folders");
				UnityEngine.Object.Destroy (this);
			}
		}
		
		public static Core Instance
		{
			get 
			{
				return instance;
			}
		}

		public Rect windowRect = new Rect (0, 0, 400, 50);
		int windowId = UnityEngine.Random.Range(int.MinValue,int.MaxValue);

		GUIhandler GUItool= new GUIhandler();

		PlanetsListReader scattererPlanetsListReader = new PlanetsListReader ();
		public List<SunFlare> customSunFlares = new List<SunFlare>();
		bool customSunFlareAdded=false;
		
		public bool visible = false;

		[Persistent]
		public bool autosavePlanetSettingsOnSceneChange=true;

		[Persistent]
		public bool disableAmbientLight=false;
		
//		[Persistent]
		public string mainSunCelestialBodyName="Sun";
		
		[Persistent]
		public bool integrateWithEVEClouds=false;

		DisableAmbientLight ambientLightScript;
		
		public List <ScattererCelestialBody > scattererCelestialBodies = new List <ScattererCelestialBody> {};

		public List<string> sunflaresList=new List<string> {};

		CelestialBody[] CelestialBodies;
		
		Light[] lights;

		//map EVE 2d cloud materials to planet names
		public Dictionary<String, List<Material> > EVEClouds = new Dictionary<String, List<Material> >();

		//map EVE CloudObjects to planet names
		//as far as I understand CloudObjects in EVE contain the 2d clouds and the volumetrics for a given
		//layer on a given planet, however due to the way they are handled in EVE they don't directly reference
		//their parent planet and the volumetrics are only created when the PQS is active
		//I map them here to facilitate accessing the volumetrics later
		public Dictionary<String, List<object>> EVECloudObjects = new Dictionary<String, List<object>>();

		GameObject sunLight,scaledspaceSunLight;
//		public GameObject copiedScaledSunLight, copiedScaledSunLight2;
//		public GameObject copiedSunLight;

		Cubemap planetShineCookieCubeMap;

		[Persistent]
		Vector2 mainMenuWindowLocation=Vector2.zero;

		[Persistent]
		Vector2 inGameWindowLocation=Vector2.zero;

		[Persistent]
		public bool overrideNearClipPlane=false;

		[Persistent]
		public float nearClipPlane=0.5f;

//		[Persistent]
//		public bool
//			render24bitDepthBuffer = true;

		[Persistent]
		public bool
			forceDisableDefaultDepthBuffer = false;

		[Persistent]
		public bool
			useOceanShaders = true;

		[Persistent]
		public bool
			oceanSkyReflections = true;

		[Persistent]
		public bool
			oceanRefraction = true;

		public int oceanRenderQueue = 2500;

		[Persistent]
		public bool
			oceanPixelLights = false;
		
		[Persistent]
		public bool
			drawAtmoOnTopOfClouds = true;
		
		[Persistent]
		public bool
			oceanCloudShadows = false;
		
		[Persistent]
		public bool
			fullLensFlareReplacement = true;
		
		[Persistent]
		public bool
			showMenuOnStart = true;

		[Persistent]
		public int scrollSectionHeight = 500;
		
		bool callCollector=false;
		

//		[Persistent]
		public bool craft_WaveInteractions = false;
		
		[Persistent]
		public bool
			useGodrays = true;
		
		[Persistent]
		public bool
			useEclipses = true;

		[Persistent]
		public bool
			useRingShadows = true;

		//[Persistent]
		public bool
			usePlanetShine = false;

		List<PlanetShineLightSource> celestialLightSourcesData=new List<PlanetShineLightSource> {};
		
		List<PlanetShineLight> celestialLightSources=new List<PlanetShineLight> {};

		public UrlDir.UrlConfig[] baseConfigs;
		public UrlDir.UrlConfig[] atmoConfigs;
		public UrlDir.UrlConfig[] oceanConfigs;

		public ConfigNode[] sunflareConfigs;

		[Persistent]
		public bool
			terrainShadows = true;

		[Persistent]
		public float shadowNormalBias=0.4f;
		
		[Persistent]
		public float shadowBias=0.125f;
		
		[Persistent]
		public float
			shadowsDistance=100000;
		
		//[Persistent]
		//float godrayResolution = 1f;

		[Persistent]
		string guiModifierKey1String=KeyCode.LeftAlt.ToString();

		[Persistent]
		string guiModifierKey2String=KeyCode.RightAlt.ToString();

		[Persistent]
		string guiKey1String=KeyCode.F10.ToString();
		
		[Persistent]
		string guiKey2String=KeyCode.F11.ToString();

		KeyCode guiKey1, guiKey2, guiModifierKey1, guiModifierKey2 ;

		//means a PQS enabled for the closest celestial body, regardless of whether it uses scatterer effects or not
		bool globalPQSEnabled = false;

		public bool isGlobalPQSEnabled
		{
			get
			{
				return globalPQSEnabled;
			}
		}

		//means a PQS enabled for a celestial body which scatterer effects are active on (is this useless?)
		bool pqsEnabledOnScattererPlanet = false;

		public bool isPQSEnabledOnScattererPlanet
		{
			get
			{
				return pqsEnabledOnScattererPlanet;
			}
		}

		public bool underwater = false;

		public BufferRenderingManager bufferRenderingManager;

		public CelestialBody sunCelestialBody;
		public CelestialBody munCelestialBody;
		public string path, gameDataPath;
		bool found = false;
		public bool extinctionEnabled = true;
		
		public Camera farCamera, scaledSpaceCamera, nearCamera;
	
		public Camera chosenCamera;
		public int layer = 15;

		[Persistent]
		public int m_fourierGridSize = 128; //This is the fourier transform size, must pow2 number. Recommend no higher or lower than 64, 128 or 256.
		
		public bool d3d9 = false;
		public bool opengl = false;
		public bool d3d11 = false;
		public bool isActive = false;
		public bool mainMenu=false;
		string versionNumber = "0.0334dev";

		CommandBuffer heatRefractionCommandBuffer;
		Material heatRefractionmaterial;

		//Material originalMaterial;
		
		public Transform GetScaledTransform (string body)
		{
			return (ScaledSpace.Instance.transform.FindChild (body));	
		}
		

		void Awake ()
		{
			string codeBase = Assembly.GetExecutingAssembly ().CodeBase;
			UriBuilder uri = new UriBuilder (codeBase);
			path = Uri.UnescapeDataString (uri.Path);
			path = Path.GetDirectoryName (path);

			int index = path.LastIndexOf ("GameData");
			gameDataPath= path.Remove(index+9, path.Length-index-9);

			//load the planets list and the settings
			loadSettings ();

			//find all celestial bodies, used for finding scatterer-enabled bodies and disabling the stock ocean
			CelestialBodies = (CelestialBody[])CelestialBody.FindObjectsOfType (typeof(CelestialBody));
			
			if (SystemInfo.graphicsDeviceVersion.StartsWith ("Direct3D 9"))
			{
				d3d9 = true;
			}
			else if (SystemInfo.graphicsDeviceVersion.StartsWith ("OpenGL"))
			{
				opengl = true;
			}
			else if (SystemInfo.graphicsDeviceVersion.StartsWith ("Direct3D 11"))
			{
				d3d11 = true;
			}

			Debug.Log ("[Scatterer] Version:"+versionNumber);
			Debug.Log ("[Scatterer] Detected " + SystemInfo.graphicsDeviceVersion + " on " +SystemInfo.operatingSystem);
			
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				isActive = true;
				windowRect.x=inGameWindowLocation.x;
				windowRect.y=inGameWindowLocation.y;
			} 

			else if (HighLogic.LoadedScene == GameScenes.MAINMENU)
			{
				mainMenu = true;
				visible = showMenuOnStart;
				windowRect.x=mainMenuWindowLocation.x;
				windowRect.y=mainMenuWindowLocation.y;
				
				//find and remove stock oceans
				if (useOceanShaders)
				{
					removeStockOceans();
				}

			}
		}

		void Update ()
		{	
			//toggle whether GUI is visible or not
			if ((Input.GetKey (guiModifierKey1) || Input.GetKey (guiModifierKey2)) && (Input.GetKeyDown (guiKey1) || (Input.GetKeyDown (guiKey2))))
				visible = !visible;

			if (isActive && ScaledSpace.Instance) {
				if (!found)
				{
					//set shadows
					setShadows();

					//find scatterer celestial bodies
					findScattererCelestialBodies();

					//find sun
					sunCelestialBody = CelestialBodies.SingleOrDefault (_cb => _cb.GetName () == mainSunCelestialBodyName);

					//find main cameras
					Camera[] cams = Camera.allCameras;
					for (int i = 0; i < cams.Length; i++)
					{
						if (cams [i].name == "Camera ScaledSpace")
						{
							scaledSpaceCamera = cams [i];
						}
						
						if (cams [i].name == "Camera 01")
						{
							farCamera = cams [i];

						}
						
						if (cams [i].name == "Camera 00")
						{
							nearCamera = cams [i];
							if (overrideNearClipPlane)
							{
								Debug.Log("[Scatterer] Override near clip plane from:"+nearCamera.nearClipPlane.ToString()+" to:"+nearClipPlane.ToString());
								nearCamera.nearClipPlane = nearClipPlane;
							}
							farCamera.nearClipPlane = nearCamera.farClipPlane-0.2f; //fixes small band in the ocean where the two cameras overlap and the transparent ocean is rendered twice
						}
					}
					

					
					//find sunlight and set shadow bias
					lights = (Light[]) Light.FindObjectsOfType(typeof( Light));

					foreach (Light _light in lights)
					{
						if (_light.gameObject.name == "Scaledspace SunLight")
						{
							scaledspaceSunLight=_light.gameObject;
							Debug.Log("Found scaled sunlight");

							_light.shadowNormalBias =shadowNormalBias;
							_light.shadowBias=shadowBias;
						}
						
						if (_light.gameObject.name == "SunLight")
						{
							sunLight=_light.gameObject;
							Debug.Log("Found Sunlight");
						}						
					}

					//load planetshine "cookie" cubemap
					if(usePlanetShine)
					{
						planetShineCookieCubeMap=new Cubemap(512,TextureFormat.ARGB32,true);
						
						Texture2D[] cubeMapFaces=new Texture2D[6];
						for (int i=0;i<6;i++)
						{
							cubeMapFaces[i]=new Texture2D(512,512);
						}
						
						cubeMapFaces[0].LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/planetShineCubemap", "_NegativeX.png")));
						cubeMapFaces[1].LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/planetShineCubemap", "_PositiveX.png")));
						cubeMapFaces[2].LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/planetShineCubemap", "_NegativeY.png")));
						cubeMapFaces[3].LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/planetShineCubemap", "_PositiveY.png")));
						cubeMapFaces[4].LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/planetShineCubemap", "_NegativeZ.png")));
						cubeMapFaces[5].LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/planetShineCubemap", "_PositiveZ.png")));
						
						planetShineCookieCubeMap.SetPixels(cubeMapFaces[0].GetPixels(),CubemapFace.NegativeX);
						planetShineCookieCubeMap.SetPixels(cubeMapFaces[1].GetPixels(),CubemapFace.PositiveX);
						planetShineCookieCubeMap.SetPixels(cubeMapFaces[2].GetPixels(),CubemapFace.NegativeY);
						planetShineCookieCubeMap.SetPixels(cubeMapFaces[3].GetPixels(),CubemapFace.PositiveY);
						planetShineCookieCubeMap.SetPixels(cubeMapFaces[4].GetPixels(),CubemapFace.NegativeZ);
						planetShineCookieCubeMap.SetPixels(cubeMapFaces[5].GetPixels(),CubemapFace.PositiveZ);
						planetShineCookieCubeMap.Apply();
					}

					//find and fix renderQueues of kopernicus rings
					foreach (CelestialBody _cb in CelestialBodies)
					{
						GameObject ringObject;
						ringObject=GameObject.Find(_cb.name+"Ring");
						if (ringObject)
						{
							ringObject.GetComponent < MeshRenderer > ().material.renderQueue = 3005;
							Debug.Log("[Scatterer] Found rings for "+_cb.name);
						}
					}

//					//find and fix renderqueue of sun corona
//					Transform scaledSunTransform=GetScaledTransform(mainSunCelestialBodyName);
//					foreach (Transform child in scaledSunTransform)
//					{
//						MeshRenderer temp = child.gameObject.GetComponent<MeshRenderer>();
//						temp.material.renderQueue = 3000;
//					}

					//set up planetshine lights
					if(usePlanetShine)
					{
						foreach (PlanetShineLightSource _aSource in celestialLightSourcesData)
						{
							var celBody = CelestialBodies.SingleOrDefault (_cb => _cb.bodyName == _aSource.bodyName);
							if (celBody)
							{
								PlanetShineLight aPsLight= new PlanetShineLight();
								aPsLight.isSun=_aSource.isSun;
								aPsLight.source=celBody;
								aPsLight.sunCelestialBody=sunCelestialBody;
								
								GameObject ScaledPlanetShineLight=(UnityEngine.GameObject) Instantiate(scaledspaceSunLight);
								GameObject LocalPlanetShineLight=(UnityEngine.GameObject) Instantiate(scaledspaceSunLight);
								
								ScaledPlanetShineLight.GetComponent<Light>().type=LightType.Point;
								if (!_aSource.isSun)
									ScaledPlanetShineLight.GetComponent<Light>().cookie=planetShineCookieCubeMap;
								
								//ScaledPlanetShineLight.GetComponent<Light>().range=1E9f;
								ScaledPlanetShineLight.GetComponent<Light>().range=_aSource.scaledRange;
								ScaledPlanetShineLight.GetComponent<Light>().color=new Color(_aSource.color.x,_aSource.color.y,_aSource.color.z);
								ScaledPlanetShineLight.name=celBody.name+"PlanetShineLight(ScaledSpace)";
								
								
								LocalPlanetShineLight.GetComponent<Light>().type=LightType.Point;
								if (!_aSource.isSun)
									LocalPlanetShineLight.GetComponent<Light>().cookie=planetShineCookieCubeMap;
								//LocalPlanetShineLight.GetComponent<Light>().range=1E9f;
								LocalPlanetShineLight.GetComponent<Light>().range=_aSource.scaledRange*6000;
								LocalPlanetShineLight.GetComponent<Light>().color=new Color(_aSource.color.x,_aSource.color.y,_aSource.color.z);
								LocalPlanetShineLight.GetComponent<Light>().cullingMask=557591;
								LocalPlanetShineLight.GetComponent<Light>().shadows=LightShadows.Soft;
								LocalPlanetShineLight.GetComponent<Light>().shadowCustomResolution=2048;
								LocalPlanetShineLight.name=celBody.name+"PlanetShineLight(LocalSpace)";
								
								aPsLight.scaledLight=ScaledPlanetShineLight;
								aPsLight.localLight=LocalPlanetShineLight;
								
								celestialLightSources.Add(aPsLight);
								Debug.Log ("[Scatterer] Added celestialLightSource "+aPsLight.source.name);
							}
						}
					}

					//create buffer manager
					if (HighLogic.LoadedScene != GameScenes.TRACKSTATION)
					{
						bufferRenderingManager = (BufferRenderingManager)farCamera.gameObject.AddComponent (typeof(BufferRenderingManager));
						bufferRenderingManager.start();
					}

					//find EVE clouds
					if (integrateWithEVEClouds)
					{
						mapEVEClouds();
					}



//					//add and test cone
//					if (FlightGlobals.ActiveVessel)
//					{
//						GameObject aConeGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//						GameObject.Destroy (aConeGO.GetComponent<Collider> ());
//					
//						aConeGO.transform.localScale = Vector3.one;
//					
//						MeshFilter mf = aConeGO.GetComponent<MeshFilter>();
//						mf.mesh = MeshFactory.MakeCone(0.5f,3f,10f,16);
//
//						mf.mesh.RecalculateBounds();
//						mf.mesh.RecalculateNormals();
//					
//						var mr = aConeGO.GetComponent<MeshRenderer>();
//						//mr.material = material;
//					
//						//			mr.castShadows = false;
//						mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
//						mr.receiveShadows = true;
//						mr.enabled = true;
//
//						//aConeGO.transform.Rotate(new Vector3(90f,0f,0f));
//						aConeGO.transform.localPosition= new Vector3(0f, 0f, 0f);
//						aConeGO.transform.up = FlightGlobals.ActiveVessel.transform.forward;
//						aConeGO.transform.parent = FlightGlobals.ActiveVessel.transform;
//					}


					//add heat refraction copier


					//refraction command buffer
					heatRefractionCommandBuffer = new CommandBuffer();
					heatRefractionCommandBuffer.name = "ScattererHeatGrabScreen";
					
					// copy screen
					heatRefractionCommandBuffer.Blit (BuiltinRenderTextureType.CurrentActive, Core.Instance.bufferRenderingManager.refractionTexture);
					heatRefractionCommandBuffer.SetGlobalTexture("_refractionTexture", Core.Instance.bufferRenderingManager.refractionTexture);
					
//					farCamera.AddCommandBuffer  (CameraEvent.AfterForwardAlpha, heatRefractionCommandBuffer);  //this shit is messed up, everything is transparent or fucked
//					nearCamera.AddCommandBuffer (CameraEvent.AfterForwardAlpha, heatRefractionCommandBuffer);  //found this to be after 2500 and before 2501

//					farCamera.AddCommandBuffer  (CameraEvent.AfterForwardOpaque, heatRefractionCommandBuffer); //this works for pretty much everything except my ocean
//					nearCamera.AddCommandBuffer (CameraEvent.AfterForwardOpaque, heatRefractionCommandBuffer); //same, found this to be after 2500 and before 2501, wtf?

					//farCamera.AddCommandBuffer  (CameraEvent.AfterEverything, heatRefractionCommandBuffer); //
					//nearCamera.AddCommandBuffer (CameraEvent.AfterEverything, heatRefractionCommandBuffer); //still working after 2500 and before 2501

					//nearCamera.AddCommandBuffer (CameraEvent.AfterHaloAndLensFlares , heatRefractionCommandBuffer); //still working after 2500 and before 2501

					//nearCamera.AddCommandBuffer (CameraEvent.AfterImageEffectsOpaque , heatRefractionCommandBuffer); //still working after 2500 and before 2501
					farCamera.AddCommandBuffer (CameraEvent.AfterForwardOpaque , heatRefractionCommandBuffer); //still working after 2500 and before 2501
					nearCamera.AddCommandBuffer (CameraEvent.AfterForwardOpaque , heatRefractionCommandBuffer); //still working after 2500 and before 2501

					//load texture and create material					
					Texture2D heatTexture=new Texture2D(1,1);
					heatTexture.LoadImage(System.IO.File.ReadAllBytes (String.Format ("{0}/{1}", path+"/heatRefraction", "heat.png")));

					heatRefractionmaterial = new Material (ShaderReplacer.Instance.LoadedShaders[ ("Scatterer/ParticleRefraction")]);

					heatRefractionmaterial.SetTexture("_heatTexture", heatTexture);
					heatRefractionmaterial.renderQueue = 3099;


					//TODO find particle emitter and replace material first and later add own particle emitter
					if (FlightGlobals.ActiveVessel)
					{
//						List<Part> parts = FlightGlobals.ActiveVessel.parts ;
//						Debug.Log("parts "+parts.Count().ToString());
//						foreach (Part _part in parts)
//						{
//							if (_part.name == "turboJet")
//							{
//								Debug.Log("_part.gameObject.name "+_part.gameObject.name);
//								Component[] comps = _part.gameObject.GetComponentsInChildren(typeof(ModuleEnginesFX)) ;
//								if (comps.Count() == 0)
//								{
//									comps = _part.gameObject.GetComponentsInChildren(typeof(ModuleEngines));
//								}
//								foreach (Component _comp in comps)
//								{
//									Debug.Log("_comp.name "+_comp.name);
//									Debug.Log("_comp.type "+_comp.GetType().ToString());
//
//									Component[] comps2 = _comp.gameObject.GetComponents(typeof(ParticleSystem));
//									if (comps2.Count() == 0)
//									{
//										Debug.Log("0 comps");
//										comps2 = _comp.gameObject.GetComponentsInChildren(typeof(ParticleSystem));
//									}
//									foreach (Component _comp2 in comps2)
//									{
//										Debug.Log("_comp2.name "+_comp2.name);
//										Debug.Log("_comp2.type "+_comp2.GetType().ToString());
//									}
//								}
//
//							}
//						}

//						ParticleSystem[] particleSystems = (ParticleSystem[]) ParticleSystem.FindObjectsOfType(typeof( ParticleSystem));
//						Debug.Log("particleSystems "+particleSystems.Count().ToString());
//						foreach (ParticleSystem _ps in particleSystems)
//						{
//							Debug.Log("name "+_ps.name);
//							Debug.Log("GO name "+_ps.gameObject.name);
//							Debug.Log("parent GO name "+_ps.gameObject.transform.parent.gameObject.name);
//						}
					}


					found = true;
				}			

				if (ScaledSpace.Instance && scaledSpaceCamera)
				{
					if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
					{
						//magically fix stupid issues when reverting to space center from map view
						MapView.MapIsEnabled = false;
					}

					if (callCollector)
					{
						GC.Collect();
						callCollector=false;
					}

					//custom lens flares
					if ((fullLensFlareReplacement) && !customSunFlareAdded)
					{
						//disable stock sun flares
						global::SunFlare[] stockFlares = (global::SunFlare[]) global::SunFlare.FindObjectsOfType(typeof( global::SunFlare));
						foreach(global::SunFlare _flare in stockFlares)
						{

							if (sunflaresList.Contains(_flare.sun.name))
							{
								Debug.Log("[Scatterer] Disabling stock sunflare for "+_flare.sun.name);
								_flare.sunFlare.enabled=false;
							}
						}

						foreach (string sunflareBody in sunflaresList)
						{
							SunFlare customSunFlare =(SunFlare) scaledSpaceCamera.gameObject.AddComponent(typeof(SunFlare));

							try
							{
								customSunFlare.source=CelestialBodies.SingleOrDefault (_cb => _cb.GetName () == sunflareBody);
								customSunFlare.sourceName=sunflareBody;
								customSunFlare.start ();
								customSunFlares.Add(customSunFlare);
							}
							catch (Exception stupid)
							{
								Debug.Log("[Scatterer] Custom sunflare cannot be added to "+sunflareBody+" "+stupid.ToString());
																
								Component.Destroy(customSunFlare);
								UnityEngine.Object.Destroy(customSunFlare);

								if (customSunFlares.Contains(customSunFlare))
								{
									customSunFlares.Remove(customSunFlare);
								}

								continue;
							}
						}
						customSunFlareAdded=true;
					}

					if (disableAmbientLight && !ambientLightScript)
					{
						ambientLightScript = (DisableAmbientLight) scaledSpaceCamera.gameObject.AddComponent (typeof(DisableAmbientLight));
					}

					globalPQSEnabled = false;
					if (FlightGlobals.currentMainBody )
					{
						if (FlightGlobals.currentMainBody.pqsController)
							globalPQSEnabled = FlightGlobals.currentMainBody.pqsController.isActive;
					}

					pqsEnabledOnScattererPlanet = false;
					underwater = false;

					foreach (ScattererCelestialBody _cur in scattererCelestialBodies)
					{
						float dist, shipDist=0f;
						if (_cur.hasTransform)
						{
							dist = Vector3.Distance (ScaledSpace.ScaledToLocalSpace( scaledSpaceCamera.transform.position),
													 ScaledSpace.ScaledToLocalSpace (_cur.transform.position));

							//don't unload planet the player ship is close to if panning away in map view
							if (FlightGlobals.ActiveVessel)
							{
								shipDist = Vector3.Distance (FlightGlobals.ActiveVessel.transform.position,
							                         ScaledSpace.ScaledToLocalSpace (_cur.transform.position));
							}

							if (_cur.active)
							{
								if (dist > _cur.unloadDistance && (shipDist > _cur.unloadDistance || shipDist == 0f )) {

									_cur.m_manager.OnDestroy ();
									UnityEngine.Object.Destroy (_cur.m_manager);
									_cur.m_manager = null;
									_cur.active = false;
									callCollector=true;

									Debug.Log ("[Scatterer] Effects unloaded for " + _cur.celestialBodyName);
								} else {

									_cur.m_manager.Update ();
									{
										if (!_cur.m_manager.m_skyNode.inScaledSpace)
										{
											pqsEnabledOnScattererPlanet = true;
										}

										if (_cur.m_manager.hasOcean && useOceanShaders && pqsEnabledOnScattererPlanet)
										{
											underwater = _cur.m_manager.GetOceanNode().isUnderwater;
										}
									}
								}
							} 
							else
							{
							if (dist < _cur.loadDistance && _cur.transform && _cur.celestialBody)
								{
									try
									{
										_cur.m_manager = new Manager ();
										_cur.m_manager.setParentCelestialBody (_cur.celestialBody);
										_cur.m_manager.setParentPlanetTransform (_cur.transform);
										
										CelestialBody currentSunCelestialBody = CelestialBodies.SingleOrDefault (_cb => _cb.GetName () == _cur.mainSunCelestialBody);
										_cur.m_manager.setSunCelestialBody (currentSunCelestialBody);
										
										//Find eclipse casters
										List<CelestialBody> eclipseCasters=new List<CelestialBody> {};
										
										if (useEclipses)
										{
											for (int k=0; k < _cur.eclipseCasters.Count; k++)
											{
												var cc = CelestialBodies.SingleOrDefault (_cb => _cb.GetName () == _cur.eclipseCasters[k]);
												if (cc==null)
													Debug.Log("[Scatterer] Eclipse caster "+_cur.eclipseCasters[k]+" not found for "+_cur.celestialBodyName);
												else
												{
													eclipseCasters.Add(cc);
													Debug.Log("[Scatterer] Added eclipse caster "+_cur.eclipseCasters[k]+" for "+_cur.celestialBodyName);
												}
											}
											_cur.m_manager.eclipseCasters=eclipseCasters;
										}
										
										List<AtmoPlanetShineSource> planetshineSources=new List<AtmoPlanetShineSource> {};
										
										if (usePlanetShine)
										{								
											for (int k=0; k < _cur.planetshineSources.Count; k++)
											{
												var cc = CelestialBodies.SingleOrDefault (_cb => _cb.GetName () == _cur.planetshineSources[k].bodyName);
												if (cc==null)
													Debug.Log("[Scatterer] planetshine source "+_cur.planetshineSources[k].bodyName+" not found for "+_cur.celestialBodyName);
												else
												{
													AtmoPlanetShineSource src=_cur.planetshineSources[k];
													src.body=cc;
													_cur.planetshineSources[k].body=cc;
													planetshineSources.Add (src);
													Debug.Log("[Scatterer] Added planetshine source"+_cur.planetshineSources[k].bodyName+" for "+_cur.celestialBodyName);
												}
											}
											_cur.m_manager.planetshineSources = planetshineSources;
										}
										
										if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
											_cur.hasOcean=false;

										_cur.m_manager.hasOcean = _cur.hasOcean;
										_cur.m_manager.usesCloudIntegration = _cur.usesCloudIntegration;
										_cur.m_manager.Awake ();
										_cur.active = true;
										
										
										GUItool.selectedConfigPoint = 0;
										GUItool.displayOceanSettings = false;
										GUItool.selectedPlanet = scattererCelestialBodies.IndexOf (_cur);
										GUItool.getSettingsFromSkynode ();
										if (_cur.hasOcean && useOceanShaders) {
											GUItool.getSettingsFromOceanNode ();
										}
										
										callCollector=true;
										Debug.Log ("[Scatterer] Effects loaded for " + _cur.celestialBodyName);
									}
									catch(Exception e)
									{
										Debug.Log ("[Scatterer] Effects couldn't be loaded for " + _cur.celestialBodyName +" because of exception: "+e.ToString());
										try
										{
										_cur.m_manager.OnDestroy();
										}
										catch(Exception ee)
										{
											Debug.Log ("[Scatterer] manager couldn't be removed for " + _cur.celestialBodyName +" because of exception: "+ee.ToString());
										}
										scattererCelestialBodies.Remove(_cur);
										Debug.Log ("[Scatterer] "+ _cur.celestialBodyName +" removed from active planets.");
										return;
									}
								}
							}
						}
					}

					if (bufferRenderingManager)
					{
						if (!bufferRenderingManager.depthTextureCleared && (MapView.MapIsEnabled || !pqsEnabledOnScattererPlanet) )
							bufferRenderingManager.clearDepthTexture();
					}

					//update sun flare
					if (fullLensFlareReplacement)
					{
						foreach (SunFlare customSunFlare in customSunFlares)
						{
							customSunFlare.updateNode();
						}
					}
						
					//update planetshine lights
					if(usePlanetShine)
					{
						foreach (PlanetShineLight _aLight in celestialLightSources)
						{
//							Debug.Log("updating "+_aLight.source.name);
							_aLight.updateLight();

						}
					}


					ParticleSystem[] particleEmitters = (ParticleSystem[]) ParticleSystem.FindObjectsOfType(typeof(ParticleSystem));
					Debug.Log("particleEmitters.Count() " + particleEmitters.Count().ToString());
					foreach (ParticleSystem _pEM in particleEmitters)
					{
						if (_pEM.gameObject)
						{
							Debug.Log("_pEM.gameObject.name "+_pEM.gameObject.name);
						}
						if (_pEM.transform)
						{
							Debug.Log("_pEM.transform.name "+_pEM.transform.name);
						}
						if (_pEM.transform.parent)
						{
							Debug.Log("_pEM.transform.parent.name "+_pEM.transform.parent.name);

							if (_pEM.transform.parent.parent)
							{
								Debug.Log("_pEM.transform.parent.parent.name "+_pEM.transform.parent.parent.name);

								if (_pEM.transform.parent.name.Contains("afterburner"))
								{
									ParticleSystemRenderer _pr =  _pEM.GetComponent<ParticleSystemRenderer>();
									if (_pr)
									{
										Debug.Log("_pr");
										_pr.material = heatRefractionmaterial;
										_pr.sortMode = ParticleSystemSortMode.OldestInFront;

										var sz = _pEM.sizeOverLifetime;
										if (!ReferenceEquals(sz,null))
										{
											//sz.enabled = false;
											//sz.
											Debug.Log("sz.size "+sz.size.ToString());
											Debug.Log("sz.sizeMultiplier "+sz.sizeMultiplier.ToString());

											Debug.Log("sz.xMultiplier "+sz.xMultiplier.ToString());
											//Debug.Log("sz.x.curve "+sz.x.curve.ToString());
//											foreach (Keyframe _keyf in sz.x.curve.keys)
//											{
//												Debug.Log("t: "+_keyf.time.ToString()+ ". x: "+_keyf.value.ToString());
////												if (_keyf.time == 1f)
////												{
////													_keyf.value = 3f;
////												}
//											}

											if (!ReferenceEquals(sz.x.curveMax,null))
												Debug.Log("sz.x.curveMax "+sz.x.curveMax.ToString());

											if (!ReferenceEquals(sz.x.curveMin,null))
												Debug.Log("sz.x.curveMin "+sz.x.curveMin.ToString());

											for (int i=0;i<sz.x.curve.keys.Length;i++)
											{
												Debug.Log("i: "+i.ToString());
												Debug.Log("t: "+sz.x.curve.keys[i].time.ToString()+ ". x: "+sz.x.curve.keys[i].value.ToString());
												if (sz.x.curve.keys[i].time == 1f)
												{
													sz.x.curve.keys[i].value = 3f;
												}
											}

											Debug.Log("sz.yMultiplier "+sz.yMultiplier.ToString());
											//Debug.Log("sz.y.curve "+sz.y.curve.ToString());
											foreach (Keyframe _keyf in sz.y.curve.keys)
											{
												Debug.Log("t: "+_keyf.time.ToString()+ ". y: "+_keyf.value.ToString());
											}

											Debug.Log("sz.zMultiplier "+sz.zMultiplier.ToString());
											//Debug.Log("sz.z.curve "+sz.z.curve.ToString());
											foreach (Keyframe _keyf in sz.z.curve.keys)
											{
												Debug.Log("t: "+_keyf.time.ToString()+ ". z: "+_keyf.value.ToString());
											}
										}


										//_pr.alignment = ParticleSystemRenderSpace.View;
										//_pr.alignment = ParticleSystemRenderSpace.Facing;
										//_pEM.inheritVelocity.mode  = ParticleSystemInheritVelocityMode.Current;
//										Debug.Log("_pEM.main.startSize "+_pEM.main.startSize.ToString());
//										Debug.Log("_pEM.main.startLifetime "+_pEM.main.startLifetime.ToString());
//										Debug.Log("_pEM.main.startSpeed "+_pEM.main.startSpeed.ToString());
//										Debug.Log("_pEM.main.duration "+_pEM.main.duration.ToString());
//										Debug.Log("_pEM.main.maxParticles "+_pEM.main.maxParticles.ToString());
//
//										Debug.Log("_pEM.emission.burstCount "+_pEM.emission.burstCount.ToString());
//										Debug.Log("_pEM.emission.rateOverTime "+_pEM.emission.rateOverTime.ToString());



										//_pEM.shape.alignToDirection = false;

									}
								}
								else
								{
									_pEM.gameObject.SetActive(false);
								}

//								if (_pEM.transform.parent.parent.parent)
//								{
//									Debug.Log("_pEM.transform.parent.parent.parent.name "+_pEM.transform.parent.parent.parent.name);
//
//									if (_pEM.transform.parent.parent.parent.parent)
//									{
//										Debug.Log("_pEM.transform.parent.parent.parent.parent.name "+_pEM.transform.parent.parent.parent.parent.name);
//									}
//								}
							}

						}
					}

//					ParticleSystem[] particleSystems = (ParticleSystem[]) ParticleSystem.FindObjectsOfType(typeof( ParticleSystem));
//					Debug.Log("particleSystems "+particleSystems.Count().ToString());
//					foreach (ParticleSystem _ps in particleSystems)
//					{
//						Debug.Log("name "+_ps.name);
//						Debug.Log("GO name "+_ps.gameObject.name);
//						Debug.Log("parent GO name "+_ps.gameObject.transform.parent.gameObject.name);
//					}

				}
			} 
		}
		

		void OnDestroy ()
		{
			if (isActive)
			{
				if(usePlanetShine)
				{
					foreach (PlanetShineLight _aLight in celestialLightSources)
					{
						_aLight.OnDestroy();
						UnityEngine.Object.Destroy(_aLight);
					}
				}

				for (int i = 0; i < scattererCelestialBodies.Count; i++) {
					
					ScattererCelestialBody cur = scattererCelestialBodies [i];
					if (cur.active) {
						cur.m_manager.OnDestroy ();
						UnityEngine.Object.Destroy (cur.m_manager);
						cur.m_manager = null;
//						ReactivateAtmosphere(cur.transformName,cur.originalPlanetMaterialBackup);
						cur.active = false;
					}
					
				}

				if (ambientLightScript)
				{
					ambientLightScript.restoreLight();
					Component.Destroy(ambientLightScript);
				}


				if (bufferRenderingManager)
				{
					bufferRenderingManager.OnDestroy();
					Component.Destroy (bufferRenderingManager);
				}

//				if(useGodrays)
//				{
//					if (godrayDepthTexture)
//					{
//						if (godrayDepthTexture.IsCreated())
//							godrayDepthTexture.Release();
//						UnityEngine.Object.Destroy (godrayDepthTexture);
//					}
//				}
				

				if (farCamera)
				{
					if (nearCamera.gameObject.GetComponent (typeof(Wireframe)))
						Component.Destroy (nearCamera.gameObject.GetComponent (typeof(Wireframe)));
					
					
					if (farCamera.gameObject.GetComponent (typeof(Wireframe)))
						Component.Destroy (farCamera.gameObject.GetComponent (typeof(Wireframe)));
					
					
					if (scaledSpaceCamera.gameObject.GetComponent (typeof(Wireframe)))
						Component.Destroy (scaledSpaceCamera.gameObject.GetComponent (typeof(Wireframe)));
				}


				if (fullLensFlareReplacement && customSunFlareAdded)
				{
					foreach (SunFlare customSunFlare in customSunFlares)
					{
						customSunFlare.cleanUp();
						Component.Destroy (customSunFlare);
					}

					//re-enable stock sun flares
					global::SunFlare[] stockFlares = (global::SunFlare[]) global::SunFlare.FindObjectsOfType(typeof( global::SunFlare));
					foreach(global::SunFlare _flare in stockFlares)
					{						
						if (sunflaresList.Contains(_flare.sun.name))
						{
							_flare.sunFlare.enabled=true;
						}
					}
				}

				inGameWindowLocation=new Vector2(windowRect.x,windowRect.y);
				saveSettings();
			}
			
			else if (mainMenu)	
			{
				//replace EVE cloud shaders when leaving main menu to game
				if (integrateWithEVEClouds)
				{
					ShaderReplacer.Instance.replaceEVEshaders();
				}

				mainMenuWindowLocation=new Vector2(windowRect.x,windowRect.y);
				saveSettings();
			}


			UnityEngine.Object.Destroy (GUItool);
			
		}

		void OnGUI ()
		{
			if (visible)
			{
				windowRect = GUILayout.Window (windowId, windowRect, GUItool.DrawScattererWindow,"Scatterer v"+versionNumber+": "
				                               + guiModifierKey1String+"/"+guiModifierKey2String +"+" +guiKey1String
				                               +"/"+guiKey2String+" toggle");

				//prevent window from going offscreen
				windowRect.x = Mathf.Clamp(windowRect.x,0,Screen.width-windowRect.width);
				windowRect.y = Mathf.Clamp(windowRect.y,0,Screen.height-windowRect.height);

				//for debugging
//				if (bufferRenderingManager.depthTexture)
//				{
//					GUI.DrawTexture(new Rect(0,0,1280, 720), bufferRenderingManager.depthTexture);
//				}
			}
		}
		
		public void loadSettings ()
		{
			//load scatterer config
			baseConfigs = GameDatabase.Instance.GetConfigs ("Scatterer_config");
			if (baseConfigs.Length == 0)
			{
				Debug.Log ("[Scatterer] No config file found, check your install");
				return;
			}

			if (baseConfigs.Length > 1)
			{
				Debug.Log ("[Scatterer] Multiple config files detected, check your install");
			}

			ConfigNode.LoadObjectFromConfig (this, (baseConfigs [0]).config);


			guiKey1 = (KeyCode)Enum.Parse(typeof(KeyCode), guiKey1String);
			guiKey2 = (KeyCode)Enum.Parse(typeof(KeyCode), guiKey2String);

			guiModifierKey1 = (KeyCode)Enum.Parse(typeof(KeyCode), guiModifierKey1String);
			guiModifierKey2 = (KeyCode)Enum.Parse(typeof(KeyCode), guiModifierKey2String);

			//load planetsList, light sources list and sunflares list
			scattererPlanetsListReader.loadPlanetsList ();
			scattererCelestialBodies = scattererPlanetsListReader.scattererCelestialBodies;
			celestialLightSourcesData = scattererPlanetsListReader.celestialLightSourcesData;
			sunflaresList = scattererPlanetsListReader.sunflares;
			//mainSunCelestialBodyName = scattererPlanetsListReader.mainSunCelestialBodyName;

			//load atmo and ocean configs
			atmoConfigs = GameDatabase.Instance.GetConfigs ("Scatterer_atmosphere");
			oceanConfigs = GameDatabase.Instance.GetConfigs ("Scatterer_ocean");

			//load sunflare configs
			sunflareConfigs = GameDatabase.Instance.GetConfigNodes ("Scatterer_sunflare");
		}
		
		public void saveSettings ()
		{
			baseConfigs [0].config = ConfigNode.CreateConfigFromObject (this);
			baseConfigs [0].config.name = "Scatterer_config";
			Debug.Log ("[Scatterer] Saving settings to: " + baseConfigs [0].parent.url+".cfg");
			baseConfigs [0].parent.SaveConfigs ();
		}

		void removeStockOceans()
		{
			//FakeOceanPQS[] fakes = (FakeOceanPQS[])FakeOceanPQS.FindObjectsOfType (typeof(FakeOceanPQS));

			//if stock oceans haven't already been replaced
			//if (fakes.Length == 0) 
			{ 
				Material invisibleOcean = new Material (ShaderReplacer.Instance.LoadedShaders[("Scatterer/invisible")]);
				foreach (ScattererCelestialBody sctBody in scattererCelestialBodies)
				{
					if (sctBody.hasOcean)
					{
						bool removed = false;
						var celBody = CelestialBodies.SingleOrDefault (_cb => _cb.bodyName == sctBody.celestialBodyName);
						if (celBody == null)
						{
							celBody = CelestialBodies.SingleOrDefault (_cb => _cb.bodyName == sctBody.transformName);
						}
						
						if (celBody != null)
						{
							//Thanks to rbray89 for this snippet and the FakeOcean class which disable the stock ocean in a clean way
							PQS pqs = celBody.pqsController;
							if (pqs != null) {

								//Debug.Log("Scatterer celbody "+celBody.name);
//								for (int i=0;i<pqs.ChildSpheres.Count();i++)
//								{
//									Debug.Log(i.ToString()+" "+pqs.ChildSpheres[i].name);
//									Debug.Log(pqs.surfaceMaterial.shader.name);
//									Debug.Log(pqs.surfaceMaterial.name);
//								}

								PQS ocean = pqs.ChildSpheres [0];
								if (ocean != null) {

									//									GameObject go = new GameObject ();
									//									FakeOceanPQS fakeOcean = go.AddComponent<FakeOceanPQS> ();
									//									fakeOcean.Apply (ocean);

									ocean.surfaceMaterial = invisibleOcean;
									ocean.surfaceMaterial.SetOverrideTag("IgnoreProjector","True");
									ocean.surfaceMaterial.SetOverrideTag("ForceNoShadowCasting","True");

									removed = true;
								}
							}
						}
						if (!removed) {
							Debug.Log ("[Scatterer] Couldn't remove stock ocean for " + sctBody.celestialBodyName);
						}
					}
				}
				Debug.Log ("[Scatterer] Removed stock oceans");
			}
//			else
//			{
//				Debug.Log ("[Scatterer] Stock oceans already removed");
//			}
		}


		void findScattererCelestialBodies()
		{
			foreach (ScattererCelestialBody sctBody in scattererCelestialBodies)
			{
				var _idx = 0;
			
				var celBody = CelestialBodies.SingleOrDefault (_cb => _cb.bodyName == sctBody.celestialBodyName);
				
				if (celBody == null)
				{
					celBody = CelestialBodies.SingleOrDefault (_cb => _cb.bodyName == sctBody.transformName);
				}
				
				Debug.Log ("[Scatterer] Celestial Body: " + celBody);
				if (celBody != null)
				{
					_idx = scattererCelestialBodies.IndexOf (sctBody);
					Debug.Log ("[Scatterer] Found: " + sctBody.celestialBodyName + " / " + celBody.GetName ());
				};
				
				sctBody.celestialBody = celBody;

				var sctBodyTransform = ScaledSpace.Instance.transform.FindChild (sctBody.transformName);
				if (!sctBodyTransform)
				{
					sctBodyTransform = ScaledSpace.Instance.transform.FindChild (sctBody.celestialBodyName);
				}
				else
				{
					sctBody.transform = sctBodyTransform;
					sctBody.hasTransform = true;
				}
				sctBody.active = false;
			}
		}

		void setShadows()
		{
			if (terrainShadows)
			{
				foreach (CelestialBody _sc in CelestialBodies)
				{
					if (_sc.pqsController)
					{
						_sc.pqsController.meshCastShadows = true;
						_sc.pqsController.meshRecieveShadows = true;

//						Debug.Log("[Scatterer] PQS material of "+_sc.name+": "
//						          +_sc.pqsController.surfaceMaterial.shader.name);

						QualitySettings.shadowDistance = shadowsDistance;

						//set shadow bias
						//fixes checkerboard artifacts aka shadow acne
						lights = (Light[]) Light.FindObjectsOfType(typeof( Light));
						foreach (Light _light in lights)
						{
							if ((_light.gameObject.name == "Scaledspace SunLight") 
							    || (_light.gameObject.name == "SunLight"))
							{
								_light.shadowNormalBias =shadowNormalBias;
								_light.shadowBias=shadowBias;
							}
						}
					}
				}
			}
		}
	
		internal static Type getType(string name)
		{
			Type type = null;
			AssemblyLoader.loadedAssemblies.TypeOperation(t =>
			                                              
			                                              {
				if (t.FullName == name)
					type = t;
			}
			);
			
			if (type != null)
			{
				return type;
			}
			return null;
		}

		//map EVE clouds to planet names
		public void mapEVEClouds()
		{
			Debug.Log ("[Scatterer] mapping EVE clouds");
			EVEClouds.Clear();
			EVECloudObjects.Clear ();

			//find EVE base type
			Type EVEType = getType("Atmosphere.CloudsManager"); 
			//Type EVEType = getType("Utils.HalfSphere"); 


			if (EVEType == null)
			{
				Debug.Log("[Scatterer] Eve assembly type not found");
				return;
			}
			else
			{
				Debug.Log("[Scatterer] Eve assembly type found");
			}

			Debug.Log("[Scatterer] Eve assembly version: " + EVEType.Assembly.GetName().ToString());

			object EVEinstance;

			const BindingFlags flags =  BindingFlags.FlattenHierarchy |  BindingFlags.NonPublic | BindingFlags.Public | 
				BindingFlags.Instance | BindingFlags.Static;

			try
			{
//				EVEinstance = EVEType.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
				EVEinstance = EVEType.GetField("instance", flags).GetValue(null) ;
			}
			catch (Exception)
			{
				Debug.Log("[Scatterer] No EVE Instance found");
				return;
			}
			if (EVEinstance == null)
			{
				Debug.Log("[Scatterer] Failed grabbing EVE Instance");
				return;
			}
			else
			{
				Debug.Log("[Scatterer] Successfully grabbed EVE Instance");
			}

			IList objectList = EVEType.GetField ("ObjectList", flags).GetValue (EVEinstance) as IList;

			foreach (object _obj in objectList)
			{
				String body = _obj.GetType().GetField("body", flags).GetValue(_obj) as String;

				if (EVECloudObjects.ContainsKey(body))
				{
					EVECloudObjects[body].Add(_obj);
				}
				else
				{
					List<object> objectsList = new List<object>();
					objectsList.Add(_obj);
					EVECloudObjects.Add(body,objectsList);
				}

				object cloud2dObj = _obj.GetType().GetField("layer2D", flags).GetValue(_obj) as object;
				if (cloud2dObj==null)
				{
					Debug.Log("[Scatterer] layer2d not found for layer on planet :"+body);
					continue;
				}
				GameObject cloudmesh = cloud2dObj.GetType().GetField("CloudMesh", flags).GetValue(cloud2dObj) as GameObject;
				if (cloudmesh==null)
				{
					Debug.Log("[Scatterer] cloudmesh null");
					return;
				}

				if (EVEClouds.ContainsKey(body))
				{
					EVEClouds[body].Add(cloudmesh.GetComponent < MeshRenderer > ().material);
				}
				else
				{
					List<Material> cloudsList = new List<Material>();
					cloudsList.Add(cloudmesh.GetComponent < MeshRenderer > ().material);
					EVEClouds.Add(body,cloudsList);
				}
				Debug.Log("[Scatterer] Detected EVE 2d cloud layer for planet: "+body);
			}
		}		
	}
}
