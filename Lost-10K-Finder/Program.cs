using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Lost_10K_Finder
{
    class Program
    {
        enum KnownMapsType
        {
            Id,
            Name,
            Hash
        }
        static void Main(string[] args)
        {
            List<string> knownkMapIds = GetKnownMaps(new string[] { "osu-ids", "search-ids" }, KnownMapsType.Id);
            List<string> knownMapNames = GetKnownMaps(new string[] { "osu-names", "search-names" }, KnownMapsType.Name);
            List<string> knownMapHashes = GetKnownMaps(new string[] { "pack-hashes", "rejected-hashes", "pending-hashes" }, KnownMapsType.Hash);

            string songsPath = AskSongsPath();

            Console.WriteLine("\nStarting search. This may take a while...");

            List<string> lost10kMapPaths = new List<string>();
            string[] mapPaths = Directory.GetDirectories(songsPath, "*", SearchOption.AllDirectories);
            foreach (string mapPath in mapPaths)
            {
                if (!Directory.Exists(mapPath))
                {
                    Console.WriteLine("This folder name is too long: " + mapPath);
                    continue;
                }

                string[] osuFilePaths = Directory.GetFiles(mapPath, "*.osu");
                if (osuFilePaths.Length == 0)
                    continue;

                // Only maps that are not known need to be checked
                if (IsKnownMap(mapPath, knownkMapIds, knownMapNames, knownMapHashes))
                    continue;

                foreach (string osuFilePath in osuFilePaths)
                {
                    // If a valid 10k osu file was found, add the map to 'lost10kMapPaths' and stop checking the map's osu files
                    if (IsValid10kOsuFile(osuFilePath))
                    {
                        lost10kMapPaths.Add(mapPath.Substring(songsPath.Length + 1));
                        break;
                    }
                }
            }

            if (lost10kMapPaths.Count != 0)
                File.WriteAllLines("lost maps.txt", lost10kMapPaths);

            End(CreateEndMessage(lost10kMapPaths));
        }

        
        static bool useServerMapLists = true;
        /// <summary>
        /// Get the given lists from github and combine them.
        /// </summary>
        static List<string> GetKnownMaps(string[] lists, KnownMapsType knownMapsType)
        {
            string[] listStrings = new string[lists.Length];

            if (useServerMapLists)
            {
                WebClient webClient = new WebClient();

                for (int i = 0; i < lists.Length; i++)
                {
                    try
                    {
                        listStrings[i] = webClient.DownloadString($"https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/{ lists[i] }.txt");
                    }
                    catch (Exception ex)
                    {
                        End(lists[i] + " couldn't be read from the server.\nPlease check your internet connection.");
                    }
                }
            }
            else
            {
                for (int i = 0; i < lists.Length; i++)
                {
                    listStrings[i] = File.ReadAllText($@"..\..\..\map lists\{ lists[i] }.txt");
                }
            }

            HashSet<string> knownMaps = new HashSet<string>();
            for (int i = 0; i < listStrings.Length; i++)
            {
                foreach (string line in listStrings[i].Split('\n'))
                {
                    string fixedLine = line;
                    if (knownMapsType == KnownMapsType.Hash)
                        fixedLine = line.Split(' ')[0];

                    knownMaps.Add(fixedLine);
                }
            }

            return knownMaps.ToList();
        }


        /// <summary>
        /// Asks the user for the path to their songs forlder and returns it if it exists.
        /// </summary>
        static string AskSongsPath()
        {
            Console.WriteLine("Please paste the path to your songs folder and press enter.");
            Console.WriteLine(@"Normally it's at: C:\Users\YOURUSERNAME\AppData\Local\osu!\Songs");
            string songsPath = Console.ReadLine().Trim();

            if (!Directory.Exists(songsPath))
                End("The path does not exist.");

            return songsPath;
        }


        // Check for " " + ("[no video]" || "(digit)")
        // Every " " can be an "_"
        // Do this check 1..* times
        static Regex filterName = new Regex(@"([ _](\[no[ _]video\]|\([0-9]+\)))+", RegexOptions.Compiled);
        /// <summary>
        /// Check if the given map name is in the known ids or names already
        /// </summary>
        static bool IsKnownMap(string mapPath, List<string> knownMapIds, List<string> knownMapNames, List<string> knownMapHashes)
        {
            string mapName = Path.GetFileName(mapPath);
            string mapId = GetMapIdFromName(mapName);

            if (mapId.Length >= 5)
            {
                // Check id
                if (knownMapIds.Contains(mapId))
                    return true;
            }
            else
            {
                // Check name
                Match match = filterName.Match(mapName);
                if (match.Success)
                    mapName = mapName.Replace(match.Value, "");

                if (knownMapNames.Contains(mapName) || knownMapNames.Contains(mapName.Replace("_", " ")))
                    return true;
            }

            // Check hash
            if (Directory.Exists(mapPath) && knownMapHashes.Contains(GetDirHash(mapPath)))
                return true;

            return false;
        }


        static string GetMapIdFromName(string mapName)
        {
            string mapId = "";

            foreach (char c in mapName)
            {
                if (!Char.IsDigit(c))
                    break;

                mapId += c;
            }

            return mapId;
        }


        static Regex checkAutomap = new Regex(@".osu.a[0-9]+.osu$", RegexOptions.Compiled);
        static string GetDirHash(string path)
        {
            string[] filePaths = Directory.GetFiles(path, "*.osu").OrderBy(p => p).ToArray();

            using (var hasher = MD5.Create())
            {
                foreach (string filePath in filePaths)
                {
                    if (!File.Exists(filePath))
                        continue;

                    // Check if it's an automap convert
                    if (checkAutomap.IsMatch(filePath))
                        continue;

                    byte[] pathBytes = Encoding.UTF8.GetBytes(filePath.Substring(path.Length + 1));
                    hasher.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    byte[] contentBytes = File.ReadAllBytes(filePath);
                    hasher.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                return BitConverter.ToString(hasher.Hash).Replace("-", "");
            }
        }


        /// <summary>
        /// Validates the given file on if it is 10k and if it has HitObjects.
        /// </summary>
        static bool IsValid10kOsuFile(string osuFilePath)
        {
            if (!File.Exists(osuFilePath))
            {
                Console.WriteLine("This filename is too long: " + osuFilePath);
                return false;
            }

            // Check if it's an automap convert
            if (checkAutomap.IsMatch(osuFilePath))
                return false;

            StreamReader file = new StreamReader(osuFilePath);
            int phase = 0;
            string line;
            int firstTime = 0;
            while ((line = file.ReadLine()) != null)
            {
                // Check if it's mania
                if (phase == 0)
                {
                    if (line.StartsWith("Mode:"))
                    {
                        if (line == "Mode: 3")
                            phase++;
                        else
                            break;
                    }
                }
                // Check if it's 10k
                else if (phase == 1)
                {
                    if (line.StartsWith("CircleSize:"))
                    {
                        if (line == "CircleSize:10")
                            phase++;
                        else
                            break;
                    }
                }
                // Wait for hitobjects section
                else if (phase == 2)
                {
                    if (line == "[HitObjects]")
                        phase++;
                }
                // Check if there are 10 or more hit objects
                else if (phase == 3)
                {
                    if (line == "")
                        break;

                    if (!int.TryParse(line.Split(',')[2], out firstTime))
                        continue;

                    phase++;
                }
                else if (phase == 4)
                {
                    if (line == "")
                        break;

                    if (!int.TryParse(line.Split(',')[2], out int time))
                        continue;

                    if ((time - firstTime) >= 15000)
                        return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Tells the user if any maps were found.
        /// And if so, which ones and where to find them.
        /// </summary>
        static string CreateEndMessage(List<string> lost10kMapPaths)
        {
            string endMessage;
            if (lost10kMapPaths.Count == 0)
            {
                endMessage = "No lost maps were found";
            }
            else
            {
                endMessage = "The following lost maps were found:\n";
                foreach (string mapPath in lost10kMapPaths)
                    endMessage += mapPath + "\n";

                endMessage += "\nThis list can also be found in \"lost maps.txt\"";
            }

            return endMessage;
        }


        /// <summary>
        /// Displays the given message and waits for any input before closing the program.
        /// </summary>
        static void End(string message)
        {
            Console.WriteLine("\n" + message);
            Console.Write("\nPress any key to exit...");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }
}
