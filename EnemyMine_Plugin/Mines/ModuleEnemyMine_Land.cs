using BDArmory.Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyMine
{
    public class ModuleEnemyMine_Land : ModuleCommand
    {

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "ARMED"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "False", enabledText = "True")]
        public bool armMine = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "ARMING DELAY TIMER"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 30, stepIncrement = 1f)]
        public float armingDelay = 15;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "PROXIMITY TRIGGER"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 2f, maxValue = 10f, stepIncrement = 1f)]
        public float proximity = 3f;

        [KSPField(isPersistant = true)]
        private bool deployed = false;
        
        private bool detonating = false;
        private bool disarming = false;
        private bool countDown = true;
        private bool countStarted = false;
        public bool disarm = false;

        public BDExplosivePart mine;
        private BDExplosivePart GetMine()
        {
            BDExplosivePart m = null;

            m = part.FindModuleImplementing<BDExplosivePart>();

            return m;
        }

        public ModuleDecouple decouple;
        private ModuleDecouple GetDecouple()
        {
            ModuleDecouple d = null;

            d = part.FindModuleImplementing<ModuleDecouple>();

            return d;
        }


        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4, ScreenMessageStyle.UPPER_CENTER));
        }

        private void ScreenMsg2(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 2, ScreenMessageStyle.UPPER_CENTER));
        }

        public override void OnStart(StartState state)
        {
            minimumCrew = 0;
            decouple = GetDecouple();
            decouple.staged = false;
            decouple.stagingEnabled = false;
            GameEvents.onVesselWasModified.Add(ReconfigureEvent);
            recalcSurfaceArea();

            if (HighLogic.LoadedSceneIsFlight)
            {
                part.force_activate();
                mine = GetMine();
            }
            base.OnStart(state);
        }

        public override void OnUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (IsTransitioning())
                {
                    recalcCloak = false;
                    calcNewCloakLevel();

                    foreach (Part p in vessel.parts)
                    {
                        if (selfCloak || (p != part))
                        {
                            p.SetOpacity(visiblilityLevel);
                            SetRenderAndShadowStates(p, visiblilityLevel > shadowCutoff, visiblilityLevel > RENDER_THRESHOLD);
                        }
                    }
                }
            }
            base.OnUpdate();
        }


        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (part.vessel.Parts.Count == 1 && part.vessel.Landed)
                {
                    if (!armMine)
                    {
                        ArmMine();
                    }
                }
                else
                {
                    disarm = false;
                    armMine = false;
                    mine.Armed = false;
                    countStarted = false;
                    countDown = true;
                    deployed = false;
                }

                if (disarm && armMine)
                {
                    if (!disarming)
                    {
                        disarming = true;
                        countStarted = false;
                        armMine = false;
                        ScreenMsg2("<color=#cfc100ff><b>DISARMING MINE</b></color>");
                        disengageCloak();
                        StartCoroutine(DisarmRoutine());
                    }
                }

                if (!disarm && deployed)
                {
                    if (!cloakOn)
                    {
                        engageCloak();
                    }

                    if (!disarming)
                    {
                        if (!countDown)
                        {
                            ProxDetect();
                        }
                        else
                        {
                            if (!countStarted)
                            {
                                countStarted = true;
                                StartCoroutine(CountDownRoutine());
                            }
                        }
                    }
                }
            }
        }

        public void drop()
        {
            armMine = true;
            decouple.Decouple();
        }


        IEnumerator DisarmRoutine()
        {
            mine = GetMine();
            armMine = false;
            mine.Armed = false;
            yield return new WaitForSeconds(2.5f);
            ScreenMsg("<color=#017c19ff><b>MINE DISARMED</b></color>");
            vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Owned);
            disarm = true;
            disarming = false;
            armMine = false;
        }


        IEnumerator CountDownRoutine()
        {
            yield return new WaitForSeconds(armingDelay);
            countDown = false;
        }

        IEnumerator DetonateMineRoutine()
        {
            detonating = true;
            mine = GetMine();
            mine.DetonateAG(new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate));
            yield return new WaitForSeconds(1);
            part.explode();
        }

        private void ArmMine()
        {
            part.vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
            mine = GetMine();
            armMine = true;
            deployed = true;
            mine.ArmAG(new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate));
            string _proximity_ = Convert.ToString(proximity);
        }

        public void Detonate()
        {
            StartCoroutine(DetonateMineRoutine());
        }


        private void ProxDetect()
        {
            part.vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
            mine = GetMine();
            if (!detonating)
            {
                foreach (Vessel v in FlightGlobals.Vessels)
                {
                    if (!v.HoldPhysics && v.speed >= 1.1f)
                    {
                        double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());

                        if (targetDistance >= 0 && targetDistance <= proximity)
                        {
                            StartCoroutine(DetonateMineRoutine());
                        }
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool autoDeploy = true;

        private float maxfade = 0.1f; // invisible:0 to uncloaked:1
        private float surfaceAreaToCloak = 0.0f;

        [KSPField(isPersistant = true, guiActiveEditor = false, guiActive = false)]
        public bool cloakOn = false;

        private static float UNCLOAKED = 1.0f;
        private static float RENDER_THRESHOLD = 0.0f;
        private float fadePerTime = 0.5f;
        private bool currentShadowState = true;
        private bool recalcCloak = true;
        private float visiblilityLevel = UNCLOAKED;
        private float fadeTime = 2f; // In seconds
        private float shadowCutoff = 0.0f;
        private bool selfCloak = true;


        //////////////////////////////////////////////////////////////////////////////


        #region Cloak
        /// <summary>
        /// Cloak code
        /// </summary>
        /// 

        public void engageCloak()
        {
            part.vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
            cloakOn = true;
            UpdateCloakField(null, null);
        }

        public void disengageCloak()
        {
            cloakOn = false;
            UpdateCloakField(null, null);
        }

        protected void UpdateSelfCloakField(BaseField field, object oldValueObj)
        {
            if (selfCloak)
            {
                SetRenderAndShadowStates(part, visiblilityLevel > shadowCutoff, visiblilityLevel > RENDER_THRESHOLD);
            }
            else
            {
                SetRenderAndShadowStates(part, true, true);
            }
            recalcCloak = true;
        }

        public void UpdateCloakField(BaseField field, object oldValueObj)
        {
            // Update in case its been changed
            calcFadeTime();
            recalcSurfaceArea();
            recalcCloak = true;
        }

        private void calcFadeTime()
        {
            // In case fadeTime == 0
            try
            { fadePerTime = (1 - maxfade) / fadeTime; }
            catch (Exception)
            { fadePerTime = 10.0f; }
        }

        private void recalcSurfaceArea()
        {
            Part p;

            if (vessel != null)
            {
                surfaceAreaToCloak = 0.0f;
                for (int i = 0; i < vessel.parts.Count; i++)
                {
                    p = vessel.parts[i];
                    if (p != null)
                        if (selfCloak || (p != part))
                            surfaceAreaToCloak = (float)(surfaceAreaToCloak + p.skinExposedArea);
                }
            }
        }

        private void SetRenderAndShadowStates(Part p, bool shadowsState, bool renderState)
        {
            if (p.gameObject != null)
            {
                int i;

                MeshRenderer[] MRs = p.GetComponentsInChildren<MeshRenderer>();
                for (i = 0; i < MRs.GetLength(0); i++)
                    MRs[i].enabled = renderState;// || !fullRenderHide;

                SkinnedMeshRenderer[] SMRs = p.GetComponentsInChildren<SkinnedMeshRenderer>();
                for (i = 0; i < SMRs.GetLength(0); i++)
                    SMRs[i].enabled = renderState;// || !fullRenderHide;

                if (shadowsState != currentShadowState)
                {
                    for (i = 0; i < MRs.GetLength(0); i++)
                    {
                        if (shadowsState)
                            MRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        else
                            MRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    for (i = 0; i < SMRs.GetLength(0); i++)
                    {
                        if (shadowsState)
                            SMRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        else
                            SMRs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                    currentShadowState = shadowsState;
                }
            }
        }

        private void ReconfigureEvent(Vessel v)
        {
            if (v == null) { return; }

            if (v == vessel)
            {   // This is the cloaking vessel - recalc EC required based on new configuration (unless this is a dock event)
                recalcCloak = true;
                recalcSurfaceArea();
            }
            else
            {   // This is the added/removed part - reset it to normal
                ModuleEnemyMine_Land mc = null;
                foreach (Part p in v.parts)
                    if ((p != null) &&
                        ((p != part) || selfCloak))
                    {
                        //p.setOpacity(UNCLOAKED); // 1.1.3
                        p.SetOpacity(UNCLOAKED); // 1.2.2 and up
                        SetRenderAndShadowStates(p, true, true);

                        // If the other vessel has a cloak device let it know it needs to do a refresh
                        mc = p.FindModuleImplementing<ModuleEnemyMine_Land>();
                        if (mc != null)
                            mc.recalcCloak = true;
                    }
            }
        }

        protected void calcNewCloakLevel()
        {
            calcFadeTime();
            float delta = Time.deltaTime * fadePerTime;
            if (cloakOn && (visiblilityLevel > maxfade))
                delta = -delta;

            visiblilityLevel = visiblilityLevel + delta;
            visiblilityLevel = Mathf.Clamp(visiblilityLevel, maxfade, UNCLOAKED);
        }

        protected bool IsTransitioning()
        {
            return (cloakOn && (visiblilityLevel > maxfade)) ||     // Cloaking in progress
                   (!cloakOn && (visiblilityLevel < UNCLOAKED)) ||  // Uncloaking in progress
                   recalcCloak;                                     // A forced refresh 
        }
        #endregion

    }
}
