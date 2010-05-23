using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;
using SBMLExtension.EmlRenderExtension;

namespace SBML2TikZ
{
    public class FillColor
    {
        private PointF _focalPoint; //always a percentage position from the left and top corner of bounding rectangle
        private List<Color> _colorList;
        private List<float> _positionList;
        private Boolean _gradient;
        private Boolean _radial;
        private double _gradient_rotation; //rotation from the vertical axis; -1 if radial
        private string _ID;

        public PointF focalPoint
        {
            get { return this._focalPoint; }
        }

        public List<Color> colorList
        {
            get { return this._colorList; }
        }

        public List<float> positionList
        {
            get { return this._positionList; }
        }

        public Boolean gradient
        {
            get { return this._gradient; }
        }

        public Boolean radial
        {
            get { return this._radial; }
        }

        public double gradient_rotation
        {
            get { return this._gradient_rotation; }
        }

        public string ID
        {
            get { return this._ID; }
        }

        public FillColor(Color singleColor)
        {
            this._colorList = new List<Color>();
            this._positionList = new List<float>();
            this._gradient = false;
            this._radial = false;
            _colorList.Add(singleColor);
            positionList.Add(0f);
        }

        // LinearGradient
        public FillColor(List<Color> colors, List<float> positions, double gradient_rotation, string spreadMethod, string ID)
        {
            this._colorList = colors;
            this._positionList = positions;
            this._gradient_rotation = gradient_rotation;
            this._gradient = true;
            this._ID = ID;
            SetColorsfromSpread(spreadMethod);
        }
        // RadialGradient
        public FillColor(List<Color> colors, List<float> positions, PointF focal, string spreadMethod, string ID)
        {
            this._colorList = colors;
            this._positionList = positions;
            this._focalPoint = focal;
            this._gradient = true;
            this._radial = true;
            this._ID = ID;
            SetColorsfromSpread(spreadMethod);
        }

        private void SetColorsfromSpread(string spreadMethod)
        {
            if (spreadMethod.Equals("pad") || string.IsNullOrEmpty(spreadMethod))
            {
                if (_positionList[0] != 0f)
                {
                    _colorList.Insert(0, _colorList[0]);
                    _positionList.Insert(0, 0f);
                }
                if (_positionList[_positionList.Count - 1] != 100f)
                {
                    _colorList.Add(_colorList[_positionList.Count - 1]);
                    _positionList.Add(100f);
                }
            }
            if (spreadMethod.Equals("reflect"))
            {
                List<Color> reflectedcol = new List<Color>();
                List<float> reflectedpos = new List<float>();

                int length = _positionList.Count;
                while (_positionList[0] > 0f)
                {
                    reflectedpos.Clear();
                    reflectedcol.Clear();
                    for (int ii = 1; ii < length; ii++)
                    {
                        float dx = _positionList[ii] - _positionList[ii - 1];
                        reflectedcol.Insert(0, _colorList[ii]);
                        reflectedpos.Insert(0, _positionList[ii - 1] - dx);
                        if (reflectedpos[0] <= 0f)
                            break;
                    }
                    _positionList.InsertRange(0, reflectedpos);
                    _colorList.InsertRange(0, reflectedcol);
                }
                while (_positionList[_positionList.Count - 1] < 100f)
                {
                    reflectedpos.Clear();
                    reflectedcol.Clear();
                    for (int ii = _positionList.Count - 1; ii > _positionList.Count - length; ii--)
                    {
                        float dx = _positionList[ii] - _positionList[ii - 1];
                        reflectedcol.Add(_colorList[ii - 1]);
                        reflectedpos.Add(_positionList[ii] + dx);
                        if (reflectedpos[reflectedpos.Count - 1] >= 100f)
                            break;
                    }
                    _positionList.AddRange(reflectedpos);
                    _colorList.AddRange(reflectedcol);
                }

            }
            if (spreadMethod.Equals("repeat"))
            {
                List<Color> repeatedcol = new List<Color>();
                List<float> repeatedpos = new List<float>();

                int length = _positionList.Count;
                float totx = _positionList[_positionList.Count - 1] - _positionList[0];

                while (_positionList[_positionList.Count - 1] < 100f)
                {
                    repeatedcol.Clear();
                    repeatedpos.Clear();
                    for (int ii = 0; ii < length; ii++)
                    {
                        repeatedcol.Add(_colorList[_colorList.Count - length + ii]);
                        repeatedpos.Add(_positionList[_colorList.Count - length + ii] + totx);
                        if (repeatedpos[repeatedpos.Count - 1] >= 100f)
                            break;
                    }
                    _colorList.AddRange(repeatedcol);
                    _positionList.AddRange(repeatedpos);
                }

                while (_positionList[0] > 0f)
                {
                    repeatedcol.Clear();
                    repeatedpos.Clear();
                    for (int ii = 0; ii < length; ii++)
                    {
                        repeatedcol.Add(_colorList[ii]);
                        repeatedpos.Add(_positionList[ii] - totx);
                    }
                    while (repeatedpos[0] < 0f && repeatedpos[1] < 0f)
                    {
                        repeatedpos.RemoveAt(0);
                        repeatedcol.RemoveAt(0);
                    }
                    _colorList.InsertRange(0, repeatedcol);
                    _positionList.InsertRange(0, repeatedpos);
                }
            }
            if (_positionList[0] < 0f)
            {
                // critical last check, otherwise pgf will not draw the shading
                for (int ii = 1; ii < _positionList.Count; ii++)
                {
                    _positionList[ii] += -_positionList[0]; // shift all other values upwards
                }
                _positionList[0] = 0f; // shift the first value to 0 
            }
            if (_positionList[_positionList.Count - 1] > 100f)
            {
                while (_positionList[_positionList.Count - 2] > 100f)
                {
                    _positionList.RemoveAt(_positionList.Count - 1);
                    _colorList.RemoveAt(_colorList.Count - 1);
                }
                if (_positionList[_positionList.Count - 1] > 110f)
                {
                    _positionList[_positionList.Count - 1] = 110f;
                }
            }
        }

        public float FindLinearGradientLength()
        {
            float gradLength = 50f;
            // cosine rule to find the shift in the shape
            if (_gradient_rotation % 90 != 0)
            {
                double dx = Math.Sqrt(Math.Pow(27, 2) + Math.Pow(27, 2) - 2 * 25 * 25 * Math.Cos(_gradient_rotation * Math.PI / 180));
                gradLength += (float)(dx);
            }
            return gradLength;
        }

        public float FindLinearGradientWidth()
        {
            float gradWidth = 70f; // the necessary desiredWidth never exceeds this value, and we are unconcerned with desiredWidth 
            return gradWidth;
        }

        public float FindRadialGradientLength()
        {
            return 35f;
        }

        public static void AssignColorRGBTex(Color assignedColor, String assignedName, IndentedTextWriter writer)
        {
            writer.WriteLine("\\definecolor{{{0}}}{{RGB}}{{{1},{2},{3}}};",
                assignedName,
                assignedColor.R,
                assignedColor.G,
                assignedColor.B);
        }

        public static void AssignGradientTex(FillColor fillcolor, String assignedName, IndentedTextWriter writer)
        {
            if (fillcolor.gradient)
            {
                if (fillcolor.radial)
                {
                    float gradLength = fillcolor.FindRadialGradientLength();
                    AssignRadialGradientTex(fillcolor, assignedName, gradLength, writer);
                }
                else
                {
                    float gradLength = fillcolor.FindLinearGradientLength();
                    float gradWidth = fillcolor.FindLinearGradientWidth();
                    AssignLinearGradientTex(fillcolor, assignedName, gradWidth, gradLength, writer);
                }
            }
        }

        private static void AssignLinearGradientTex(FillColor fillcolor, String assignedName, float gradwidth, float gradlength, IndentedTextWriter writer)
        {
            for (int ii = 0; ii < fillcolor.colorList.Count; ii++)
            {
                Color curcol = fillcolor.colorList[ii];
                FillColor.AssignColorRGBTex(curcol, "color" + ii.ToString(), writer);
            }

            Color firstcol = fillcolor.colorList[0];
            writer.Write("\\pgfdeclareverticalshading {{{3}}} {{ {0}pt }} {{color({1}pt)=({2})",
                gradwidth,
                fillcolor.positionList[0] * gradlength / 100f,
                "color0" + AlphaValTex(firstcol),
                assignedName);

            for (int ii = 1; ii < fillcolor.colorList.Count; ii++)
            {
                Color curcol = fillcolor.colorList[ii];
                writer.Write("; color({0}pt)=({1})",
                    fillcolor.positionList[ii] * gradlength / 100f,
                    "color" + ii.ToString() + AlphaValTex(curcol));
            }

            writer.WriteLine("}");
        }

        private static void AssignRadialGradientTex(FillColor fillcolor, String assignedName, float gradLength, IndentedTextWriter writer)
        {
            for (int ii = 0; ii < fillcolor.colorList.Count; ii++)
            {
                Color curcol = fillcolor.colorList[ii];
                FillColor.AssignColorRGBTex(curcol, "color" + ii.ToString(), writer);
            }

            Color firstcol = fillcolor.colorList[0];

            writer.Write("\\pgfdeclareradialshading {{{4}}} {{\\pgfpoint{{{0}pt}}{{{1}pt}}}} {{color({2}pt)=({3})",
                (fillcolor.focalPoint.X-50) * gradLength/100f , //50% is center
                (50 - fillcolor.focalPoint.Y) * gradLength / 100f,
                fillcolor.positionList[0] * gradLength / 100f,
                "color0!" + AlphaValTex(firstcol),
                assignedName);

            for (int ii = 1; ii < fillcolor.colorList.Count; ii++)
            {
                Color curcol = fillcolor.colorList[ii];
                writer.Write("; color({0}pt)=({1})",
                    fillcolor.positionList[ii] * gradLength / 100f,
                    "color" + ii.ToString() + AlphaValTex(curcol));
            }

            writer.WriteLine("}");
        }

        public static string AlphaValTex(Color linecolor)
        {
            if (!linecolor.Equals(Color.Empty))
            {
                return "!" + (linecolor.A * 100 / 255).ToString();
            }
            return "";
        }

        public static Boolean isGradientID(string gradientText, System.Collections.Generic.List<GradientDefinition> definitions)
        {
            for (int ii = 0; ii < definitions.Count; ii++)
            {
                GradientDefinition def = definitions[ii];
                if (def.ID.Equals(gradientText))
                {
                    return true;
                }
            }
            return false;
        }

        public static Boolean isColorID(string strokeText, System.Collections.Generic.List<ColorDefinition> definitions)
        {
            for (int ii = 0; ii < definitions.Count; ii++)
            {
                ColorDefinition def = definitions[ii];
                if (def.ID.Equals(strokeText))
                {
                    return true;
                }
            }
            return false;
        }

        public static string getStrokeRef(GraphicalPrimitive1D graphItem, RenderInformation rendinfo, Group refgroup)
        {
            if (FillColor.isColorID(graphItem.Stroke, rendinfo.ColorDefinitions))
            {
                return graphItem.Stroke;
            }
            else if (FillColor.isColorID(refgroup.Stroke, rendinfo.ColorDefinitions))
            {
                return refgroup.Stroke;
            }
            else if (FillColor.isColorID(refgroup.Fill, rendinfo.ColorDefinitions))
            {
                return refgroup.Fill;
            }
            else
            {
                return null;
            }
        }

        public static string getColorRef(GraphicalPrimitive2D graphItem, RenderInformation rendinfo, Group refgroup)
        {
            if (FillColor.isColorID(graphItem.Fill, rendinfo.ColorDefinitions))
            {
                return graphItem.Fill;
            }
            else if (FillColor.isColorID(refgroup.Fill, rendinfo.ColorDefinitions))
            {
                return refgroup.Fill;
            }
            else
            {
                return null;
            }
        }

        // gets the degree of rotation from the vertical axis for 2 points, returns -1 if they are the same point
        public static double GetGradientRotation(PointF start, PointF end)
        {
            if (start.X == end.X && start.Y == end.Y)
                return -1;
            if (start.X == end.X)
            {
                if (start.Y < end.Y)
                    return 0;
                return 180;
            }

            if (start.Y == end.Y)
            {
                if (start.X > end.X)
                    return -90;
                return 90;
            }

            double alpha = Math.Atan((double)((end.X - start.X) / (end.Y - start.Y))) * 180 / Math.PI;
            if (alpha < 0) // 4th or 2nd quadrant
            {
                if (end.Y - start.Y < 0) // 2nd quadrant
                {
                    alpha = -180 + alpha;
                }
            }
            else // 1st or 3rd quadrant
            {
                if (end.Y - start.Y < 0) // 3rd quadrant
                {
                    alpha = -180 + alpha;
                }
            }
            return alpha; // TikZ shade angle is anticlockwise rotated
        }
    }
}
