using BDArmory.Modules;
using System.Collections;
using UnityEngine;

namespace EnemyMine
{
    public class ModuleEnemyMine_Naval : ModuleCommand, IPartMassModifier
    {

//        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "ARMING DELAY"),
//         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 30, stepIncrement = 1f)]
        public float armingDelay = 5;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "FUSE DELAY"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.0f, maxValue = 3, stepIncrement = 0.1f)]
        public float fuseDelay = 0.5f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "DEPLOY DEPTH"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0, maxValue = 100, stepIncrement = 1f)]
        public float depth = 0;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "PROXIMITY TRIGGER"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 2f, maxValue = 50f, stepIncrement = 1f)]
        public float proximity = 10f;

        [KSPField(isPersistant = true)]
        private bool deployed = false;

        [KSPField(isPersistant = true)]
        public bool armMine = false;

        public bool depthCheck = true;
        private bool detonating = false;
        private bool disarming = false;
        private bool countDown = true;
        private bool countStarted = false;
        public bool disarm = false;
        private bool ballastAdded = false;
        private bool checkingDepth = false;
        private bool checkIfArmed = true;
        private bool setInvisible = true;

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

        private void ScreenMsg3(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 1, ScreenMessageStyle.UPPER_CENTER));
        }

        public override void OnStart(StartState state)
        {
            decouple = GetDecouple();
            minimumCrew = 0;
            decouple.staged = false;
            decouple.stagingEnabled = false;

            if (HighLogic.LoadedSceneIsFlight)
            {
                part.force_activate();
                mine = GetMine();
            }
            base.OnStart(state);
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (part.vessel.Parts.Count == 1)
                {
                    if (disarm && !disarming)
                    {
                        StartCoroutine(DisarmRoutine());
                    }

                    if (!deployed && !disarm)
                    {
                        if (checkIfArmed)
                        {
                            StartCoroutine(ArmMine());
                        }
                    }

                    if (depthCheck && !checkingDepth)
                    {
                        StartCoroutine(DepthCheck());
                    }

                    if (armMine)
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


        /// <summary>
        /// Part Mass Change Interface
        /// </summary>
        /// <returns></returns>

        private float massModifier = 0.0f;

        public void setMassModifier(float massModifier)
        {
            this.massModifier = massModifier;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            return defaultMass * massModifier;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.CONSTANTLY;
        }

        //////////////////////////////////////////////////////////////////////////

        IEnumerator ArmMine()
        {
            part.vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
            checkIfArmed = false;
            yield return new WaitForSeconds(1f);
            mine = GetMine();
            if (part.vessel.altitude <= 0 || part.vessel.altitude == 0)
            {
                armMine = true;
                deployed = true;
                part.vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
                mine.ArmAG(new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate));
            }
            else
            {
                checkIfArmed = true;
            }
        }

        public void Detonate()
        {
            StartCoroutine(DetonateMineRoutine());
        }


        IEnumerator DepthCheck()
        {
            checkingDepth = true;

            if (depth >= 0)
            {
                var _depthRatio = part.vessel.altitude / -depth;

                if (_depthRatio <= 0.9f)
                {
                    StartCoroutine(AddBallast());
                }

                if (_depthRatio >= 0.9 && _depthRatio <= 1)
                {
                    StartCoroutine(AddBallastFine());
                }

                if (_depthRatio >= 1)
                {
                    StartCoroutine(BalanceBallast());
                }
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                checkingDepth = false;
            }
        }

        private void ProxDetect()
        {
            mine = GetMine();
            if (!detonating)
            {
                foreach (Vessel v in FlightGlobals.Vessels)
                {
                    double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());

                    if (targetDistance <= proximity)
                    {
                        if (targetDistance >= 0 && v.speed >= 1.5f)
                        {
                            StartCoroutine(DetonateMineRoutine());
                        }
                    }
                }
            }
        }

        public void drop()
        {
            part.vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Unowned);
            decouple.Decouple();
        }

        IEnumerator AddBallast()
        {
            ballastAdded = true;
            massModifier = 0.75f;
            yield return new WaitForSeconds(0.5f);
            checkingDepth = false;
        }

        IEnumerator AddBallastFine()
        {
            ballastAdded = true;
            massModifier = 0.55f;
            yield return new WaitForSeconds(0.5f);
            checkingDepth = false;
        }

        IEnumerator BalanceBallast()
        {
            ballastAdded = true;
            massModifier = 0.4f;
            yield return new WaitForSeconds(0.5f);
            checkingDepth = false;
        }


        IEnumerator DisarmRoutine()
        {
            mine = GetMine();
            armMine = false;
            mine.Armed = false;
            yield return new WaitForSeconds(2.5f);
            ScreenMsg("<color=#017c19ff><b>MINE DISARMED</b></color>");
            vessel.DiscoveryInfo.SetLevel(DiscoveryLevels.Owned);
            massModifier = 0;
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

    }
}
