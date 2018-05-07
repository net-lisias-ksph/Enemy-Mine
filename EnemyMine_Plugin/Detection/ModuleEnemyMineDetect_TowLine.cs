using BDArmory.Parts;
using System.Collections;
using UnityEngine;
using System;
using System.Linq;

namespace EnemyMine
{
    public class ModuleEnemyMineDetect_TowLine : PartModule
    {

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "SCAN FOR MINES"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "", enabledText = "SCANNING")]
        public bool scanning = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "EC DRAW"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 1f, maxValue = 10f, stepIncrement = 1f)]
        public float ecPerSec = 5f;


        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "EXCLUDE"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "", enabledText = "SCANNING")]
        public bool exclude = false;

        private bool partChecked = false;

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 4, ScreenMessageStyle.UPPER_CENTER));
        }

        private void ScreenMsg2(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 2 , ScreenMessageStyle.UPPER_CENTER));
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
            }
            base.OnStart(state);
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (scanning)
                {
                    DetectMine();
                }

                if (exclude && !partChecked)
                {
                    CheckPart();
                }
            }
        }

        private void CheckPart()
        {
            partChecked = true;

            string tow1 = "DeepSixTSA";
            string tow2 = "TactAssTSA";

            if (part.name != tow1 || part.name != tow2)
            {
                part.explode();
            }
        }

        public void DetectMine()
        {
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v.Parts.Count == 1 && v.Splashed)
                {
                    var MINE = v.FindPartModuleImplementing<ModuleEnemyMine_Naval>();

                    if (MINE != null)
                    {
                        double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());
                        var _targetDist = ecPerSec * 50;

                        if (targetDistance <= _targetDist)
                        {
                            MINE.Detonate();
                        }
                    }
                }
            }
            drawEC();
        }

        protected void drawEC()
        {
            var RequiredEC = Time.deltaTime * ecPerSec * 2;

            float AcquiredEC = part.RequestResource("ElectricCharge", RequiredEC);
            if (AcquiredEC < RequiredEC * 0.8f)
            {
                if (vessel.isActiveVessel)
                {
                    ScreenMsg("Not Enough Electrical Charge");
                }
                scanning = false;
            }

            foreach (var p in vessel.parts)
            {
                double totalAmount = 0;
                double maxAmount = 0;
                PartResource r = p.Resources.Where(n => n.resourceName == "ElectricCharge").FirstOrDefault();
                if (r != null)
                {
                    totalAmount += r.amount;
                    maxAmount += r.maxAmount;
                    if (totalAmount < maxAmount * 0.02)
                    {
                        if (vessel.isActiveVessel)
                        {
                            ScreenMsg("Not Enough Electrical Charge");
                        }
                        scanning = false;
                    }
                }
            }
        }
    }
}
