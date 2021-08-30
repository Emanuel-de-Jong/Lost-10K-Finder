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
        private enum KnownMapsType
        {
            Id,
            Name,
            Hash
        }


        private static readonly Regex checkAutomap = new Regex(@".osu.[0-9]*a[0-9]+.osu$", RegexOptions.Compiled);
        private static void Main(string[] args)
        {
            Console.Write("Downloading lists of known map ids, names and hashes... ");
            List<string> knownMapIds = GetKnownMaps(new string[] { "osu-ids", "search-ids" }, KnownMapsType.Id);
            List<string> knownMapNames = GetKnownMaps(new string[] { "osu-names", "search-names" }, KnownMapsType.Name);
            List<string> knownMapHashes = GetKnownMaps(new string[] { "pack-hashes", "rejected-hashes", "pending-hashes" }, KnownMapsType.Hash);
            Console.WriteLine("complete\n");

            string songsPath = GetSongsPath();

            Console.WriteLine("\n Searching... This may take a while");

            List<string> lost10kMapPaths = new List<string>();
            string[] mapPaths = Directory.GetDirectories(songsPath, "*", SearchOption.AllDirectories);
            foreach (string mapPath in mapPaths)
            {
                if (!Directory.Exists(mapPath))
                {
                    Console.WriteLine("This folder name is too long: " + mapPath);
                    continue;
                }

                List<string> osuFilePaths = new List<string>();
                foreach (string filePath in Directory.GetFiles(mapPath, "*.osu"))
                {
                    if (checkAutomap.IsMatch(filePath))
                        continue;

                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine("This filename is too long: " + filePath);
                        continue;
                    }

                    osuFilePaths.Add(filePath);
                }

                if (osuFilePaths.Count == 0)
                    continue;

                string mapName = mapPath.Substring(songsPath.Length + 1);
                Console.WriteLine(mapName);

                if (!HasValid10kOsuFile(osuFilePaths))
                    continue;

                if (!IsKnownMap(mapPath, osuFilePaths, knownMapIds, knownMapNames, knownMapHashes))
                    lost10kMapPaths.Add(mapName);
            }

            if (lost10kMapPaths.Count != 0)
                File.WriteAllLines("lost maps.txt", lost10kMapPaths);

            Console.Clear();
            End(CreateEndMessage(lost10kMapPaths));
        }


        private static readonly bool useServerMapLists = true;
        private static readonly string githubUrl = "https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/Assets/Lists/";
        /// <summary>
        /// Get the given lists from github and combine them.
        /// </summary>
        private static List<string> GetKnownMaps(string[] lists, KnownMapsType knownMapsType)
        {
            List<string[]> listLines = new List<string[]>();

            if (useServerMapLists)
            {
                WebClient webClient = new WebClient();

                foreach (string list in lists)
                {
                    try
                    {
                        listLines.Add(webClient.DownloadString(githubUrl + list + ".txt").Split('\n'));
                    }
                    catch (Exception)
                    {
                        End($"\n{ list } couldn't be read from the server.\nPlease check your internet connection.");
                    }
                }
            }
            else
            {
                foreach(string list in lists)
                {
                    listLines.Add(File.ReadAllLines($@"..\..\..\..\Assets\Lists\{ list }.txt"));
                }
            }

            HashSet<string> knownMaps = new HashSet<string>();
            foreach (string[] lines in listLines)
            {
                foreach (string line in lines)
                {
                    string fixedLine = line;

                    if (knownMapsType == KnownMapsType.Hash)
                    {
                        fixedLine = line.Split(' ')[0];
                        if (fixedLine == "")
                            continue;
                    }

                    knownMaps.Add(fixedLine);
                }
            }

            return knownMaps.ToList();
        }


        /// <summary>
        /// Check if the program is in the songs folder
        /// Otherwise ask the user for the path to their songs forlder
        /// </summary>
        private static string GetSongsPath()
        {
            string currentDir = Directory.GetCurrentDirectory();

            if (currentDir.EndsWith(@"\osu!\Songs"))
            {
                Console.Write("\nPress any key to start searching...");
                Console.ReadKey();

                return currentDir;
            }
            else
            {
                Console.WriteLine("Please paste the path to your songs folder and press enter.");
                Console.WriteLine(@"Normally it's at: C:\Users\YOURUSERNAME\AppData\Local\osu!\Songs");
                Console.Write("Path: ");
                string songsPath = Console.ReadLine().Trim();

                if (!Directory.Exists(songsPath))
                    End("The path does not exist.");

                return songsPath;
            }
        }


        // Check for " " + ("[no video]" || "(digit)")
        // Do this check 1..* times
        // Every " " can be an "_"
        private static readonly Regex filterName = new Regex(@"([ _](\[no[ _]video\]|\([0-9]+\)))+", RegexOptions.Compiled);
        /// <summary>
        /// Check if the given map is uploaded, in the pack, or rejected already
        /// </summary>
        private static bool IsKnownMap(string mapPath, List<string> osuFilePaths, List<string> knownMapIds, List<string> knownMapNames, List<string> knownMapHashes)
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
            string mapHash = GetDirHash(mapPath, osuFilePaths);
            if (knownMapHashes.Contains(mapHash))
            {
                return true;
            }
            else
            {
                knownMapHashes.Add(mapHash);
            }

            return false;
        }


        private static string GetMapIdFromName(string mapName)
        {
            string mapId = "";

            foreach (char c in mapName)
            {
                if (!char.IsDigit(c))
                    break;

                mapId += c;
            }

            return mapId;
        }


        /// <summary>
        /// Make a MD5 hash from the osu files of a map
        /// </summary>
        private static string GetDirHash(string mapPath, List<string> osuFilePaths)
        {
            osuFilePaths = osuFilePaths.OrderBy(p => p).ToList();

            using (MD5 hasher = MD5.Create())
            {
                foreach (string osuFilePath in osuFilePaths)
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(osuFilePath.Substring(mapPath.Length + 1));
                    hasher.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                    byte[] contentBytes = File.ReadAllBytes(osuFilePath);
                    hasher.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }

                hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                return BitConverter.ToString(hasher.Hash).Replace("-", "");
            }
        }


        /// <summary>
        /// Check if one of the given osu files is valid and 10k
        /// </summary>
        private static bool HasValid10kOsuFile(List<string> osuFilePaths)
        {
            foreach (string osuFilePath in osuFilePaths)
            {
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
            }

            return false;
        }


        /// <summary>
        /// Tell the user if any maps were found
        /// And if so, which ones and where to find them
        /// </summary>
        private static string CreateEndMessage(List<string> lost10kMapPaths)
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
                {
                    endMessage += mapPath + "\n";
                }

                endMessage += "\nThis list can also be found in \"lost maps.txt\"";
            }

            return endMessage;
        }


        /// <summary>
        /// Display the given message and wait for any input before closing the program
        /// </summary>
        private static void End(string message)
        {
            Console.WriteLine(message);
            Console.Write("\nPress any key to exit...");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }
}
