using System;
using System.Collections.Generic;
using System.IO;
using SBML2TikZ;

namespace SBML2TikZ_Console
{
    class RunConsole
    {
        static void Main(string[] args)
        {
            if (args.Length < 2 || args[0].ToLower().Equals("help"))
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("SBML2TikZ generates TeX macros for PGF/TikZ that draw render extension data in SBML files.");
                Console.WriteLine("SBML2TikZ requires at least 2 input arguments:");
                Console.WriteLine("SBML2TikZ <SBML filename> <output filename>");
                Console.WriteLine();
                Console.WriteLine("Optional arguments following the default arguments include:");
                Console.WriteLine("-pdflatex: compiles the TeX macros using pdfLaTeX.");
                Console.WriteLine();
                Console.WriteLine();
            }
            else
            {
                string fileName = args[0];
                string outputFileName = args[1];

                

                outputFileName = Path.GetFileNameWithoutExtension(outputFileName) + ".tex";
                Converter conv = new Converter(fileName);

                if ((new List<string>(args)).Contains("-pdflatex"))
                {
                    string pdfFileName = Path.GetFileNameWithoutExtension(outputFileName) + ".pdf";

                    File.WriteAllBytes(pdfFileName, Converter.ToPDF(fileName));
                }
                else
                {
                    using (StreamWriter writer = new StreamWriter(outputFileName))
                    {
                        writer.WriteLine(conv.WriteFromLayout());
                    }

                }

            }
        }
    }
}
