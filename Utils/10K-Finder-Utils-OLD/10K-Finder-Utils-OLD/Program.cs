using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _10K_Finder_Utils_OLD
{
    class Program
    {
        static void Main(string[] args)
        {
            string mainPath = @"E:\Media\Downloads\";

            string[] dirNames = File.ReadAllLines(mainPath + "lost_maps.txt");

            string dirPath;
            foreach (string dirName in dirNames)
            {
                dirPath = mainPath + @"thing\" + dirName;
                Directory.CreateDirectory(dirPath);
                File.Copy(mainPath + "10k diff.osu", dirPath + @"\10k diff.osu");
            }
        }





        static void Main2(string[] args)
        {
            string[] files = Directory.GetFiles(@"E:\Media\Downloads\test");

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

            File.WriteAllLines(@"E:\Media\Downloads\result.txt", result);

            Console.WriteLine("done");
        }





        static void Main3(string[] args)
        {
            List<string> ids1 = File.ReadAllLines(@"E:\Coding\Repos\Lost-10K-Finder Resources\ids\chimu.txt").ToList();
            string[] ids2 = File.ReadAllLines(@"E:\Coding\Repos\Lost-10K-Finder Resources\ids\osu.txt");

            foreach (string id2 in ids2)
                ids1.Remove(id2);

            File.WriteAllLines(@"E:\Coding\Repos\Lost-10K-Finder Resources\ids\chimu exclusive osu.txt", ids1);

            Console.WriteLine("done");
        }





        static void Main4(string[] args)
        {
            string[] paths = File.ReadAllLines(@"toDelete.txt");

            foreach (string path in paths)
                Directory.Delete(path, true);

            Console.WriteLine("done");
        }





        static void Main5(string[] args)
        {
            List<string> wrongPath = new List<string>();
            string[] dirs = Directory.GetDirectories(@"D:\Media\Downloads\10k\dirs");
            string[] oszs = Directory.GetFiles(@"D:\Media\Downloads\10k\osz");

            string oszToDir;
            foreach (string osz in oszs)
            {
                oszToDir = @"D:\Media\Downloads\10k\dirs\" + osz.Substring(27);
                oszToDir = oszToDir.Remove(oszToDir.Length - 4, 4);

                if (!dirs.Contains(oszToDir))
                {
                    wrongPath.Add(osz);
                }
            }

            foreach (string path in wrongPath)
            {
                Console.WriteLine(path);
            }

            Console.WriteLine("done");
        }





        static bool hasHitobjects;
        static bool has10k;
        static void Main6(string[] args)
        {
            string[] audioFormats = new string[]
            {
                ".mp3", ".ogg", ".wav", ".m4a", ".flac", ".mp4", ".wma", ".aac", ".pcm", ".aiff", ".alac", ".dsd"
            };
            List<string> noOsu = new List<string>();
            List<string> no10k = new List<string>();
            List<string> noAudio = new List<string>();
            List<string> noHitobjects = new List<string>();

            string path = @"D:\Games\osu!\Songs";

            bool hasOsu, hasAudio;
            string format;
            string[] files;
            string[] dirs = Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
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


            File.WriteAllLines("noOsu.txt", noOsu.ToArray());
            File.WriteAllLines("no10k.txt", no10k.ToArray());
            File.WriteAllLines("noAudio.txt", noAudio.ToArray());
            File.WriteAllLines("noHitobjects.txt", noHitobjects.ToArray());



            Console.WriteLine("done");
            Console.ReadKey();
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
                    is10k = line.Contains("18");
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
    }
}
