using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Lost_10K_Finder
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> knownkMapIds = GetKnownMapIds();
            List<string> knownCustomMapNames = GetKnownCustomMapNames();

            string songsPath = AskSongsPath();

            Console.WriteLine("\nStarting search. This may take a while...");

            List<string> lost10kMapNames = new List<string>();
            string[] mapPaths = Directory.GetDirectories(songsPath);
            string[] osuFilePaths;
            string mapName;
            foreach (string mapPath in mapPaths)
            {
                mapName = Path.GetFileName(mapPath);

                // Only maps that are not known need to be checked
                if (IsKnownMap(mapName, knownkMapIds, knownCustomMapNames))
                    continue;

                osuFilePaths = Directory.GetFiles(mapPath, "*.osu");
                foreach (string osuFilePath in osuFilePaths)
                {
                    // If a valid 10k osu file was found, add the map to 'lost10kMapNames' and stop checking the map's osu files
                    if (IsValid10kOsuFile(osuFilePath))
                    {
                        lost10kMapNames.Add(mapName);
                        break;
                    }
                }
            }

            if (lost10kMapNames.Count != 0)
                File.WriteAllLines("lost maps.txt", lost10kMapNames);

            End(CreateEndMessage(lost10kMapNames));
        }


        /// <summary>
        /// Get the map id lists from github and combine them.
        /// </summary>
        static List<string> GetKnownMapIds()
        {
            WebClient webClient = new WebClient();

            string packMapIdsString = "";
            string pendingMapIdsString = "";
            string uploadedMapIdsString = "";
            try
            {
                packMapIdsString = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/PackMapIds.txt");
                pendingMapIdsString = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/PendingMapIds.txt");
                uploadedMapIdsString = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/UploadedMapIds.txt");
            }
            catch (Exception e)
            {
                End("Map ids couldn't be read from the server.\nPlease check your internet connection.");
            }

            string[] packMapIds = packMapIdsString.Split('\n');
            string[] pendingMapIds = pendingMapIdsString.Split('\n');
            string[] uploadedMapIds = uploadedMapIdsString.Split('\n');

            return packMapIds.Union(pendingMapIds.Concat(uploadedMapIds)).ToList();
        }


        /// <summary>
        /// Get the custom map names from github and combine them.
        /// </summary>
        static List<string> GetKnownCustomMapNames()
        {
            WebClient webClient = new WebClient();

            string packCustomMapNamesString = "";
            string pendingCustomMapNamesString = "";
            try
            {
                packCustomMapNamesString = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/PackCustomMapNames.txt");
                pendingCustomMapNamesString = webClient.DownloadString("https://raw.githubusercontent.com/Emanuel-de-Jong/Lost-10K-Finder/main/PendingCustomMapNames.txt");
            }
            catch (Exception e)
            {
                End("Custom map names couldn't be read from the server.\nPlease check your internet connection.");
            }

            string[] packCustomMapNames = packCustomMapNamesString.Split('\n');
            string[] pendingCustomMapNames = pendingCustomMapNamesString.Split('\n');

            return packCustomMapNames.Union(pendingCustomMapNames).ToList();
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


        /// <summary>
        /// Check if the given map name is in the known ids or names already
        /// </summary>
        static bool IsKnownMap(string mapName, List<string> knownMapIds, List<string> knownCustomMapNames)
        {
            if (!Char.IsDigit(mapName[0]))
            {
                if (knownCustomMapNames.Contains(mapName))
                    return true;
            }
            else
            {
                string mapId = GetMapIdFromName(mapName);

                if (knownMapIds.Contains(mapId))
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


        /// <summary>
        /// Validates the given file on if it is 10k and if it has HitObjects.
        /// </summary>
        static bool IsValid10kOsuFile(string osuFilePath)
        {
            string[] lines = File.ReadAllLines(osuFilePath);

            // First get the needed data from the file
            string circleSizeLine = "";
            string versionLine = "";
            int hitObjectCount = 0;
            bool hitObjectsStarted = false;
            foreach (string line in lines)
            {
                if (!hitObjectsStarted)
                {
                    if (line.StartsWith("CircleSize:"))
                    {
                        circleSizeLine = line;
                    }
                    else if (line.StartsWith("Version:"))
                    {
                        versionLine = line;
                    }
                    else if (line == "[HitObjects]")
                    {
                        hitObjectsStarted = true;
                    }
                }
                else
                {
                    hitObjectCount++;

                    if (hitObjectCount == 10)
                        break;
                }
            }

            // Then do the validation checks
            if (circleSizeLine != "CircleSize:10")
                return false;

            if (versionLine.Contains("A10K"))
                return false;

            if (hitObjectCount < 10)
                return false;

            return true;
        }


        /// <summary>
        /// Tells the user if any maps were found.
        /// And if so, which ones and where to find them.
        /// </summary>
        static string CreateEndMessage(List<string> lost10kMapNames)
        {
            string endMessage;
            if (lost10kMapNames.Count == 0)
            {
                endMessage = "No lost maps were found";
            }
            else
            {
                endMessage = "The following lost maps were found:\n";
                foreach (string mapName in lost10kMapNames)
                    endMessage += mapName + "\n";

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
