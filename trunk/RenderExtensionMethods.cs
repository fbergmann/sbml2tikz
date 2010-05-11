using System;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using SBMLExtension.EmlRenderExtension;
using SBMLExtension.LayoutExtension;
using System.Configuration;

namespace SBML2TikZ
{
    public static class RenderExtensionMethods
    {
        // Adapted from EmlRenderExtension.Group.Draw()
        public static void ToTex(this Group group, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, RectangleF refbounds, double scale, Hashtable fontTexTable)
        {
            if (group.Children.Count == 0)
            {
                if (glyph is TextGlyph)
                {
                    group.TextTex((TextGlyph)glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
                }
                else if (glyph is ReactionGlyph)
                {
                    group.ReactionTex((ReactionGlyph)glyph, writer, g, rendinfo, refgroup, refbounds, scale, fontTexTable);
                }
                else if (glyph is SpeciesReference)
                {
                    SpeciesReference graphElement = (SpeciesReference)glyph;
                    Style styleForObjectID = rendinfo.GetStyleForObjectId(graphElement);

                    if (styleForObjectID == null)
                        styleForObjectID = rendinfo.GetStyleForObjectRole(graphElement);

                    if (styleForObjectID == null)
                        styleForObjectID = rendinfo.GetStyleForObjectType(graphElement);

                    if (styleForObjectID == null && string.IsNullOrEmpty(graphElement.ObjectRole))
                    {
                        graphElement.ObjectRole = graphElement.Role;
                        styleForObjectID = rendinfo.GetStyleForObjectRole(graphElement);
                    }
                    if (styleForObjectID == null)
                    {
                        group.SpeciesReferenceTex((SpeciesReference)glyph, writer, g, rendinfo, group, scale, fontTexTable);
                    }
                    else
                    {
                        group.SpeciesReferenceTex((SpeciesReference)glyph, writer, g, rendinfo, styleForObjectID.Group, scale, fontTexTable);
                    }
                }
                else
                {
                    Style styleForObjectRole = rendinfo.GetStyleForObjectId(glyph);
                    if (styleForObjectRole == null)
                        styleForObjectRole = rendinfo.GetStyleForObjectRole(glyph);
                    if (styleForObjectRole == null)
                        styleForObjectRole = rendinfo.GetStyleForObjectType(glyph);
                    if ((styleForObjectRole != null) && ((styleForObjectRole.Group != null) && (styleForObjectRole.Group.Children.Count != 0)))
                    {
                        styleForObjectRole.Group.ToTex(glyph, writer, g, rendinfo, styleForObjectRole.Group, refbounds, scale, fontTexTable);
                    }
                }
            }
            else{
                foreach (SBMLExtension.EmlRenderExtension.GraphicalObject childObj in group.Children)
                {
                    // Determine what type is the childObj and find the style accordingly
                    if (childObj is SBMLExtension.EmlRenderExtension.Image)
                    {
                        SBMLExtension.EmlRenderExtension.Image childImage = (SBMLExtension.EmlRenderExtension.Image)childObj;
                        childImage.ImageTex(glyph, writer, rendinfo, refgroup, scale);
                    }
                    else
                    {
                        // childObj is a GraphicalPrimitve1D
                        if (childObj is Text)
                        {
                            Text childText = (Text)childObj;
                            childText.TextTex(glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
                        }
                        else if (childObj is Group)
                        {
                            writer.WriteLine("{");
                            writer.Indent += 1;
                            CommentTex(glyph, childObj, writer);
                            Group childgrp = (Group)childObj;
                            childgrp.ToTex(glyph, writer, g, rendinfo, refgroup, refbounds, scale, fontTexTable);
                            writer.Indent -= 1;
                            writer.WriteLine("}");
                        }
                        else
                        {
                            // childObj is a GraphicalPrimitve2D
                            if (childObj is SBMLExtension.EmlRenderExtension.Rectangle)
                            {
                                SBMLExtension.EmlRenderExtension.Rectangle childRect = (SBMLExtension.EmlRenderExtension.Rectangle)childObj;
                                childRect.RectangleTex(glyph, writer, rendinfo, refgroup, refbounds, scale);
                            }
                            if (childObj is SBMLExtension.EmlRenderExtension.Polygon)
                            {
                                Polygon childPoly = (Polygon)childObj;
                                childPoly.PolygonTex(glyph, writer, rendinfo, refgroup, refbounds, scale);
                            }
                            if (childObj is SBMLExtension.EmlRenderExtension.LineEnding)
                            {
                                LineEnding childEnd = (LineEnding)childObj;
                                childEnd.LineEndingTex(glyph, writer, g, rendinfo, refgroup, scale, true, fontTexTable);
                            }
                            if (childObj is SBMLExtension.EmlRenderExtension.Ellipses)
                            {
                                Ellipses childEllipse = (Ellipses)childObj;
                                childEllipse.EllipseTex(glyph, rendinfo, writer, refgroup, refbounds, scale);
                            }
                            if (childObj is SBMLExtension.EmlRenderExtension.Curve)
                            {
                                SBMLExtension.EmlRenderExtension.Curve childCurve = (SBMLExtension.EmlRenderExtension.Curve)childObj;
                                childCurve.CurveTex(glyph, rendinfo, writer, refgroup, refbounds, scale);
                            }
                        }
                    }
                }
                if (glyph is ReactionGlyph)
                {
                    ReactionGlyph rGlyph = (ReactionGlyph)glyph;
                    foreach (SpeciesReference reference in rGlyph.SpeciesReferences)
                    {
                        Style styleForObjectType = rendinfo.GetStyleForObjectId(reference);
                        if (styleForObjectType == null)
                            styleForObjectType = rendinfo.GetStyleForObjectRole(reference);
                        if (styleForObjectType == null)
                            styleForObjectType = rendinfo.GetStyleForObjectType(reference);
                        if (styleForObjectType == null)
                            group.SpeciesReferenceTex(reference, writer, g, rendinfo, group, scale, fontTexTable);
                        else
                            group.SpeciesReferenceTex(reference, writer, g, rendinfo, styleForObjectType.Group, scale, fontTexTable);
                    }
                }
            }

            if (!string.IsNullOrEmpty(group.EndHead))
            {
                LineEnding endhead = rendinfo.GetLineEnding(group.StartHead);
                endhead.LineEndingTex(glyph, writer, g, rendinfo, group, scale, false, fontTexTable);
            }
            if (!string.IsNullOrEmpty(group.StartHead))
            {
                LineEnding starthead = rendinfo.GetLineEnding(group.EndHead);
                starthead.LineEndingTex(glyph, writer, g, rendinfo, group, scale, false, fontTexTable);
            }
        }

        private static void RotateAndShift(this SBMLExtension.EmlRenderExtension.GraphicalObject obj, RectangleF bound_rectangle, IndentedTextWriter writer)
        {
            writer.WriteLine("\\pgftransformshift{{\\pgfpoint{{ {0}pt }}{{ {1}pt }} }}",
                   bound_rectangle.X.ToString(),
                   (bound_rectangle.Y).ToString());
            if (!string.IsNullOrEmpty(obj.Transform))
            {
                Matrix matrix = obj.GetMatrix(bound_rectangle.X, bound_rectangle.Y);
                writer.WriteLine("\\pgftransformcm {{ {0} }}{{ {1} }}{{ {2} }}{{ {3} }}{{\\pgfpoint{{ {4}pt }}{{ {5}pt }} }}",
                    matrix.Elements[0].ToString(),
                    (matrix.Elements[1]).ToString(),
                    (matrix.Elements[2]).ToString(),
                    matrix.Elements[3].ToString(),
                    matrix.Elements[4].ToString(),
                    (matrix.Elements[5]).ToString());
            }
        }

        public static void ImageTex(this SBMLExtension.EmlRenderExtension.Image image, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            if (string.IsNullOrEmpty(image.FullPath))
            {
                image.FullPath = image.HRef;
                if (!File.Exists(image.FullPath))
                {
                    string dir = (string) SBMLExtension.Util.CurrentDirectory;
                    image.FullPath = dir + "\\" + image.FullPath;
                }
            }
            if (!string.IsNullOrEmpty(image.FullPath) && File.Exists(image.FullPath))
            {
                writer.WriteLine("{");
                writer.Indent += 1;
                CommentTex(glyph, image, writer);
                RectangleF bound_rectangle = glyph.Bounds.toRect();
                PointF location = new PointF(image.X.Contains("%") ? (bound_rectangle.X + ((bound_rectangle.Width * SBMLExtension.Util.readSingle(image.X)) / 100f)) : (bound_rectangle.X + SBMLExtension.Util.readSingle(image.X)),
                                             image.Y.Contains("%") ? (bound_rectangle.Y + ((bound_rectangle.Height * SBMLExtension.Util.readSingle(image.Y)) / 100f)) : (bound_rectangle.Y + SBMLExtension.Util.readSingle(image.Y)));

                SizeF size = new SizeF(image.Width.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(image.Width)) / 100f) : SBMLExtension.Util.readSingle(image.Width), 
                                       image.Height.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(image.Height)) / 100f) : SBMLExtension.Util.readSingle(image.Height));
                size.Height = size.Height * (float)scale;
                size.Width = size.Width * (float)scale;
                //note that pgftransformcm does not rotate imported images
                double rotate = 0;
                if (!string.IsNullOrEmpty(image.Transform))
                {
                    Matrix matrix = image.GetMatrix(bound_rectangle.X, bound_rectangle.Y);
                    if (Math.Abs(matrix.Elements[0]) <= 1)
                    {
                        rotate = Math.Asin(matrix.Elements[0]);
                    }
                }

                writer.WriteLine("\\draw ({1}pt, {2}pt) node []{{ \\includegraphics[height = {3}pt, width = {4}pt, angle = {5}]{{{0}}} }};",
                    Path.GetFileName(image.FullPath),
                    location.X,
                    location.Y,
                    size.Height,
                    size.Width,
                    rotate
                    );
                writer.Indent -= 1;
                writer.WriteLine("}");
            }
        }

        public static void LineEndingTex(this LineEnding ending, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Boolean notInReactionGlyph, Hashtable fontTexTable)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, ending, writer);
            RectangleF bound_rectangle = ending.BoundingBox.toRect();
            if (notInReactionGlyph)
            {
                bound_rectangle = glyph.Bounds.toRect();
                RotateAndShift(ending, bound_rectangle, writer);
            }
            ending.Group.ToTex(glyph, writer, g, rendinfo, refgroup, bound_rectangle, scale, fontTexTable);
            writer.Indent -= 1;
            writer.WriteLine("}");
        }



        // adapted from EmlRenderExtension.Rectangle.draw
        public static void RectangleTex(this SBMLExtension.EmlRenderExtension.Rectangle rectangle, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, RectangleF refbounds, double scale)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, rectangle, writer);
            // Check what kind of filling 
            RectangleF bound_rectangle = glyph.Bounds.toRect();
            if (!refbounds.IsEmpty)
                bound_rectangle = refbounds;
            FillColor fill = rectangle.GetFillColor(rendinfo, bound_rectangle, refgroup);

            RotateAndShift(rectangle, bound_rectangle, writer);

            if (fill == null)
            {
                fill = new FillColor(Color.Empty);
            }
            if (fill.gradient && !fill.radial && fill.colorList.Count > 1)
            {
                rectangle.LinearGradTex(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
            else if (fill.gradient && fill.colorList.Count > 1)
            {
                rectangle.RadialGradTex(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
            else
            {
                rectangle.SolidColorTex(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }

            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        public static void PolygonTex(this Polygon polygon, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, RectangleF refbounds, double scale)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, polygon, writer);
            RectangleF bound_rectangle = glyph.Bounds.toRect();
            if (!refbounds.IsEmpty)
                bound_rectangle = refbounds;
            FillColor fill = polygon.GetFillColor(rendinfo, bound_rectangle, refgroup);

            RotateAndShift(polygon, bound_rectangle, writer);

            if (fill == null)
            {
                fill = new FillColor(Color.Empty);
            }

            if (fill.gradient && !fill.radial && fill.colorList.Count > 1)
            {
                polygon.LinearGradientPolygon(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
            else if (fill.gradient && fill.colorList.Count > 1)
            {
                polygon.PathGradientPolygon(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
            else
            {
                polygon.SolidColorPolygon(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }

            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        // adapted from EmlRenderExtension.Ellipses.draw
        public static void EllipseTex(this Ellipses ellipse, SBMLExtension.LayoutExtension.GraphicalObject glyph, RenderInformation rendinfo, IndentedTextWriter writer, Group refgroup, RectangleF refbounds, double scale)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, ellipse, writer);
            // Check what kind of filling
            RectangleF bound_rectangle = glyph.Bounds.toRect();
            if (!refbounds.IsEmpty)
                bound_rectangle = refbounds;
            FillColor fill = ellipse.GetFillColor(rendinfo, bound_rectangle, refgroup);

            RotateAndShift(ellipse, bound_rectangle, writer);

            if (fill == null)
            {
                fill = new FillColor(Color.Empty);
            }

            if (fill.gradient && !fill.radial)
            {
                ellipse.LinearGradTex(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
            else if (fill.gradient)
            {
                ellipse.RadialGradTex(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
            else
            {
                ellipse.SolidColorTex(bound_rectangle, fill, writer, rendinfo, refgroup, scale);
            }
 
            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        public static void CurveTex(this SBMLExtension.EmlRenderExtension.Curve curve, SBMLExtension.LayoutExtension.GraphicalObject glyph, RenderInformation rendinfo, IndentedTextWriter writer, Group refgroup, RectangleF refbounds, double scale)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, curve, writer);
            RectangleF bound_rectangle = glyph.Bounds.toRect();
            if (!refbounds.IsEmpty)
                bound_rectangle = refbounds;
            RotateAndShift(curve, bound_rectangle, writer);

            // obtain fill and stroke information
            Color linecolor = curve.GetStrokeColor(rendinfo, refgroup);
            string strokewidth = curve.GetStrokeWidth(rendinfo, refgroup, scale);
            FillColor fill = curve.GetFillColor(rendinfo, bound_rectangle, refgroup);
            string dashed = "";
            if (!string.IsNullOrEmpty(curve.GetDashType(refgroup)))
            {
                dashed = ", " + curve.DashTex(curve.GetDashType(refgroup));
            }

            // refer stroke information to a ColorDefinition or assign a new colordefinition to "curLineColor"
            string strokeRef = FillColor.getStrokeRef(curve, rendinfo, refgroup);
            if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curLineColor";
                FillColor.AssignColorRGBTex(linecolor, "curLineColor", writer);
            }
            
            // now either draw the outline if fill is null, or draw and fill otherwise
            if (fill == null || fill.colorList[0].Equals(Color.Empty))
            {
                if (curve.CurveSegments.Count != 0)
                {
                    // draw all curveSegments
                    writer.WriteLine("\\draw [line width = {0}pt, color = {3}{1} {2}] ",
                        strokewidth,
                        FillColor.AlphaValTex(linecolor),
                        dashed,
                        strokeRef);
                    SegmentsTex(curve.CurveSegments, bound_rectangle, writer, false);
                }

                if (curve.ListOfElements.Count != 0)
                {
                    // draw all listOfElements
                    writer.Write("\\draw [line width = {0}pt, color = {3}{1} {2}] ",
                        strokewidth,
                        FillColor.AlphaValTex(linecolor),
                        dashed,
                        strokeRef);
                    ElementsTex(curve.ListOfElements, writer, bound_rectangle, false, false);
                }
            }
            else if (fill.gradient) 
            {
                if (!FillColor.isGradientID(fill.ID, rendinfo.GradientDefinitions))
                {
                    FillColor.AssignGradientTex(fill, fill.ID, writer);
                }
                if (curve.CurveSegments.Count != 0)
                {
                    // draw all curveSegments
                    writer.WriteLine("\\draw [line width = {0}pt, color = {5}{1},shading = {4}, shading angle = {2} {3}] ",
                        strokewidth,
                        FillColor.AlphaValTex(linecolor),
                        fill.gradient_rotation,
                        dashed,
                        fill.ID,
                        strokeRef);
                    PolygonSegmentsTex(curve.CurveSegments, bound_rectangle, writer, false);
                }

                if (curve.ListOfElements.Count != 0)
                {
                    // draw all listOfElements
                    writer.WriteLine("\\draw [line width = {0}pt, color = {5}{1}, shading = {4}, shading angle = {2} {3}] ",
                        strokewidth,
                        FillColor.AlphaValTex(linecolor),
                        fill.gradient_rotation,
                        dashed,
                        fill.ID,
                        strokeRef);
                    ElementsTex(curve.ListOfElements, writer, bound_rectangle, false, true);
                }
            }

            else //solid fill
            {
                Color curSolidColor = fill.colorList[0];
                string solidRef = FillColor.getColorRef(curve, rendinfo, refgroup);
                if (String.IsNullOrEmpty(solidRef) || !FillColor.isColorID(solidRef, rendinfo.ColorDefinitions))
                {
                    solidRef = "curSolidColor";
                    FillColor.AssignColorRGBTex(curSolidColor, "curSolidColor", writer);
                }
                if (curve.CurveSegments.Count != 0)
                {
                    // draw all curveSegments
                    writer.WriteLine("\\draw [line width = {0}pt, {4}{1}, fill = {5}{2} {3}] ",
                        strokewidth,
                        FillColor.AlphaValTex(linecolor),
                        FillColor.AlphaValTex(curSolidColor),
                        dashed,
                        strokeRef,
                        solidRef);
                    PolygonSegmentsTex(curve.CurveSegments, bound_rectangle, writer, false);
                }

                if (curve.ListOfElements.Count != 0)
                {
                    // draw all listOfElements
                    writer.WriteLine("\\draw [line width = {0}pt, color = {4}{1}, fill = {5}{2} {3}] ",
                        strokewidth,
                        FillColor.AlphaValTex(linecolor),
                        FillColor.AlphaValTex(curSolidColor),
                        dashed,
                        strokeRef,
                        solidRef);
                    ElementsTex(curve.ListOfElements, writer, bound_rectangle, false, true);
                }
            }
            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        private static void PathGradientPolygon(this Polygon polygon, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            string strokewidth = polygon.GetStrokeWidth(rendinfo, refgroup, scale);
            string dashed = "";
            if (!string.IsNullOrEmpty(polygon.GetDashType(refgroup)))
            {
                dashed = ", " + polygon.DashTex(polygon.GetDashType(refgroup));
            }
            Color linecolor = polygon.GetStrokeColor(rendinfo, refgroup);

            // refer stroke information to a ColorDefinition or assign a new colordefinition to "curLineColor"
            string strokeRef = FillColor.getStrokeRef(polygon, rendinfo, refgroup);
            if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curLineColor";
                FillColor.AssignColorRGBTex(linecolor, "curLineColor", writer);
            }

            if (!FillColor.isGradientID(fillcolor.ID, rendinfo.GradientDefinitions))
            {
                FillColor.AssignGradientTex(fillcolor, fillcolor.ID, writer);
            }

            // draw the filling
            if (fillcolor.colorList.Count == fillcolor.positionList.Count)
            {
                writer.WriteLine("\\draw [line width = {0}pt, color = {5}{1}, shading = {4}, shading angle = {2} {3}] ",
                    strokewidth,
                    FillColor.AlphaValTex(linecolor),
                    dashed,
                    fillcolor.ID,
                    strokeRef);

                PolygonSegmentsTex(polygon.CurveSegments, bound_rectangle, writer, false);
                PolygonElementsTex(polygon.ListOfElements, writer, bound_rectangle, false);
            }
        }

        private static void LinearGradientPolygon(this Polygon polygon, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            string strokewidth = polygon.GetStrokeWidth(rendinfo, refgroup, scale);
            string dashed = "";
            if (!string.IsNullOrEmpty(polygon.GetDashType(refgroup)))
            {
                dashed = ", " + polygon.DashTex(polygon.GetDashType(refgroup));
            }
            Color linecolor = polygon.GetStrokeColor(rendinfo, refgroup);
            // refer stroke information to a ColorDefinition or assign a new colordefinition to "curLineColor"
            string strokeRef = FillColor.getStrokeRef(polygon, rendinfo, refgroup);
            if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curLineColor";
                FillColor.AssignColorRGBTex(linecolor, "curLineColor", writer);
            }

            if (!FillColor.isGradientID(fillcolor.ID, rendinfo.GradientDefinitions))
            {
                FillColor.AssignGradientTex(fillcolor, fillcolor.ID, writer);
            }

            writer.WriteLine("\\draw [line width = {0}pt, color = {5}{1}, shading = {4}, shading angle = {2} {3}] ",
                strokewidth,
                FillColor.AlphaValTex(linecolor),
                -180+fillcolor.gradient_rotation,
                dashed,
                fillcolor.ID,
                strokeRef);

            PolygonSegmentsTex(polygon.CurveSegments, bound_rectangle, writer, false);
            PolygonElementsTex(polygon.ListOfElements, writer, bound_rectangle, false);
        }

        private static void SolidColorPolygon(this Polygon polygon, RectangleF bound_rectangle, FillColor fillColor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            string strokewidth = polygon.GetStrokeWidth(rendinfo, refgroup, scale);
            string dashed = "";
            if (!string.IsNullOrEmpty(polygon.GetDashType(refgroup)))
            {
                dashed = ", " + polygon.DashTex(polygon.GetDashType(refgroup));
            }
            // obtain stroke color and filling color
            Color linecolor = polygon.GetStrokeColor(rendinfo, refgroup);
            Color curSolidColor = fillColor.colorList[0];

            // refer stroke information to a ColorDefinition ID or assign a new colordefinition to "curLineColor"
            string strokeRef = FillColor.getStrokeRef(polygon, rendinfo, refgroup);
            if (linecolor.Equals(Color.Empty))
            {
                strokeRef = "none";
            }
            else if (!FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curSolidColor";
                FillColor.AssignColorRGBTex(curSolidColor, "curSolidColor", writer);
            }

            // refer solidfill information to a ColorDefinition ID or assign a new colordefinition to "curSolidColor"
            string solidRef = FillColor.getColorRef(polygon, rendinfo, refgroup);
            if (curSolidColor.Equals(Color.Empty))
            {
                solidRef = "none";
            }
            else if (!FillColor.isColorID(solidRef, rendinfo.ColorDefinitions))
            {
                solidRef = "curSolidColor";
                FillColor.AssignColorRGBTex(curSolidColor, "curSolidColor", writer);
            }

            writer.WriteLine("\\draw [line width = {0}pt, color = {4}{1}, fill = {5}{2} {3}] ",
                strokewidth,
                FillColor.AlphaValTex(linecolor),
                FillColor.AlphaValTex(curSolidColor),
                dashed,
                strokeRef,
                solidRef);

            PolygonSegmentsTex(polygon.CurveSegments, bound_rectangle, writer, false);
            PolygonElementsTex(polygon.ListOfElements, writer, bound_rectangle,false);
        }

        private static void PolygonSegmentsTex(IList<CurveSegment> curveSegments, RectangleF bound_rectangle, IndentedTextWriter writer, Boolean relocate)
        {
            if (curveSegments.Count != 0)
            {
                // sort the curveSegments first into consecutive order
                // if they can be sorted this way, they form a polygon
                Boolean sorted = true;
                for (int ii = 1; ii < curveSegments.Count; ii++)
                {
                    CurveSegment nextSeg = curveSegments[ii];
                    if (nextSeg.Start != curveSegments[ii - 1].End) // ie if not in order
                    {
                        sorted = false;
                        for (int jj = ii; (jj < curveSegments.Count && !sorted); jj++)
                        {
                            CurveSegment seg = curveSegments[jj];
                            if (seg.Start == curveSegments[ii - 1].End)
                            {
                                curveSegments.RemoveAt(jj);
                                curveSegments.Insert(ii, seg);
                                nextSeg = seg;
                                sorted = true;
                            }
                        }
                    }
                }
                if (curveSegments[curveSegments.Count - 1].End != curveSegments[0].Start)
                    sorted = false;

                if (sorted) // we know the segments form a polygon 
                {
                    foreach (CurveSegment seg in curveSegments)
                    {
                        CurveSegment segAbs = seg.ToAbsolute(bound_rectangle, relocate);
                        if (segAbs.Type == "LineSegment" || string.IsNullOrEmpty(segAbs.Type))
                            writer.Write("({0}pt,{1}pt)--", segAbs.Start.X, segAbs.Start.Y);
                        else
                            writer.Write("({0}pt, {1}pt) .. controls ({2}pt, {3}pt) and ({4}pt, {5}pt) .. ",
                                segAbs.Start.X, segAbs.Start.Y,
                                segAbs.BasePoint1.X, segAbs.BasePoint1.Y,
                                segAbs.BasePoint2.X, segAbs.BasePoint2.Y);
                    }
                    CurveSegment lastAbs = curveSegments[curveSegments.Count - 1].ToAbsolute(bound_rectangle, relocate);
                    writer.WriteLine("({0}pt, {1}pt) -- cycle;",
                        lastAbs.End.X,
                        lastAbs.End.Y);
                }
                else // we know the segments may not form a polygon; don't try to close
                {
                    SegmentsTex(curveSegments, bound_rectangle, writer, relocate);
                }
            }
        }

        // writes the coordinates out into Tex; NOTE: need the style "\\draw []" written first before calling this
        private static void SegmentsTex(IList<CurveSegment> curveSegments, RectangleF bound_rectangle, IndentedTextWriter writer, Boolean relocate)
        {
            for (int ii = 0; ii < curveSegments.Count-1; ii++)
            {
                CurveSegment segAbs = curveSegments[ii].ToAbsolute(bound_rectangle, relocate);
                if (segAbs.Type == "CubicBezier")
                {
                    writer.Write("({0}pt, {1}pt) .. controls ({2}pt, {3}pt) and ({4}pt, {5}pt) ..({6}pt,{7}pt)--",
                       segAbs.Start.X, segAbs.Start.Y,
                       segAbs.BasePoint1.X, segAbs.BasePoint1.Y,
                       segAbs.BasePoint2.X, segAbs.BasePoint2.Y,
                       segAbs.End.X, segAbs.End.Y);
                }
                else
                {
                    writer.Write("({0}pt,{1}pt)--({2}pt,{3}pt)--",
                     segAbs.Start.X, segAbs.Start.Y,
                     segAbs.End.X, segAbs.End.Y);
                }
            }

            CurveSegment lastAbs = curveSegments[curveSegments.Count - 1].ToAbsolute(bound_rectangle, relocate);
            if (lastAbs.Type == "CubicBezier")
            {
                writer.Write("({0}pt, {1}pt) .. controls ({2}pt, {3}pt) and ({4}pt, {5}pt) ..({6}pt,{7}pt)",
                      lastAbs.Start.X, lastAbs.Start.Y,
                      lastAbs.BasePoint1.X, lastAbs.BasePoint1.Y,
                      lastAbs.BasePoint2.X, lastAbs.BasePoint2.Y,
                      lastAbs.End.X, lastAbs.End.Y);
            }else
            {
                writer.Write("({0}pt,{1}pt)--({2}pt,{3}pt)",
                     lastAbs.Start.X, lastAbs.Start.Y,
                     lastAbs.End.X, lastAbs.End.Y);
            }
            writer.WriteLine(";");
        }

       private static void PolygonElementsTex(IList<RenderPoint> listOfElements, IndentedTextWriter writer, RectangleF bound_rectangle, Boolean relocate)
        {
            //then call elementstex
            ElementsTex(listOfElements, writer, bound_rectangle, relocate, true);
        }

        private static void ElementsTex(IList<RenderPoint> listOfElements, IndentedTextWriter writer, RectangleF bound_rectangle, Boolean relocate, Boolean closed)
        {
            if (listOfElements.Count >=2)
            {
                for (int ii = 1; ii < listOfElements.Count-1; ii++)
                {
                    if (listOfElements[ii] is RenderCubicBezier)
                    {
                        PointF lastPoint = GetRelocatablePointForAttribute(listOfElements[ii - 1].X, listOfElements[ii - 1].Y, bound_rectangle, relocate);
                        RenderCubicBezier curPoint = (RenderCubicBezier)listOfElements[ii];
                        PointF basePoint1 = GetRelocatablePointForAttribute(curPoint.BasePoint1_X, curPoint.BasePoint1_Y, bound_rectangle, relocate);
                        PointF basePoint2 = GetRelocatablePointForAttribute(curPoint.BasePoint2_X, curPoint.BasePoint2_Y, bound_rectangle, relocate);

                        RenderPoint curPoint2 = (RenderPoint)listOfElements[ii];
                        PointF end = GetRelocatablePointForAttribute(curPoint2.X, curPoint2.Y, bound_rectangle, relocate);

                        writer.Write("({0}pt, {1}pt) .. controls ({2}pt, {3}pt) and ({4}pt, {5}pt) ..({6}pt,{7}pt)--",
                            lastPoint.X, lastPoint.Y,
                            basePoint1.X, basePoint1.Y,
                            basePoint2.X, basePoint2.Y,
                            end.X, end.Y);
                    }
                    else
                    {
                        PointF lastPoint = GetRelocatablePointForAttribute(listOfElements[ii - 1].X, listOfElements[ii - 1].Y, bound_rectangle, relocate);
                        PointF curPoint = GetRelocatablePointForAttribute(listOfElements[ii].X, listOfElements[ii].Y, bound_rectangle, relocate);
                        writer.Write("({0}pt,{1}pt)--({2}pt,{3}pt)--",
                            lastPoint.X, lastPoint.Y,
                            curPoint.X, curPoint.Y);
                    }
                }

                string cycle = " ";
                if (closed)
                {
                    cycle = "--cycle";
                }

                int lastElementNo = listOfElements.Count - 1;
                if (listOfElements[lastElementNo] is RenderCubicBezier)
                {
                    PointF lastPoint = GetRelocatablePointForAttribute(listOfElements[lastElementNo - 1].X, listOfElements[lastElementNo - 1].Y, bound_rectangle, relocate);
                    RenderCubicBezier curPoint = (RenderCubicBezier)listOfElements[lastElementNo];
                    PointF basePoint1 = GetRelocatablePointForAttribute(curPoint.BasePoint1_X, curPoint.BasePoint1_Y, bound_rectangle, relocate);
                    PointF basePoint2 = GetRelocatablePointForAttribute(curPoint.BasePoint2_X, curPoint.BasePoint2_Y, bound_rectangle, relocate);

                    RenderPoint curPoint2 = (RenderPoint)listOfElements[lastElementNo];
                    PointF end = GetRelocatablePointForAttribute(curPoint2.X, curPoint2.Y, bound_rectangle, relocate);

                    writer.Write("({0}pt, {1}pt) .. controls ({2}pt, {3}pt) and ({4}pt, {5}pt) ..({6}pt,{7}pt){8}",
                        lastPoint.X, lastPoint.Y,
                        basePoint1.X, basePoint1.Y,
                        basePoint2.X, basePoint2.Y,
                        end.X, end.Y, cycle);
                }
                else
                {
                    PointF lastPoint = GetRelocatablePointForAttribute(listOfElements[lastElementNo - 1].X, listOfElements[lastElementNo - 1].Y, bound_rectangle, relocate);
                    PointF curPoint = GetRelocatablePointForAttribute(listOfElements[lastElementNo].X, listOfElements[lastElementNo].Y, bound_rectangle, relocate);
                    writer.Write("({0}pt,{1}pt)--({2}pt,{3}pt){4}",
                        lastPoint.X, lastPoint.Y,
                        curPoint.X, curPoint.Y, cycle);
                }

                writer.WriteLine(";");
            }
        }

        private static PointF GetRelocatablePointForAttribute(string strX, string strY, RectangleF bound_rectangle, Boolean relocate)
        {
            if (bound_rectangle.IsEmpty)
            {
                return RenderPoint.GetPointForAttribute(strX, strY);
            }
            else
            {
                if (relocate)
                {
                    return RenderPoint.GetPointForAttribute(strX, strY, bound_rectangle);
                }
                float x = strX.Contains("%") ? ((SBMLExtension.Util.readSingle(strX) * bound_rectangle.Width) / 100f) : SBMLExtension.Util.readSingle(strX);
                return new PointF(x, strY.Contains("%") ? ((SBMLExtension.Util.readSingle(strY) * bound_rectangle.Height) / 100f) : SBMLExtension.Util.readSingle(strY));
            }
        }

        private static void DefineTex(this SBMLExtension.EmlRenderExtension.Rectangle rectangle, RectangleF bound_rectangle, IndentedTextWriter writer)
        {
            PointF[] Coords = RectPositionInBounds(rectangle, bound_rectangle);
            writer.WriteLine("\\def \\wholeShape {{({0}pt,{1}pt) rectangle({2}pt,{3}pt)}}",
                Coords[0].X,
                Coords[0].Y,
                Coords[1].X,
                Coords[1].Y);
        }

        private static void RadialGradTex(this SBMLExtension.EmlRenderExtension.Rectangle rectangle, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            // define the overall shape
            rectangle.DefineTex(bound_rectangle, writer);

            // draw the filling
            if (fillcolor.colorList.Count == fillcolor.positionList.Count)
            {
                if (!FillColor.isGradientID(fillcolor.ID, rendinfo.GradientDefinitions))
                {
                    FillColor.AssignGradientTex(fillcolor, fillcolor.ID, writer);
                }

                if (string.IsNullOrEmpty(rectangle.RX) || (SBMLExtension.Util.readSingle(rectangle.RX) == 0f))
                {
                    writer.WriteLine("\\fill[shading = {0}] \\wholeShape;", fillcolor.ID);
                }

                else
                {
                    float fRadiusX = rectangle.RX.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(rectangle.RX)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RX);
                    float fRadiusY = rectangle.RY.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(rectangle.RY)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RY);

                    writer.WriteLine("\\fill[rounded corners = {0}pt, shading = {1}] \\wholeShape;",
                            fRadiusX, fillcolor.ID);
                }
            }
            // draw the outline
            rectangle.OutlineTex(bound_rectangle, "wholeShape", writer, rendinfo, refgroup, scale);
        }

        // draws a rectangle with a linear gradient
        private static void LinearGradTex(this SBMLExtension.EmlRenderExtension.Rectangle rectangle, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            // define the overall shape
            rectangle.DefineTex(bound_rectangle, writer);

            // draw the filling
            if (fillcolor.colorList.Count == fillcolor.positionList.Count)
            {
                if (!FillColor.isGradientID(fillcolor.ID, rendinfo.GradientDefinitions))
                {
                    FillColor.AssignGradientTex(fillcolor, fillcolor.ID, writer);
                }

                if (string.IsNullOrEmpty(rectangle.RX) || (SBMLExtension.Util.readSingle(rectangle.RX) == 0f))
                {
                    writer.WriteLine("\\fill[shading = {1}, shading angle = {0}] \\wholeShape;",
                        -180+fillcolor.gradient_rotation, 
                        fillcolor.ID);
                }

                else
                {
                    float fRadiusX = rectangle.RX.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(rectangle.RX)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RX);
                    float fRadiusY = rectangle.RY.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(rectangle.RY)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RY);

                    writer.WriteLine("\\fill[rounded corners = {0}pt, shading = {2}, shading angle = {1} ] \\wholeShape;",
                            fRadiusX,
                            -180+fillcolor.gradient_rotation,
                            fillcolor.ID);
                }
            }
            // draw the outline
            rectangle.OutlineTex(bound_rectangle, "wholeShape", writer, rendinfo, refgroup, scale);
        }

        //draws a solid color rectangle
        private static void SolidColorTex(this SBMLExtension.EmlRenderExtension.Rectangle rectangle, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            //define the overall shape
            rectangle.DefineTex(bound_rectangle, writer);

            // draw the filling
            Color curSolidColor = fillcolor.colorList[0];

            // refer solidfill information to a ColorDefinition ID or assign a new colordefinition to "curSolidColor"
            string solidRef = FillColor.getColorRef(rectangle, rendinfo, refgroup);
            if (fillcolor.colorList[0].Equals(Color.Empty))
            {
                solidRef = "none";
            }
            else if (!FillColor.isColorID(solidRef, rendinfo.ColorDefinitions))
            {
                solidRef = "curSolidColor";
                FillColor.AssignColorRGBTex(curSolidColor, "curSolidColor", writer);
            }

            if (string.IsNullOrEmpty(rectangle.RX) || (SBMLExtension.Util.readSingle(rectangle.RX) == 0f))
            {
                writer.WriteLine("\\fill[fill = {1}{0}] \\wholeShape;",
                    FillColor.AlphaValTex(curSolidColor),
                    solidRef);
            }

            else
            {
                float fRadiusX = rectangle.RX.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(rectangle.RX)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RX);
                float fRadiusY = rectangle.RY.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(rectangle.RY)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RY);

                writer.WriteLine("\\draw[rounded corners = {0}pt, fill = {2}{1}] \\wholeShape;",
                        fRadiusX,
                        FillColor.AlphaValTex(curSolidColor),
                        solidRef);
            }
            // draw the outline
            rectangle.OutlineTex(bound_rectangle, "wholeShape", writer, rendinfo, refgroup, scale);
        }

        private static void OutlineTex(this SBMLExtension.EmlRenderExtension.Rectangle rectangle, RectangleF bound_rectangle, string shapename, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            string strokewidth = rectangle.GetStrokeWidth(rendinfo, refgroup, scale);
            string dashed = "";
            if (!string.IsNullOrEmpty(rectangle.GetDashType(refgroup)))
            {
                dashed = ", " + rectangle.DashTex(rectangle.GetDashType(refgroup));
            }
            Color linecolor = rectangle.GetStrokeColor(rendinfo, refgroup);

            // refer stroke information to a ColorDefinition ID or assign a new colordefinition to "curLineColor"
            string strokeRef = FillColor.getStrokeRef(rectangle, rendinfo, refgroup);
            if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curLineColor";
                FillColor.AssignColorRGBTex(linecolor, "curLineColor", writer);
            }

            if (string.IsNullOrEmpty(rectangle.RX) || (SBMLExtension.Util.readSingle(rectangle.RX) == 0f))
            {
                writer.WriteLine("\\draw[line width = {0}pt, color = {4}{1} {2}] \\{3};",
                    strokewidth,
                    FillColor.AlphaValTex(linecolor),
                    dashed,
                    shapename,
                    strokeRef);
            }

            else
            {
                float fRadiusX = rectangle.RX.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(rectangle.RX)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RX);
                float fRadiusY = rectangle.RY.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(rectangle.RY)) / 100f) : SBMLExtension.Util.readSingle(rectangle.RY);

                writer.WriteLine("\\draw[line width = {0}pt, color = {5}{1}, rounded corners = {2}pt {3}] \\{4};",
                    strokewidth,
                    FillColor.AlphaValTex(linecolor),
                    fRadiusX,
                    dashed,
                    shapename,
                    strokeRef);
            } 
        }

        private static void DefineTex(this Ellipses ellipse, PointF position, PointF radii, IndentedTextWriter writer)
        {
            writer.WriteLine("\\def \\wholeShape {{({0}pt,{1}pt) ellipse({2}pt and {3}pt)}}",
                position.X,
                position.Y,
                radii.X,
                radii.Y);
        }

        private static void RadialGradTex(this Ellipses ellipse, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            PointF position = EllipsePositionInBounds(ellipse, bound_rectangle);
            PointF radii = CorrectEllipseRadii(ellipse, bound_rectangle); ;

            //define the overall shape
            ellipse.DefineTex(position, radii, writer);

            if (fillcolor.colorList.Count == fillcolor.positionList.Count)
            {
                if (!FillColor.isGradientID(fillcolor.ID, rendinfo.GradientDefinitions))
                {
                    FillColor.AssignGradientTex(fillcolor, fillcolor.ID, writer);
                }

                writer.WriteLine("\\fill[shading = {0}, shading angle = {1}] \\wholeShape;",
                fillcolor.ID,
                -180 + fillcolor.gradient_rotation);
            }

            //draw the outline
            ellipse.OutlineTex(bound_rectangle, "wholeShape", writer, rendinfo, refgroup, scale);
        }

        private static void LinearGradTex(this Ellipses ellipse, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            PointF position = EllipsePositionInBounds(ellipse, bound_rectangle);
            PointF radii = CorrectEllipseRadii(ellipse, bound_rectangle); ;

            //define the overall shape
            ellipse.DefineTex(position, radii, writer);

            if (fillcolor.colorList.Count == fillcolor.positionList.Count)
            {
                if (!FillColor.isGradientID(fillcolor.ID, rendinfo.GradientDefinitions))
                {
                    FillColor.AssignGradientTex(fillcolor, fillcolor.ID, writer);
                }

                writer.WriteLine("\\fill[shading = {0}, shading angle = {1}] \\wholeShape;",
                fillcolor.ID,
                -180 + fillcolor.gradient_rotation);
            }

            //draw the outline
            ellipse.OutlineTex(bound_rectangle, "wholeShape", writer, rendinfo, refgroup, scale);
        }

        private static void SolidColorTex(this Ellipses ellipse, RectangleF bound_rectangle, FillColor fillcolor, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            PointF position = EllipsePositionInBounds(ellipse, bound_rectangle);
            PointF radii = CorrectEllipseRadii(ellipse, bound_rectangle); ;

            //define the overall shape
            ellipse.DefineTex(position, radii, writer);

            // obtain fill color information
            Color curSolidColor = fillcolor.colorList[0];

            // refer solidfill information to a ColorDefinition ID or assign a new colordefinition to "curSolidColor"
            string solidRef = FillColor.getColorRef(ellipse, rendinfo, refgroup);
            if (fillcolor.colorList[0].Equals(Color.Empty))
            {
                solidRef = "none";
            }
            else if (!FillColor.isColorID(solidRef, rendinfo.ColorDefinitions))
            {
                solidRef = "curSolidColor";
                FillColor.AssignColorRGBTex(curSolidColor, "curSolidColor", writer);
            }

            writer.WriteLine("\\fill[color = {1}{0}] \\wholeShape;",
                FillColor.AlphaValTex(curSolidColor),
                solidRef);

            //draw the outline
            ellipse.OutlineTex(bound_rectangle, "wholeShape", writer, rendinfo, refgroup, scale);
        }

        private static void OutlineTex(this Ellipses ellipse, RectangleF bound_rectangle, string shapename, IndentedTextWriter writer, RenderInformation rendinfo, Group refgroup, double scale)
        {
            string strokewidth = ellipse.GetStrokeWidth(rendinfo, refgroup, scale);
            string dashed = "";
            if (!string.IsNullOrEmpty(ellipse.GetDashType(refgroup)))
            {
                dashed = ", " + ellipse.DashTex(ellipse.GetDashType(refgroup));
            }
            Color linecolor = ellipse.GetStrokeColor(rendinfo, refgroup);

            // refer stroke information to a ColorDefinition ID or assign a new colordefinition to "curLineColor"
            string strokeRef = FillColor.getStrokeRef(ellipse, rendinfo, refgroup);
            if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curLineColor";
                FillColor.AssignColorRGBTex(linecolor, "curLineColor", writer);
            }

            writer.WriteLine("\\draw[line width = {0}pt, color = {4}{1} {2}] \\{3};",
                    strokewidth,
                    FillColor.AlphaValTex(linecolor),
                    dashed,
                    shapename,
                    strokeRef);
        }

        private static void CommentTex(SBMLExtension.LayoutExtension.GraphicalObject glyph, SBMLExtension.EmlRenderExtension.GraphicalObject graphItem, IndentedTextWriter writer)
        {
            string glyphtype;
            if (graphItem is Group)
            {
                glyphtype = "Group";
            }
            else if (graphItem is Text)
            {
                glyphtype = "Text";
            }
            else if (graphItem is Polygon)
            {
                glyphtype = "Polygon";
            }
            else if (graphItem is SBMLExtension.EmlRenderExtension.Rectangle)
            {
                glyphtype = "Rectangle";
            }
            else if (graphItem is Ellipses)
            {
                glyphtype = "Ellipses";
            }
            else if (graphItem is LineEnding)
            {
                glyphtype = "LineEnding";
            }
            else if (graphItem is SBMLExtension.EmlRenderExtension.Image)
            {
                glyphtype = "Image import";
            }
            else if (graphItem is SBMLExtension.EmlRenderExtension.Curve)
            {
                glyphtype = "Curve";
            }
            else
            {
                glyphtype = "Unknown";
            }

            string reference = glyph.Name;
            if (string.IsNullOrEmpty(reference))
            {
                reference = glyph.ID;
            }
            if (string.IsNullOrEmpty(reference))
            {
                reference = "unspecified";
            }

            string comment = "% " + glyphtype + " for " + reference;
            writer.WriteLine(comment);
        }

        private static string DashTex(this GraphicalPrimitive1D graphItem, string dashType)
        {
            string dashed = " ";
            string[] values = dashType.Split(',');
            if (values.Length > 1)
            {
                dashed = "dash pattern=";
                bool on = true;
                foreach (string num in values)
                {
                    if (on)
                    {
                        dashed += " on " + num;
                    }
                    else
                    {
                        dashed += " off " + num;
                    }
                    on = !on;
                }
            }
            return dashed;
        }

        private static PointF[] RectPositionInBounds(SBMLExtension.EmlRenderExtension.Rectangle rectangle, RectangleF bound_rectangle)
        {
            PointF topCorner = new PointF(rectangle.X.Contains("%") ? (((bound_rectangle.Width * SBMLExtension.Util.readSingle(rectangle.X)) / 100f)) : (SBMLExtension.Util.readSingle(rectangle.X)),
                 rectangle.Y.Contains("%") ? (((bound_rectangle.Height * SBMLExtension.Util.readSingle(rectangle.Y)) / 100f)) : (SBMLExtension.Util.readSingle(rectangle.Y)));
            if (string.IsNullOrEmpty(rectangle.RY))
                rectangle.RY = rectangle.RX;
            if (string.IsNullOrEmpty(rectangle.RX))
                rectangle.RX = rectangle.RY;
            SizeF size = new SizeF(rectangle.Width.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(rectangle.Width)) / 100f) : SBMLExtension.Util.readSingle(rectangle.Width),
                                   rectangle.Height.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(rectangle.Height)) / 100f) : SBMLExtension.Util.readSingle(rectangle.Height));

            PointF botCorner = new PointF(topCorner.X + size.Width, topCorner.Y + size.Height);
            PointF[] coords = new PointF[2] { topCorner, botCorner };
            return coords;
        }

        private static PointF EllipsePositionInBounds(Ellipses ellipse, RectangleF bound_rectangle)
        {
            PointF position = new PointF(ellipse.CX.Contains("%") ? (((bound_rectangle.Width * SBMLExtension.Util.readSingle(ellipse.CX)) / 100f)) : (SBMLExtension.Util.readSingle(ellipse.CX)),
                ellipse.CY.Contains("%") ? (((bound_rectangle.Height * SBMLExtension.Util.readSingle(ellipse.CY)) / 100f)) : (SBMLExtension.Util.readSingle(ellipse.CY)));
            return position;
        }

        private static PointF CorrectEllipseRadii(Ellipses ellipse, RectangleF bound_rectangle)
        {
            if (string.IsNullOrEmpty(ellipse.RX))
                ellipse.RX = ellipse.RY;
            if (string.IsNullOrEmpty(ellipse.RY))
                ellipse.RY = ellipse.RX;
            return new PointF(ellipse.RX.Contains("%") ? ((bound_rectangle.Width * SBMLExtension.Util.readSingle(ellipse.RX)) / 100f) : SBMLExtension.Util.readSingle(ellipse.RX),
                ellipse.RY.Contains("%") ? ((bound_rectangle.Height * SBMLExtension.Util.readSingle(ellipse.RY)) / 100f) : SBMLExtension.Util.readSingle(ellipse.RY));
        }

        // Adapted from Group.RenderText
        public static void TextTex(this Group group, TextGlyph glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Hashtable fontTexTable)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, group, writer);
            string text = glyph.GetTextToDraw();
            RectangleF bound_rectangle = glyph.Bounds.toRect();

            Font font = group.GetScaledFont(scale, refgroup);
            SizeF string_size = g.MeasureString(text, font);
            PointF text_coord = group.GetPoint(group, bound_rectangle);

            Color text_color = group.GetStrokeColor(rendinfo, refgroup);
            if (!text_color.Equals(Color.Empty))
            {
                // refer stroke information to a ColorDefinition ID or assign a new colordefinition to "curLineColor"
                string strokeRef = FillColor.getStrokeRef(group, rendinfo, refgroup);
                if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
                {
                    strokeRef = "curLineColor";
                    FillColor.AssignColorRGBTex(text_color, "curLineColor", writer);
                }

                //adapted from Group.CorrectPosition 
                switch (group.GetTextAnchor(group))
                {
                    // TikZ text is automatically centered at the drawing point
                    case Text.TextAnchors.start:
                        text_coord.X += string_size.Width / 2f;
                        break;
                    case Text.TextAnchors.end:
                        text_coord.X -= string_size.Width / 2f;
                        break;
                }

                writer.WriteLine("\\draw ({0}pt, {1}pt) node[text = {7}{2}, font = \\fontsize{{ {3} }} {{ {4} }}\\fontfamily{6}\\selectfont] {{ {5} }};",
                    (text_coord.X).ToString(),
                    (text_coord.Y).ToString(),
                    FillColor.AlphaValTex(text_color),
                    font.SizeInPoints,
                    font.SizeInPoints,
                    correctText(text),
                    FontTex(font, fontTexTable),
                    strokeRef);
            }
            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        public static void TextTex(this Text text, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Hashtable fontTexTable)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, text, writer);
            RectangleF bound_rectangle = glyph.Bounds.toRect();
            
            Text.TextAnchors horizontalAnchor = text.GetHorizontalAnchor(refgroup);
            Text.VerticalTextAnchors verticalAnchor = text.GetVerticalAnchor(refgroup);
            Font font = text.GetScaledFont(scale, refgroup);
           
            SizeF string_size = g.MeasureString(text.TheText, font);
            PointF text_coord = text.GetPoint(bound_rectangle);

            Color text_color = text.GetStrokeColor(rendinfo, refgroup);
            if (!text_color.Equals(Color.Empty))
            {
                // refer stroke information to a ColorDefinition ID or assign a new colordefinition to "curLineColor"
                string strokeRef = FillColor.getStrokeRef(text, rendinfo, refgroup);
                if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
                {
                    strokeRef = "curLineColor";
                    FillColor.AssignColorRGBTex(text_color, "curLineColor", writer);
                }

                switch (horizontalAnchor)
                {
                    case Text.TextAnchors.start:
                        text_coord.X += string_size.Width / 2f;
                        break;

                    case Text.TextAnchors.middle:
                        text_coord.X += bound_rectangle.Width / 2f;
                        break;

                    case Text.TextAnchors.end:
                        text_coord.X += bound_rectangle.Width - string_size.Width / 2f;
                        break;
                }
                switch (verticalAnchor)
                {
                    case Text.VerticalTextAnchors.top:
                        text_coord.Y += string_size.Height / 2f;
                        break;

                    case Text.VerticalTextAnchors.middle:
                        text_coord.Y += (bound_rectangle.Height / 2f);
                        break;

                    case Text.VerticalTextAnchors.bottom:
                        text_coord.Y += bound_rectangle.Height - string_size.Height / 2f;
                        break;
                }

                writer.WriteLine("\\draw ({0}pt, {1}pt) node[text = {7}{2}, font = \\fontsize{{ {3} }} {{ {4} }}\\fontfamily{6}\\selectfont] {{ {5} }};",
                   (text_coord.X).ToString(),
                   (text_coord.Y).ToString(),
                   FillColor.AlphaValTex(text_color),
                   font.SizeInPoints,
                   font.SizeInPoints,
                   correctText(text.TheText),
                   FontTex(font, fontTexTable),
                   strokeRef);
            }
            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        // Adapted from Group.RenderReaction
        public static void ReactionTex(this Group group, ReactionGlyph glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, RectangleF refbounds, double scale, Hashtable fontTexTable)
        {
            writer.WriteLine("{");
            writer.Indent += 1;
            CommentTex(glyph, group, writer);
            RectangleF bound_rectangle = glyph.Bounds.toRect();

            foreach (SpeciesReference reference in glyph.SpeciesReferences)
            {
                Style styleForReference = rendinfo.GetStyleForObject(reference);
                if (styleForReference != null)
                {
                    group.ToTex(reference, writer, g, rendinfo, styleForReference.Group, refbounds, scale, fontTexTable);
                }
                else
                {
                    group.ToTex(reference, writer, g, rendinfo, refgroup, refbounds, scale, fontTexTable);
                }
            }
            if (glyph.Curve.CurveSegments.Count != 0)
            {
                group.ReactionLineStartTex(glyph.Curve.CurveSegments[0], glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
                //group.ReactionLineEndingTex(glyph.Curve.CurveSegments[0], glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
            }

            foreach (CurveSegment segment in glyph.Curve.CurveSegments)
            {
                group.SingleSegmentTex(segment, glyph, glyph.Curve.CurveSegments.Count == 1, writer, g, rendinfo, group, scale, fontTexTable);
            }
            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        // works like group.DrawSegment for TeX 
        public static void SingleSegmentTex(this Group group, CurveSegment segment, SBMLExtension.LayoutExtension.GraphicalObject glyph, Boolean endOfLine, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Hashtable fontTexTable)
        {
            string strokewidth = group.GetStrokeWidth(rendinfo, refgroup, scale);
            Color linecolor = group.GetStrokeColor(rendinfo, refgroup);

            string strokeRef = FillColor.getStrokeRef(group, rendinfo, refgroup);
            if (String.IsNullOrEmpty(strokeRef) || !FillColor.isColorID(strokeRef, rendinfo.ColorDefinitions))
            {
                strokeRef = "curLineColor";
                FillColor.AssignColorRGBTex(linecolor, "curLineColor", writer);
            }
            
            string dashed = ", " + group.DashTex(group.GetDashType(refgroup));
            string segmentHead = "[line width = " + strokewidth + "pt, color = " + strokeRef + FillColor.AlphaValTex(linecolor) + dashed + "]";

                if (segment.Type == "CubicBezier")
                {
                    writer.WriteLine("\\draw{8} ({0}pt, {1}pt) .. controls ({2}pt, {3}pt) and ({4}pt, {5}pt) .. ({6}pt, {7}pt);",
                    segment.Start.X, segment.Start.Y,
                    segment.BasePoint1.X, segment.BasePoint1.Y,
                    segment.BasePoint2.X, segment.BasePoint2.Y,
                    segment.End.X, segment.End.Y,
                    segmentHead);
                }
                else if (segment.Type == "LineSegment")
                {
                    writer.WriteLine("\\draw{4} ({0}pt, {1}pt) -- ({2}pt, {3}pt);",
                    segment.Start.X, segment.Start.Y,
                    segment.End.X, segment.End.Y,
                    segmentHead);
                }

                if (endOfLine)
                {
                    group.ReactionLineEndingTex(segment, glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
                }
        }

        // works like Group.DrawLineEnding
        private static void ReactionLineEndingTex(this Group group, CurveSegment segment, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Hashtable fontTexTable)
        {
            if (!string.IsNullOrEmpty(refgroup.EndHead))
            {
                PointF p1 = segment.End.ToPointF();
                PointF p2 = segment.Start.ToPointF();
                if (segment.Type != "LineSegment")
                {
                    p2 = segment.BasePoint2.ToPointF();
                }
                LineEnding ending = rendinfo.GetLineEnding(refgroup.EndHead);
                writer.WriteLine("{");
                writer.Indent += 1;

                writer.WriteLine("\\pgftransformshift{{\\pgfpoint{{ {0}pt }}{{ {1}pt }} }}",
                       p1.X.ToString(),
                       (p1.Y).ToString());

                RotationalMappingLineEndTex(ending, p1, p2, writer, refgroup);

                ending.LineEndingTex(glyph, writer, g, rendinfo, refgroup, scale, false, fontTexTable); // pass false as we have already done rotation
                writer.Indent -= 1;
                writer.WriteLine("}");
            }

            if (!string.IsNullOrEmpty(refgroup.StartHead))
            {
                PointF p1 = segment.End.ToPointF();
                PointF p2 = segment.Start.ToPointF();
                if (segment.Type != "LineSegment")
                {
                    p2 = segment.BasePoint2.ToPointF();
                }
                LineEnding ending = rendinfo.GetLineEnding(refgroup.StartHead);
                writer.WriteLine("{");
                writer.Indent += 1;

                writer.WriteLine("\\pgftransformshift{{\\pgfpoint{{ {0}pt }}{{ {1}pt }} }}",
                       p1.X.ToString(),
                       (p1.Y).ToString());

                RotationalMappingLineEndTex(ending, p1, p2, writer, refgroup);

                ending.LineEndingTex(glyph, writer, g, rendinfo, refgroup, scale, false, fontTexTable); // pass false as we have already done rotation
                writer.Indent -= 1;
                writer.WriteLine("}");
            }
        }

        public static void ReactionLineStartTex(this Group group, CurveSegment segment, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Hashtable fontTexTable)
        {
            if (!string.IsNullOrEmpty(refgroup.StartHead))
            {
                PointF p1 = segment.Start.ToPointF();
                PointF p2 = segment.End.ToPointF();
                if (segment.Type != "LineSegment")
                {
                    p2 = segment.BasePoint2.ToPointF();
                }
                LineEnding ending = rendinfo.GetLineEnding(refgroup.StartHead);
                writer.WriteLine("{");
                writer.Indent += 1;

                writer.WriteLine("\\pgftransformshift{{\\pgfpoint{{ {0}pt }}{{ {1}pt }} }}",
                       p1.X.ToString(),
                       (p1.Y).ToString());

                RotationalMappingLineEndTex(ending, p1, p2, writer, refgroup);

                ending.LineEndingTex(glyph, writer, g, rendinfo, refgroup, scale, false, fontTexTable); // pass false as we have already done rotation
                writer.Indent -= 1;
                writer.WriteLine("}");
            }
        }

        // based on Group.RotationalMapping()
        private static void RotationalMappingLineEndTex(LineEnding ending, PointF p1, PointF p2, IndentedTextWriter writer, Group refgroup)
        {
            if (ending.EnableRotationalMapping)
            {
                PointF tf = p2;
                PointF vector = new PointF(p1.X - tf.X, p1.Y - tf.Y);
                vector = SBMLExtension.Util.NormalizePoint(vector);
                if ((vector.X != 0f) || (vector.Y != 0f))
                {
                    PointF tf3;
                    if (vector.X == 0f)
                    {
                        tf3 = new PointF(vector.Y, 0f);
                    }
                    else
                    {
                        tf3 = new PointF(-vector.Y * vector.X, 1f - (vector.Y * vector.Y));
                    }
                    tf3 = SBMLExtension.Util.NormalizePoint(tf3);

                    writer.WriteLine("\\pgftransformcm {{ {0} }}{{ {1} }}{{ {2} }}{{ {3} }}{{\\pgfpoint{{ {4}pt }}{{ {5}pt }} }}",
                        (vector.X).ToString(),
                        (vector.Y).ToString(),
                        (tf3.X).ToString(),                       
                        (tf3.Y).ToString(),
                        0f.ToString(),
                        0f.ToString());
                }
            }
        }

        // adapted from RenderSpeciesReference
        private static void SpeciesReferenceTex(this Group group, SBMLExtension.LayoutExtension.GraphicalObject glyph, IndentedTextWriter writer, Graphics g, RenderInformation rendinfo, Group refgroup, double scale, Hashtable fontTexTable)
        {
            SpeciesReference reference = (SpeciesReference) glyph;
            RectangleF bound_rectangle = glyph.Bounds.toRect();

            SolidBrush brush = (SolidBrush)group.GetBrush(rendinfo, bound_rectangle, refgroup);
            Color fillcolor = brush.Color;

            if (reference.Curve.CurveSegments.Count != 0)
            {
                group.ReactionLineStartTex(reference.Curve.CurveSegments[0], glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
                //group.ReactionLineEndingTex(reference.Curve.CurveSegments[0], glyph, writer, g, rendinfo, refgroup, scale, fontTexTable);
            }

            for (int i = 0; i < reference.Curve.CurveSegments.Count; i++)
            {
                bool endOfLine = i == (reference.Curve.CurveSegments.Count - 1);
                group.SingleSegmentTex(reference.Curve.CurveSegments[i], glyph, endOfLine, writer, g, rendinfo, refgroup, scale, fontTexTable);
            }
        }

        // This is basically group.GetFont, but with modified font size 
        private static Font GetScaledFont(this Group group, double scale, Group refgroup)
        {
            string fontFamily = group.GetFontFamily(refgroup, FontFamily.GenericSansSerif.Name);
            string str3 = fontFamily.ToLower();
            if (str3 != null)
            {
                if ((!(str3 == "sans") && !(str3 == "sansserif")) && !(str3 == "sans-serif"))
                {
                    if (str3 == "serif")
                    {
                        fontFamily = FontFamily.GenericSerif.Name;
                    }
                    else if (str3 == "monospace")
                    {
                        fontFamily = FontFamily.GenericMonospace.Name;
                    }
                }
                else
                {
                    fontFamily = FontFamily.GenericSansSerif.Name;
                }
            }
            double fSize = double.Parse(group.GetFontSize(refgroup, "10.0")) * scale;
            fSize = fSize < 5 ? 5 : fSize;
            string fontSize = fSize.ToString();
            return new Font(fontFamily, SBMLExtension.Util.readSingle(fontSize), group.GetFontStyle(refgroup), GraphicsUnit.Point);
        }

        // selects the appropriate font command in LaTeX for a given font
        private static string FontTex(Font font, Hashtable fontTexTable)
        {
            string fontcmd = "\\sfdefault"; // sans serif is the default font

            if (fontTexTable.ContainsKey(font.Name.ToLower()))
            {
                fontcmd = "{"+(string)fontTexTable[font.Name.ToLower()]+"}";
            }

            if (font.Bold)
            {
                fontcmd = fontcmd + "\\bfseries";
            }
            if (font.Italic)
            {
                fontcmd = fontcmd + "\\itshape";
            }

            return fontcmd;
        }

        private static Font GetScaledFont(this Text text, double scale, Group refgroup)
        {
            string fontFamily = text.GetFontFamily(refgroup, FontFamily.GenericSansSerif.Name);
            string str3 = fontFamily.ToLower();
            if (str3 != null)
            {
                if ((!(str3 == "sans") && !(str3 == "sansserif")) && !(str3 == "sans-serif"))
                {
                    if (str3 == "serif")
                    {
                        fontFamily = FontFamily.GenericSerif.Name;
                    }
                    else if (str3 == "monospace")
                    {
                        fontFamily = FontFamily.GenericMonospace.Name;
                    }
                }
                else
                {
                    fontFamily = FontFamily.GenericSansSerif.Name;
                }
            }
            double fSize = double.Parse(text.GetFontSize(refgroup, "10.0")) * scale;
            fSize = fSize < 5 ? 5 : fSize;
            string fontSize = fSize.ToString();
            return new Font(fontFamily, SBMLExtension.Util.readSingle(fontSize), text.GetFontStyle(refgroup), GraphicsUnit.Point);
        }

        private static string GetStrokeWidth(this GraphicalPrimitive1D graphItem, RenderInformation rendinfo, Group refgroup, double scale)
        {
            string strokeW = graphItem.StrokeWidth;
            if (string.IsNullOrEmpty(strokeW))
                strokeW = refgroup.StrokeWidth;

            if (string.IsNullOrEmpty(strokeW))
                strokeW = "1.0";

            double strokeWidth = double.Parse(strokeW);
            strokeWidth = strokeWidth * scale;
            return strokeWidth.ToString();
        }

        private static Color GetStrokeColor(this GraphicalPrimitive1D graphItem, RenderInformation rendinfo, Group refgroup)
        {
            Color strokeColor = Color.Empty;
            if (string.IsNullOrEmpty(graphItem.Stroke) || graphItem.Stroke == "none")
                strokeColor = rendinfo.GetColor(refgroup.Stroke);
            else
                strokeColor = rendinfo.GetColor(graphItem.Stroke);

            if (strokeColor.IsEmpty)
                strokeColor = rendinfo.GetColor(refgroup.Fill);

            return strokeColor;
        }

        // adapted from GraphicalPrimitve2D.GetBrush()
        private static FillColor GetFillColor(this GraphicalPrimitive2D graphItem, RenderInformation rendinfo, RectangleF bound_rectangle, Group refgroup)
        {
            try
            {
                if (string.IsNullOrEmpty(graphItem.Fill) || (graphItem.Fill == "none"))
                    return rendinfo.GetFillColor(refgroup.Fill, bound_rectangle);

                return rendinfo.GetFillColor(graphItem.Fill, bound_rectangle);
            }
            catch (Exception)
            {
                GraphicalPrimitive1D graphBase = (GraphicalPrimitive1D) graphItem;
                return rendinfo.GetFillColor(graphBase.Stroke, bound_rectangle);
            }
        }

        // adapted from RenderInformation.GetBrush()
        private static FillColor GetFillColor(this RenderInformation rendinfo, String sId, RectangleF bound_rectangle)
        {
            if (sId == "none")
            {
                return null;
            }
            FillColor gradFillColor = rendinfo.GetGradFillColor(sId, bound_rectangle);
            if (gradFillColor != null)
            {
                return gradFillColor;
            }
            // rendinfo.GetColor
            return new FillColor(rendinfo.GetColor(sId));
        }

        // adapted from RenderInformation.GetGradientBrush
        private static FillColor GetGradFillColor(this RenderInformation rendinfo, string sId, RectangleF bound_rectangle)
        {
            foreach (GradientDefinition def in rendinfo.GradientDefinitions)
            {
                if (def.ID == sId)
                {
                    if (def is LinearGradient)
                    {
                        LinearGradient lindef = (LinearGradient)def;
                        return lindef.GetLinearGradFillColor(rendinfo);
                    }
                    // else it is radialGradient
                    RadialGradient raddef = (RadialGradient)def;
                    return raddef.GetRadialFillColor(rendinfo);
                }
            }

            if (!string.IsNullOrEmpty(rendinfo.ReferenceRenderInformation))
            {
                RenderInformation refrendinfo = Layout.CurrentLayout.GetReferenceRenderInformation(rendinfo.ReferenceRenderInformation);
                if (refrendinfo != null)
                {
                    FillColor thisFill = refrendinfo.GetGradFillColor(sId, bound_rectangle);
                    if (thisFill != null)
                    {
                        return thisFill;
                    }
                }
            }
            if (rendinfo.IsLocal)
            {
                foreach (RenderInformation rendinfo2 in Layout.GlobalRenderInformation)
                {
                    return rendinfo2.GetGradFillColor(sId, bound_rectangle);
                }
            }
            return null;
        }

        // adapted from LinearGradient.GetBrush
        public static FillColor GetLinearGradFillColor(this LinearGradient linearGrad, RenderInformation rendinfo)
        {
            List<Color> colors = new List<Color>();
            List<float> positions = new List<float>();
            foreach (GradientStop stop in linearGrad.Stop)
            {
                colors.Add(rendinfo.GetColor(stop.StopColor));
                positions.Add(SBMLExtension.Util.readSingle(stop.Offset));
            }
            PointF startP = new PointF(SBMLExtension.Util.readSingle(linearGrad.X1), SBMLExtension.Util.readSingle(linearGrad.Y1));
            PointF endP = new PointF(SBMLExtension.Util.readSingle(linearGrad.X2), SBMLExtension.Util.readSingle(linearGrad.Y2));

            if (startP.X != endP.X || startP.Y != endP.Y)
            {
                double gradient_rotation = FillColor.GetGradientRotation(startP, endP);
                return new FillColor(colors, positions, gradient_rotation, linearGrad.SpreadMethod, linearGrad.ID);
            }

            return null; //the two points are on the same spot; this is not a gradient
        }

        // adapted from RadialGradient.GetBrush
        public static FillColor GetRadialFillColor(this RadialGradient radialGrad, RenderInformation rendinfo)
        {
            GradientDefinition gradBase = (GradientDefinition)radialGrad;
            ColorBlend interpolationColors = gradBase.GetInterpolationColors(rendinfo);
            List<Color> colors = new List<Color>();
            List<float> positions = new List<float>();
            for (int ii = 0; ii < interpolationColors.Colors.Length; ii++)
            {
                colors.Add(interpolationColors.Colors[ii]);
                positions.Add(interpolationColors.Positions[ii]*100f); // convert to percent
            }

            float focalX;
            float focalY;
            if (!string.IsNullOrEmpty(radialGrad.FX))
            {
                //focalX = radialGrad.FX.Contains("%") ? SBMLExtension.Util.readSingle(radialGrad.FX) : SBMLExtension.Util.readSingle(radialGrad.FX)/bound_rectangle.Width * 100f;
                focalX = SBMLExtension.Util.readSingle(radialGrad.FX); 
            }
            else
            {
                //focalX = radialGrad.CX.Contains("%") ? SBMLExtension.Util.readSingle(radialGrad.CX) : SBMLExtension.Util.readSingle(radialGrad.CX)/ bound_rectangle.Width * 100f;
                focalX = SBMLExtension.Util.readSingle(radialGrad.CX);
            }
            if (!string.IsNullOrEmpty(radialGrad.FY))
            {
                //focalY = radialGrad.FY.Contains("%") ? SBMLExtension.Util.readSingle(radialGrad.FY) : SBMLExtension.Util.readSingle(radialGrad.FY)/ bound_rectangle.Height * 100f;
                focalY = SBMLExtension.Util.readSingle(radialGrad.FY);
            }
            else
            {
                //focalY = radialGrad.CY.Contains("%") ? SBMLExtension.Util.readSingle(radialGrad.CY) : SBMLExtension.Util.readSingle(radialGrad.CY)/ bound_rectangle.Height * 100f;
                focalY = SBMLExtension.Util.readSingle(radialGrad.CY);
            }

            PointF focal = new PointF(focalX, focalY);
            return new FillColor(colors, positions, focal, gradBase.SpreadMethod, radialGrad.ID); 
        }

        // corrects a string to TeX compatible output
        private static string correctText(string text)
        {
            text = text.Replace("_", "\\_");
            text = text.Replace("%", "\\%");
            return text;
        }
    }
}
