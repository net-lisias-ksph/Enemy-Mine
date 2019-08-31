﻿using BDArmory.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyMine
{
    public class ModuleEnemyMine_DCLauncher : PartModule
    {
//        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Spread"),
//         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 1, maxValue = 10, stepIncrement = 1f)]
        public float spread = 1;

//        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fire Delay"),
//         UI_FloatRange(controlEnabled = true, scene = UI_Scene.All, minValue = 0.1f, maxValue = 2, stepIncrement = 0.1f)]
        public float delay = 0.5f;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "FIRE CONTROL"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "BANK A", enabledText = "BANK B")]
        public bool secondary = false;

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                part.force_activate();
            }
            base.OnStart(state);
        }

        public void Fire()
        {
            StartCoroutine(FireSpread());
        }

        IEnumerator FireSpread()
        {
            double count = 0;

            List<Part> childParts = this.part.children;
            foreach (Part p in childParts)
            {
                var mine = p.FindModuleImplementing<ModuleEnemyMine_Depth>();

                if (mine != null)
                {
                    if (count <= 1)
                    {
                        count += 1;
                        mine.drop();
                        yield return new WaitForSeconds(delay);
                    }
                }
            }
        }
    }
}