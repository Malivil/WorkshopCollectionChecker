using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WorkshopCollectionChecker
{
    public class Program
    {
        private static IConfiguration configuration;
        private const string AlternativesKey = "Alternatives";
        private const string ConVarsKey = "ConVars";
        private const string SharedAlternativeKey = "Alternative";

        private static readonly string[] ExcludedSections = [AlternativesKey, ConVarsKey];

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintAndPause($"USAGE: {Assembly.GetExecutingAssembly().GetName().Name} workshop_id [conflict_list]");
                return;
            }

            // Use whichever list is provided, or the default
            var conflictList = "All";
            if (args.Length >= 2)
            {
                conflictList = args[1];
            }

            var builder = new ConfigurationBuilder()
                          .SetBasePath(AppContext.BaseDirectory)
                          .AddJsonFile("appSettings.json", true, true);
            configuration = builder.Build();

            var workshopId = args[0];
            Console.WriteLine("Gathering addons list from workshop collection: " + workshopId);
            var addonIds = GetCollectionMembers(workshopId);
            var alternatives = GetSettingDictionary(AlternativesKey);

            if (conflictList.Equals("all", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (var section in configuration.GetChildren().Where(s => !ExcludedSections.Contains(s.Key) && !s.Key.EndsWith(SharedAlternativeKey)))
                {
                    Console.WriteLine($"Checking for conflicts in {section.Key} list");
                    string sharedAlternative = configuration.GetSection(section.Key + SharedAlternativeKey)?.Value;
                    CheckForConflicts(addonIds, section.Key, alternatives, sharedAlternative);
                }
            }
            else
            {
                Console.WriteLine($"Checking for conflicts in {conflictList} list");
                string sharedAlternative = configuration.GetSection(conflictList + SharedAlternativeKey)?.Value;
                CheckForConflicts(addonIds, conflictList, alternatives, sharedAlternative);
            }

            Console.WriteLine($"Checking for conflicts in {ConVarsKey} list");
            CheckForConVarConflicts(addonIds);

            Console.WriteLine("Done!");
        }

        private static void CheckForConflicts(IEnumerable<string> addonIds, string conflictList, Dictionary<string, string> alternatives, string sharedAlternative)
        {
            var conflicts = GetSettingDictionary(conflictList);
            foreach (var addonId in addonIds)
            {
                if (!conflicts.TryGetValue(addonId, out var conflict))
                {
                    continue;
                }

                PrintError($"Conflict found: {conflict} ({addonId})");
                if (alternatives.TryGetValue(addonId, out var alternative))
                {
                    PrintError($"\tAlternative: {alternative}");
                }
                else if (!string.IsNullOrWhiteSpace(sharedAlternative))
                {
                    PrintError($"\tAlternative: {sharedAlternative}");
                }
            }
        }

        private static void CheckForConVarConflicts(List<string> addonIds)
        {
            var convarsSection = configuration.GetSection(ConVarsKey);
            var convarsChildren = convarsSection.GetChildren();
            foreach (var convarChild in convarsChildren)
            {
                if (!addonIds.Contains(convarChild.Key))
                {
                    continue;
                }

                var conflicts = new ConVarConflicts();
                convarChild.Bind(conflicts);

                PrintWarning($"Potential conflict found: {conflicts.AddonName} ({convarChild.Key})");

                foreach (var conflict in conflicts.Conflicts)
                {
                    PrintWarning($"\tIf '{conflict.Name}' is set to '{conflict.Value}' then it can cause issues");
                    if (!string.IsNullOrWhiteSpace(conflict.Alternative))
                    {
                        PrintWarning($"\tAlternative: {conflict.Alternative}");
                    }
                }
            }
        }

        private static List<string> GetCollectionMembers(string collectionId)
        {
            var url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={collectionId}";
            var web = new HtmlWeb();
            var doc = web.Load(url);
            var collectionItems = doc.DocumentNode.Descendants("div").Where(n => n.HasClass("collectionItem"));

            var addonIds = new List<string>();
            foreach (var collectionItem in collectionItems)
            {
                var itemLink = collectionItem.Descendants("a").First();
                var itemUrl = itemLink.Attributes["href"].Value;
                var itemId = Regex.Match(itemUrl, ".*\\?id=(.*)").Groups[1].Value;
                addonIds.Add(itemId);
            }
            Console.WriteLine($"Found {addonIds.Count} collection items");

            return addonIds;
        }

        private static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void PrintAndPause(string msg)
        {
            Console.WriteLine(msg);
            Console.ReadKey();
        }

        private static Dictionary<string, string> GetSettingDictionary(string setting)
            => configuration.GetSection(setting).GetChildren()
                            .ToDictionary(x => x.Key, x => x.Value);
    }
}
