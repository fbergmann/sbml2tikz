using System;
using System.Collections.Generic;
using SBMLExtension.LayoutExtension;

namespace SBML2TikZ
{
    public enum units { pts, cm, inches };
    public enum papersize { a0paper, a1paper, a2paper, a3paper, a4paper, a5paper, a6paper }

    public class RenderSpecs
    {
        private double _desiredHeight;
        private double _desiredWidth;
        private double _height;
        private double _width;
        private units _heightUnits;
        private units _widthUnits;
        private papersize _size;
        private double[] _allPaperHeightsInInch;
        private double[] _allPaperWidthsInInch;
        

        public double desiredHeight
        {
            get { return RenderSpecs.convertToPoints(_desiredHeight, _heightUnits); }
        }

        public double desiredWidth
        {
            get { return RenderSpecs.convertToPoints(_desiredWidth, _widthUnits); }
        }

        public double height
        {
            get { return _height; }
        }

        public double width
        {
            get { return _width; }
        }
  
        public units heightUnits
        {
            get { return _heightUnits; }
        }

        public units widthUnits
        {
            get { return _widthUnits; }
        }

        public papersize size
        {
            get { return _size; }
        }

        public double[] allPaperHeightsInInch
        {
            get { return _allPaperHeightsInInch; }
            set
            {
                int paperSizes = Enum.GetValues(typeof(papersize)).Length;
                if (value.Length == paperSizes)
                {
                    _allPaperHeightsInInch = value;
                }
                else
                {
                    throw new System.ArgumentException("Length of input does not match the number of paper sizes supported", "allPaperHeightsInInch");
                }
            }
        }

        public double[] allPaperWidthsInInch
        {
            get { return _allPaperWidthsInInch; }
            set
            {
                int paperSizes = Enum.GetValues(typeof(papersize)).Length;
                if (value.Length == paperSizes)
                {
                    _allPaperWidthsInInch = value;
                }
                else
                {
                    throw new System.ArgumentException("Length of input does not match the number of paper sizes supported", "allPaperWidthsInInch");
                }
            }
        }


        public RenderSpecs(Layout layout, double dheight, double dwidth) : this(layout)
        {
            _desiredHeight = dheight;
            _desiredWidth = dwidth;
            _widthUnits = units.pts;
            _heightUnits = units.pts;
        }

        public RenderSpecs(Layout layout)
        {
            _desiredHeight = layout.Dimensions.Height;
            _desiredWidth = layout.Dimensions.Width;
            _height = layout.Dimensions.Height;
            _width = layout.Dimensions.Width;
            _widthUnits = units.pts;
            _heightUnits = units.pts;
            setStandardPageSizes();
            _size = findOptimalSize(_desiredHeight, units.pts, _desiredWidth, units.pts);
        }

        public void setDimensions(double h, double w, units hunits, units wunits)
        {
            _desiredHeight = h;
            _desiredWidth = w;
            _heightUnits = hunits;
            _widthUnits = wunits;
            _size = findOptimalSize(_desiredHeight, hunits, _desiredWidth, wunits);
        }

        private void setStandardPageSizes()
        {
            _allPaperHeightsInInch = new double[] { 46.8, 33.1, 23.4, 16.5, 11.7, 8.3, 5.8 };
            _allPaperWidthsInInch = new double[] { 33.1, 23.4, 16.5, 11.7, 8.3, 5.8, 4.1 };
            if (_desiredHeight > _desiredWidth)
            {
                double[] temp = _allPaperHeightsInInch;
                _allPaperHeightsInInch = _allPaperWidthsInInch;
                allPaperWidthsInInch = temp;
            }
        }

        public static double convertLengthUnits(double value, units newUnit, units oldUnit)
        {
            if (newUnit.Equals(oldUnit))
                return value;

            switch (newUnit)
            {
                case units.pts:
                    return convertToPoints(value, oldUnit);

                case units.inches:
                    return convertToInches(value, oldUnit);

                case units.cm:
                    return convertToCm(value, oldUnit);
            }
            return 0;
        }

        private static double convertToPoints(double value, units unit)
        {
            if (unit.Equals(units.inches))
            {
                value = value / 0.013836; 
            }
            if (unit.Equals(units.cm))
            {
                value = value * 28.3464567; 
            }
            return value;
        }

        private static double convertToInches(double value, units unit)
        {
            if (unit.Equals(units.cm))
            {
                value = value * 0.393700787;
            }
            if (unit.Equals(units.pts))
            {
                value = value * 0.013836;
            }
            return value;
        }

        private static double convertToCm(double value, units unit)
        {
            if (unit.Equals(units.inches))
            {
                value = value / 0.393700787;
            }
            if (unit.Equals(units.pts))
            {
                value = value / 28.3464567;
            }
            return value;
        }

        private papersize findOptimalSize(double height, units heightunits, double width, units widthunits)
        {
            double heightInInches = RenderSpecs.convertToInches(height, heightunits);
            double widthInInches = RenderSpecs.convertToInches(width, widthunits);

            int heightSize = 5;
            int widthSize = 5;
            for (int ii = 0; ii < allPaperHeightsInInch.Length-1; ii++)
            {
                if (heightInInches < allPaperHeightsInInch[ii] && heightInInches > allPaperHeightsInInch[ii + 1])
                {
                    heightSize = ii;
                }
                if (widthInInches < allPaperWidthsInInch[ii] && widthInInches > allPaperWidthsInInch[ii + 1])
                {
                    widthSize = ii;
                }
            }
            return (papersize) (Math.Min(heightSize, widthSize));
        }
    }
}
