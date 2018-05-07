using BDArmory.Parts;
using System.Collections;
using UnityEngine;
using System;

namespace EnemyMine
{
    public class ModuleEnemyMineDetect_Naval : PartModule
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

        private float maxDistance = 10f;
        private float failureProb = 3f;
        private bool detecting = false;
        private bool disarming = false;

        private void ScreenMsg(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 2, ScreenMessageStyle.UPPER_LEFT));
        }

        private void ScreenMsg2(string msg)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(msg, 2 , ScreenMessageStyle.UPPER_LEFT));
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
                    var MINE = v.FindPartModuleImplementing<ModuleEnemyMine_Naval>();

                    if (MINE != null)
                    {
                        double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());

                        if (targetDistance <= 100)
                        {
                            if (MINE.armMine && mineCount == 0)
                            {
                                mineCount += 1;
                                if (vessel.isActiveVessel)
                                {
                                    string _targetDistance_ = Convert.ToString(targetDistance);
                                    var _targetDistance = string.Format("{0:0.##}", _targetDistance_);

                                    if (targetDistance <= 100 && targetDistance >= 40)
                                    {
                                        ScreenMsg("<color=#cfc100ff><b>ALERT - ACTIVE MINE IN VICINITY</b></color>");
                                    }

                                    if (targetDistance <= 40 && targetDistance >= 20)
                                    {
                                        ScreenMsg("<color=#cc4500ff><b>>CAUTION - MINE WITHIN 40 METERS</b></color>");
                                    }

                                    if (targetDistance <= 20 && targetDistance >= 10)
                                    {
                                        ScreenMsg("<color=#cc4500ff><b>>DANGER - MINE WITHIN 20 METERS</b></color>");
                                    }

                                    if (targetDistance <= 10)
                                    {
                                        ScreenMsg("<color=#890000ff><b>WARNING - MINE WITHIN " + _targetDistance + " METERS</b></color>");
                                    }
                                }
                                yield return new WaitForSeconds(1);
                            }
                        }
                    }
                }
            }

            if (mineCount == 0)
            {
                ScreenMsg("<color=#017c19ff><b>NO MINES FOUND</b></color>");
            }
            else
            {
                ScreenMsg("<color=#cfc100ff><b>" + mineCount + " MINES DETECTED</b></color>");

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
            soundFatality.minDistance = 0.5f;
            soundFatality.maxDistance = 1f;
        }


        IEnumerator DisarmRoutine()
        {
            var mineCount = 0.0f;

            disarming = true;
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (!v.HoldPhysics && v.Parts.Count == 1)
                {
                    var mine = v.FindPartModuleImplementing<ModuleEnemyMine_Naval>();

                    if (mine !=null && mineCount == 0)
                    {
                        mineCount += 1;

                        double targetDistance = Vector3d.Distance(this.vessel.GetWorldPos3D(), v.GetWorldPos3D());

                        if (targetDistance <= maxDistance)
                        {

                            if (fatalityToggle)
                            {
                                var failPercentMod = targetDistance * failureProb;

                                System.Random random = new System.Random();
                                int randomNumber = random.Next(0, 100);

                                if (randomNumber <= failPercentMod)
                                {
                                    ScreenMsg("<color=#890000ff><b>DISARMING FAILED ... BETTER RUN</b></color>");

                                    yield return new WaitForSeconds(1.5f);

                                    mine.Detonate();

                                    if (part.vessel.isActiveVessel)
                                    {
                                        if (part.vessel.isEVA)
                                        {
                                            System.Random num = new System.Random();
                                            int randomNum = num.Next(0, 5);

                                            if (randomNum == 1)
                                            {
                                                soundFatality.Play();
                                                ScreenMsg("<color=#890000ff><b>THAT'S GOTTA HURT ...</b></color>");
                                            }

                                            if (randomNum == 2)
                                            {
                                                soundFatality.Play();
                                                ScreenMsg("<color=#890000ff><b>GET A LITTLE CLOSER ... DON'T BE SHY</b></color>");
                                            }

                                            if (randomNum == 3)
                                            {
                                                soundFatality.Play();
                                                ScreenMsg("<color=#890000ff><b>WATCH YOU'RE STEP</b></color>");
                                            }

                                            if (randomNum == 4)
                                            {
                                                soundFatality.Play();
                                                ScreenMsg("<color=#890000ff><b>FATALITY</b></color>");
                                            }

                                            if (randomNum == 5)
                                            {
                                                soundFatality.Play();
                                                ScreenMsg("<color=#890000ff><b>FATALITY</b></color>");
                                            }
                                        }
                                        else
                                        {
                                            System.Random num = new System.Random();
                                            int randomNum = num.Next(0, 5);

                                            if (randomNum == 1)
                                            {
                                                ScreenMsg("<color=#890000ff><b>THAT'S GOTTA HURT ...</b></color>");
                                            }

                                            if (randomNum == 2)
                                            {
                                                ScreenMsg("<color=#890000ff><b>GET A LITTLE CLOSER ... DON'T BE SHY</b></color>");
                                            }

                                            if (randomNum == 3)
                                            {
                                                ScreenMsg("<color=#890000ff><b>DIDN'T SEE THAT COMING ...</b></color>");
                                            }

                                            if (randomNum == 4)
                                            {
                                                ScreenMsg("<color=#890000ff><b>OUCH ...</b></color>");
                                            }

                                            if (randomNum == 5)
                                            {
                                                ScreenMsg("<color=#890000ff><b>ALL HAIL THE KRACKEN ...</b></color>");
                                            }
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
            }
            yield return new WaitForSeconds(1);
            disarming = false;
            disarm = false;
        }
    }
}
