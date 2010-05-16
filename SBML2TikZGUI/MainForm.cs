using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Collections;
using SBML2TikZ;

public delegate void fileNameChangeHandler(object sender, string name);
public delegate void unitsChangeHandler(ListBox sender, TextBox target, units newUnit, units oldUnit);
public delegate void compileToPdfChangeHandler(object sender, Boolean compiledToPDF); //useful if we want to extend support to other TeX environments

namespace SBML2TikZ_GUI
{
    public partial class MainForm : Form
    {
        private string sbml; // the sbml file to be converted
        private Boolean compileWithPdflatex;
        private Boolean useSBGN;
        private Boolean showOutDir;
        private Boolean showPDF;
        private Converter conv;
        private event fileNameChangeHandler nameChange; //event when user selects a new SBML file
        private event unitsChangeHandler heightUnitsChange; //event when user changes the desired height units
        private event unitsChangeHandler widthUnitsChange; // event when user changes the desired width units
        private event compileToPdfChangeHandler compileWithPdfLaTeXChange; //event when compileWithPdfLaTeXCheckBox has checkbox changed

        public MainForm()
        {
            conv = new Converter();
            Hashtable fontTeXTable = getFonts();
            if (fontTeXTable.Count != 0)
            {
                conv.fontTeXTable = fontTeXTable;
            }
            compileWithPdflatex = false;
            nameChange += new fileNameChangeHandler(displayChange); // Used to change the sbml file loaded
            compileWithPdfLaTeXChange += new compileToPdfChangeHandler(enableShowPDFCheckBox);
            //Complete mainform initialization
            InitializeComponent();
            InitializeMyComponents(); // sets default values to UserAppDataRegistry values            
        }

        private void displayChange(object sender, string name)
        {
            this.xmlfiledisplay.Text = Path.GetFileName(name);
            heightBoxUnits.SelectedItem = units.pts;
            widthBoxUnits.SelectedItem = units.pts;
            dHeightBoxUnits.SelectedItem = units.pts;
            dWidthBoxUnits.SelectedItem = units.pts;
            this.sbml = name;
            conv.ReadFromSBML(name, useSBGN);
            // set the size boxes and their units 
            if (conv.specs != null)
            {
                heightBox.Text = conv.specs.height.ToString();
                widthBox.Text = conv.specs.width.ToString();
                desiredHeightBox.Text = conv.specs.desiredHeight.ToString();
                desiredWidthBox.Text = conv.specs.desiredWidth.ToString();
            }
            // set the selected layout box
            LayoutSelectionBox.Items.Clear();
            foreach (SBMLExtension.LayoutExtension.Layout layout in SBMLExtension.Util.Layouts)
            {
                LayoutSelectionBox.Items.Add(layout.ID);
            }
            if (LayoutSelectionBox.Items.Count != 0)
            {
                LayoutSelectionBox.SelectedItem = LayoutSelectionBox.Items[0];
            }
        }

        private void heightUnitsChanged(ListBox sender, TextBox target, units newUnit, units oldUnit)
        {
            if (conv.specs == null) return;
            double value = RenderSpecs.convertLengthUnits(conv.specs.height, newUnit, oldUnit);
            target.Text = Math.Truncate(value).ToString();
        }

        private void widthUnitsChanged(ListBox sender, TextBox target, units newUnit, units oldUnit)
        {
            if (conv.specs == null) return;
            double value = RenderSpecs.convertLengthUnits(conv.specs.width, newUnit, oldUnit);
            target.Text = Math.Truncate(value).ToString();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.Title = "Load SBML File";
            fDialog.Filter = "xml files (*.xml)|*.xml";

            if (Application.UserAppDataRegistry.GetValue("inputdir") != null)
            {
                string inputdir = (string)Application.UserAppDataRegistry.GetValue("inputdir");
                fDialog.InitialDirectory = inputdir;
            }
            else
            {
                fDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            
            if (fDialog.ShowDialog() == DialogResult.OK)
            {
                sbml = fDialog.FileName;
                Application.UserAppDataRegistry.SetValue("inputdir", Path.GetDirectoryName(fDialog.FileName));
                if (nameChange != null){
                    nameChange(new object(), sbml);
                    Application.UserAppDataRegistry.SetValue("xmlname", sbml);
                }
            }
        }

        private void enableShowPDFCheckBox(object sender, Boolean compiledToPDF)
        {
            showPDFCheckBox.Enabled = compiledToPDF;
            if (!compiledToPDF)
                showPDFCheckBox.Checked = false; //if the TeX will not be compiled to PDF, then no pdf will be shown
        }

        private void Convert2pdf_Click(object sender, EventArgs e)
        {
            Application.UserAppDataRegistry.SetValue("compileWithPdflatex", compileWithPdflatex);
            
            double hval;
            double wval;
           
            if (this.conv.layout == null)
                MessageBox.Show("Please provide a valid SBML input file!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            else if (!(double.TryParse(desiredWidthBox.Text, out wval) && double.TryParse(desiredHeightBox.Text, out hval)))
                MessageBox.Show("Please input numerical values for Desired Height and Width!",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            else
            {
                conv.specs.setDimensions(hval, wval, (units)dHeightBoxUnits.SelectedItem, (units)dWidthBoxUnits.SelectedItem);
                // Prompt for location for the converted file
                FileDialog fDialog = new SaveFileDialog();
                fDialog.Filter = "tex files (*.tex)|*.tex";
                string SBMLFileName = Path.GetFileName(sbml);
                string rootName = Path.GetFileNameWithoutExtension(SBMLFileName);
                fDialog.FileName = rootName + ".tex";

                // Set initialDirectory to the last used output directory if possible
                if (Application.UserAppDataRegistry.GetValue("outputdir") != null)
                {
                    string outputdir = (string)Application.UserAppDataRegistry.GetValue("outputdir");
                    fDialog.InitialDirectory = outputdir;
                }

                if ((fDialog.ShowDialog() == DialogResult.OK) && !String.IsNullOrEmpty(sbml))
                {
                    string filename = fDialog.FileName;
                    // Save the output directory as a user preferense 
                    Application.UserAppDataRegistry.SetValue("outputdir", Path.GetDirectoryName(filename));

                    using (StreamWriter writer = new StreamWriter(filename))
                    {
                        if (LayoutSelectionBox.SelectedIndices.Count == 1)
                        {
                            writer.WriteLine(conv.WriteFromLayout(LayoutSelectionBox.SelectedIndices[0]));
                        }
                        else
                        {
                            writer.WriteLine(conv.WriteFromLayout());
                        }
                    }

                    if (showOutDir)
                    {
                        MessageBox.Show(Path.GetFileName(filename) + " has been successfully written to" + Path.GetDirectoryName(filename) + ".",
                                        "Conversion Successful",
                                        MessageBoxButtons.OK);
                    }
                    if (compileWithPdflatex)
                    {
                        compiletolatex(filename);
                    }
                }
            }
        }

        private void compiletolatex(string texfilename)
        {
            Boolean errorflag = false;
            // this is the file we want to generate with pdflatex
            string pdffilename = Path.Combine(Path.GetDirectoryName(texfilename), Path.GetFileNameWithoutExtension(texfilename))+".pdf";
            // Create a temp Directory to run pdflatex from
            ProcessStartInfo tex2pdfinfo = new ProcessStartInfo();
            string oldDir = Directory.GetCurrentDirectory();
            string tempDir = (Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            Directory.CreateDirectory(tempDir);
            string destTexFile = System.IO.Path.Combine(tempDir, Path.GetFileName(texfilename));
            System.IO.File.Copy(texfilename, destTexFile);
            Directory.SetCurrentDirectory(tempDir);

            try
            {
                tex2pdfinfo.Arguments = Path.GetFileName(texfilename);
                tex2pdfinfo.FileName = "pdflatex";
                tex2pdfinfo.CreateNoWindow = true;
                tex2pdfinfo.UseShellExecute = true;
                tex2pdfinfo.ErrorDialog = true;

                try
                {
                    Process p = Process.Start(tex2pdfinfo);
                    p.WaitForExit();
                    p.Close();

                    string sourcepdf = Path.Combine(tempDir, Path.GetFileName(pdffilename));
                    string destpdf = Path.Combine(Path.GetDirectoryName(pdffilename), Path.GetFileNameWithoutExtension(pdffilename) + ".pdf");
                    // If we reach here, the user has already elected to overwrite, so we overwrite by default
                    if (File.Exists(destpdf))
                        File.Delete(destpdf);
                    System.IO.File.Move(sourcepdf, destpdf);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                    errorflag = true;
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(oldDir);
                Directory.Delete(tempDir, true);
            }

            if (!errorflag)
            {
                try
                {
                    if (showPDF)
                        Process.Start(pdffilename);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                }
            }
        }

        private Hashtable getFonts()
        {
            Hashtable fontTexTable = new Hashtable();
            try
            {
                foreach (string key in ConfigurationSettings.AppSettings.AllKeys)
                {
                    fontTexTable.Add(key, ConfigurationSettings.AppSettings[key]);
                }
            }
            catch
            {
                fontTexTable.Clear();
            }
            return fontTexTable;
        }

        private void CompileCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.compileWithPdflatex = CompileCheckBox.Checked;
            if (compileWithPdfLaTeXChange != null)
            {
                compileWithPdfLaTeXChange(new object(), compileWithPdflatex);
            }
        }

        private void heightBoxUnits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (heightUnitsChange != null)
            {
                heightUnitsChange(heightBoxUnits, heightBox,(units)heightBoxUnits.SelectedItem, units.pts);
            }
        }

        private void widthBoxUnits_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (heightUnitsChange != null)
            {
                widthUnitsChange(widthBoxUnits, widthBox, (units)widthBoxUnits.SelectedItem, units.pts);
            }
        }

        private void LayoutSelectionBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.conv.specs = new RenderSpecs(SBMLExtension.Util.Layouts[LayoutSelectionBox.SelectedIndex]);
            if (conv.specs != null)
            {
                heightBox.Text = conv.specs.height.ToString();
                widthBox.Text = conv.specs.width.ToString();
                desiredHeightBox.Text = conv.specs.desiredHeight.ToString();
                desiredWidthBox.Text = conv.specs.desiredWidth.ToString();
            }
        }

        private void SBGNCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.useSBGN = SBGNCheckBox.Checked;
            conv.ReadFromSBML(sbml, useSBGN);
        }

        private void showOutDirCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.showOutDir = showOutDirCheckBox.Checked;
        }

        private void showPDFCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.showPDF = showPDFCheckBox.Checked;
        }

    }
}
