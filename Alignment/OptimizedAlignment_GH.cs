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
    public class OptimizedAlignment_GH : GH_Component
    {
        public OptimizedAlignment_GH()
          : base("Optimized Alignment", "OptiAlign", "Align a list of Offcuts along a given curve", "Spruce Beetle", "    Alignment")
        {
        }


        // value list
        GH_ValueList valueList = null;
        IGH_Param parameter = null;


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to align Offcuts to", GH_ParamAccess.item);
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddTextParameter("Offcut Position", "OcP", "Position of the Offcuts", GH_ParamAccess.item, "mid-mid");
            pManager.AddNumberParameter("Start Angle", "SA", "Rotate the start of the alignement by the provided angle", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("End Angle", "EA", "Rotate the end of the alignment by the provided angle", GH_ParamAccess.item, 0);

            parameter = pManager[2];

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
                valueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 200, this.Attributes.Pivot.Y - 0);
                valueList.ListItems.Clear();

                List<string> baseOrientation = Offcut.BasePosition();

                foreach (string param in baseOrientation)
                    valueList.ListItems.Add(new GH_ValueListItem(param, $"\"{param}\""));

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
            }
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            Curve curve = null;
            List<Offcut> offcutData = new List<Offcut>();
            string ocBaseType = "";
            double rotateStart = 0;
            double rotateEnd = 0;

            // access input parameters
            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetDataList(1, offcutData)) return;
            if (!DA.GetData(2, ref ocBaseType)) return;
            if (!DA.GetData(3, ref rotateStart)) return;
            if (!DA.GetData(4, ref rotateEnd)) return;

            // reparameterize curve
            curve.Domain = new Interval(0.0, 1.0);

            // create an interval between start and end angle to specifiy the rotation angle
            Interval angleBounds = new Interval(rotateStart, rotateEnd);

            // get Offcut orientation base type
            Dictionary<string, int> ocBaseDict = Offcut.GetBasePosition();
            int ocBaseIndex = ocBaseDict[ocBaseType];

            // initialise list to store all the data
            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();
            List<Offcut> offcutList = new List<Offcut>();

            List<Plane> secondPlane = new List<Plane>();

            double tIntersect = 0.0;
            int dataCount = offcutData.Count;

            // set distance
            double distance = curve.PointAtStart.DistanceTo(curve.PointAtEnd);

            // add last item
            bool addLast = true;

            // check if the curve is linear and execute the alignment accordingly
            // curve is linear
            if (curve.IsLinear())
            {
                for (int i = 0; i < dataCount; i++)
                {
                    // get maximum z value
                    Utility.GetMaxZ(offcutData, out Offcut maxOffcut, out int maxIndex);

                    if (distance > maxOffcut.Z)
                    {
                        // get angle for rotation
                        double angle = Utility.Remap(tIntersect, new Interval(0.0, 1.0), angleBounds);

                        // call mtethods to align the Offucts along the line-like curve
                        Utility.GetOptimizedLinearIndex(curve, offcutData, secondPlane, angleBounds, out Plane startPlane, out int offcutIndex);
                        Utility.LinearAlignment(curve, offcutData[offcutIndex], startPlane, angle, ocBaseIndex, false, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                        // adding data to the output list
                        Offcut localOffcut = new Offcut(offcutData[offcutIndex])
                        {
                            OffcutGeometry = alignedOffcut,
                            FabVol = vol,
                            FirstPlane = planeList[0],
                            SecondPlane = planeList[1],
                            AveragePlane = planeList[2],
                            MovedAveragePlane = planeList[3],
                            BasePlane = Offcut.ComputeBasePlane(planeList[2], offcutData[offcutIndex].X, offcutData[offcutIndex].Y, offcutData[offcutIndex].Z),
                            PositionIndex = ocBaseIndex
                        };

                        // store data in Offcut list
                        offcutList.Add(localOffcut);

                        // store data in Offcut_GH list
                        Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                        offcutGHList.Add(offcutGH);

                        // remove used Offcut from list
                        offcutData.RemoveAt(offcutIndex);

                        // add the last plane to the list as new starting plane
                        secondPlane.Add(planeList[1]);

                        // check the t value for the new starting plane
                        curve.ClosestPoint(planeList[1].Origin, out tIntersect);

                        // check the distance for the new starting plane
                        distance = planeList[1].Origin.DistanceTo(curve.PointAtEnd);
                    }

                    else if (addLast)
                    {
                        // get angle for rotation
                        double angle = Utility.Remap(tIntersect, new Interval(0.0, 1.0), angleBounds);

                        // call mtethods to align the Offucts along the line-like curve
                        Utility.GetOptimizedLinearIndex(curve, offcutData, secondPlane, angleBounds, out Plane startPlane, out int offcutIndex);
                        Utility.LinearAlignment(curve, offcutData[offcutIndex], startPlane, angle, ocBaseIndex, true, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                        // adding data to the output list
                        Offcut localOffcut = new Offcut(offcutData[offcutIndex])
                        {
                            OffcutGeometry = alignedOffcut,
                            FabVol = vol,
                            FirstPlane = planeList[0],
                            SecondPlane = planeList[1],
                            AveragePlane = planeList[2],
                            MovedAveragePlane = planeList[3],
                            BasePlane = Offcut.ComputeBasePlane(planeList[2], offcutData[offcutIndex].X, offcutData[offcutIndex].Y, offcutData[offcutIndex].Z),
                            PositionIndex = ocBaseIndex
                        };

                        // store data in Offcut list
                        offcutList.Add(localOffcut);

                        // store data in Offcut_GH list
                        Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                        offcutGHList.Add(offcutGH);

                        // remove used Offcut from list
                        offcutData.RemoveAt(offcutIndex);

                        // set add last to false
                        addLast = false;
                    }

                    else
                        break;
                }
            }

            // curve is not linear
            else
            {
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
                        Utility.AlignOffcuts(curve, offcutData[offcutIndex], startPlane, adjustmentValue, angle, ocBaseIndex, false, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                        // adding data to the output list
                        Offcut localOffcut = new Offcut(offcutData[offcutIndex])
                        {
                            OffcutGeometry = alignedOffcut,
                            FabVol = vol,
                            FirstPlane = planeList[0],
                            SecondPlane = planeList[1],
                            AveragePlane = planeList[2],
                            MovedAveragePlane = planeList[3],
                            BasePlane = Offcut.ComputeBasePlane(planeList[2], offcutData[offcutIndex].X, offcutData[offcutIndex].Y, offcutData[offcutIndex].Z),
                            PositionIndex = ocBaseIndex
                        };

                        // store data in Offcut list
                        offcutList.Add(localOffcut);

                        // store data in Offcut_GH list
                        Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                        offcutGHList.Add(offcutGH);

                        // remove used Offcut from list
                        offcutData.RemoveAt(offcutIndex);

                        // add the last plane to the list as new starting plane
                        secondPlane.Add(planeList[1]);

                        // check the t value for the new starting plane
                        curve.ClosestPoint(planeList[1].Origin, out tIntersect);

                        // check the distance for the new starting plane
                        distance = planeList[1].Origin.DistanceTo(curve.PointAtEnd);
                    }

                    else if (addLast)
                    {
                        // get angle for rotation
                        double angle = Utility.Remap(tIntersect, new Interval(0.0, 1.0), angleBounds);

                        // call methods to align the Offcuts along the curve in an optimized manner
                        Utility.GetOptimizedOffcutIndex(curve, offcutData, secondPlane, angleBounds, out Plane startPlane, out int offcutIndex, out double adjustmentValue);
                        Utility.AlignOffcuts(curve, offcutData[maxIndex], startPlane, adjustmentValue, angle, ocBaseIndex, true, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                        // adding data to the output list
                        Offcut localOffcut = new Offcut(offcutData[maxIndex])
                        {
                            OffcutGeometry = alignedOffcut,
                            FabVol = vol,
                            FirstPlane = planeList[0],
                            SecondPlane = planeList[1],
                            AveragePlane = planeList[2],
                            MovedAveragePlane = planeList[3],
                            BasePlane = Offcut.ComputeBasePlane(planeList[2], offcutData[maxIndex].X, offcutData[maxIndex].Y, offcutData[maxIndex].Z),
                            PositionIndex = ocBaseIndex
                        };

                        // store data in Offcut list
                        offcutList.Add(localOffcut);

                        // store data in Offcut_GH list
                        Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                        offcutGHList.Add(offcutGH);

                        // remove used Offcut from list
                        offcutData.RemoveAt(maxIndex);

                        // set add last to false
                        addLast = false;
                    }

                    else
                        break;
                }
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
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_OptimizedAlignment;

        // component giud
        public override Guid ComponentGuid => new Guid("7D351E29-9320-4F03-807F-31896403D1CF");
    }
}