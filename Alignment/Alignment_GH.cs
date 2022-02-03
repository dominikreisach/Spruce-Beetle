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
    public class Alignment_GH : GH_Component
    {
        public Alignment_GH()
          : base("Curve Alignment", "Align", "Align a list of Offcuts along a given curve", "Spruce Beetle", "    Alignment")
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

            for (int i = 0; i < dataCount; i++)
            {
                if (tIntersect < 0.96)
                {
                    // get angle for rotation
                    double angle = Utility.Remap(tIntersect, new Interval(0.0, 1.0), angleBounds);

                    // call methods to align the Offcuts along the curve
                    Utility.GetOffcutIndex(curve, secondPlane, angleBounds, out Plane startPlane, out double adjustmentValue);
                    Utility.AlignOffcuts(curve, offcutData[0], startPlane, adjustmentValue, angle, ocBaseIndex, out Brep alignedOffcut, out List<Plane> planeList, out double vol);

                    // adding data to the output list
                    Offcut localOffcut = new Offcut(offcutData[0])
                    {
                        OffcutGeometry = alignedOffcut,
                        FabVol = vol,
                        FirstPlane = planeList[0],
                        SecondPlane = planeList[1],
                        AveragePlane = planeList[2],
                        MovedAveragePlane = planeList[3],
                        PositionIndex = ocBaseIndex
                    };

                    // store data in Offcut list
                    offcutList.Add(localOffcut);

                    // store data in Offcut_GH list
                    Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                    offcutGHList.Add(offcutGH);

                    // remove used Offcut from list
                    offcutData.RemoveAt(0);

                    // add the last plane to the list as new starting plane
                    secondPlane.Add(planeList[1]);

                    // check the t value for the new starting plane
                    curve.ClosestPoint(planeList[1].Origin, out tIntersect);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BasicAlignment;

        // component giud
        public override Guid ComponentGuid => new Guid("2231BF6F-C1B9-462C-ABC0-56008841043F");
    }
}