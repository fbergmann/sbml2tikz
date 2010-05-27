using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.CodeDom.Compiler; 
using System.Collections;
using System.Diagnostics;
using SBMLExtension.LayoutExtension;
using SBMLExtension.EmlRenderExtension;

namespace SBML2TikZ
{
    public class Converter
    {
        private string _SBML;
        public string SBML
        {
            get { return _SBML; }
            set { _SBML = value; }
        }
        private Layout _layout;
        public Layout layout
        {
            get { return _layout; }
            set { _layout = value; }
        }

        private Hashtable _fontTeXTable;
        public Hashtable fontTeXTable
        {
            set { _fontTeXTable = value; }
        }

        public RenderSpecs specs;

        public Converter()
        {
            specs = new RenderSpecs();
            setDefaultFontTexTable();
        }

        public Converter(string filename)
            : this(filename, false)
        {
        }

        public Converter(string filename, bool useSBGN)
            : this()
        {
            ReadFromSBML(filename, useSBGN);
        }



        public Converter(Layout layout) : this()
        {
            ReadFromLayout(layout);
        }

        public static Converter FromFile(string fileName)
        {
            return FromFile(fileName, false);
        }

        public static Converter FromFile(string fileName, bool useSBGN)
        {
            return new Converter(fileName, useSBGN);
        }

        public static Converter FromSBMLContent(string sbmlContent)
        {
            return FromSBMLContent(sbmlContent, false);
        }

        public static Converter FromSBMLContent(string sbmlContent, bool useSBGN)
        {
            var converter = new Converter();
            converter.ReadFromSBMLString(sbmlContent, useSBGN);
            return converter;
        }

        public static Converter FromLayout(Layout layout)
        {
            return new Converter(layout);
        }

        // convenience methods for obtaining a TikZ document; executes all the necessary methods in the right order
        public static string ToTex(string filename)
        {
            Converter conv = new Converter(filename);
            return conv.WriteFromLayout();
        }

        public static string ToTeX(string filename, Boolean useSBGN)
        {
            Converter conv = new Converter();
            conv.ReadFromSBML(filename, useSBGN);
            return conv.WriteFromLayout();
        }

        public static string ToTeX(string filename, int selectedLayoutNum, Boolean useSBGN)
        {
            Converter conv = new Converter();
            conv.ReadFromSBML(filename, useSBGN);
            return conv.WriteFromLayout(selectedLayoutNum);
        }

        public string ToTeX()
        {
            return WriteFromLayout();
        }

        public byte[] ToPDF()
        {
            var tikzString = ToTeX();
            return CompileTikZToPDF(tikzString);
        }

        private static byte[] CompileTikZToPDF(string TikZstrings)
        {
            //Create a temp directory to generate the pdf and tex files
            string tempDir = (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);

            string tempFileName = Path.GetRandomFileName();
            string TeXfilename = tempDir + "\\" + Path.GetFileNameWithoutExtension(tempFileName) + ".tex";
            string PDFfilename = tempDir + "\\" + Path.GetFileNameWithoutExtension(tempFileName) + ".pdf";

            // write TikZstrings into TeXfilename
            using (StreamWriter writer = new StreamWriter(TeXfilename))
            {
                writer.WriteLine(TikZstrings);
            }

            //Now convert the TeX file to PDF
            Boolean compiled;
            compiletoPDF(out compiled, TeXfilename);

            //if the compilation was successful, we convert the PDF to a byte buffer
            if (compiled)
            {
                byte[] PDFdata = File.ReadAllBytes(PDFfilename);
                //delete the tempDir
                Directory.Delete(tempDir, true);
                return PDFdata;
            }
            Directory.Delete(tempDir, true);
            return new byte[] { }; //return an empty array
        }
        public static byte[] ToPDF(string filename)
        {
            string TikZstrings = Converter.ToTex(filename);

            return CompileTikZToPDF(TikZstrings);
        }

        //generates a pdf file given the path to an existing tex file using PDFLaTeX
        //compiled shows whether the compilation by PDFLaTeX was successful
        public static void compiletoPDF(out Boolean compiled, string texfilename)
        {
            compiled = true;
            try
            {
                string oldDir = Directory.GetCurrentDirectory();
                ProcessStartInfo pdfLaTeXinfo = new ProcessStartInfo();
                pdfLaTeXinfo.CreateNoWindow = true;
                pdfLaTeXinfo.UseShellExecute = false;
                Directory.SetCurrentDirectory(Path.GetDirectoryName(texfilename));

                pdfLaTeXinfo.Arguments = Path.GetFileName(texfilename);
                pdfLaTeXinfo.FileName = "pdflatex";
                
                Process p = Process.Start(pdfLaTeXinfo);
                p.WaitForExit();
                p.Close();
                Directory.SetCurrentDirectory(oldDir);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                compiled = false;
            }
        }

        public void ReadFromSBML(string filename, Boolean useSBGN)
        {
            if (File.Exists(filename))
            {
                string sbmlContent = File.ReadAllText(filename);
                ReadFromSBMLString(sbmlContent, useSBGN);
                SBMLExtension.Util.CurrentDirectory = Path.GetDirectoryName(filename);
            }
        }

        /// <summary>
        /// Added A similar version of the above method that operates on the content of an SBML file (i.e.: the raw SBML)
        /// </summary>
        /// <param name="sbmlContent">the raw SBML (xml)</param>
        /// <param name="useSBGN">boolean indicating whether the layout should be replaced by SBGN default</param>
        public void ReadFromSBMLString(string sbmlContent, Boolean useSBGN)
        {
            _SBML = sbmlContent;
            Layout.ReplaceLayoutWithSBGNDefault = useSBGN;
            Layout layout = SBMLExtension.Util.readLayout(_SBML);
            ReadFromLayout(layout);
        }

        /// <summary>
        /// Added a version that initializes from a layout object
        /// </summary>
        /// <param name="layout">a laout object to initialize from</param>
        public void ReadFromLayout(Layout layout)
        {
            _layout = layout; //default use the first layout
            specs = new RenderSpecs(_layout);
        }
        //TODO: what is the purpose of this method, why not use ToTex()
        public string WriteFromLayout() // renders the first layout
        {
            if (layout != null)
            {
                return ToTex(layout);
            }
            return "There is no layout loaded.";
        }

        public string WriteFromLayout(int layoutNum) //renders the specified layout
        {
            if (layout != null && layoutNum < SBMLExtension.Util.Layouts.Count)
            {
                Layout selectedLayout = SBMLExtension.Util.Layouts[layoutNum];
                if (selectedLayout != null)
                {
                    return ToTex(selectedLayout);
                }
            }
            return "There is no layout loaded.";
        }

        public string ToTex(Layout selectedLayout)
        {
            StringBuilder builder = new StringBuilder();
            using (StringWriter writer = new StringWriter(builder))
            {
                System.CodeDom.Compiler.IndentedTextWriter indentedwriter = new IndentedTextWriter(writer, "   ");
                WriteTo(indentedwriter, selectedLayout);
                writer.Flush();
                writer.Close();
            }
            return builder.ToString();
        }

        private void WriteTo(IndentedTextWriter writer, Layout selectedLayout)
        {
            if (selectedLayout == null || !selectedLayout.hasLayout())
                return;

            double xscale = specs.desiredWidth / selectedLayout.Dimensions.Width;
            double yscale = specs.desiredHeight / selectedLayout.Dimensions.Height;
            double scale = Math.Min(xscale, yscale);
            string papersize = Enum.GetName(typeof(papersize), specs.size);
            string orientation = specs.desiredWidth > specs.desiredHeight ? "landscape" : "portrait";
            writer.WriteLine("\\documentclass{article}");
            writer.WriteLine("\\usepackage{tikz}");
            writer.WriteLine("\\usepackage{pgf}");
            writer.WriteLine("\\usepackage[total={{{0}pt,{1}pt}}, centering, {2}, {3}]{{geometry}}", specs.desiredWidth, specs.desiredHeight, papersize, orientation);
            writer.WriteLine("\\pagestyle{empty}");
            writer.WriteLine("\\begin{document}");
            writer.WriteLine("\\begin{center}");
            writer.WriteLine("\\begin{{tikzpicture}}[xscale = {0}, yscale = -{1}]", xscale, yscale);
            writer.WriteLine("{");


            // _layout._EmlRenderInformation is a list of LocalRenderInformation
            // Each LocalRenderInformation has lists of GradientDefinitions, ColorDefinitions & LineEndings 
            // It also contains a list of Styles
            // Each Style can be applied to items that share a role with its rolelist or a type with its typelist
            // Presently we do not need to worry about GlobalRenderInformation as it is dealt with by the 
            // RenderInformation.GetStyleForObject, although this may be revised

            // if there are more than one renderinformation objects, we need to ask the user to pick one

            // in order to use some of the classes in the EmlRenderExtension we need a Graphics object
            var dummyControl = new Control();               
            try
            {
                var g = dummyControl.CreateGraphics();


                DefineColorsAndGradients(selectedLayout._EmlRenderInformation[0].ColorDefinitions, selectedLayout._EmlRenderInformation[0].GradientDefinitions, selectedLayout._EmlRenderInformation[0], writer);

                foreach (var glyph in selectedLayout.ReactionGlyphs)
                {
                    glyphToTex(glyph, selectedLayout, writer, g, scale);
                }

                foreach (var glyph in selectedLayout.SpeciesGlyphs)
                {
                    glyphToTex(glyph, selectedLayout, writer, g, scale);
                }

                foreach (var glyph in selectedLayout.CompartmentGlyphs)
                {
                    glyphToTex(glyph, selectedLayout, writer, g, scale);
                }

                foreach (var glyph in selectedLayout.TextGlyphs)
                {
                    glyphToTex(glyph, selectedLayout, writer, g, scale);
                }

                foreach (var glyph in selectedLayout.AdditionalGraphicalObjects)
                {
                    glyphToTex(glyph, selectedLayout, writer, g, scale);
                }

                writer.WriteLine("}");
                writer.WriteLine("\\end{tikzpicture}");
                writer.WriteLine("\\end{center}");
                writer.WriteLine("\\end{document}");
            }
            finally
            {
                //TODO: you are creating a control, to obtain a graphics handle, but then dispose the control, 
                //      so this handle is invalid!
                dummyControl.Dispose();

            }
        }

        private void glyphToTex(SBMLExtension.LayoutExtension.GraphicalObject glyph, Layout selectedLayout, IndentedTextWriter writer, Graphics g, double text_scale)
        {
            Style style = selectedLayout._EmlRenderInformation[0].GetStyleForObject(glyph);
            RectangleF refbounds = new RectangleF(); //refbounds is later set by lineEndings if the glyph is a reactionglyph; this sets the size of endings drawn
            if (style != null)
            {
                Group group = style.Group;
                group.ToTex(glyph, writer, g, selectedLayout._EmlRenderInformation[0], group, refbounds, text_scale, _fontTeXTable);
            }
        }

        private void setDefaultFontTexTable()
        {
            _fontTeXTable = new Hashtable();
            _fontTeXTable.Add("sans-serif", "\\sfdefault");
            _fontTeXTable.Add("serif", "\\rmdefault");
            _fontTeXTable.Add("monospace", "\\ttdefault");
            _fontTeXTable.Add("helvetica", "phv");
            _fontTeXTable.Add("avante garde", "pag");
            _fontTeXTable.Add("bookman", "pbk");
            _fontTeXTable.Add("charter", "bch");
            _fontTeXTable.Add("courier", "pcr");
            _fontTeXTable.Add("new century schoolbook", "pnc");
            _fontTeXTable.Add("palatino", "ppl");
            _fontTeXTable.Add("times", "ptm");
            _fontTeXTable.Add("zapf zhancery", "pzc");
            _fontTeXTable.Add("utopia", "put");
        }

        //private void DefineLineEndings(System.Collections.Generic.List<LineEnding> lineEndingDefs, RenderInformation rendinfo, IndentedTextWriter writer, Graphics g, double scale, Hashtable fontTeXTable)
        //{
        //    if (lineEndingDefs.Count > 0)
        //    {
        //        writer.WriteLine("% List of LineEndings used: ");
        //        for (int ii = 0; ii < lineEndingDefs.Count; ii++)
        //        {
        //            LineEnding ending = lineEndingDefs[ii];
        //            writer.WriteLine("\\def \\{0} {{", ending.ID);
        //            writer.Indent += 1;
        //            SBMLExtension.LayoutExtension.GraphicalObject glyph = new SBMLExtension.LayoutExtension.GraphicalObject();
        //            RectangleF refbounds = new RectangleF();
        //            ending.Group.ToTex(glyph, writer, g, rendinfo, ending.Group, refbounds, scale, fontTeXTable);
        //            writer.Indent -= 1;
        //            writer.WriteLine("}");
        //        }
        //    }
        //}

        private void DefineColorsAndGradients(System.Collections.Generic.List<ColorDefinition> ColorDefinitions, System.Collections.Generic.List<GradientDefinition> GradientDefinitions, RenderInformation rendinfo, IndentedTextWriter writer)
        {
            if (ColorDefinitions.Count > 0)
            {
                writer.WriteLine("% List of colors used: ");
                for (int ii = 0; ii < ColorDefinitions.Count; ii++)
                {
                    ColorDefinition def = ColorDefinitions[ii];
                    Color color = rendinfo.GetColor(def.ID);
                    FillColor.AssignColorRGBTex(color, def.ID, writer);
                }
                writer.WriteLine();
            }
            if (GradientDefinitions.Count > 0)
            {
                writer.WriteLine("% List of gradients used: ");
                for (int ii = 0; ii < GradientDefinitions.Count; ii++)
                {
                    GradientDefinition def = GradientDefinitions[ii];
                    FillColor fill;
                    if (def is LinearGradient)
                    {
                        LinearGradient lindef = (LinearGradient)def;
                        fill = lindef.GetLinearGradFillColor(rendinfo);
                        FillColor.AssignGradientTex(fill, lindef.ID, writer);
                    }
                    else
                    {
                        RadialGradient raddef = (RadialGradient)def;
                        fill = raddef.GetRadialFillColor(rendinfo);
                        FillColor.AssignGradientTex(fill, raddef.ID, writer);
                    }
                    writer.WriteLine();
                }
            }
        }
    }
}
