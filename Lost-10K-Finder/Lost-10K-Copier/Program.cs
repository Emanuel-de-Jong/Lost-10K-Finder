using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lost_10K_Copier
{
    class Program
    {
        static void Main(string[] args)
        {
            string lostMapsFilePath = GetLostMapsFilePath();

            string lostMapsPath = Directory.GetCurrentDirectory() + @"\lost maps\";
            Directory.CreateDirectory(lostMapsPath);

            foreach (string mapPath in File.ReadLines(lostMapsFilePath))
            {
                if (mapPath == "")
                    continue;

                if (Directory.Exists(mapPath))
                {
                    string newMapPath = lostMapsPath + Path.GetFileName(mapPath);
                    if (!Directory.Exists(newMapPath))
                    {
                        Console.WriteLine("Copying: " + mapPath);
                        FileSystem.CopyDirectory(mapPath, newMapPath);
                    }
                    else
                    {
                        Console.WriteLine("Already copied: " + mapPath);
                    }
                }
                else
                {
                    Console.WriteLine("Can't find: " + mapPath);
                }
            }

            End("\nThe maps have been copied to the \"lost maps\" folder.");
        }


        private static string GetLostMapsFilePath()
        {
            string lostMapsFilePath = SuggestLostMapFiles(Directory.GetCurrentDirectory());
            if (lostMapsFilePath != "")
                return lostMapsFilePath;

            Console.Write("What is the path to your lost maps txt file?\nYour path: ");
            lostMapsFilePath = Console.ReadLine().Trim();

            if (File.Exists(lostMapsFilePath))
            {
                if (lostMapsFilePath.EndsWith(".txt"))
                    return lostMapsFilePath;

                End("The file is not a text file.");
            }
            else if (Directory.Exists(lostMapsFilePath))
            {
                lostMapsFilePath = SuggestLostMapFiles(lostMapsFilePath, true);
                if (lostMapsFilePath != "")
                    return lostMapsFilePath;

                End("Your path doesn't have the right lost maps txt file.");
            }

            End("Your path does not exist.");
            return "";
        }


        private static string SuggestLostMapFiles(string path, bool isUserPath = false)
        {
            string[] txtFilePaths = Directory.GetFiles(path, "*.txt");
            List<string> lostMapsFilePaths = new List<string>();
            foreach (string txtFilePath in txtFilePaths)
            {
                if (txtFilePath.Contains("lost maps"))
                    lostMapsFilePaths.Add(txtFilePath);
            }

            if (lostMapsFilePaths.Count == 1)
            {
                if (isUserPath)
                {
                    return lostMapsFilePaths[0];
                }
                else
                {

                    Console.Write($"Do you want to copy the maps in: { lostMapsFilePaths[0] } (yes/no)\nYour answer: ");

                    string answer = Console.ReadLine().Trim().ToLower();
                    if (answer.Contains("yes") || answer == "y")
                        return lostMapsFilePaths[0];

                    Console.WriteLine();
                }
            }
            else if (lostMapsFilePaths.Count > 1)
            {
                if (isUserPath)
                {
                    Console.WriteLine("Give the number of the file with the maps you want to copy.");
                }
                else
                {
                    Console.WriteLine("Give the number of the file with the maps you want to copy.\nOr give \"0\" to give your own path.");
                    Console.WriteLine("0) Give your own path");
                }

                for (int i = 0; i < lostMapsFilePaths.Count; i++)
                {
                    Console.WriteLine($"{ i + 1 }) { lostMapsFilePaths[i] }");
                }

                Console.Write("Your answer: ");
                string answer = Console.ReadLine().Trim();

                if (int.TryParse(answer, out int answerInt) && (answerInt > 0) && (answerInt <= lostMapsFilePaths.Count + 1))
                    return lostMapsFilePaths[answerInt - 1];

                Console.WriteLine();
            }

            return "";
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
