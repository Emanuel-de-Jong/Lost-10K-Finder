using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace _10K_Finder_Utils
{
    class Program
    {
        private static readonly string outputPath = @"..\..\..\..\Output\";


        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            HashPaths(Console.ReadLine());
            //HashPaths(@"E:\Coding\Repos\Lost-10K-Finder Resources\pending");
            //SaveDupes(@"E:\Coding\Other\osu collections\Non-10K-Finder\bin\Debug\net5.0\hashes.txt");
            //DeleteFromPathFile(@"E:\Coding\Repos\Lost-10K-Finder Resources\pending\dupes.txt");

            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);

            Console.ReadKey();
        }





        private static readonly Regex checkAutomap = new Regex(@".osu.[0-9]*a[0-9]+.osu$", RegexOptions.Compiled);
        static void HashPaths(string path)
        {
            List<string> hashes = new List<string>();

            string[] mapPaths = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
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

                string hash = GetDirHash(mapPath, osuFilePaths);
                Console.WriteLine(hash);
                hashes.Add(hash);
            }

            File.WriteAllLines(outputPath + "hashes.txt", hashes);
        }


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

                return BitConverter.ToString(hasher.Hash).Replace("-", "") + " - " + Path.GetFileName(mapPath);
            }
        }





        static void IsValid10K1(string path)
        {
            List<string> invalid = new List<string>();
            HashSet<string> cols = new HashSet<string>();

            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            foreach (string dir in dirs)
            {
                string[] files = Directory.GetFiles(dir, "*.osu");
                if (files.Length == 0)
                    continue;

                bool valid = false;
                foreach (string file in files)
                {
                    List<string> lines = File.ReadAllLines(file).ToList();

                    if (lines.IndexOf("Mode: 3") == -1)
                        continue;

                    if (lines.IndexOf("CircleSize:10") == -1)
                        continue;

                    int objsIndex = lines.IndexOf("[HitObjects]");
                    List<string> hitobjects = lines.GetRange(objsIndex + 1, lines.Count - objsIndex - 1);
                    foreach (string hitobject in hitobjects)
                    {
                        cols.Add(hitobject.Substring(0, hitobject.IndexOf(',')));

                        if (cols.Count == 10)
                        {
                            valid = true;
                            break;
                        }
                    }

                    cols.Clear();

                    if (valid)
                        break;
                }

                if (!valid)
                    invalid.Add(dir);
            }

            if (invalid.Count != 0)
                File.WriteAllLines(outputPath + "noCols.txt", invalid);
        }





        static bool hasHitobjects;
        static bool has10k;
        static void IsValid10K2(string path)
        {
            string[] audioFormats = new string[]
            {
                ".mp3", ".ogg", ".wav", ".m4a", ".flac", ".mp4", ".wma", ".aac", ".pcm", ".aiff", ".alac", ".dsd"
            };
            List<string> noOsu = new List<string>();
            List<string> no10k = new List<string>();
            List<string> noAudio = new List<string>();
            List<string> noHitobjects = new List<string>();

            bool hasOsu, hasAudio;
            string format;
            string[] files;
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
            foreach (string dir in dirs)
            {
                if (Directory.GetFiles(dir, "*.osu").Length == 0)
                    continue;

                hasOsu = false;
                has10k = false;
                hasAudio = false;
                hasHitobjects = false;

                files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    format = Path.GetExtension(file);
                    format = format.ToLower();

                    if (!hasHitobjects && format == ".osu")
                    {
                        hasOsu = true;
                        CheckOsuFile(file);
                    }
                    else if (!hasAudio && audioFormats.Contains(format))
                    {
                        hasAudio = true;
                    }

                    if (hasHitobjects && hasAudio)
                        break;
                }

                if (!hasOsu)
                {
                    noOsu.Add(dir);
                }
                else if (!has10k)
                {
                    no10k.Add(dir);
                }

                if (!hasAudio)
                {
                    noAudio.Add(dir);
                }

                if (!hasHitobjects)
                {
                    noHitobjects.Add(dir);
                }
            }

            if (noOsu.Count != 0)
                File.WriteAllLines(outputPath + "noOsu.txt", noOsu.ToArray());
            if (no10k.Count != 0)
                File.WriteAllLines(outputPath + "no10k.txt", no10k.ToArray());
            if (noAudio.Count != 0)
                File.WriteAllLines(outputPath + "noAudio.txt", noAudio.ToArray());
            if (noHitobjects.Count != 0)
                File.WriteAllLines(outputPath + "noHitobjects.txt", noHitobjects.ToArray());
        }


        public static void CheckOsuFile(string file)
        {
            string[] lines = File.ReadAllLines(file);

            bool is10k = false;

            foreach (string line in lines)
            {
                if (line.Contains("Mode:") && !line.Contains("3"))
                {
                    return;
                }
                else if (line.Contains("CircleSize"))
                {
                    is10k = line.Contains("10");
                    break;
                }
            }

            if (is10k)
            {
                has10k = true;
                bool hitobjectFound = false;

                foreach (string line in lines)
                {
                    if (!hitobjectFound)
                    {
                        if (line.Contains("[HitObjects]"))
                            hitobjectFound = true;
                    }
                    else if (line.Length > 5)
                    {
                        hasHitobjects = true;
                        return;
                    }
                }
            }
        }





        static void DeleteFromPathFile(string path)
        {
            string[] paths = File.ReadAllLines(path);

            foreach (string p in paths)
            {
                if (!Directory.Exists(p))
                {
                    Console.WriteLine("Doesn't exist: " + p);
                    continue;
                }

                Directory.Delete(p, true);
            }
        }





        static void SaveDifferenceBetweenTwoFiles(string path)
        {
            path += "\\";

            List<string> ids1 = File.ReadAllLines(path + @"test.txt").ToList();
            string[] ids2 = File.ReadAllLines(path + @"lost maps.txt");
            List<string> ids3 = new List<string>();

            foreach (string id2 in ids2)
            {
                ids1.Remove(id2);

                //if (ids1.Contains(id2))
                //    ids3.Add(id2);
            }

            File.WriteAllLines(outputPath + "difference.txt", ids1);
        }





        static void SaveDupes(string path)
        {
            string[] maps = File.ReadAllLines(path);
            List<string> dupes = new List<string>();

            for (int i = 0; i < maps.Length; i++)
            {
                string map1 = maps[i].Split(' ')[0];
                for (int j = i + 1; j < maps.Length; j++)
                {
                    string map2 = maps[j].Split(' ')[0];

                    if (map1 == map2)
                    {
                        Console.WriteLine($"{maps[i]}\n{maps[j]}\n");
                        dupes.Add($"{maps[i]}\n{maps[j]}\n");
                    }
                }
            }

            File.WriteAllLines(outputPath + "dupes.txt", dupes);
        }





        static void FillDirsWithOsuFile(string path)
        {
            string[] dirNames = File.ReadAllLines(path + "lost_maps.txt");

            string dirPath;
            foreach (string dirName in dirNames)
            {
                dirPath = path + @"thing\" + dirName;
                Directory.CreateDirectory(dirPath);
                File.Copy(path + "10k diff.osu", dirPath + @"\10k diff.osu");
            }
        }





        static void CountCols(string path)
        {
            string[] files = Directory.GetFiles(path);

            List<string> result = new List<string>();
            List<string> lines, hitobjects;
            int timeCount = 1;
            int hitobjectsIndex;
            string lastTime = "";
            string time;
            foreach (string file in files)
            {
                lines = File.ReadAllLines(file).ToList();

                hitobjectsIndex = lines.IndexOf("[HitObjects]");
                hitobjects = lines.GetRange(hitobjectsIndex + 1, lines.Count - hitobjectsIndex - 1);

                foreach (string hitobject in hitobjects)
                {
                    time = hitobject.Split(',')[2];

                    if (time == lastTime)
                    {
                        timeCount++;
                    }
                    else
                    {
                        timeCount = 1;
                    }

                    lastTime = time;

                    if (timeCount == 7)
                    {
                        result.Add(file + "    -    " + time);
                        break;
                    }
                }
            }

            File.WriteAllLines(outputPath + "result.txt", result);
        }
    }
}
