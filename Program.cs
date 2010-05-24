using System;
using System.IO;
using SBML2TikZ;

namespace SBML2TikZ_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("Pass one or two arguments; the name of the SBML file and optionally, the output .tex file name");
                Environment.Exit(-1);
            }

            string fileName = args[0];
            Converter conv = new Converter(fileName);
            string outputFileName;
            if (args.Length == 1)
            {
                outputFileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".tex");
            }
            else
            {
                outputFileName = args[1];
            }

            using (StreamWriter writer = new StreamWriter(outputFileName))
            {
                writer.WriteLine(conv.WriteFromLayout());
            }
        }
    }
}
