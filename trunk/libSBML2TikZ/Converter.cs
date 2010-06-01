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
    /// <summary>
    /// The Converter class converts rendering 
    /// information in an SBML file, the Layout 
    /// from an SBML file, or the string contents 
    /// of an SBML file and generates a string 
    /// of arguments in PGF/TikZ.
    /// </summary>
    public class Converter
    {
        private string _SBML;
        public string SBML
        {
            get { return _SBML; }
            set { 
                _SBML = value;
                ReadFromSBMLString(value);
            }
        }
        private Layout _layout;
        public Layout layout
        {
            get { return _layout; }
            set { 
                _layout = value;
                ReadFromLayout(value);
            }
        }

        private Hashtable _fontTeXTable;
        public Hashtable fontTeXTable
        {
            set { _fontTeXTable = value; }
        }

        public RenderSpecs specs;

        /// <summary>
        /// Initializes a new instance of the <c>Converter</c> class. 
        /// </summary>
        public Converter()
        {
            specs = new RenderSpecs();
            setDefaultFontTexTable();
        }
        /// <summary>
        /// Initializes a new instance of the 
        /// <c>Converter</c> class with 
        /// <c>RenderSpecs</c> & <c>Layout</c> 
        /// for the file in the path passed. 
        /// </summary>
        /// <param name="filename">string 
        /// indicating path of the SBML file</param>
        public Converter(string filename)
            : this(filename, false)
        {
        }
        /// <summary>
        /// Initializes a new instance of the 
        /// <c>Converter</c> class with 
        /// <c>RenderSpecs</c> & <c>Layout</c> 
        /// for the file in the path passed. 
        /// The <c>Layout</c> may be overwritten 
        /// with SBGN.
        /// </summary>
        /// <param name="filename">string indicating 
        /// path of the SBML file</param>
        /// <param name="useSBGN">boolean indicating 
        /// whether the layout should be replaced by SBGN default</param>
        public Converter(string filename, bool useSBGN)
            : this()
        {
            ReadFromSBML(filename, useSBGN);
        }
        /// <summary>
        /// Initializes a new instance of the
        /// <c>Converter</c> class with the 
        /// <c>Layout</c> passed and a 
        /// <c>RenderSpecs</c> initialized for that 
        /// <c>Layout</c>
        /// </summary>
        /// <param name="layout">Layout (presumably 
        /// read from some SBML file) to be converted 
        /// to PGF/TikZ</param>
        public Converter(Layout layout) : this()
        {
            ReadFromLayout(layout);
        }

        /// <summary>
        ///  Returns a <c>Converter</c> object
        ///  with <c>Layout</c> and 
        ///  <c>RenderSpecs</c> for the file in the 
        ///  path passed.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns><c>Converter</c> with a 
        /// <c>Layout</c> and <c>RenderSpecs</c>
        /// </returns>
        public static Converter FromFile(string fileName)
        {
            return FromFile(fileName, false);
        }
        /// <summary>
        /// Returns a <c>Converter</c> object with 
        /// <c>Layout</c> and <c>RenderSpecs</c> for 
        /// the file in the path passed. The <c>Layout</c> 
        /// may be overwritten with SBGN.
        /// </summary>
        /// <param name="fileName">string indicating 
        /// path of the SBML file</param>
        /// <param name="useSBGN">boolean indicating
        /// whether the layout should be replaced by 
        /// SBGN default</param>
        /// <returns><c>Converter</c> with a <c>Layout</c> 
        /// and <c>RenderSpecs</c></returns>
        public static Converter FromFile(string fileName, bool useSBGN)
        {
            return new Converter(fileName, useSBGN);
        }
        /// <summary>
        /// Returns a <c>Converter</c> object with 
        /// <c>Layout</c> and <c>RenderSpecs</c> 
        /// for the SBML string passed.
        /// </summary>
        /// <param name="sbmlContent">string of 
        /// SBML file contents</param>
        /// <returns><c>Converter</c> with a 
        /// <c>Layout</c> and <c>RenderSpecs</c>
        /// </returns>
        public static Converter FromSBMLContent(string sbmlContent)
        {
            return FromSBMLContent(sbmlContent, false);
        }
        /// <summary>
        /// Returns a <c>Converter</c> object with
        /// <c>Layout</c> and <c>RenderSpecs</c> 
        /// for the SBML string passed. The 
        /// <c>Layout</c> may be overwritten with SBGN. 
        /// </summary>
        /// <param name="sbmlContent">stroring of 
        /// SBML file contents</param>
        /// <param name="useSBGN">boolean indicating 
        /// whether the layout should be replaced by 
        /// SBGN default</param>
        /// <returns><c>Converter</c> with a 
        /// <c>Layout</c> and <c>RenderSpecs</c>
        /// </returns>
        public static Converter FromSBMLContent(string sbmlContent, bool useSBGN)
        {
            var converter = new Converter();
            converter.ReadFromSBMLString(sbmlContent, useSBGN);
            return converter;
        }
        /// <summary>
        /// Returns a <c>Converter</c> object with 
        /// <c>Layout</c> and <c>RenderSpecs</c> for 
        /// the <c>Layout</c> passed.
        /// </summary>
        /// <param name="layout">Layout from an SBML 
        /// file</param>
        /// <returns><c>Converter</c> with a 
        /// <c>Layout</c> and <c>RenderSpecs</c>
        /// </returns>
        public static Converter FromLayout(Layout layout)
        {
            return new Converter(layout);
        }

        /// <summary>
        /// Convenience method for obtaining a PGF/TikZ 
        /// document. Returns a string of PGF/TikZ that 
        /// draws the first set of rendering information 
        /// in the SBML file of the path passed.
        /// </summary>
        /// <param name="filename">string indicating path 
        /// of the SBML file</param>
        /// <returns>string of PGF/TikZ commands</returns>
        public static string ToTex(string filename)
        {
            Converter conv = new Converter(filename);
            return conv.WriteFromLayout();
        }
        /// <summary>
        /// Convenience method for obtaining a PGF/TikZ
        /// document. Returns a string of PGF/TikZ that
        /// draws the first set of rendering information
        /// in the SBML file of the path passed. The 
        /// rendering may be replaced with SBGN default.
        /// </summary>
        /// <param name="filename">string indicating 
        /// path of the SBML file</param>
        /// <param name="useSBGN">boolean indicating
        /// whether the layout should be replaced by 
        /// SBGN default</param>
        /// <returns>string of PGF/TikZ commands</returns>
        public static string ToTex(string filename, Boolean useSBGN)
        {
            Converter conv = new Converter();
            conv.ReadFromSBML(filename, useSBGN);
            return conv.WriteFromLayout();
        }
        /// <summary>
        /// Convenience method for obtaining a PGF/TikZ
        /// document. Returns a string of PGF/TikZ that
        /// draws a specified set of rendering information
        /// in the SBML file of the path passed. The 
        /// rendering may be replaced with SBGN default.
        /// </summary>
        /// <param name="filename">string indicating
        /// path of the SBML file</param>
        /// <param name="selectedLayoutNum">int indicating
        /// which set of rendering information to use</param>
        /// <param name="useSBGN">boolean indicating 
        /// whether the layout should be replaced by 
        /// SBGN default</param>
        /// <returns></returns>
        public static string ToTex(string filename, int selectedLayoutNum, Boolean useSBGN)
        {
            Converter conv = new Converter();
            conv.ReadFromSBML(filename, useSBGN);
            return conv.WriteFromLayout(selectedLayoutNum);
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        //public string ToTeX()
        //{
        //    if (layout != null)
        //    {
        //        return ToTex(layout);
        //    }
        //    return "There is no layout loaded.";
        //}

        public byte[] ToPDF()
        {
            var tikzString = WriteFromLayout();
            return CompileTikZToPDF(tikzString);
        }

        private static byte[] CompileTikZToPDF(string TikZstrings)
        {
            //Create a temp directory to generate the pdf and tex files
            string tempDir = (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);

            string tempFileName = Path.GetRandomFileName();
            string TeXfilename = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(tempFileName) + ".tex");
            string PDFfilename = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(tempFileName) + ".pdf");
            //string TeXfilename = tempDir + "\\" + Path.GetFileNameWithoutExtension(tempFileName) + ".tex";
            //string PDFfilename = tempDir + "\\" + Path.GetFileNameWithoutExtension(tempFileName) + ".pdf";

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
        /// <summary>
        /// Generates a PDF byte file from a given SBML file
        /// </summary>
        /// <param name="filename">SBML file path</param>
        /// <returns></returns>
        public static byte[] ToPDF(string filename)
        {
            string TikZstrings = Converter.ToTex(filename);

            return CompileTikZToPDF(TikZstrings);
        }

        public static byte[] ToPDF(string filename, Boolean useSBGN)
        {
            string TikZstrings = Converter.ToTex(filename, useSBGN);

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

        /// <summary>
        /// Updates the <c>Layout</c> of this <c>Converter</c> 
        /// with the <c>Layout</c> found in the file of 
        /// the path passed
        /// </summary>
        /// <param name="filename">string indicating the path 
        /// of the SBML file</param>
        public void ReadFromSBML(string filename)
        {
            ReadFromSBML(filename, false);
        }
        /// <summary>
        /// Updates the <c>Layout</c> of this <c>Converter</c> 
        /// with the <c>Layout</c> found in the file of 
        /// the path passed. May overwrite the <c>Layout</c> 
        /// with SBGN default.
        /// </summary>
        /// <param name="filename">string indicating the path
        /// of the SBML file</param>
        /// <param name="useSBGN">boolean indicating whether
        /// the Layout should be replaced with SBGN default
        /// </param>
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
        public void ReadFromSBMLString(string sbmlContent)
        {
            ReadFromSBMLString(sbmlContent, false);
        }
        /// <summary>
        /// Added a similar version of the above method that operates on the content of an SBML file (i.e.: the raw SBML)
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
