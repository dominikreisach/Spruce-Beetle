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
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;


namespace SpruceBeetle.Alignment
{
    public class DirectAlignment_GH : GH_Component
    {
        public DirectAlignment_GH()
          : base("Direct Alignment", "DirAlign", "Directly align a list of Offcuts along a given curve", "Spruce Beetle", "    Alignment")
        {
        }


        // value list
        GH_ValueList valueList = null;
        IGH_Param parameter = null;

        // second value list
        GH_ValueList secValueList = null;
        IGH_Param secParameter = null;


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to align Offcuts to", GH_ParamAccess.item);
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddTextParameter("Offcut Position", "OcP", "Position of the Offcuts", GH_ParamAccess.item, "mid-mid");
            pManager.AddTextParameter("Joint Type", "JT", "Adds the specified joint type", GH_ParamAccess.item);
            pManager.AddNumberParameter("Start Angle", "SA", "Rotate the start of the alignement by the provided angle", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("End Angle", "EA", "Rotate the end of the alignment by the provided angle", GH_ParamAccess.item, 0);

            parameter = pManager[2];
            secParameter = pManager[3];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "Aligned Offcuts on the curve", GH_ParamAccess.list);
            pManager.AddGenericParameter("Unused Offcuts", "UOc", "Unused Offcuts to be used on another alignment", GH_ParamAccess.list);
            pManager.AddCurveParameter("Centroid Curve", "CC", "The curve at the center of the aligned Offucts", GH_ParamAccess.item);

            pManager.HideParameter(2);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // create value list
        protected override void BeforeSolveInstance()
        {
            // first value list
            if (valueList == null)
            {
                if (parameter.Sources.Count == 0)
                    valueList = new GH_ValueList();
                else
                {
                    foreach (var source in parameter.Sources)
                    {
                        if (source is GH_ValueList)
                            valueList = source as GH_ValueList;

                        return;
                    }
                }

                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 200, this.Attributes.Pivot.Y - 5);
                valueList.ListItems.Clear();

                List<string> baseOrientation = Offcut.BasePosition();

                foreach (string param in baseOrientation)
                    valueList.ListItems.Add(new GH_ValueListItem(param, $"\"{param}\""));

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
            }


            // second value list
            if (secValueList == null)
            {
                if (secParameter.Sources.Count == 0)
                    secValueList = new GH_ValueList();
                else
                {
                    foreach (var source in secParameter.Sources)
                    {
                        if (source is GH_ValueList)
                            secValueList = source as GH_ValueList;

                        return;
                    }
                }

                secValueList.CreateAttributes();
                secValueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 250, this.Attributes.Pivot.Y + 50);
                secValueList.ListItems.Clear();

                List<string> directJointType = Joint.DirectJointType();

                foreach (string param in directJointType)
                    secValueList.ListItems.Add(new GH_ValueListItem(param, $"\"{param}\""));

                Instances.ActiveCanvas.Document.AddObject(secValueList, false);
                secParameter.AddSource(secValueList);
                secParameter.CollectData();
            }
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            Curve curve = null;
            List<Offcut> offcutData = new List<Offcut>();
            string ocBaseType = "";
            string jointKey = "";
            double rotateStart = 0;
            double rotateEnd = 0;

            // access input parameters
            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetDataList(1, offcutData)) return;
            if (!DA.GetData(2, ref ocBaseType)) return;
            if (!DA.GetData(3, ref jointKey)) return;
            if (!DA.GetData(4, ref rotateStart)) return;
            if (!DA.GetData(5, ref rotateEnd)) return;

            // reparameterize curve
            curve.Domain = new Interval(0.0, 1.0);

            // create an interval between start and end angle to specifiy the rotation angle
            Interval angleBounds = new Interval(rotateStart, rotateEnd);

            // get joint type
            Dictionary<string, int> jointDict = Joint.GetDirectJointType();
            int jointType = jointDict[jointKey];

            // get Offcut orientation base type
            Dictionary<string, int> ocBaseDict = Offcut.GetBasePosition();
            int ocBaseIndex = ocBaseDict[ocBaseType];

            // initialise lists to store all the data
            List<Offcut> offcutList = new List<Offcut>();
            List<List<Plane>> allPlanes = new List<List<Plane>>();
            List<Plane> secondPlane = new List<Plane>();

            double tIntersect = 0.0;
            int dataCount = offcutData.Count;

            // set distance
            double distance = curve.PointAtStart.DistanceTo(curve.PointAtEnd);

            // add last item
            bool addLast = true;

            for (int i = 0; i < dataCount; i++)
            {
                // get maximum z value
                Utility.GetMaxZ(offcutData, out Offcut maxOffcut, out int maxIndex);

                if (distance > maxOffcut.Z)
                {
                    // get angle for rotation
                    double angle = Utility.Remap(tIntersect, new Interval(0.0, 1.0), angleBounds);

                    // call methods to align the Offcuts along the curve in an optimized manner
                    Utility.GetOptimizedOffcutIndex(curve, offcutData, secondPlane, angleBounds, out Plane startPlane, out int offcutIndex, out double adjustmentValue);
                    Utility.DirectlyAlignOffcuts(curve, offcutData[offcutIndex], startPlane, adjustmentValue, angle, ocBaseIndex, i, false, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                    // adding data to the output list
                    Offcut localOffcut = new Offcut(offcutData[offcutIndex])
                    {
                        OffcutGeometry = alignedOffcut,
                        FabVol = vol,
                        FirstPlane = planeList[1],
                        SecondPlane = planeList[2],
                        AveragePlane = planeList[4],
                        MovedAveragePlane = planeList[7],
                        PositionIndex = ocBaseIndex
                    };

                    // add planes to plane list
                    allPlanes.Add(planeList);

                    // store data in Offcut_GH list
                    offcutList.Add(localOffcut);

                    // remove used Offcut from list
                    offcutData.RemoveAt(offcutIndex);

                    // add the last plane to the list as new starting plane
                    secondPlane.Add(planeList[2]);

                    // check the t value for the new starting plane
                    curve.ClosestPoint(planeList[2].Origin, out tIntersect);

                    // check the distance for the new starting plane
                    distance = planeList[1].Origin.DistanceTo(curve.PointAtEnd);
                }

                else if (addLast)
                {
                    // get angle for rotation
                    double angle = Utility.Remap(tIntersect, new Interval(0.0, 1.0), angleBounds);

                    // call methods to align the Offcuts along the curve in an optimized manner
                    Utility.GetOptimizedOffcutIndex(curve, offcutData, secondPlane, angleBounds, out Plane startPlane, out int offcutIndex, out double adjustmentValue);
                    Utility.DirectlyAlignOffcuts(curve, offcutData[maxIndex], startPlane, adjustmentValue, angle, ocBaseIndex, i, true, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                    // adding data to the output list
                    Offcut localOffcut = new Offcut(offcutData[maxIndex])
                    {
                        OffcutGeometry = alignedOffcut,
                        FabVol = vol,
                        FirstPlane = planeList[1],
                        SecondPlane = planeList[2],
                        AveragePlane = planeList[4],
                        MovedAveragePlane = planeList[7],
                        PositionIndex = ocBaseIndex
                    };

                    // add planes to plane list
                    allPlanes.Add(planeList);

                    // store data in Offcut_GH list
                    offcutList.Add(localOffcut);

                    // remove used Offcut from list
                    offcutData.RemoveAt(maxIndex);
                }

                else
                    break;
            }

            // initialise array to store Offcut data after joint creation
            Brep[] outputOffcuts = new Brep[offcutList.Count];
            double[] outputOffcutVol = new double[offcutList.Count];

            // parallel computed joints with boolean difference
            System.Threading.Tasks.Parallel.For(0, offcutList.Count, (i, state) =>
            {
                // get the base for the joint position of each Offcut
                List<double[]> minimumDimensions = Utility.GetMinimumDimension(offcutList, i);
                double[] firstMin = minimumDimensions[0];
                double[] secondMin = minimumDimensions[1];

                // create joints with their respective volumes according to the joint type
                if (i == 0)
                {
                    // call CreateCutters function to generate cutters
                    Brep cutBreps = CreateCutters(allPlanes[i][2], allPlanes[i][3], allPlanes[i][6], secondMin, offcutList[i].PositionIndex, jointType, 1);

                    Brep[] cutterBreps = new Brep[] { cutBreps };

                    // call CutOffcut function
                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, offcutList[i].OffcutGeometry);

                    // output data to array
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }
                else if (i == offcutList.Count - 1)
                {
                    // call CreateCutters method for both sides of the Offcut
                    Brep cutBreps = CreateCutters(allPlanes[i][0], allPlanes[i][1], allPlanes[i][5], firstMin, offcutList[i].PositionIndex, jointType, -1);

                    Brep[] cutterBreps = new Brep[] { cutBreps };

                    // call CutOffcut function
                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, offcutList[i].OffcutGeometry);

                    // output data to array
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }
                else
                {
                    // call CreateCutters method for both sides of the Offcut
                    Brep firstCutters = CreateCutters(allPlanes[i][0], allPlanes[i][1], allPlanes[i][5], firstMin, offcutList[i].PositionIndex, jointType, -1);
                    Brep secondCutters = CreateCutters(allPlanes[i][2], allPlanes[i][3], allPlanes[i][6], secondMin, offcutList[i].PositionIndex, jointType, 1);

                    Brep[] cutterBreps = new Brep[] { firstCutters, secondCutters };

                    // call CutOffcut function
                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, offcutList[i].OffcutGeometry);

                    // output data to array
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }

                // stop parallel loop
                if (i >= offcutList.Count)
                {
                    state.Stop();
                    return;
                }

                if (state.IsStopped)
                    return;
            });

            // output data to new Offcut_GH list
            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();

            for (int i = 0; i < offcutList.Count; i++)
            {
                offcutList[i].OffcutGeometry = outputOffcuts[i];
                offcutList[i].FabVol = outputOffcutVol[i];

                Offcut_GH offcutGH = new Offcut_GH(offcutList[i]);
                offcutGHList.Add(offcutGH);
            }

            // add unused Offcuts to list
            List<Offcut_GH> unusedOffcutsGH = new List<Offcut_GH>();
            foreach (Offcut offcut in offcutData)
            {
                Offcut_GH offcutGH = new Offcut_GH(offcut);
                unusedOffcutsGH.Add(offcutGH);
            }

            // access output parameters
            DA.SetDataList(0, offcutGHList);
            DA.SetDataList(1, unusedOffcutsGH);
            DA.SetData(2, Utility.GetOffcutBaseCurve(offcutList));
        }


        //------------------------------------------------------------
        // CreateCutters method
        //------------------------------------------------------------
        protected Brep CreateCutters(Plane fPlane, Plane sPlane, Plane avrgPlane, double[] minValue, int positionIndex, int jointType, int planeDir)
        {
            // initialise empty brep variable
            Brep cutterBreps;

            // create new planes
            Plane firstPlane = new Plane(fPlane);
            Plane secondPlane = new Plane(sPlane);
            Plane averagePlane = new Plane(avrgPlane);

            // setting new origin point for the planes
            Rectangle3d firstRect = Offcut.GetOffcutBase(minValue[0], minValue[1], firstPlane, positionIndex);
            Rectangle3d secondRect = Offcut.GetOffcutBase(minValue[0], minValue[1], secondPlane, positionIndex);

            // change origin of planes
            firstPlane.Origin = firstRect.Center;
            secondPlane.Origin = secondRect.Center;
            averagePlane.Origin = (firstRect.Center + secondRect.Center) / 2;

            // create joints according to joint type
            switch (jointType)
            {
                case 0:
                    {
                        // create and weave points as base for the curves
                        List<Point3d> pointList = WeavePoints(firstPlane, secondPlane, minValue[0]);

                        // create base curves for the negative joints
                        Curve cutterBase = CreateJointBase(pointList, averagePlane, planeDir * 0.1, planeDir * 0.0001);

                        // move base curves to intersect the Offcut brep completely
                        cutterBase.Transform(Transform.Translation(planeDir * averagePlane.YAxis * minValue[1] * 0.5 * 1.2));

                        // extrude base
                        Brep cutter = Extrusion.Create(cutterBase, minValue[1] * 4, true).ToBrep();

                        // clean-up
                        cutter.Faces.SplitKinkyFaces(0.0001);
                        if (BrepSolidOrientation.Inward == cutter.SolidOrientation)
                            cutter.Flip();

                        // add cutter to output
                        cutterBreps = cutter;
                    }
                    break;

                case 1:
                    {
                        // create and weave points as base for the curves
                        List<Point3d> pointList = WeavePoints(firstPlane, secondPlane, minValue[0]);

                        // create base curves for the negative joints
                        Curve firstBase = CreateJointBase(pointList, averagePlane, planeDir * minValue[0], planeDir * 0.0001);

                        // move base curves to intersect the Offcut brep completely
                        firstBase.Transform(Transform.Translation(planeDir * averagePlane.YAxis * minValue[1] / 6));

                        // extrude base
                        Brep firstCutter = Extrusion.Create(firstBase, minValue[1] / 3, true).ToBrep();

                        // clean-up
                        firstCutter.Faces.SplitKinkyFaces(0.0001);
                        if (BrepSolidOrientation.Inward == firstCutter.SolidOrientation)
                            firstCutter.Flip();


                        // second cutter base
                        Curve secondBase = null;
                        Curve unfortunateBase = null;

                        if (planeDir == -1)
                        {
                            secondBase = GetCutterBase(Joint.DivideCurve(firstPlane, minValue[0], 17), averagePlane, planeDir * 0.1);
                            unfortunateBase = GetCutterBase(Joint.DivideCurve(secondPlane, minValue[0], 17), averagePlane, planeDir * 0.1);
                        }
                        else
                        {
                            secondBase = GetCutterBase(Joint.DivideCurve(secondPlane, minValue[0], 17), averagePlane, planeDir * 0.1);
                            unfortunateBase = GetCutterBase(Joint.DivideCurve(firstPlane, minValue[0], 17), averagePlane, planeDir * 0.1);
                        }


                        // move base curves to intersect the Offcut brep completely
                        secondBase.Transform(Transform.Translation(planeDir * averagePlane.YAxis * minValue[1] / 6));
                        unfortunateBase.Transform(Transform.Translation(-planeDir * averagePlane.YAxis * minValue[1] / 6));

                        // extrude base
                        Brep secondCutter = Extrusion.Create(secondBase, -minValue[1], true).ToBrep();

                        // clean-up
                        secondCutter.Faces.SplitKinkyFaces(0.0001);
                        if (BrepSolidOrientation.Inward == secondCutter.SolidOrientation)
                            secondCutter.Flip();

                        // extrude base
                        Brep unfortunateCutter = Extrusion.Create(unfortunateBase, minValue[1], true).ToBrep();

                        // clean-up
                        unfortunateCutter.Faces.SplitKinkyFaces(0.0001);
                        if (BrepSolidOrientation.Inward == unfortunateCutter.SolidOrientation)
                            unfortunateCutter.Flip();

                        List<Brep> cutterList = new List<Brep>
                        {
                            firstCutter,
                            secondCutter,
                            unfortunateCutter
                        };

                        Brep unionBreps = Brep.CreateBooleanUnion(cutterList, 0.0001)[0];

                        // clean-up
                        unionBreps.Faces.SplitKinkyFaces(0.0001);
                        unionBreps.MergeCoplanarFaces(0.00001);
                        unionBreps.Faces.SplitKinkyFaces(0.0001);
                        if (BrepSolidOrientation.Inward == unionBreps.SolidOrientation)
                            unionBreps.Flip();

                        cutterBreps = unionBreps;
                    }
                    break;

                default:
                    {
                        // create and weave points as base for the curves
                        List<Point3d> pointList = WeavePoints(firstPlane, secondPlane, minValue[0]);

                        // create base curves for the negative joints
                        Curve cutterBase = CreateJointBase(pointList, averagePlane, planeDir * 0.1, planeDir * 0.0001);

                        // move base curves to intersect the Offcut brep completely
                        cutterBase.Transform(Transform.Translation(planeDir * averagePlane.YAxis * minValue[1] * 0.5 * 1.01));

                        // extrude base
                        Brep cutter = Extrusion.Create(cutterBase, minValue[1] * 3, true).ToBrep();

                        // clean-up
                        cutter.Faces.SplitKinkyFaces(0.0001);
                        if (BrepSolidOrientation.Inward == cutter.SolidOrientation)
                            cutter.Flip();

                        // add cutter to output
                        cutterBreps = cutter;
                    }
                    break;
            }

            // return cutter breps
            return cutterBreps;
        }


        //------------------------------------------------------------
        // CreateJointBase method
        //------------------------------------------------------------
        protected Curve CreateJointBase(List<Point3d> pointList, Plane initialPlane, double size, double offset)
        {
            Plane plane = new Plane(initialPlane);

            PolylineCurve initialCurve = new PolylineCurve(pointList);

            // rotate plane for project and offset operations
            double angle = Utility.ConvertToRadians(90);
            plane.Rotate(angle, plane.XAxis, plane.Origin);

            // scale the rectangle base a bit down to avoid coincidial faces
            Transform scale = Transform.Scale(plane, 1.0, 0.99, 1.0);   /// scaled by 0.99 in y direction so that the faces are not coincident
            initialCurve.Transform(scale);

            // set courve domain
            Interval reparam = new Interval(0.0, 1.0);
            initialCurve.Domain = reparam;

            // extend courve 
            Curve firstCrv = initialCurve.Extend(CurveEnd.Start, 0.0125, CurveExtensionStyle.Line);
            Curve baseCurve = firstCrv.Extend(CurveEnd.End, 0.0125, CurveExtensionStyle.Line);

            // project curve to plane
            Curve projectedCurve = Curve.ProjectToPlane(baseCurve, plane);

            // move new instances of start and end points
            Point3d firstPt = projectedCurve.PointAtStart;
            Point3d secondPt = projectedCurve.PointAtEnd;

            firstPt.Transform(Transform.Translation(plane.YAxis * size));
            secondPt.Transform(Transform.Translation(plane.YAxis * size));

            // create array of points
            Point3d[] pointCollection = new Point3d[] { baseCurve.PointAtStart, firstPt, secondPt, baseCurve.PointAtEnd };

            // create polyline curve out of points
            PolylineCurve closingCurve = new PolylineCurve(pointCollection);

            // join all curves and fillet them
            Curve[] curveArray = new Curve[] { projectedCurve, closingCurve };
            Curve joinedCurves = Curve.JoinCurves(curveArray, 0.0001)[0];
            Curve filletCurve = Curve.CreateFilletCornersCurve(joinedCurves, 0.0025, 0.0001, 0.0001);

            // project curve to plane
            Curve finalProjection = Curve.ProjectToPlane(filletCurve, plane);

            //// offset curve for tolerance reasons
            //Curve finalBase = finalProjection.Offset(plane, offset, 0.0000001, CurveOffsetCornerStyle.None)[0];

            // return curve
            return finalProjection;
        }


        //------------------------------------------------------------
        // GetCutterBase method
        //------------------------------------------------------------
        protected static Curve GetCutterBase(Point3d[] pointList, Plane averagePlane, double size)
        {
            Plane plane = new Plane(averagePlane);

            PolylineCurve initialCurve = new PolylineCurve(pointList);

            // rotate plane for project and offset operations
            double angle = Utility.ConvertToRadians(90);
            plane.Rotate(angle, plane.XAxis, plane.Origin);

            // scale the rectangle base a bit down to avoid coincidial faces
            Transform scale = Transform.Scale(plane, 1.0, 0.99, 1.0);

            initialCurve.Transform(scale);

            // set courve domain
            Interval reparam = new Interval(0.0, 1.0);
            initialCurve.Domain = reparam;

            // extend courve 
            Curve firstCrv = initialCurve.Extend(CurveEnd.Start, 0.0125, CurveExtensionStyle.Line);
            Curve baseCurve = firstCrv.Extend(CurveEnd.End, 0.0125, CurveExtensionStyle.Line);

            // project curve to plane
            Curve projectedCurve = Curve.ProjectToPlane(baseCurve, plane);

            // move new instances of start and end points
            Point3d firstPt = projectedCurve.PointAtStart;
            Point3d secondPt = projectedCurve.PointAtEnd;

            firstPt.Transform(Transform.Translation(plane.YAxis * size));
            secondPt.Transform(Transform.Translation(plane.YAxis * size));

            // create array of points
            Point3d[] pointCollection = new Point3d[] { baseCurve.PointAtStart, firstPt, secondPt, baseCurve.PointAtEnd };

            // create polyline curve out of points
            PolylineCurve closingCurve = new PolylineCurve(pointCollection);

            // join all curves and fillet them
            Curve[] curveArray = new Curve[] { projectedCurve, closingCurve };

            // join curves
            Curve joinedCurves = Curve.JoinCurves(curveArray, 0.0001)[0];

            Curve filletCurve = Curve.CreateFilletCornersCurve(joinedCurves, 0.0025, 0.0001, 0.0001);

            // return final projected curve
            return Curve.ProjectToPlane(filletCurve, plane);
        }


        //------------------------------------------------------------
        // WeavePoints method
        //------------------------------------------------------------
        protected List<Point3d> WeavePoints(Plane firstPlane, Plane secondPlane, double length)
        {
            // initialise output list
            List<Point3d> returnPoints = new List<Point3d>();

            // initialise pattern array
            int[] pattern = new int[] { 0, 0, 1, 1, 1, 1, 0, 0 };

            // base points for the finger joints in a nested list
            List<Point3d[]> pointCollection = new List<Point3d[]>
            {
                Joint.DivideCurve(firstPlane, length, 17),
                Joint.DivideCurve(secondPlane, length, 17)
            };

            // declare an array to hold the item indices to pick from each list
            int[] indexRef = new int[2];

            indexRef[0] = 0;
            indexRef[1] = 0;

            // total number of points
            int pointCount = pointCollection[0].Length + pointCollection[1].Length;

            // while pointCount > 0, add points to the list according to the pattern
            while (pointCount > 0)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    // get the index of the list referenced at i
                    int listIndex = pattern[i];

                    // get the point list at the index
                    Point3d[] currentList = pointCollection[listIndex];

                    // get the item index from the reference array
                    int itemIndex = indexRef[listIndex];

                    // check if the list is not null and if the item index is smaller than the length of the list
                    if (currentList != null && itemIndex < currentList.Length)
                    {
                        // get the item at the itemIndex from the list
                        Point3d currentItem = currentList[itemIndex];

                        // add the item to the output list
                        returnPoints.Add(currentItem);

                        // increase item index and update index reference
                        itemIndex++;
                        indexRef[listIndex] = itemIndex;

                        // decrease pointCount
                        pointCount--;
                    }
                    else
                        pointCount--;
                }
            }

            // return point list
            return returnPoints;
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_DirectAlignment;

        // component giud
        public override Guid ComponentGuid => new Guid("4B4A23CF-8342-4C9B-B5ED-F85DB3A0411B");
    }
}