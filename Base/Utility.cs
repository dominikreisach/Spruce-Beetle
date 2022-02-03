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


using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;


namespace SpruceBeetle
{
    public class Utility
    {
        //------------------------------------------------------------
        // CurveCurvature method
        //------------------------------------------------------------
        public static Interval CurveCurvature(Curve crv)
        {
            List<Vector3d> vecList = new List<Vector3d>();
            List<double> lengthList = new List<double>();

            double accuracy = 400;

            for (int i = 0; i <= accuracy; i++)
            {
                var curvature = crv.CurvatureAt(i * (1 / accuracy));
                vecList.Add(curvature);
                lengthList.Add(curvature.Length);
            }

            // define interval of the curvature
            var curvatureMax = lengthList.Max();
            var curvatureMin = lengthList.Min();

            // check if curvature is too high
            if (CurvatureRadius(crv, curvatureMax) < 1)
                throw new Exception("Curvature is too high!");
            else if (CurvatureRadius(crv, curvatureMin) < 1)
                throw new Exception("Curvature is too high!");

            // create interval
            Interval curveBounds = new Interval(curvatureMin, curvatureMax);

            return curveBounds;
        }


        //------------------------------------------------------------
        // CurvatureRadius method
        //------------------------------------------------------------
        private static double CurvatureRadius(Curve crv, double t)
        {
            Vector3d curvature = crv.CurvatureAt(t);
            double curvatureRadius = 1.0 / curvature.Length;

            return curvatureRadius;
        }


        //------------------------------------------------------------
        // Remap method
        //------------------------------------------------------------
        public static double Remap(double targetValue, Interval sourceBounds, Interval targetBounds)
        {
            // define target value
            double normValue = (targetValue - sourceBounds.T0) / sourceBounds.Length;
            double newValue = targetBounds.T0 + normValue * targetBounds.Length;

            return newValue;
        }


        //------------------------------------------------------------
        // GetOptimizedOffcutIndex method
        //------------------------------------------------------------
        public static void GetOptimizedOffcutIndex(Curve crv, List<Offcut> offcutDim, List<Plane> initialPlane, Interval angleBounds, out Plane plane, out int index, out double curvature)
        {
            // get first plane
            Plane firstPlane;

            bool isEmpty = !initialPlane.Any();
            if (isEmpty)
            {
                crv.PerpendicularFrameAt(0.0, out firstPlane);

                // determine the rotation of the Offcuts
                double angle = Utility.Remap(0.0, new Interval(0.0, 1.0), angleBounds);
                firstPlane.Transform(Transform.Rotation(ConvertToRadians(angle), firstPlane.ZAxis, firstPlane.Origin));
            }
            else
                firstPlane = initialPlane.LastOrDefault();

            // get t value for first plane
            bool ClosestPt = crv.ClosestPoint(firstPlane.Origin, out double tClosest);

            // check if end of curve
            if (tClosest >= 1)
                throw new Exception("End of curve!");

            // call CurveCurvature method
            Interval curveBounds = CurveCurvature(crv);

            // append all Z values to a list
            List<double> dzList = new List<double>();
            for (int i = 0; i < offcutDim.Count; i++)
                dzList.Add(offcutDim[i].Z);

            // define interval of the Offcuts' Z values
            Interval dBounds = new Interval(dzList.Max(), dzList.Min());

            // curvature vector at t
            Vector3d targetVector = crv.CurvatureAt(tClosest);

            // call Remap method to remap the Z values
            double newValue = Remap(targetVector.Length, curveBounds, dBounds);

            // match new value and closest Z value
            double closestNum = dzList.Aggregate((x, y) => Math.Abs(x - newValue) < Math.Abs(y - newValue) ? x : y);
            int offcutIndex = dzList.IndexOf(closestNum);

            // remap maximum curvature value to a set range -----> perhaps needs rethinking? matching to eurocode for double-curved glulam?
            Interval maxRange = new Interval(10, 0);
            Interval targetRange = new Interval(0.63, 0.830);

            double maxCurvature = Remap(curveBounds.T1, maxRange, targetRange);

            // remap the curvature to the adjusted values
            Interval planeAdjustment = new Interval(maxCurvature + 0.114, maxCurvature);
            double curvatureValue = Remap(targetVector.Length, curveBounds, planeAdjustment);

            // assign return values
            plane = firstPlane;
            index = offcutIndex;
            curvature = curvatureValue;
        }


        //------------------------------------------------------------
        // GetOffcutIndex method
        //------------------------------------------------------------
        public static void GetOffcutIndex(Curve crv, List<Plane> initialPlane, Interval angleBounds, out Plane plane, out double curvature)
        {
            // reparameterize curve
            Interval reparam = new Interval(0.0, 1.0);
            crv.Domain = reparam;

            // get first plane
            Plane firstPlane;

            bool isEmpty = !initialPlane.Any();
            if (isEmpty)
            {
                crv.PerpendicularFrameAt(0.0, out firstPlane);

                // determine the rotation of the Offcuts
                double angle = Utility.Remap(0.0, new Interval(0.0, 1.0), angleBounds);
                firstPlane.Transform(Transform.Rotation(ConvertToRadians(angle), firstPlane.ZAxis, firstPlane.Origin));
            }
            else
                firstPlane = initialPlane.LastOrDefault();

            // get t value for first plane
            crv.ClosestPoint(firstPlane.Origin, out double tClosest);

            // check if end of curve
            if (tClosest >= 1)
                throw new Exception("End of curve!");

            // call CurveCurvature method
            Interval curveBounds = CurveCurvature(crv);

            // curvature vector at t
            Vector3d targetVector = crv.CurvatureAt(tClosest);

            // remap maximum curvature value to a set range -----> perhaps needs rethinking? matching to eurocode for double-curved glulam?
            Interval maxRange = new Interval(10, 0);
            Interval targetRange = new Interval(0.600, 0.800);

            double maxCurvature = Remap(curveBounds.T1, maxRange, targetRange);

            // remap the curvature to the adjusted values
            Interval planeAdjustment = new Interval(maxCurvature + 0.114, maxCurvature);
            double curvatureValue = Remap(targetVector.Length, curveBounds, planeAdjustment);

            // assign return values
            plane = firstPlane;
            curvature = curvatureValue;
        }


        //------------------------------------------------------------
        // AlignOffcuts method
        //------------------------------------------------------------
        public static void AlignOffcuts(Curve crv, Offcut offcutDim, Plane firstPlane, double adjustmentVal, double angle, int ocBaseIndex, out Brep offcut, out List<Plane> planes, out double vol)
        {
            // create sphere to intersect with curve; the sphere has to be a bit smaller than the Z value
            Brep sphere = new Sphere(firstPlane.Origin, offcutDim.Z * adjustmentVal).ToBrep();
            Intersection.CurveBrep(crv, sphere, 0.01, out _, out Point3d[] intersectionPts);
            crv.ClosestPoint(intersectionPts.Last(), out double tIntersect);

            // create second plane on curve
            crv.PerpendicularFrameAt(tIntersect, out Plane secondPlane);

            // determine the rotation of the Offcuts
            //double angle = Utility.Remap(crv.CurvatureAt(tIntersect).Length, new Interval(0.0, 1.0), angleBounds);
            secondPlane.Transform(Transform.Rotation(ConvertToRadians(angle), secondPlane.ZAxis, secondPlane.Origin));

            // create average plane out of the first and second one
            Point3d originPt = new Point3d((firstPlane.Origin + secondPlane.Origin) / 2);
            Plane averagePlane = new Plane(originPt, (firstPlane.XAxis + secondPlane.XAxis) / 2, (firstPlane.YAxis + secondPlane.YAxis) / 2);

            // copy average plane and move to the center of the to be constructed Offcut - it is needed for further operations
            Plane movedAvrgPlane = new Plane(averagePlane);
            Rectangle3d avrgPosition = Offcut.GetOffcutBase(offcutDim.X, offcutDim.Y, averagePlane, ocBaseIndex);

            movedAvrgPlane.Origin = avrgPosition.Center;

            // add planes to a list
            List<Plane> planeList = new List<Plane>
            {
                firstPlane,
                secondPlane,
                averagePlane,
                movedAvrgPlane
            };

            // call CreateOffcutBrep method
            Brep unionOffcuts = Offcut.CreateOffcutBrep(offcutDim, averagePlane, ocBaseIndex);

            // trim Offcut for optimal alignment
            firstPlane.Flip();

            Brep firstTrim = unionOffcuts.Trim(firstPlane, 0.0001)[0];
            Brep firstCap = firstTrim.CapPlanarHoles(0.0001);
            Brep secondTrim = firstCap.Trim(secondPlane, 0.0001)[0];

            firstPlane.Flip();

            // cap holes and split kinky faces
            Brep outputOffcut = secondTrim.CapPlanarHoles(0.0001);
            outputOffcut.Faces.SplitKinkyFaces(0.0001);

            // assign return values
            offcut = outputOffcut;
            planes = planeList;
            vol = outputOffcut.GetVolume();
        }


        //------------------------------------------------------------
        // DirectlyAlignOffcuts method
        //------------------------------------------------------------
        public static void DirectlyAlignOffcuts(Curve crv, Offcut offcutDim, Plane initialPlane, double adjustmentVal, double angle, int ocBaseIndex, int i, out Brep offcut, out List<Plane> planes, out double vol)
        {
            // initialise new Plane
            Plane firstPlane;
            
            if (i == 0)
            {
                firstPlane = initialPlane;
            }
            else
            {
                // create sphere to intersect with curve; the first intersection point will be the base for the next Offcut
                Brep firstSphere = new Sphere(initialPlane.Origin, 0.03).ToBrep();
                Intersection.CurveBrep(crv, firstSphere, 0.0001, out _, out Point3d[] firstIntPts);
                crv.ClosestPoint(firstIntPts[0], out double tFIntersect);

                // create first plane on curve
                crv.PerpendicularFrameAt(tFIntersect, out firstPlane);
            }

            // create sphere to intersect with curve; the sphere has to be a bit smaller than the Z value
            Brep secondSphere = new Sphere(firstPlane.Origin, offcutDim.Z * adjustmentVal).ToBrep();
            Intersection.CurveBrep(crv, secondSphere, 0.0001, out _, out Point3d[] secondIntPts);
            crv.ClosestPoint(secondIntPts.Last(), out double tSIntersect);

            // create second plane on curve
            crv.PerpendicularFrameAt(tSIntersect, out Plane secondPlane);

            // determine the orientation of the Offcuts
            firstPlane.Transform(Transform.Rotation(ConvertToRadians(angle), firstPlane.ZAxis, firstPlane.Origin));
            secondPlane.Transform(Transform.Rotation(ConvertToRadians(angle), secondPlane.ZAxis, secondPlane.Origin));

            // create average plane out of the first and second one
            Point3d originPt = new Point3d((firstPlane.Origin + secondPlane.Origin) / 2);
            Plane averagePlane = new Plane(originPt, (firstPlane.XAxis + secondPlane.XAxis) / 2, (firstPlane.YAxis + secondPlane.YAxis) / 2);

            // call CreateOffcutBrep method
            Brep unionOffcuts = Offcut.CreateOffcutBrep(offcutDim, averagePlane, ocBaseIndex);

            // trim Offcut for optimal alignment
            firstPlane.Flip();

            Brep firstTrim = unionOffcuts.Trim(firstPlane, 0.0001)[0];
            Brep firstCap = firstTrim.CapPlanarHoles(0.0001);
            Brep secondTrim = firstCap.Trim(secondPlane, 0.0001)[0];

            firstPlane.Flip();

            // cap holes and split kinky faces
            Brep outputOffcut = secondTrim.CapPlanarHoles(0.0001);
            outputOffcut.Faces.SplitKinkyFaces(0.0001);

            // first average plane
            Point3d firstOrigin = new Point3d((firstPlane.Origin + initialPlane.Origin) / 2);
            Plane firstAvrgPlane = new Plane(firstOrigin, (firstPlane.XAxis + initialPlane.XAxis) / 2, (firstPlane.YAxis + initialPlane.YAxis) / 2);

            // second average plane
            Brep secSphere = new Sphere(secondPlane.Origin, 0.03).ToBrep();
            Intersection.CurveBrep(crv, secSphere, 0.0001, out _, out Point3d[] secIntPts);
            crv.ClosestPoint(secIntPts[0], out double tSecIntersect);

            // create first plane on curve
            crv.PerpendicularFrameAt(tSecIntersect, out Plane movedSecondPlane);

            // rotate movedSecondPlane
            movedSecondPlane.Transform(Transform.Rotation(ConvertToRadians(angle), movedSecondPlane.ZAxis, movedSecondPlane.Origin));

            Point3d secondOrigin = new Point3d((movedSecondPlane.Origin + secondPlane.Origin) / 2);
            Plane secondAvrgPlane = new Plane(secondOrigin, (movedSecondPlane.XAxis + secondPlane.XAxis) / 2, (movedSecondPlane.YAxis + secondPlane.YAxis) / 2);

            // copy average plane and move to the center of the to be constructed Offcut - it is needed for further operations
            Plane movedAvrgPlane = new Plane(averagePlane);
            Rectangle3d avrgPosition = Offcut.GetOffcutBase(offcutDim.X, offcutDim.Y, averagePlane, ocBaseIndex);

            movedAvrgPlane.Origin = avrgPosition.Center;

            // add planes to a list
            List<Plane> planeList = new List<Plane>
            {
                initialPlane,
                firstPlane,
                secondPlane,
                movedSecondPlane,
                averagePlane,
                firstAvrgPlane,
                secondAvrgPlane,
                movedAvrgPlane
            };

            // assign return values
            offcut = outputOffcut;
            planes = planeList;
            vol = outputOffcut.GetVolume();
        }


        //------------------------------------------------------------
        // MinimumDimensions method
        //------------------------------------------------------------
        public static double[] MinimumDimensions(List<Offcut> offcutData)
        {
            // empty lists to store Offcut x and y dimensions
            List<double> x = new List<double>();
            List<double> y = new List<double>();

            // store Offcut dimensions in list
            foreach (Offcut offcut in offcutData)
            {
                x.Add(offcut.X);
                y.Add(offcut.Y);
            }

            // get min and max values
            double minX = x.Min();
            double minY = y.Min();

            // create and return an array
            double[] minimumDimensions = { minX, minY };

            return minimumDimensions;
        }


        //------------------------------------------------------------
        // Degrees to radians method
        //------------------------------------------------------------
        public static double ConvertToRadians(double angle)
        {
            // return degrees to radians value
            return (Math.PI / 180) * angle;
        }


        //------------------------------------------------------------
        // GetMinimumDimensions method
        //------------------------------------------------------------
        public static List<double[]> GetMinimumDimension(List<Offcut> offcutList, int i)
        {
            // get the base for the joint position of each Offcut
            double[] firstMin = new double[2];
            double[] secondMin = new double[2];

            if (i == 0)
            {
                firstMin = Joint.GetMinValue(offcutList[i].X, offcutList[i].Y, offcutList[offcutList.Count - 1].X, offcutList[offcutList.Count - 1].Y);
                secondMin = Joint.GetMinValue(offcutList[i].X, offcutList[i].Y, offcutList[i + 1].X, offcutList[i + 1].Y);
            }

            else if (0 < i && i < offcutList.Count - 1)
            {
                firstMin = Joint.GetMinValue(offcutList[i].X, offcutList[i].Y, offcutList[i - 1].X, offcutList[i - 1].Y);
                secondMin = Joint.GetMinValue(offcutList[i].X, offcutList[i].Y, offcutList[i + 1].X, offcutList[i + 1].Y);
            }
            else
            {
                firstMin = Joint.GetMinValue(offcutList[i].X, offcutList[i].Y, offcutList[i - 1].X, offcutList[i - 1].Y);
                secondMin = Joint.GetMinValue(offcutList[i].X, offcutList[i].Y, offcutList[0].X, offcutList[0].Y);
            }

            List<double[]> returnList = new List<double[]> { firstMin, secondMin };
            return returnList;
        }


        //------------------------------------------------------------
        // GetOffcutBaseCurve method
        //------------------------------------------------------------
        public static Curve GetOffcutBaseCurve(List<Offcut> offcutList)
        {
            // create new list of points
            List<Point3d> originList = new List<Point3d>();

            // call MinimumDimensions method
            double[] dimensions = MinimumDimensions(offcutList);

            for (int i = 0; i < offcutList.Count; i++)
            {
                // initialise new planes
                Plane firstPlane = new Plane(offcutList[i].FirstPlane);

                // setting new origin point for the planes
                Rectangle3d firstRect = Offcut.GetOffcutBase(dimensions[0], dimensions[1], firstPlane, offcutList[i].PositionIndex);

                // add center points to point list
                originList.Add(firstRect.Center);

                // include the last plane
                if (i == offcutList.Count - 1)
                    originList.Add(Offcut.GetOffcutBase(dimensions[0], dimensions[1], offcutList[i].SecondPlane, offcutList[i].PositionIndex).Center);
            }

            // create curve by interpolation
            Curve baseCurve = Curve.CreateInterpolatedCurve(originList, 3);

            // return curve
            return baseCurve;
        }
    }
}
