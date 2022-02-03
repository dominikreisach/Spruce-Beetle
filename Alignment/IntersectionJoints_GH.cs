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
            pManager.AddBooleanParameter("Flip Joint", "FJ", "Flip the direction of the joint", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Width", "W", "The width of the lap joint", GH_ParamAccess.item, 1.0);

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
            bool flipJoint = false;
            double width = 2.0;

            // access input parameters
            if (!DA.GetDataList(0, firstOffcuts)) return;
            if (!DA.GetDataList(1, secondOffcuts)) return;
            if (!DA.GetData(2, ref intPt)) return;
            if (!DA.GetData(3, ref flipJoint)) return;
            if (!DA.GetData(4, ref width)) return;

            // initialise data trees and paths
            DataTree<Offcut_GH> offcutGHList = new DataTree<Offcut_GH>();
            DataTree<Offcut_GH> intOffcutGHList = new DataTree<Offcut_GH>();

            GH_Path firstPath = new GH_Path(0);
            GH_Path secondPath = new GH_Path(1);
            
            // call GetRotationAngle method
            GetRotationAngle(flipJoint, out double angle);

            // call GetCuttingPlanes method
            GetCuttingPlanes(firstOffcuts, intPt, angle, out List<int> firstIndices, out Offcut firstOffcut, out Plane firstPlane);
            GetCuttingPlanes(secondOffcuts, intPt, angle, out List<int> secondIndices, out Offcut secondOffcut, out Plane secondPlane);

            // rotate first plane
            firstPlane.Transform(Transform.Rotation(Utility.ConvertToRadians(180), firstPlane.ZAxis, firstPlane.Origin));

            // create average plane out of the first and second one
            Point3d originPt = new Point3d((firstPlane.Origin + secondPlane.Origin) / 2);
            Plane averagePlane = new Plane(originPt, (firstPlane.XAxis + secondPlane.XAxis) / 2, (firstPlane.YAxis + secondPlane.YAxis) / 2);

            // call GetCuttingGeometry method
            GetCuttingGeometry(averagePlane, firstPlane, firstOffcut, 1, width, out Brep firstBrep);
            GetCuttingGeometry(averagePlane, secondPlane, secondOffcut, -1, width, out Brep secondBrep);

            // call CreateLapJoint method
            CreateLapJoint(firstBrep, firstOffcuts, firstIndices, out List<Offcut> firstOffcutList, out List<Offcut> firstIntOffcuts);
            CreateLapJoint(secondBrep, secondOffcuts, secondIndices, out List<Offcut> secondOffcutList, out List<Offcut> secondIntOffcuts);

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

            // access output parameters
            DA.SetDataTree(0, offcutGHList);
            DA.SetDataTree(1, intOffcutGHList);
        }


        //------------------------------------------------------------
        // CreateLapJoint method
        //------------------------------------------------------------
        protected void CreateLapJoint(Brep cutter, List<Offcut> offcutList, List<int> indices, out List<Offcut> outOffcutList, out List<Offcut> intersectOffcut)
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

            // initialise output list
            List<Brep> brepList = new List<Brep>();

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
                    brepList.Add(brep);
                }
                else
                    brepList.Add(brep);
            }

            outOffcutList = new List<Offcut>();
            intersectOffcut = new List<Offcut>();

            for (int i = 0; i < offcutList.Count; i++)
            {
                if (i == indices[0])
                {
                    Offcut newOffcut = new Offcut(offcutList[i]) { OffcutGeometry = brepList[0] };
                    outOffcutList.Add(newOffcut);
                    intersectOffcut.Add(newOffcut);
                }

                else if (i == indices[1])
                {
                    Offcut newOffcut = new Offcut(offcutList[i]) { OffcutGeometry = brepList[1] };
                    outOffcutList.Add(newOffcut);
                    intersectOffcut.Add(newOffcut);
                }

                else if (i == indices[2])
                {
                    Offcut newOffcut = new Offcut(offcutList[i]) { OffcutGeometry = brepList[2] };
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
        // GetRotationAngle method
        //------------------------------------------------------------
        protected void GetRotationAngle(bool flipJoint, out double angle)
        {
            switch (flipJoint)
            {
                case false:
                    angle = 0;
                    break;

                case true:
                    angle = 180;
                    break;
            }
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
                Point3d closestPt = offcut.OffcutGeometry.ClosestPoint(intPt);
                double distance = intPt.DistanceTo(closestPt);
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