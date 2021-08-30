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
            Console.Write("What is the path to your \"lost maps.txt\"?\nYour path: ");
            string filePath = Console.ReadLine();

            if (!filePath.EndsWith(".txt"))
                filePath += @"\lost maps.txt";

            Console.WriteLine();

            if (!File.Exists(filePath))
            {
                End("The path does not exist.");
            }

            string lostMapsPath = Directory.GetCurrentDirectory() + @"\lost maps\";
            Directory.CreateDirectory(lostMapsPath);

            foreach (string mapPath in File.ReadLines(filePath))
            {
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

            End("\nDONE!");
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
