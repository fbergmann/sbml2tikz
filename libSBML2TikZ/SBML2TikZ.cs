using System;
using System.IO;

// This class was originally used to test the library and is now obsolete.
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
            string fileName = @"C:\Users\Si yuan\Documents\SBML Models\FOO.xml"; ;
            //string curDir = Directory.GetCurrentDirectory();
            //string fontdatafile = curDir + "\\Fonts.txt";

            Converter conv = new Converter(fileName, false);
            string outputFileName = @"C:\Users\Si yuan\Desktop\test";
            //using (StreamWriter writer = new StreamWriter(outputFileName))
            //{
            //    writer.WriteLine(conv.WriteFromLayout());
            //}
            File.WriteAllBytes(outputFileName, conv.ToPDF());
        }
    }
}
