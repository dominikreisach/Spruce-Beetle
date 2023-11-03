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
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;


namespace SpruceBeetle.Alignment
{
    public class IntersectionJoints_GH : GH_Component
    {
        public IntersectionJoints_GH()
          : base("Intersection Joints", "IntJoints", "Create joints at the intersections of the alignments of Offcuts", "Spruce Beetle", "    Alignment")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("First Alignment", "FA", "First list of aligned Offcuts", GH_ParamAccess.list);
            pManager.AddGenericParameter("Second Alignment", "SA", "Second list of aligned Offcuts", GH_ParamAccess.list);
            pManager.AddPointParameter("Intersection Point", "IP", "Intersection point between the two alignments", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotate Joint", "RJ", "Rotate the joint to alter its direction", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Width", "W", "The width of the lap joint", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("Joint Type", "JT", "Adds the specified joint type: 0 = spline joint, 1 = cross-lap joint", GH_ParamAccess.item, 1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "List of aligned and intersected Offcuts", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Intersection Offcuts", "IOc", "List of intersecting Offcuts", GH_ParamAccess.tree);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> firstOffcuts = new List<Offcut>();
            List<Offcut> secondOffcuts = new List<Offcut>();
            Point3d intPt = new Point3d();
            double angle = 0.0;
            double width = 2.0;
            int jointType = 0;

            // access input parameters
            if (!DA.GetDataList(0, firstOffcuts)) return;
            if (!DA.GetDataList(1, secondOffcuts)) return;
            if (!DA.GetData(2, ref intPt)) return;
            if (!DA.GetData(3, ref angle)) return;
            if (!DA.GetData(4, ref width)) return;
            if (!DA.GetData(5, ref jointType)) return;

            // initialise data trees and paths
            DataTree<Offcut_GH> offcutGHList = new DataTree<Offcut_GH>();
            DataTree<Offcut_GH> intOffcutGHList = new DataTree<Offcut_GH>();
            List<Brep> joints = new List<Brep>();
            GH_Path firstPath = new GH_Path(0);
            GH_Path secondPath = new GH_Path(1);

            switch (jointType)
            {
                // spline joint
                case 0:
                    {                   
                        // call GetCuttingPlanes method
                        GetCuttingPlanes(firstOffcuts, intPt, angle, out _, out Offcut firstOffcut, out _);
                        GetCuttingPlanes(secondOffcuts, intPt, angle, out List<int> secondIndices, out _, out _);

                        // create Cutter geometry through scaling the closest brep
                        Brep offcutCutter = firstOffcut.OffcutGeometry.DuplicateBrep();
                        offcutCutter.Transform(Transform.Scale(firstOffcut.AveragePlane, 2, width, 4));

                        // call CreateIntersectionJoint method
                        CreateIntersectionJoint(offcutCutter, secondOffcuts, secondIndices, out List<Offcut> secondOffcutList, out List<Offcut> secondIntOffcuts);

                        // call GetSplineBase method
                        GetSplineBase(Utility.GetOffcutBaseCurve(secondOffcuts), secondIntOffcuts, intPt, out List<Plane> basePlanes, out List<Offcut> intOffcuts);

                        //// call CreateIntersectionSpline method
                        //for (int i = 0; i < basePlanes.Count; i++)
                        //{
                        //    CreateIntersectionSpline(basePlanes[i], intOffcuts[i], out Brep cutter, out Brep display);
                        //    joints.Add(cutter);
                        //    joints.Add(display);
                        //}

                        // add all Offcuts to the respective data tree
                        for (int i = 0; i < firstOffcuts.Count; i++)
                            offcutGHList.Add(new Offcut_GH(firstOffcuts[i]), firstPath);

                        for (int i = 0; i < secondOffcutList.Count; i++)
                            offcutGHList.Add(new Offcut_GH(secondOffcutList[i]), secondPath);

                        for (int i = 0; i < secondIntOffcuts.Count; i++)
                        {
                            intOffcutGHList.Add(new Offcut_GH(secondIntOffcuts[i]), secondPath);
                        }
                    }
                    break;

                // cross-lap joint
                case 1:
                    {
                        // call GetCuttingPlanes method
                        GetCuttingPlanes(firstOffcuts, intPt, angle, out List<int> firstIndices, out Offcut firstOffcut, out Plane firstPlane);
                        GetCuttingPlanes(secondOffcuts, intPt, angle, out List<int> secondIndices, out Offcut secondOffcut, out Plane secondPlane);

                        // rotate first plane
                        firstPlane.Transform(Transform.Rotation(Utility.ConvertToRadians(180), firstPlane.ZAxis, firstPlane.Origin));

                        // create average plane out of the first and second one
                        Point3d originPt = new Point3d((firstPlane.Origin + secondPlane.Origin) / 2);
                        Plane averagePlane = new Plane(originPt, (firstPlane.XAxis + secondPlane.XAxis) / 2, (firstPlane.YAxis + secondPlane.YAxis) / 2);
                        averagePlane.Origin = intPt;

                        // call GetCuttingGeometry method
                        GetCuttingGeometry(averagePlane, firstPlane, firstOffcut, 1, width, out Brep firstBrep);
                        GetCuttingGeometry(averagePlane, secondPlane, secondOffcut, -1, width, out Brep secondBrep);

                        // call CreateIntersectionJoint method
                        CreateIntersectionJoint(firstBrep, firstOffcuts, firstIndices, out List<Offcut> firstOffcutList, out List<Offcut> firstIntOffcuts);
                        CreateIntersectionJoint(secondBrep, secondOffcuts, secondIndices, out List<Offcut> secondOffcutList, out List<Offcut> secondIntOffcuts);

                        // add all Offcuts to the respective data tree
                        for (int i = 0; i < firstOffcutList.Count; i++)
                            offcutGHList.Add(new Offcut_GH(firstOffcutList[i]), firstPath);

                        for (int i = 0; i < secondOffcutList.Count; i++)
                            offcutGHList.Add(new Offcut_GH(secondOffcutList[i]), secondPath);

                        for (int i = 0; i < firstIntOffcuts.Count; i++)
                        {
                            intOffcutGHList.Add(new Offcut_GH(firstIntOffcuts[i]), firstPath);
                            intOffcutGHList.Add(new Offcut_GH(secondIntOffcuts[i]), secondPath);
                        }
                    }
                    break;

                default:
                    {
                        // call GetCuttingPlanes method
                        GetCuttingPlanes(firstOffcuts, intPt, angle, out _, out Offcut firstOffcut, out _);
                        GetCuttingPlanes(secondOffcuts, intPt, angle, out List<int> secondIndices, out _, out _);

                        // create Cutter geometry through scaling the closest brep
                        Brep offcutCutter = firstOffcut.OffcutGeometry.DuplicateBrep();
                        offcutCutter.Transform(Transform.Scale(firstOffcut.AveragePlane, 2, width, 4));

                        // call CreateIntersectionJoint method
                        CreateIntersectionJoint(offcutCutter, secondOffcuts, secondIndices, out List<Offcut> secondOffcutList, out List<Offcut> secondIntOffcuts);

                        // call GetSplineBase method
                        GetSplineBase(Utility.GetOffcutBaseCurve(secondOffcuts), secondIntOffcuts, intPt, out List<Plane> basePlanes, out List<Offcut> intOffcuts);

                        //// call CreateIntersectionSpline method
                        //for (int i = 0; i < basePlanes.Count; i++)
                        //{
                        //    CreateIntersectionSpline(basePlanes[i], intOffcuts[i], out Brep cutter, out Brep display);
                        //    joints.Add(cutter);
                        //    joints.Add(display);
                        //}

                        // add all Offcuts to the respective data tree
                        for (int i = 0; i < firstOffcuts.Count; i++)
                            offcutGHList.Add(new Offcut_GH(firstOffcuts[i]), firstPath);

                        for (int i = 0; i < secondOffcutList.Count; i++)
                            offcutGHList.Add(new Offcut_GH(secondOffcutList[i]), secondPath);

                        for (int i = 0; i < secondIntOffcuts.Count; i++)
                        {
                            intOffcutGHList.Add(new Offcut_GH(secondIntOffcuts[i]), secondPath);
                        }
                    }
                    break;
            }

            // access output parameters
            DA.SetDataTree(0, offcutGHList);
            DA.SetDataTree(1, intOffcutGHList);
        }


        //------------------------------------------------------------
        // CreateIntersectionSpline method
        //------------------------------------------------------------
        protected void CreateIntersectionSpline(Plane plane, Offcut intOffcut, out Brep cutter, out Brep display)
        {
            // initialise new plane
            Plane basePlane = new Plane(plane);

            // joint dimensions
            Interval dX = new Interval(-0.025 / 2, 0.025 / 2);
            Interval dY = new Interval(-0.05 / 2, 0.05 / 2);
            double dZ = intOffcut.X * 2;

            // rotate base plane
            double angle = Utility.ConvertToRadians(90);
            basePlane.Rotate(angle, basePlane.YAxis, basePlane.Origin);
            basePlane.Rotate(angle, basePlane.ZAxis, basePlane.Origin);

            // move basePlane for centered extrusion
            basePlane.Transform(Transform.Translation(basePlane.ZAxis * intOffcut.X));

            // create rectangle
            Rectangle3d baseRect = new Rectangle3d(basePlane, dX, dY);
            Curve baseCurve = SplineJoints_GH.DovetailRect(baseRect, basePlane, 0.006);

            // extrude first base to create first joint
            cutter = Extrusion.Create(baseCurve, -dZ, true).ToBrep();

            // cutter clean-up
            cutter.Faces.SplitKinkyFaces(0.0001);
            if (BrepSolidOrientation.Inward == cutter.SolidOrientation)
                cutter.Flip();

            // create display joints
            basePlane.Transform(Transform.Translation(basePlane.ZAxis * -intOffcut.X / 2));
            display = Extrusion.Create(SplineJoints_GH.DovetailRect(new Rectangle3d(basePlane, dX, dY), basePlane, 0.006), -intOffcut.X, true).ToBrep();

            // display clean-up
            display.Faces.SplitKinkyFaces(0.0001);
            if (BrepSolidOrientation.Inward == display.SolidOrientation)
                display.Flip();
        }



        //------------------------------------------------------------
        // CreateIntersectionJoint method
        //------------------------------------------------------------
        protected void CreateIntersectionJoint(Brep cutter, List<Offcut> offcutList, List<int> indices, out List<Offcut> outOffcutList, out List<Offcut> intersectOffcut)
        {
            // list & array to store data for boolean difference
            List<Brep> cutters = new List<Brep> { cutter };
            List<Brep> cut = new List<Brep>();

            for (int i = 0; i < offcutList.Count; i++)
            {
                if (i == indices[0] || i == indices[1] || i == indices[2])
                {
                    Offcut offcut = new Offcut(offcutList[i]);
                    cut.Add(offcut.OffcutGeometry);
                }
                else
                    continue;
            }

            // perform boolean difference
            Brep[] cutBreps = Brep.CreateBooleanDifference(cut, cutters, 0.0001);

            // initialise output list and list of volumes to get the biggest piece
            List<Brep> brepList = new List<Brep>();
            List<double> volumeList = new List<double>();

            // clean-up and add to output list
            foreach (Brep brep in cutBreps)
            {
                // brep clean-up
                brep.Faces.SplitKinkyFaces(0.0001);
                brep.MergeCoplanarFaces(0.0001);
                brep.Faces.SplitKinkyFaces(0.0001);

                // check solid orientation and return
                if (BrepSolidOrientation.Inward == brep.SolidOrientation)
                {
                    brep.Flip();
                    volumeList.Add(brep.GetVolume(0.0001, 0.0001));
                    brepList.Add(brep);
                }
                else
                {
                    volumeList.Add(brep.GetVolume(0.0001, 0.0001));
                    brepList.Add(brep);
                }
            }

            // get the three biggest volumes by sorting and reversing the volume list
            List<double> duplicateVolume = new List<double>(volumeList);

            volumeList.Sort();
            volumeList.Reverse();

            var maxThree = volumeList.Take(3);

            // get indices of max volumes
            List <int> maxIndices = new List<int>();
            foreach (double maxVal in maxThree)
                maxIndices.Add(duplicateVolume.IndexOf(maxVal));

            // sort list of indices
            maxIndices.Sort();

            // output lists
            outOffcutList = new List<Offcut>();
            intersectOffcut = new List<Offcut>();

            // create new Offcuts and update the Offcut's geometry
            for (int i = 0; i < offcutList.Count; i++)
            {
                if (i == indices[0])
                {
                    Offcut newOffcut = new Offcut(offcutList[i]) { OffcutGeometry = brepList[maxIndices[0]] };
                    outOffcutList.Add(newOffcut);
                    intersectOffcut.Add(newOffcut);
                }

                else if (i == indices[1])
                {
                    Offcut newOffcut = new Offcut(offcutList[i]) { OffcutGeometry = brepList[maxIndices[1]] };
                    outOffcutList.Add(newOffcut);
                    intersectOffcut.Add(newOffcut);
                }

                else if (i == indices[2])
                {
                    Offcut newOffcut = new Offcut(offcutList[i]) { OffcutGeometry = brepList[maxIndices[2]] };
                    outOffcutList.Add(newOffcut);
                    intersectOffcut.Add(newOffcut);
                }

                else
                {
                    Offcut newOffcut = new Offcut(offcutList[i]);
                    outOffcutList.Add(newOffcut);
                }
            }
        }


        //------------------------------------------------------------
        // GetCuttingGeometry method
        //------------------------------------------------------------
        protected void GetSplineBase(Curve curve, List<Offcut> intersectionOffcuts, Point3d intersectionPoint, out List<Plane> basePlanes, out List<Offcut> intOffcuts)
        {
            // array of points to store the intersection points
            Point3d[,] pointArray = new Point3d[intersectionOffcuts.Count, 2];

            // get the two intersection points per brep
            for (int i = 0; i < intersectionOffcuts.Count; i++)
            {
                Intersection.CurveBrep(curve, intersectionOffcuts[i].OffcutGeometry, 0.0001, out _, out Point3d[] intPoints);
                pointArray[i, 0] = intPoints[0];
                pointArray[i, 1] = intPoints[1];
            }

            // get the closest point per brep
            List<Point3d> closestPt = new List<Point3d>();

            for (int i = 0; i < intersectionOffcuts.Count; i++)
            {
                if (intersectionPoint.DistanceTo(pointArray[i, 0]) < intersectionPoint.DistanceTo(pointArray[i, 1]))
                    closestPt.Add(pointArray[i, 0]);
                else
                    closestPt.Add(pointArray[i, 1]);
            }

            // get the two closest points
            List<double> minDistance = new List<double>();

            foreach (Point3d pt in closestPt)
                minDistance.Add(intersectionPoint.DistanceTo(pt));

            // duplicate and sort distances
            List<double> minDuplicate = new List<double>(minDistance);
            minDistance.Sort();

            // create list of indices
            List<int> indices = new List<int> { minDuplicate.IndexOf(minDistance[0]), minDuplicate.IndexOf(minDistance[1]) };

            // return lists
            basePlanes = new List<Plane>();
            intOffcuts = new List<Offcut>();

            // assign closest points as origin to average plane and add data to return lists
            foreach (int index in indices)
            {
                Plane plane = new Plane(intersectionOffcuts[index].AveragePlane);
                plane.Origin = closestPt[index];
                basePlanes.Add(plane);

                intOffcuts.Add(intersectionOffcuts[index]);
            }
        }


        //------------------------------------------------------------
        // GetCuttingGeometry method
        //------------------------------------------------------------
        protected void GetCuttingGeometry(Plane averagePlane, Plane originalPlane, Offcut offcut, int dir, double width, out Brep cutterBrep)
        {
            Plane average = new Plane(averagePlane);
            double angle = Vector3d.VectorAngle(averagePlane.ZAxis, originalPlane.ZAxis);
            average.Transform(Transform.Rotation(dir * angle, averagePlane.XAxis, averagePlane.Origin));

            // create cutter breps
            cutterBrep = Offcut.CreateOffcutBrep(offcut, average, offcut.PositionIndex);

            // move cutter
            cutterBrep.Transform(Transform.Translation(dir * average.XAxis * offcut.X / 2));

            // scale cutter
            cutterBrep.Transform(Transform.Scale(average, 3, width, 3));
        }


        //------------------------------------------------------------
        // GetCuttingPlanes method
        //------------------------------------------------------------
        protected void GetCuttingPlanes(List<Offcut> offcutList, Point3d intPt, double angle, out List<int> indices, out Offcut offcut, out Plane basePlane)
        {
            // call GetClosestOffcutIndex method
            GetClosestOffcutIndex(offcutList, intPt, out int index, out indices);

            // get the Offcut closest to the intersection point
            offcut = new Offcut(offcutList[index]);

            // get base plane
            basePlane = new Plane(offcutList[index].MovedAveragePlane);

            // rotate base plane as indicated
            basePlane.Transform(Transform.Rotation(Utility.ConvertToRadians(angle), basePlane.ZAxis, basePlane.Origin));
            //basePlane.Transform(Transform.Rotation(Utility.ConvertToRadians(secAngle), basePlane.XAxis, basePlane.Origin));
        }


        //------------------------------------------------------------
        // GetCuttingGeometry method
        //------------------------------------------------------------
        protected void GetBasePlane(List<Offcut> offcutList, Point3d intPt, double angle, out List<int> indicies, out Plane basePlane)
        {
            // call GetClosestOffcutIndex method
            GetClosestOffcutIndex(offcutList, intPt, out int index, out indicies);

            // get base plane
            basePlane = new Plane(offcutList[index].MovedAveragePlane);

            // rotate base plane as indicated
            basePlane.Transform(Transform.Rotation(Utility.ConvertToRadians(angle), basePlane.ZAxis, basePlane.Origin));
        }


        //------------------------------------------------------------
        // GetClosestOffcutIndex method
        //------------------------------------------------------------
        protected void GetClosestOffcutIndex(List<Offcut> offcutList, Point3d intPt, out int closestIndex, out List<int> indexList)
        {
            // initialise list to store the distances
            List<double> distances = new List<double>();

            // calculate the distances from the intersection point to a closest point of each Offcut
            foreach (Offcut offcut in offcutList)
            {
                //Point3d closestPt = offcut.OffcutGeometry.ClosestPoint(intPt);
                double distance = intPt.DistanceTo(offcut.AveragePlane.Origin);
                distances.Add(distance);
            }

            // get the index of the Offcut closest to the intersection point
            closestIndex = distances.IndexOf(distances.Min());

            // duplicate distances list to sort and find smallest three distances
            List<double> distancesCopy = new List<double>(distances);

            // sort list and take three
            distancesCopy.Sort();

            var minThree = distancesCopy.Take(3);

            // add data to the return list
            indexList = new List<int>();
            foreach (double min in minThree)
                indexList.Add(distances.IndexOf(min));
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_IntersectionJoints;

        // component giud
        public override Guid ComponentGuid => new Guid("7B5A7106-2285-4056-9117-BC95A627FD04");
    }
}