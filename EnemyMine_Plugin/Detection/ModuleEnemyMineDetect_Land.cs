using BDArmory.Parts;
using System.Collections;
using UnityEngine;
using System;

namespace EnemyMine
{
    public class ModuleEnemyMineDetect_Land : PartModule
    {
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "SCAN FOR MINES"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "", enabledText = "SCANNING")]
        public bool scanning = false;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "DISARM MINE"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "", enabledText = "DISARMING")]
        public bool disarm = false;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "10% FATALITY RATE"),
         UI_Toggle(controlEnabled = true, scene = UI_Scene.All, disabledText = "FALSE", enabledText = "TRUE")]
        public bool fatalityToggle = false;

        private float failureProb = 10;
        private bool detecting = false;
        private bool disarming = false;

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 2, ScreenMessageStyle.UPPER_CENTER));
        }

        public override void OnStart(StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GetSounds();
            }
            base.OnStart(state);
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (scanning && !detecting)
                {
                    StartCoroutine(DetectMine());
                }

                if (disarm && !disarming)
                {
                    StartCoroutine(DisarmRoutine());
                }
            }
        }

        IEnumerator DetectMine()
        {
            detecting = true;
            part.RequestResource("ElectricCharge", 0.02);
            var mineCount = 0.0f;
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v.Parts.Count == 1)
                {
                    if (!v.HoldPhysics && v.altitude <= 6000)
                    {
                        var MINE = v.FindPartModuleImplementing<ModuleEnemyMine_Land>();

                        if (MINE != null)
                        {
                            if (MINE.armMine && mineCount == 0)
                            {
                                mineCount += 1;
                                double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());
                                string _targetDistance_ = Convert.ToString(targetDistance);
                                var _targetDistance = string.Format("{0:0.##}", _targetDistance_);

                                if (targetDistance <= 15)
                                {
                                    ScreenMsg("<color=#cfc100ff><b>ALERT - MINE IN VICINITY</b></color>");
                                    yield return new WaitForSeconds(1);
                                }

                                if (targetDistance <= 10 && targetDistance >= 5)
                                {
                                    ScreenMsg("<color=#cc4500ff><b>>CAUTION - MINE WITHIN " + _targetDistance + " METERS</b></color>");
                                    yield return new WaitForSeconds(1);
                                }

                                if (targetDistance <= 5 && targetDistance >= 3)
                                {
                                    ScreenMsg("<color=#cc4500ff><b>>DANGER - MINE WITHIN " + _targetDistance + " METERS</b></color>");
                                    yield return new WaitForSeconds(1);
                                }

                                if (targetDistance <= 3)
                                {
                                    ScreenMsg("<color=#890000ff><b>DANGER - MINE WITHIN " + _targetDistance + " METERS</b></color>");
                                    yield return new WaitForSeconds(1);
                                }
                            }
                        }
                    }
                }
            }

            if (mineCount == 0)
            {
                ScreenMsg("<color=#017c19ffff><b>NO MINES FOUND</b></color>");
            }
            yield return new WaitForSeconds(2);

            detecting = false;
        }

        private AudioSource soundFatality;
        private void GetSounds()
        {
            soundFatality = gameObject.AddComponent<AudioSource>();
            soundFatality.clip = GameDatabase.Instance.GetAudioClip("EnemyMine/Sounds/Fatality");

            soundFatality.loop = false;
            soundFatality.volume = GameSettings.AMBIENCE_VOLUME;
            soundFatality.dopplerLevel = 0f;
            soundFatality.rolloffMode = AudioRolloffMode.Logarithmic;
            soundFatality.minDistance = 0.01f;
            soundFatality.maxDistance = 25f;
        }


        IEnumerator DisarmRoutine()
        {
            disarming = true;
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (!v.HoldPhysics && v.Parts.Count == 1)
                {
                    double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());
                    if (targetDistance <= 1.5f)
                    {
                        var mine = v.FindPartModuleImplementing<ModuleEnemyMine_Land>();

                        if (fatalityToggle)
                        {
                            System.Random random = new System.Random();
                            int randomNumber = random.Next(0, 100);

                            if (randomNumber <= failureProb)
                            {
                                ScreenMsg("<color=#890000ff><b>DISARMING FAILED ... BETTER RUN</b></color>");

                                yield return new WaitForSeconds(1.5f);

                                mine.Detonate();
                                soundFatality.Play();

                                if (part.vessel.isActiveVessel)
                                {
                                    if (part.vessel.isEVA)
                                    {
                                        ScreenMsg("<color=#890000ff><b>FATALITY</b></color>");
                                    }
                                    else
                                    {
                                        ScreenMsg("<color=#890000ff><b>DIDN'T SEE THAT COMING ...</b></color>");
                                    }
                                }

                            }
                            else
                            {
                                mine.disarm = true;
                            }
                        }
                        else
                        {
                            mine.disarm = true;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(1);
            disarming = false;
            disarm = false;
        }
    }
}
