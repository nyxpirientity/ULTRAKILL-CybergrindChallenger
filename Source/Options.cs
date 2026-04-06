using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions;

namespace Nyxpiri.ULTRAKILL.CybergrindChallenger
{
    public static class Options
    {
        public static Dictionary<string, List<string>> Challenges = null;
        public static List<string> StandardChallenges = null;
        public static List<string> IntenseChallenges = null;
        public static string ChallengesConfigPath = null;

        internal static void Initialize(ConfigFile config)
        {
            Assert.IsNotNull(config);
 
            Config = config;

            ChallengesConfigPath = $"{BepInEx.Paths.ConfigPath}/com.nyxpiri.cybergrind-challenger.challenges-cfg.json";

            Challenges = new Dictionary<string, List<string>>
            {
                {"SEEING DOUBLE", new List<string>{HeckPuppets, GiveEnemiesFriends}},
                {"BRUTAL HECK", new List<string>{AggressiveAgony, HeatOfHeck}},
                {"GET UP ON THEIR BACK", new List<string>{HydraMode, HeatOfHeck}},
                {"MISCONFIGURED", new List<string>{SelfConscience, BadGyro}},
                {"ULTRACARE", new List<string>{GiveEnemiesFriends, BloodFueledEnemies}},
                {"NOW SWAP",new List<string>{NyxLib.Cheats.SandAllEnemies, BloodFueledEnemies}},
                {"A LOOK IN THE MIRROR", new List<string>{ BloodFueledEnemies, FeedbackersForEveryone }},
                {"EVERYONE'S A BOOSTER", new List<string>{ AggressiveAgony, FeedbackersForEveryone }},
                {"BAD GAME DESIGN", new List<string>{ BadGyro, MundaneMurder }},
                {"PAINFULLY SALTY", new List<string>{ SaltyEnemies, AggressiveAgony }},
                {"AA FORCERAD", new List<string>{ AggressiveAgony, NyxLib.Cheats.RadiantAllEnemies }},

                {"SSADISTIC HECK", new List<string>{ SaltyEnemies, SelfConscience, HeatOfHeck, AggressiveAgony }},
                {"HECK SPECIAL", new List<string>{ SaltyEnemies, HeckPuppets, HeatOfHeck, AggressiveAgony }},
                {"STYLE ISSUE", new List<string>{ SaltyEnemies, SelfConscience, HeatOfHeck, HeckPuppets }},
                {"HEAT OF GREED", new List<string>{ NyxLib.Cheats.SandAllEnemies, BloodFueledEnemies, HeatOfHeck }},
                {"TECH ISSUES", new List<string>{ BadGyro, HeatOfHeck, SelfConscience }},
                {"ULTRASWAP",new List<string>{NyxLib.Cheats.SandAllEnemies, BloodFueledEnemies, FeedbackersForEveryone}},
                {"COUNTER-COUNTER-ER", new List<string>{ AggressiveAgony, FeedbackersForEveryone, HeatOfHeck }},
                {"FURIOUS MITOSIS", new List<string>{ SaltyEnemies, HeatOfHeck, HydraMode }}
            };

            StandardChallenges = new List<string>
            {
                "BRUTAL HECK", "ULTRACARE", "NOW SWAP", "PAINFULLY SALTY", "AA FORCERAD", "A LOOK IN THE MIRROR", "EVERYONE'S A BOOSTER",
            };

            IntenseChallenges = new List<string>
            {
                "SSADISTIC HECK", "HECK SPECIAL", "STYLE ISSUE", "HEAT OF GREED", "COUNTER-COUNTER-ER", "ULTRASWAP",
            };

            if (!File.Exists(ChallengesConfigPath))
            {
                SaveChallengesConfig();
            }
            else
            {
                try
                {
                    LoadChallengesConfig();                    
                }
                catch (System.Exception e)
                {
                    if (e is InvalidDataException)
                    {
                        Log.Error($"Failed to read challenges config due to InvalidDataException... {e}");
                    }
                }
            }
        }

        private static void LoadChallengesConfig()
        {
            string jsonString = File.ReadAllText(ChallengesConfigPath);
            var data = JsonConvert.DeserializeObject<JObject>(jsonString) ?? throw new InvalidDataException();
            var challengesGeneric = data.GetValue("Challenges");

            if (challengesGeneric == null)
            {
                throw new InvalidDataException("Challenges was not present");
            }

            if (challengesGeneric.Type != JTokenType.Object)
            {
                throw new InvalidDataException("Challenges was not an JsonObject");
            }

            var jChallenges = challengesGeneric as JObject;
            var challenges = new Dictionary<string, List<string>>();

            Assert.IsNotNull(jChallenges);

            foreach (var item in jChallenges)
            {
                var cheatsGeneric = item.Value;

                if (cheatsGeneric.Type != JTokenType.Array)
                {
                    continue;
                }

                var jCheats = cheatsGeneric as JArray;

                Assert.IsNotNull(cheatsGeneric);

                List<string> cheats = new List<string>(jCheats.Count);

                foreach (var cheat in jCheats)
                {
                    if (cheat.Type != JTokenType.String)
                    {
                        continue;
                    }

                    cheats.Add(cheat.ToString());
                }

                challenges.Add(item.Key, cheats);
            }

            List<string> standardChallenges = new List<string>();
            ExtractChallengesDataFromJArray(data, challenges, standardChallenges, "StandardChallenges");

            List<string> intenseChallenges = new List<string>();
            ExtractChallengesDataFromJArray(data, challenges, intenseChallenges, "IntenseChallenges");

            Challenges = challenges;
            StandardChallenges = standardChallenges;
            IntenseChallenges = intenseChallenges;
        }

        private static void ExtractChallengesDataFromJArray(JObject data, Dictionary<string, List<string>> challenges, List<string> challengeNames, string jArrayName)
        {
            var challengeNamesGeneric = data.GetValue(jArrayName);

            if (challengeNamesGeneric == null)
            {
                throw new InvalidDataException($"{jArrayName} did not exist, which is invalid");
            }

            if (challengeNamesGeneric.Type != JTokenType.Array)
            {
                throw new InvalidDataException($"{jArrayName} was not an array of strings, which is invalid");
            }

            var jChallengeNames = challengeNamesGeneric as JArray;
            foreach (var challengeGeneric in jChallengeNames)
            {
                if (challengeGeneric.Type != JTokenType.String)
                {
                    throw new InvalidDataException($"{jArrayName} contained a token which wasn't a string, which is invalid");
                }

                string challengeName = challengeGeneric.ToString();

                if (!challenges.ContainsKey(challengeName))
                {
                    throw new InvalidDataException($"{jArrayName} contained challenge by name {challengeName} which did not seem to exist");
                }

                challengeNames.Add(challengeName);
            }
        }

        private static void SaveChallengesConfig()
        {
            Dictionary<string, object> JsonData = new Dictionary<string, object>
            {
                {"Challenges", Challenges},
                {"StandardChallenges", StandardChallenges},
                {"IntenseChallenges", IntenseChallenges},
            };

            string jsonString = JsonConvert.SerializeObject(JsonData, Formatting.Indented, new JsonSerializerSettings { });

            File.WriteAllText(ChallengesConfigPath, jsonString);
        }

        internal static void Load()
        {
            LoadChallengesConfig();
        }

        const string BadGyro = "nyxpiri.bad-gyro";
        const string AggressiveAgony = "nyxpiri.aggressive-agony";
        const string HeatOfHeck = "nyxpiri.heat-of-heck";
        const string GiveEnemiesFriends = "nyxpiri.give-enemies-friends";
        const string BloodFueledEnemies = "nyxpiri.blood-fueled-enemies";
        const string SelfConscience = "nyxpiri.self-conscience";
        const string MundaneMurder = "nyxpiri.mundane-murder";
        const string SaltyEnemies = "nyxpiri.salty-enemies";
        const string HeckPuppets = "nyxpiri.heck-puppets";
        const string HydraMode = "nyxpiri.hydra-mode";
        const string FeedbackersForEveryone = "nyxpiri.feedbackers-for-everyone";

        private static ConfigFile Config = null;
    }
}
