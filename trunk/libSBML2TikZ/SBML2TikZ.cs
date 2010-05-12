using System;
using System.IO;

// This is merely a testing unit for the library. The final version will not carry this file; it will be cleaned 
// up and compiled as a console application.
namespace SBML2TikZ
{
    class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length < 1 || args.Length > 2)
            //{
            //    Console.WriteLine("Pass one or two arguments; the name of the SBML file and optionally, the output .tex file name");
            //    Environment.Exit(-1);
            //}

            //string fileName = args[0];
            //Converter conv = new Converter(fileName);
            //string outputFileName;
            //if (args.Length == 1)
            //{
            //    outputFileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".tex");
            //}
            //else
            //{
            //    outputFileName = args[1];
            //}

            //using (StreamWriter writer = new StreamWriter(outputFileName))
            //{
            //    writer.WriteLine(conv.WriteFromLayout());
            //}

            //string fileName = "C:\\Users\\Si Yuan\\Desktop\\testcases\\CaMK-Activation.xml";
            string fileName = "C:\\Users\\Si Yuan\\Documents\\SBML Models\\color.xml"; ;
            //string curDir = Directory.GetCurrentDirectory();
            //string fontdatafile = curDir + "\\Fonts.txt";

            Converter conv = new Converter(fileName);
            string outputFileName = "C:\\Users\\Si Yuan\\Desktop\\testresults\\test.tex"; 
            using (StreamWriter writer = new StreamWriter(outputFileName))
            {
                writer.WriteLine(conv.WriteFromLayout());
            }
        }
    }
}
