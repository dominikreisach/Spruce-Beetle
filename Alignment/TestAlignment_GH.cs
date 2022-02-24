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
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace SpruceBeetle.Alignment
{
    public class TestAlignment_GH : GH_Component
    {
        public TestAlignment_GH()
          : base("Test Alignment", "TestAlign", "Test an alignment on a curve", "Spruce Beetle", "    Alignment")
        {
        }

        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to align Offcuts to", GH_ParamAccess.item);
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Alignment Points", "APt", "The base points of the alignment", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Alignment Planes", "AP", "The base planes of the alignment", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            Curve curve = null;
            List<Offcut> offcutData = new List<Offcut>();

            // access input parameters
            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetDataList(1, offcutData)) return;

            // reparameterize curve
            curve.Domain = new Interval(0.0, 1.0);

            // initialise list to store all the data
            List<Point3d> alignmentBasePts = new List<Point3d> { curve.PointAtStart };

            curve.PerpendicularFrameAt(0, out Plane initialPlane);
            List<Plane> alignmentBasePlanes = new List<Plane> { initialPlane};

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
                        // call mtethods to align the Offucts along the line-like curve
                        Utility.GetOptimizedLinearIndex(curve, offcutData, secondPlane, new Interval(0, 0), out Plane startPlane, out int offcutIndex);
                        Utility.TestAlignment(curve, offcutData[offcutIndex], startPlane, 1, false, out List<Plane> planeList);

                        // store point in point list
                        alignmentBasePts.Add(planeList[1].Origin);
                        alignmentBasePlanes.Add(planeList[1]);

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
                        // call mtethods to align the Offucts along the line-like curve
                        Utility.GetOptimizedLinearIndex(curve, offcutData, secondPlane, new Interval(0, 0), out Plane startPlane, out int offcutIndex);
                        Utility.TestAlignment(curve, offcutData[offcutIndex], startPlane, 1, true, out List<Plane> planeList);

                        // store point in point list
                        alignmentBasePts.Add(planeList[1].Origin);
                        alignmentBasePlanes.Add(planeList[1]);

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
                        // call methods to align the Offcuts along the curve in an optimized manner
                        Utility.GetOptimizedOffcutIndex(curve, offcutData, secondPlane, new Interval(0, 0), out Plane startPlane, out int offcutIndex, out double adjustmentValue);
                        Utility.TestAlignment(curve, offcutData[offcutIndex], startPlane, adjustmentValue, false, out List<Plane> planeList);

                        // store point in point list
                        alignmentBasePts.Add(planeList[1].Origin);
                        alignmentBasePlanes.Add(planeList[1]);

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
                        // call methods to align the Offcuts along the curve in an optimized manner
                        Utility.GetOptimizedOffcutIndex(curve, offcutData, secondPlane, new Interval(0, 0), out Plane startPlane, out int offcutIndex, out double adjustmentValue);
                        Utility.TestAlignment(curve, offcutData[offcutIndex], startPlane, adjustmentValue, true, out List<Plane> planeList);

                        // store point in point list
                        alignmentBasePts.Add(planeList[1].Origin);
                        alignmentBasePlanes.Add(planeList[1]);

                        // remove used Offcut from list
                        offcutData.RemoveAt(maxIndex);

                        // set add last to false
                        addLast = false;
                    }

                    else
                        break;
                }
            }

            // access output parameter
            DA.SetDataList(0, alignmentBasePts);
            DA.SetDataList(1, alignmentBasePlanes);
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.obscure;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_TestAlignment;

        // component giud
        public override Guid ComponentGuid => new Guid("50E3220C-8E98-42C8-B345-14A2925CA85C");
    }
}