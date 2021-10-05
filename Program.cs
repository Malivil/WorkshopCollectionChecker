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

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintAndPause($"USAGE: {Assembly.GetExecutingAssembly().GetName().Name} workshop_id");
                return;
            }

            var builder = new ConfigurationBuilder()
                          .SetBasePath(AppContext.BaseDirectory)
                          .AddJsonFile("appSettings.json", true, true);
            configuration = builder.Build();

            var workshopId = args[0];
            Console.WriteLine("Gathering addons list from workshop collection: " + workshopId);
            var addonIds = GetCollectionMembers(workshopId);

            var conflicts = GetSettingList("ConflictIds").ToList();
            foreach (var addonId in addonIds)
            {
                if (conflicts.Contains(addonId))
                {
                    Console.Error.WriteLine("Conflict found: " + addonId);
                }
            }
            Console.WriteLine("Done!");
        }

        private static IList<string> GetCollectionMembers(string collectionId)
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

        private static void PrintAndPause(string msg)
        {
            Console.WriteLine(msg);
            Console.ReadKey();
        }

        private static IEnumerable<string> GetSettingList(string setting)
            => configuration[setting].Split(';', ',', '|').ToList();
    }
}
