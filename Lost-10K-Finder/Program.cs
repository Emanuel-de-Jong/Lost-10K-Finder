using System;
using System.Collections.Generic;
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
        static void Main(string[] args)
        {
            List<string> knownkMapIds = GetKnownMapIds();
            List<string> knownMapNames = GetKnownMapNames();
            List<string> knownMapHashes = GetKnownMapHashes();

            string songsPath = AskSongsPath();

            Console.WriteLine("\nStarting search. This may take a while...");

            List<string> lost10kMapPaths = new List<string>();
            string[] mapPaths = Directory.GetDirectories(songsPath, "*", SearchOption.AllDirectories);
            string[] osuFilePaths;
            foreach (string mapPath in mapPaths)
            {
                if (!Directory.Exists(mapPath))
                {
                    Console.WriteLine("This folder name is too long: " + mapPath);
                    continue;
                }

                osuFilePaths = Directory.GetFiles(mapPath, "*.osu");
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
        /// Get the map id lists from github and combine them.
        /// </summary>
        static List<string> GetKnownMapIds()
        {
            string osuStr = "";
            string searchStr = "";

            if (useServerMapLists)
            {
                WebClient webClient = new WebClient();

                try
                {
                    osuStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/osu%20ids.txt");
                    searchStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/search%20ids.txt");
                }
                catch (Exception ex)
                {
                    End("Map ids couldn't be read from the server.\nPlease check your internet connection.");
                }
            }
            else
            {
                osuStr = File.ReadAllText(@"..\..\..\map lists\osu ids.txt");
                searchStr = File.ReadAllText(@"..\..\..\map lists\search ids.txt");
            }

            string[] osu = osuStr.Split('\n');
            string[] search = searchStr.Split('\n');

            return osu.Union(search).ToList();
        }


        /// <summary>
        /// Get the map names from github and combine them.
        /// </summary>
        static List<string> GetKnownMapNames()
        {
            string osuStr = "";
            string searchStr = "";

            if (useServerMapLists)
            {
                WebClient webClient = new WebClient();

                try
                {
                    osuStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/osu%20names.txt");
                    searchStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/search%20names.txt");
                }
                catch (Exception ex)
                {
                    End("Map names couldn't be read from the server.\nPlease check your internet connection.");
                }
            }
            else
            {
                osuStr = File.ReadAllText(@"..\..\..\map lists\osu names.txt");
                searchStr = File.ReadAllText(@"..\..\..\map lists\search names.txt");
            }

            string[] osu = osuStr.Split('\n');
            string[] search = searchStr.Split('\n');

            return osu.Union(search).ToList();
        }

        /// <summary>
        /// Get the map hashes from github and combine them.
        /// </summary>
        static List<string> GetKnownMapHashes()
        {
            string packStr = "";
            string pendingStr = "";
            string rejectedStr = "";

            if (useServerMapLists)
            {
                WebClient webClient = new WebClient();

                try
                {
                    packStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/pack%20hash.txt");
                    pendingStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/pending%20hash.txt");
                    rejectedStr = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/map%20lists/rejected%20hash.txt");
                }
                catch (Exception ex)
                {
                    End("Map hashes couldn't be read from the server.\nPlease check your internet connection.");
                }
            }
            else
            {
                packStr = File.ReadAllText(@"..\..\..\map lists\pack hashes.txt");
                pendingStr = File.ReadAllText(@"..\..\..\map lists\pending hashes.txt");
                rejectedStr = File.ReadAllText(@"..\..\..\map lists\removed hashes.txt");
            }

            string[] pack = packStr.Split('\n');
            string[] pending = pendingStr.Split('\n');
            string[] rejected = rejectedStr.Split('\n');

            return pack.Union(pending.Concat(rejected)).ToList();
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

            if (mapId.Length >= 5 && knownMapIds.Contains(mapId))
            {
                return true;
            }
            else if (knownMapHashes.Contains(GetDirHash(mapPath)))
            {
                return true;
            }
            else
            {
                Match match = filterName.Match(mapName);
                if (match.Success)
                    mapName = mapName.Replace(match.Value, "");

                if (knownMapNames.Contains(mapName) ||
                        knownMapNames.Contains(mapName + "[no video]") ||
                        knownMapNames.Contains(mapName.Replace("_", " ")) ||
                        knownMapNames.Contains(mapName.Replace("_", " ") + "[no video]"))
                    return true;
            }

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


        static string GetDirHash(string path)
        {
            string[] filePaths = Directory.GetFiles(path, "*.osu").OrderBy(p => p).ToArray();

            using (var hasher = MD5.Create())
            {
                foreach (var filePath in filePaths)
                {
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
            if (osuFilePath.EndsWith("a10.osu"))
                return false;

            StreamReader file = new StreamReader(osuFilePath);
            int hitObjectCount = 0;
            int phase = 0;
            string line;
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
                    hitObjectCount++;

                    if (hitObjectCount == 10)
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
