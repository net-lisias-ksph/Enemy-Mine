using System;
using KSP.UI.Screens;
using UnityEngine;
using BDArmory;
using BDArmory.UI;
using BDArmory.Parts;
using System.Collections.Generic;
using System.Collections;

namespace EnemyMine
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class EnemyMine : MonoBehaviour
    {
        private const float WindowWidth = 200;
        private const float DraggableHeight = 40;
        private const float LeftIndent = 12;
        private const float ContentTop = 20;
        public static EnemyMine Fetch;
        public static bool GuiEnabled;
        public static bool HasAddedButton;
        private readonly float _incrButtonWidth = 26;
        private readonly float contentWidth = WindowWidth - 2 * LeftIndent;
        private readonly float entryHeight = 20;
        private float _contentWidth;
        private bool _gameUiToggle;
        public float _delayTimerNaval = 0.0f;
        public float _delayTimer = 0.0f;
        public float _proximity = 0.0f;
        public float _spread = 0.0f;
        public float _depth = 0.0f;
        public float _delay = 0.0f;
        public float _depthNaval = 0.0f;
        public float _proximityNaval = 0.0f;
        public float fuseDelay = 0.0f;
        public float _launcherDelay = 0.0f;
        private float count = 0.0f;

        private float _windowHeight = 250;
        private Rect _windowRect;

        public string Name = String.Empty;

        private static bool secondary;
        private bool firing = false;

        private void Awake()
        {        
            if (Fetch)
                Destroy(Fetch);

            Fetch = this;
        }

        private void Start()
        {
            _windowRect = new Rect(Screen.width - WindowWidth - 40, 100, WindowWidth, _windowHeight);
            AddToolbarButton();
            GameEvents.onHideUI.Add(GameUiDisable);
            GameEvents.onShowUI.Add(GameUiEnable);
            _gameUiToggle = true;
            _delayTimer = 5;
            _proximity = 10;
            _spread = 3;
            _depth = 20;
            _delay = 1;
            _depthNaval = 0;
            _delayTimerNaval = 5;
            _proximityNaval = 25;
            fuseDelay = 0.5f;
            _launcherDelay = 0.5f;
        }

        private void OnGUI()
        {
            if (GuiEnabled && _gameUiToggle)
            {
                _windowRect = GUI.Window(2261, _windowRect, GuiWindow, "");
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (FlightGlobals.ActiveVessel.Parts.Count == 1 && !FlightGlobals.ActiveVessel.isEVA)
                {
                    CheckForMine();
                }
            }
        }

        private void CheckForMine()
        {
            var navalMine = FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleEnemyMine_Naval>();
            var landMine = FlightGlobals.ActiveVessel.FindPartModuleImplementing<ModuleEnemyMine_Land>();

            if (navalMine != null || landMine != null)
            {
                count = 0.0f;
                foreach (Vessel v in FlightGlobals.Vessels)
                {
                    if (!v.HoldPhysics)
                    {
                        double targetDistance = Vector3d.Distance(FlightGlobals.ActiveVessel.GetWorldPos3D(), v.GetWorldPos3D());

                        if (targetDistance <= 2500 && count <= 1)
                        {
                            count += 1;
                            FlightGlobals.ActiveVessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
                            FlightGlobals.ForceSetActiveVessel(v);
                            FlightInputHandler.ResumeVesselCtrlState(v);
                            return;
                        }
                    }
                }
            }
        }

        public void SwitchVessel()
        {
        }


        private void GetSettings()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                List<ModuleEnemyMine_Launcher> launcherParts = new List<ModuleEnemyMine_Launcher>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    launcherParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Launcher>());
                }
                foreach (ModuleEnemyMine_Launcher launcherPart in launcherParts)
                {
                    if (launcherPart != null)
                    {
                        _launcherDelay = launcherPart.delay;
                        _spread = launcherPart.spread;

                    }
                }

                List<ModuleEnemyMine_Land> mineParts = new List<ModuleEnemyMine_Land>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    mineParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Land>());
                }
                foreach (ModuleEnemyMine_Land minePart in mineParts)
                {
                    if (minePart != null)
                    {
                        _delayTimer = minePart.armingDelay;
                        _proximity = minePart.proximity;
                    }
                }

                List<ModuleEnemyMine_Naval> seamineParts = new List<ModuleEnemyMine_Naval>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    seamineParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Naval>());
                }
                foreach (ModuleEnemyMine_Naval seaminePart in seamineParts)
                {
                    if (seaminePart != null)
                    {
                        _delayTimerNaval = seaminePart.armingDelay;
                        _depthNaval = seaminePart.depth;
                    }
                }

                List<ModuleEnemyMine_Depth> depthParts = new List<ModuleEnemyMine_Depth>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    depthParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Depth>());
                }
                foreach (ModuleEnemyMine_Depth depthPart in depthParts)
                {
                    if (depthPart != null)
                    {
                        _depth = depthPart.depth;
                    }
                }
            }
        }

        private void SendSettings()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                List<ModuleEnemyMine_Launcher> launcherParts = new List<ModuleEnemyMine_Launcher>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    launcherParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Launcher>());
                }
                foreach (ModuleEnemyMine_Launcher launcherPart in launcherParts)
                {
                    if (launcherPart != null)
                    {
                        launcherPart.delay = _launcherDelay;
                        launcherPart.spread = _spread;
                    }
                }

                List<ModuleEnemyMine_Land> mineParts = new List<ModuleEnemyMine_Land>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    mineParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Land>());
                }
                foreach (ModuleEnemyMine_Land minePart in mineParts)
                {
                    if (minePart != null)
                    {
                        minePart.armingDelay = _delayTimer;
                        minePart.proximity = _proximity;
                    }
                }

                List<ModuleEnemyMine_Naval> seamineParts = new List<ModuleEnemyMine_Naval>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    seamineParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Naval>());
                }
                foreach (ModuleEnemyMine_Naval seaminePart in seamineParts)
                {
                    if (seaminePart != null)
                    {
                        seaminePart.armingDelay = _delayTimerNaval;
                        seaminePart.depth = _depthNaval;
                        seaminePart.proximity = _proximity;
                    }
                }

                List<ModuleEnemyMine_Hedge> hedgeParts = new List<ModuleEnemyMine_Hedge>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    hedgeParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Hedge>());
                }
                foreach (ModuleEnemyMine_Hedge hedgePart in hedgeParts)
                {
                    if (hedgePart != null)
                    {
                        hedgePart.depth = _depth;
                    }
                }

                List<ModuleEnemyMine_Depth> depthParts = new List<ModuleEnemyMine_Depth>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    depthParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Depth>());
                }
                foreach (ModuleEnemyMine_Depth depthPart in depthParts)
                {
                    if (depthPart != null)
                    {
                        depthPart.depth = _depth;
                    }
                }
            }
        }

        private void DropNavalMine()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                SendSettings();

                var navalCount = 0.0f;

                List<ModuleEnemyMine_Naval> navalParts = new List<ModuleEnemyMine_Naval>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    navalParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Naval>());
                }
                foreach (ModuleEnemyMine_Naval navalPart in navalParts)
                {
                    if (navalPart != null && navalCount == 0)
                    {
                        navalCount += 1;
                        navalPart.depth = _depthNaval;
                        if (_depthNaval >=0)
                        {
                            navalPart.depthCheck = true;
                            navalPart.drop();
                        }
                        else
                        {
                            navalPart.drop();
                        }
                    }
                }

            }
        }

        private void DepthControl()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                SendSettings();

                var navalCount = 0.0f;

                List<ModuleEnemyMine_Naval> navalParts = new List<ModuleEnemyMine_Naval>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    navalParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Naval>());
                }
                foreach (ModuleEnemyMine_Naval navalPart in navalParts)
                {
                    if (navalPart != null && navalCount == 0)
                    {
                        navalCount += 1;
                        navalPart.depth = _depthNaval;
                        navalPart.depthCheck = true;
                    }
                }
            }
        }

        private void DropDepthCharge()
        {
            SendSettings();

            List<ModuleEnemyMine_DCLauncher> depthLaunchers = new List<ModuleEnemyMine_DCLauncher>(200);
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                depthLaunchers.AddRange(p.FindModulesImplementing<ModuleEnemyMine_DCLauncher>());
            }
            foreach (ModuleEnemyMine_DCLauncher depthLauncher in depthLaunchers)
            {
                if (depthLauncher != null)
                {
                    if (secondary)
                    {
                        if (depthLauncher.secondary)
                        {
                            depthLauncher.Fire();
                        }
                    }
                    else
                    {
                        if (!depthLauncher.secondary)
                        {
                            depthLauncher.Fire();
                        }
                    }
                }
            }
        }

        private void DropMine()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                SendSettings();

                var mineCount = 0.0f;

                List<ModuleEnemyMine_Land> mineParts = new List<ModuleEnemyMine_Land>(200);
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    mineParts.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Land>());
                }
                foreach (ModuleEnemyMine_Land minePart in mineParts)
                {
                    if (minePart != null && mineCount == 0)
                    {
                        mineCount += 1;
                        minePart.drop();
                    }
                }
            }
        }
        
        private void FireSpreadCheck()
        {
            if (count <= _spread)
            {
                StartCoroutine(Fire());
            }
        }

        public void FireSpread()
        {
            firing = true;
            count = 0;
            SendSettings();
            StartCoroutine(Fire());
        }

        IEnumerator Fire()
        {
            count += 1;

            List<ModuleEnemyMine_Launcher> depthLaunchers = new List<ModuleEnemyMine_Launcher>(200);
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                depthLaunchers.AddRange(p.FindModulesImplementing<ModuleEnemyMine_Launcher>());
            }
            foreach (ModuleEnemyMine_Launcher depthLauncher in depthLaunchers)
            {
                if (depthLauncher != null)
                {
                    if (secondary)
                    {
                        if (depthLauncher.secondary)
                        {
                            depthLauncher.Fire();
                        }
                    }
                    else
                    {
                        if (!depthLauncher.secondary)
                        {
                            depthLauncher.Fire();
                        }
                    }
                }
            }
            yield return new WaitForSeconds(_launcherDelay);
            FireSpreadCheck();
        }

        #region GUI
        /// <summary>
        /// GUI
        /// </summary>

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4, ScreenMessageStyle.UPPER_CENTER));
        }

        private void GuiWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, WindowWidth, DraggableHeight));
            float line = 0;
            _contentWidth = WindowWidth - 2 * LeftIndent;

            DrawTitle();
            DrawText(line);
            line++;
            DrawArmingDelayText(line);
            line++;
            DrawArmingDelay(line);
            line++;
            DrawProximityText(line);
            line++;
            DrawProximity(line);
            line++;
            DrawDropMine(line);
            line++;
            line++;
            DrawNavalText(line);
            line++;
            DrawNavalDepthText(line);
            line++;
            DrawNavalDepth(line);
            line++;
            DrawNavalFuseDelayText(line);
            line++;
            DrawFuseDelayNaval(line);
            line++;
            DrawProximityNavalText(line);
            line++;
            DrawProximityNaval(line);
            line++;
            DrawDropNavalMine(line);
            line++;
            line++;
            DrawDepthChargeText(line);
            line++;
            DrawDepthText(line);
            line++;
            DrawDepth(line);
            line++;
            DrawSpreadText(line);
            line++;
            DrawSpread(line);
            line++;
            DrawDepthDelayText(line);
            line++;
            DrawDepthDelay(line);
            line++;
            SecondaryToggle(line);
            line++;
            DrawDropDepthCharge(line);
            line++;
            DrawFireSpread(line);


            _windowHeight = ContentTop + line * entryHeight + entryHeight + (entryHeight / 2);
            _windowRect.height = _windowHeight;
        }

        private void SecondaryToggle(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);

            if (secondary)
            {
                if (GUI.Button(saveRect, "Bank B"))
                    secondary = false;
            }
            else
            {
                if (GUI.Button(saveRect, "Bank A"))
                    secondary = true;

            }
        }


        private void AddToolbarButton()
        {
            string textureDir = "EnemyMine/Plugin/";

            if (!HasAddedButton)
            {
                Texture buttonTexture = GameDatabase.Instance.GetTexture(textureDir + "EM_icon", false); //texture to use for the button
                ApplicationLauncher.Instance.AddModApplication(EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy,
                    ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
                HasAddedButton = true;
            }
        }

        private void EnableGui()
        {
            GuiEnabled = true;
            Debug.Log("[Enemy Mine]: Showing GUI");
        }

        private void DisableGui()
        {
            GuiEnabled = false;
            Debug.Log("[Enemy Mine]: Hiding GUI");
        }

        private void GameUiEnable()
        {
            _gameUiToggle = true;
        }

        private void GameUiDisable()
        {
            _gameUiToggle = false;
        }

        private void DrawTitle()
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, 0, WindowWidth, 20), "ENEMY MINE", titleStyle);
        }

        private void DrawText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 20),
                "Land Mines",
                titleStyle);
        }

        private void DrawNavalText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 20),
                "Naval Mines",
                titleStyle);
        }

        private void DrawDepthChargeText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 20),
                "Depth Charges/Hedgehog",
                titleStyle);
        }

        private void DrawFireSpread(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            if (GUI.Button(saveRect, "FIRE SPREAD"))
            {
                count = 0;
                FireSpread();
            }
        }

        private void DrawNavalFuseDelayText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "FUSE DELAY",
                titleStyle);
        }

        private void DrawFuseDelayNaval(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(10, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(95, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(178, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "3");
            _delayTimerNaval = GUI.HorizontalSlider(saveRect, _delayTimerNaval, 0, 30);
        }

        private void DrawProximityNavalText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "PROXIMITY TRIGGER",
                titleStyle);
        }

        private void DrawProximityNaval(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(10, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "25");
            GUI.Label(new Rect(178, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "50");
            _proximityNaval = GUI.HorizontalSlider(saveRect, _proximityNaval, 0, 50);
        }

        private void DrawNavalDepthText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "DEPLOY DEPTH",
                titleStyle);
        }

        private void DrawNavalDepth(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(10, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(95, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(176, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "250");
            _depthNaval = GUI.HorizontalSlider(saveRect, _depthNaval, 0, 100);
        }

        private void DrawDepthText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "DEPLOY DEPTH",
                titleStyle);
        }

        private void DrawDepth(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(8, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(176, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "100");
            _depth = GUI.HorizontalSlider(saveRect, _depth, 0, 100);
        }

        private void DrawDepthDelayText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "SPREAD DELAY",
                titleStyle);
        }

        private void DrawDepthDelay(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(8, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(176, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "2");
            _launcherDelay = GUI.HorizontalSlider(saveRect, _launcherDelay, 0, 2);
        }


        private void DrawSpreadText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "HEGEHOG SPREAD",
                titleStyle);
        }

        private void DrawSpread(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(8, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "1");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(178, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "28");
            _spread = GUI.HorizontalSlider(saveRect, _spread, 1, 28);
        }

        private void DrawDelayText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "SPREAD DELAY",
                titleStyle);
        }

        private void DrawDelay(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(8, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(179, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "2");
            _delay = GUI.HorizontalSlider(saveRect, _delay, 0, 2);
        }

        private void DrawArmingDelayText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "ARMING DELAY",
                titleStyle);
        }

        private void DrawArmingDelay(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(8, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(178, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "30");
            _delayTimer = GUI.HorizontalSlider(saveRect, _delayTimer, 0, 30);
        }

        private void DrawProximityText(float line)
        {
            var centerLabel = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            var titleStyle = new GUIStyle(centerLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.Label(new Rect(0, ContentTop + line * entryHeight, WindowWidth, 18),
                "PROXIMITY TRIGGER",
                titleStyle);
        }

        private void DrawProximity(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            GUI.Label(new Rect(8, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "0");
            GUI.Label(new Rect(90, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "|");
            GUI.Label(new Rect(178, ContentTop + line * entryHeight, contentWidth * 0.9f, 20), "10");
            _proximity = GUI.HorizontalSlider(saveRect, _proximity, 0, 10);
        }

        private void DrawDropMine(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            if (GUI.Button(saveRect, "DROP MINE"))
            {
                DropMine();
            }
        }

        private void DrawDropNavalMine(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            if (GUI.Button(saveRect, "DROP MINE"))
            {
                DropNavalMine();
            }
        }

        private void DrawDropDepthCharge(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            if (GUI.Button(saveRect, "DEPTH CHARGE"))
            {
                DropDepthCharge();
            }
        }

        private void DrawDepthControl(float line)
        {
            var saveRect = new Rect(LeftIndent * 1.5f, ContentTop + line * entryHeight, contentWidth * 0.9f, entryHeight);
            if (GUI.Button(saveRect, "DEPTH CONTROL"))
            {
                DepthControl();
            }
        }

        #endregion

        private void Dummy()
        {
        }
    }
}