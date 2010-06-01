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
                Console.WriteLine("Optional arguments following the required arguments include:");
                Console.WriteLine();
                Console.WriteLine("-pdflatex: compiles the TeX macros using pdfLaTeX.");
                Console.WriteLine();
                Console.WriteLine("-UseSBGN: renders the graph with Systems Biology Graphical Notation.");
                Console.WriteLine();
                Console.WriteLine("-layout{x}: Sometimes models have multiple sets of rendering information. This argument tells SBML2TikZ to render set number x.");
                Console.WriteLine("\t Eg.-layout{2} will draw the 2nd graph stored in the model.");
                Console.WriteLine("\t The default graph drawn is the first in the model. If the graph requested by the user in -layout{x} is not available, SBML2TikZ draws the first graph.");
                Console.WriteLine();
                Console.WriteLine("-dimensions{w,h}: sets the size of the output graph. Use this argument to overwrite the recommended dimensions.");
                Console.WriteLine("\t Eg. -dimensionpts{400pt,500pt} sets the width to 400 points and the height to 500 points.");
                Console.WriteLine("\t -dimensions{w,h} accepts pts, cm and inches as input.");
                Console.WriteLine();
                Console.WriteLine();
            }
            else
            {
                ////commented args below is for testing purposes
                //args = new string[]{"C:\\Users\\Si Yuan\\Documents\\SBML Models\\BorisEJB.xml", "C:\\Users\\Si Yuan\\Desktop\\testresults\\BorisEJB.tex", "-UseSBGN", "-dimensions{400pts,300pts}"};
                Converter conv = ConverterFromArgs(args);
                //string fileName = Path.GetFullPath(args[0]);
                //string outputFileName = Path.GetFullPath(args[1]);
                string fileName = Path.GetFullPath(args[0]);
                string outputFileName = Path.GetFullPath(args[1]);
                outputFileName = Path.Combine(Path.GetDirectoryName(outputFileName), Path.GetFileNameWithoutExtension(outputFileName) + ".tex");

                if (Array.IndexOf(args, "-pdflatex")>-1)
                {
                    string pdfFileName = Path.Combine(Path.GetDirectoryName(outputFileName), Path.GetFileNameWithoutExtension(outputFileName) + ".pdf");

                    File.WriteAllBytes(pdfFileName, Converter.ToPDF(fileName, Array.IndexOf(args, "-UseSBGN") != -1));
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

        private static Converter ConverterFromArgs(string[] args)
        {
            Converter conv = new Converter();

                conv.ReadFromSBML(args[0], Array.IndexOf(args, "-UseSBGN") != -1);

            foreach (string arg in args)
            {
                // if dimensions{w,h} is in args
                if (arg.IndexOf("}") == arg.Length - 1 && arg.Contains("-dimensions{"))
                {
                    string dims = arg.Split(new string[] { "{", "}" }, StringSplitOptions.RemoveEmptyEntries)[1];
                    try
                    {
                        string[] dimArgs = dims.Split(',');
                        if (dimArgs.Length != 2)
                        {
                            throw new Exception("The number of arguments in -dimensions{w,h} is incorrect");
                        }
                        string width = dimArgs[0];
                        string height = dimArgs[1];

                        double w = parseDimToPoints(width);
                        double h = parseDimToPoints(height);
                        conv.specs.setDimensions(h, w, units.pts, units.pts);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }
            }
            return conv;
        }

        private static double parseDimToPoints(string dimArg)
        {
            string val;
            units valUnit;
            if (dimArg.Contains(units.cm.ToString()))
            {
                valUnit = units.cm;
                val = dimArg.Remove(dimArg.Length - units.cm.ToString().Length);
            }
            else if (dimArg.Contains(units.inches.ToString()))
            {
                valUnit = units.inches;
                val = dimArg.Remove(dimArg.Length - units.inches.ToString().Length);
            }
            else if (dimArg.Contains(units.pts.ToString()))
            {
                valUnit = units.pts;
                val = dimArg.Remove(dimArg.Length - units.pts.ToString().Length);
            }
            else
            {
                throw new Exception("The units passed to -dimensions{w,h} are incorrect. Please pass pts, inches or cm.");
            }
            try
            {
                double value;
                Boolean parsed = Double.TryParse(val, out value);
                if (parsed)
                {
                    value = RenderSpecs.convertLengthUnits(value, units.pts, valUnit);
                    return value;
                }
                else
                {
                    throw new Exception("The values for at least one of the dimensions could not be parsed.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }
        }
    }
}
