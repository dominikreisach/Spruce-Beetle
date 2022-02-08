/*
 * MIT License
 * 
 * Copyright (c) 2022 Dominik Reisach
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */


using System.Collections.Generic;
using Rhino.Geometry;


namespace SpruceBeetle
{
    public class Joint
    {
        // list of inserted joint types as reference for the value list
        public static List<string> TenonTypes()
        {         
            return new List<string>
            {
                "tenon",
                "cross tenon",
                "custom tenon",
            };
        }

        // Dictionary of inserted joint types
        public static Dictionary<string, int> GetJointType()
        {
            return new Dictionary<string, int>
            {
                { "tenon", 0 },
                { "cross tenon", 1 },
                { "custom tenon", 2 },
            };
        }


        // list of 'direct' joint types as reference for the value list
        public static List<string> DirectJointType()
        {
            return new List<string>
            {
                "finger joint",
                "lapped finger joint",
            };
        }

        // Dictionary of 'direct' joint types
        public static Dictionary<string, int> GetDirectJointType()
        {
            return new Dictionary<string, int>
            {
                {"finger joint", 0 },
                {"lapped finger joint", 1 },
            };
        }


        //------------------------------------------------------------
        // Divide curve and return two points
        //------------------------------------------------------------
        public static double[] GetMinValue(double minThisX, double minThisY, double minOtherX, double minOtherY)
        {
            double[] minValues = new double[2];

            // get the minimum x value
            if (minThisX <= minOtherX)
            {
                minValues[0] = minThisX;
            }
            else
            {
                minValues[0] = minOtherX;
            }

            // get the minimum y value
            if (minThisY <= minOtherY)
            {
                minValues[1] = minThisY;
            }
            else
            {
                minValues[1] = minOtherY;
            }

            // return double array
            return minValues;
        }


        //------------------------------------------------------------
        // Divide curve and return two points
        //------------------------------------------------------------
        public static Point3d[] GetPoints(Plane plane, double length, int count)
        {
            // create lines
            Line firstLine = new Line(plane.Origin, plane.XAxis, length / 2);
            Line secondLine = new Line(plane.Origin, plane.XAxis, -length / 2);

            // add lines to list and convert to crv
            List<Curve> lineList = new List<Curve>
            {
            firstLine.ToNurbsCurve(),
            secondLine.ToNurbsCurve()
            };

            // join curves
            Curve centerLine = Curve.JoinCurves(lineList, 0.001)[0];

            // create point container and divide curve by count
            centerLine.DivideByCount(count + 1, false, out Point3d[] points);

            // return points
            return points;
        }


        //------------------------------------------------------------
        // DivideCurve method
        //------------------------------------------------------------
        public static Point3d[] DivideCurve(Plane plane, double length, int count)
        {
            Curve centerLine = CreateCenterline(plane, length);

            // create point container and divide curve by count
            centerLine.DivideByCount(count, false, out Point3d[] points);

            // return points
            return points;
        }


        //------------------------------------------------------------
        // CreateCenterline method
        //------------------------------------------------------------
        public static Curve CreateCenterline(Plane plane, double length)
        {
            // create lines
            Line firstLine = new Line(plane.Origin, plane.XAxis, length / 2);
            Line secondLine = new Line(plane.Origin, plane.XAxis, -length / 2);

            // add lines to list and convert to crv
            List<Curve> lineList = new List<Curve>
            {
            firstLine.ToNurbsCurve(),
            secondLine.ToNurbsCurve()
            };

            // join curves
            Curve centerLine = Curve.JoinCurves(lineList, 0.001)[0];

            return centerLine;
        }


        //------------------------------------------------------------
        // CutOffcut method
        //------------------------------------------------------------
        public static Brep CutOffcut(Brep[] cuttingGeometry, Brep offcut)
        {
            Brep[] offcutBrep = new Brep[] { offcut };

            // boolean difference
            Brep[] finOffcut = Brep.CreateBooleanDifference(offcutBrep, cuttingGeometry, 0.0001);

            // brep clean-up
            finOffcut[0].Faces.SplitKinkyFaces(0.0001);
            finOffcut[0].MergeCoplanarFaces(0.0001);
            finOffcut[0].Faces.SplitKinkyFaces(0.0001);

            // check solid orientation and return
            if (BrepSolidOrientation.Inward == finOffcut[0].SolidOrientation)
            {
                finOffcut[0].Flip();
                return finOffcut[0];
            }
            else
                return finOffcut[0];
        }
    }
}
