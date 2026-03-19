using UnityEngine;
using BepInEx;
using System.Collections.Generic;
using Nyxpiri.ULTRAKILL.NyxLib;
using ULTRAKILL.Cheats;
using System;
using System.IO;

namespace Nyxpiri.ULTRAKILL.CybergrindChallenger
{
    public static class Cheats
    {
        public const string CybergrindChallenger = "nyxpiri.cybergrind-challenger";
    }
    
    [BepInPlugin("nyxpiri.ultrakill.cybergrind-challenger", "Cybergrind Challenger", "0.0.0.1")]
    [BepInProcess("ULTRAKILL.exe")]
    public class CybergrindChallenger : BaseUnityPlugin
    {
        protected void Awake()
        {
            Log.Initialize(Logger);
            Options.Initialize(Config);
            Cybergrind.PreCybergrindBegin += OnCybergrindBegin;
            Cybergrind.PostCybergrindNextWave += OnNextWaveBegin;
            NyxLib.Cheats.ReadyForCheatRegistration += RegisterCheats;
        }

        private void RegisterCheats(CheatsManager cheatsManager)
        {
            cheatsManager.RegisterCheat(new ToggleCheat(
                "Cybergrind Challenger", 
                Cheats.CybergrindChallenger,
                onDisable: (cheat) =>
                {
                },
                onEnable: (cheat, manager) =>
                {
                    if (Cybergrind.IsActive)
                    {
                        RefreshChallenges();
                    }
                }
            ), "CYBERGRIND");
        }

        protected void Start()
        {
        }

        protected void Update()
        {

        }

        protected void LateUpdate()
        {

        }

        private void OnCybergrindBegin(EventMethodCanceler cancelInfo, EndlessGrid endlessGrid)
        {
            if (NyxLib.Cheats.IsCheatDisabled(Cheats.CybergrindChallenger))
            {
                return;
            }

            RefreshChallenges();
        }

        private void RefreshChallenges()
        {
            IntenseChallenges.Clear();
            Challenges.Clear();

            try
            {
                Options.Load();
            }
            catch (System.Exception e)
            {
                if (e is InvalidDataException)
                {
                    Log.Error($"Failed to load options. :c Here's info maybe probably '{e.Message}'");
                    QuickMsgPool.DisplayQuickMsg("CYBERGRINDCHALLENGER FAILED TO LOAD OPTIONS :c", Color.red, 8.0f, velocity: Vector3.down * 400.0f, 42.0f, false);
                    QuickMsgPool.DisplayQuickMsg($"{e.Message}", Color.red, 8.0f, velocity: Vector3.down * 560.0f, 16.0f, false);
                    return;
                }
                else
                {
                    throw;
                }
            }

            var challengesConfigs = Options.Challenges;

            var intenseChallengesNames = Options.IntenseChallenges;
            var standardChallengesNames = Options.StandardChallenges;

            bool hitProblems = false;

            challengeIdx = 0;
            intenseChallengeIdx = 0;
            CheatsEnabledByUs = new string[0];

            FieldPublisher<CheatsManager, Dictionary<string, ICheat>> idToCheat = new FieldPublisher<CheatsManager, Dictionary<string, ICheat>>(NyxLib.Cheats.Manager, "idToCheat");

            foreach (var challenge in challengesConfigs)
            {
                Log.Debug($"Challenge exists by named {challenge.Key}");
            }

            foreach (var challengeName in intenseChallengesNames)
            {
                if (!challengesConfigs.TryGetValue(challengeName, out var challengeCheats))
                {
                    Log.Warning($"Failed to find challenge in intense challenge list, named {challengeName}");
                    hitProblems = true;
                    continue;
                }

                bool nonExistentCheatFound = false;
                foreach (var cheatName in challengeCheats)
                {
                    if (!idToCheat.Value.ContainsKey(cheatName))
                    {
                        Log.Warning($"Failed to find cheat by name {cheatName}, thus canceling intense challenge {challengeName}! (You may need a mod for the cheat? Including default included cheats!)");
                        hitProblems = true;
                        nonExistentCheatFound = true;
                        break;
                    }
                }

                if (nonExistentCheatFound)
                {
                    continue;
                }

                Log.Debug($"Adding intense challenge by name {challengeName}");
                IntenseChallenges.Add((challengeName, challengeCheats.ToArray()));
            }

            foreach (var challengeName in standardChallengesNames)
            {
                if (!challengesConfigs.TryGetValue(challengeName, out var challengeCheats))
                {
                    Log.Warning($"Failed to find challenge in standard challenge list, named {challengeName}");
                    hitProblems = true;
                    continue;
                }
                
                bool nonExistentCheatFound = false;
                foreach (var cheatName in challengeCheats)
                {
                    if (!idToCheat.Value.ContainsKey(cheatName))
                    {
                        Log.Warning($"Failed to find cheat by name {cheatName}, thus canceling standard challenge {challengeName}! (You may need a mod for the cheat? Including default included cheats!)");
                        hitProblems = true;
                        nonExistentCheatFound = true;
                        break;
                    }
                }

                if (nonExistentCheatFound)
                {
                    continue;
                }

                Log.Debug($"Adding challenge by name {challengeName}");
                Challenges.Add((challengeName, challengeCheats.ToArray()));
            }

            if (hitProblems)
            {
                QuickMsgPool.DisplayQuickMsg("CybergrindChallenger ran into some problems!", Color.yellow, 8.0f, velocity: Vector3.down * 400.0f, 42.0f, false);
                QuickMsgPool.DisplayQuickMsg("Logs should have more info, so long as BepInEx is configured to display the right log types!", Color.yellow, 8.0f, velocity: Vector3.down * 560.0f, 16.0f, false);
            }
        }

        private void OnNextWaveBegin(EventMethodCancelInfo cancelInfo, EndlessGrid eg)
        {
            for (int i = 0; i < CheatsEnabledByUs.Length; i++)
            {
                NyxLib.Cheats.Manager.DisableCheat(CheatsEnabledByUs[i]);
            }
            
            CheatsEnabledByUs = new string[0];

            if (NyxLib.Cheats.IsCheatDisabled(Cheats.CybergrindChallenger))
            {
                return;
            }

            if (NyxLib.Cheats.Enabled)
            {
                NyxLib.Cheats.Manager.ToggleCheat(NyxLib.Cheats.Manager.GetCheatInstance<KillAllEnemies>());
            }

            bool useIntenseChallenges = (((eg.currentWave % 5) == 0 || (eg.currentWave % 5) == 2) && eg.currentWave != eg.startWave) && IntenseChallenges.Count > 0;

            if (Challenges.Count == 0 && IntenseChallenges.Count == 0)
            {

                QuickMsgPool.DisplayQuickMsg($"WE HAVE NO USABLE CHALLENGES? :c", new Color(0.9f, 0.9f, 0.6f), 8.0f, velocity: Vector3.down * 200.0f, 42.0f);
                return;
            }

            List<(string, string[])> challengePool = useIntenseChallenges ? IntenseChallenges : Challenges;
            
            ref int currentChallengeIdx = ref (useIntenseChallenges ? ref challengeIdx : ref intenseChallengeIdx);

            if (currentChallengeIdx == 0)
            {
                if (useIntenseChallenges)
                {
                    for (int i = 0; i < IntenseChallenges.Count; i++)
                    {
                        int targetIdx = UnityEngine.Random.Range(0, IntenseChallenges.Count - 1);
                        var movingValue = IntenseChallenges[i];
                        IntenseChallenges.RemoveAt(i);
                        IntenseChallenges.Insert(targetIdx, movingValue);
                    }
                }
                else
                {
                    for (int i = 0; i < Challenges.Count; i++)
                    {
                        int targetIdx = UnityEngine.Random.Range(0, Challenges.Count - 1);
                        var movingValue = Challenges[i];
                        Challenges.RemoveAt(i);
                        Challenges.Insert(targetIdx, movingValue);
                    }
                }
            }
            
            var challenge = challengePool[currentChallengeIdx];
            currentChallengeIdx = (currentChallengeIdx + 1) % (useIntenseChallenges ? IntenseChallenges.Count : Challenges.Count);
            
            FieldPublisher<CheatsManager, Dictionary<string, ICheat>> idToCheat = new FieldPublisher<CheatsManager, Dictionary<string, ICheat>>(NyxLib.Cheats.Manager, "idToCheat");
            CheatsEnabledByUs = challenge.Item2;

            float downwardOffsetBase = 0.0f;

            if (useIntenseChallenges)
            {
                QuickMsgPool.DisplayQuickMsg($"INTENSITY SPIKE", new Color(1.0f, 0.1f, 0.1f), 5.0f, velocity: Vector3.down * 200.0f, 42.0f);
                QuickMsgPool.DisplayQuickMsg($"{challenge.Item1}", new Color(1.0f, 0.3f, 0.3f), 5.0f, velocity: Vector3.down * 320.0f, 36.0f);
                downwardOffsetBase = 500.0f;
            }
            else
            {
                QuickMsgPool.DisplayQuickMsg($"{challenge.Item1}", new Color(1.0f, 0.4f, 0.4f), 5.0f, velocity: Vector3.down * 220.0f, 36.0f);
                downwardOffsetBase = 350.0f;
            }

            for (int i = 0; i < challenge.Item2.Length; i++)
            {
                string cheatName = challenge.Item2[i];
                var cheat = idToCheat.Value[cheatName];
                
                QuickMsgPool.DisplayQuickMsg($"+ {cheat.LongName.ToUpper()}", new Color(0.875f, 0.75f, 1.0f), 5.0f, velocity: Vector3.down * ((float)((i) * 90.0f) + downwardOffsetBase), 24.0f);
                cheat.Enable(NyxLib.Cheats.Manager);
            }
            
            NyxLib.Cheats.Manager.RefreshCheatStates();
        }

        private int intenseChallengeIdx = 0;
        private int challengeIdx = 0;
        private string[] CheatsEnabledByUs = new string[0];
        private List<(string, string[])> Challenges = new List<(string, string[])>();
        private List<(string, string[])> IntenseChallenges = new List<(string, string[])>();
    }
}
