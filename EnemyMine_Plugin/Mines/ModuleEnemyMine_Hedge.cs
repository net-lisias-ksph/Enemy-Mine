using BDArmory.Parts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyMine
{
    public class ModuleEnemyMine_Hedge : ModuleCommand//, IPartMassModifier
    {
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "TRIGGER DEPTH"),
         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0, maxValue = 250, stepIncrement = 1f)]
        public float depth = 20;

        [KSPField(isPersistant = true)]
        private bool deployed = false;

        public bool armMine = false;
        public bool detonating = false;
        public bool depthCheck = true;
        private bool checkIfArmed = true;
        private bool impactCheck = true;

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
                    if (!deployed)
                    {
                        if (checkIfArmed)
                        {
                            StartCoroutine(ArmMine());
                        }
                    }
                    else
                    {
                        if (vessel.altitude <= -depth)
                        {
                            StartCoroutine(DetonateMineRoutine());
                        }
                    }
                }
            }
        }

        IEnumerator ArmMine()
        {
            checkIfArmed = false;
            yield return new WaitForSeconds(1f);
            mine = GetMine();
            if (part.vessel.Splashed)
            {
                armMine = true;
                deployed = true;
                mine.ArmAG(new KSPActionParam(KSPActionGroup.None, KSPActionType.Activate));
            }
            else
            {
                checkIfArmed = true;
            }
        }

        public void drop()
        {
            decouple.Decouple();
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
