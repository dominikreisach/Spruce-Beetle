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


namespace SpruceBeetle.Create
{
    public class DeconstructOffcut_GH : GH_Component
    {
        public DeconstructOffcut_GH()
          : base("Deconstruct Offcut", "DeOffcut", "Deconstruct an Offcut and get all the relevant data", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut", "Oc", "Provide one or more Offcuts", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        //public bool show = false;

        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {           
            pManager.AddNumberParameter("Index", "i", "Index of the Offcut", GH_ParamAccess.list);
            pManager.AddNumberParameter("x-Dimension", "x", "x-Dimension of the Offcut", GH_ParamAccess.list);
            pManager.AddNumberParameter("y-Dimension", "y", "y-Dimension of the Offcut", GH_ParamAccess.list);
            pManager.AddNumberParameter("z-Dimension", "z", "z-Dimension of the Offcut", GH_ParamAccess.list);

            pManager.AddNumberParameter("Volume", "vol", "Volume of the Offcut", GH_ParamAccess.list);
            pManager.AddNumberParameter("Fabricated Volume", "fvol", "Volume post fabrication", GH_ParamAccess.list);

            pManager.AddBrepParameter("Breps", "B", "Offcuts as Breps", GH_ParamAccess.list);

            pManager.AddPlaneParameter("First Plane", "fp", "First plane of the Offcut", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Second Plane", "sp", "Second plane of the Offcut", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Average Plane", "ap", "Average plane of the Offcut", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Moved Average Plane", "map", "Moved average plane of the Offcut", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Position Index", "pi", "Index of the base position", GH_ParamAccess.list);

            // hide plane parameters
            pManager.HideParameter(7);
            pManager.HideParameter(8);
            pManager.HideParameter(9);
            pManager.HideParameter(10);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // list to reference the input parameter to
            List<SpruceBeetle.Offcut> inputOffcut = new List<SpruceBeetle.Offcut>();

            // access input parameter
            DA.GetDataList(0, inputOffcut);

            // initialise lists to store all the data
            List<double> indexList = new List<double>();
            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            List<double> zList = new List<double>();
            List<double> volList = new List<double>();
            List<double> fvolList = new List<double>();
            List<Brep> offcutList = new List<Brep>();
            List<Plane> firstPlaneList = new List<Plane>();
            List<Plane> secondPlaneList = new List<Plane>();
            List<Plane> averagePlaneList = new List<Plane>();
            List<Plane> movedAveragePlaneList = new List<Plane>();
            List<int> basePositionList = new List<int>();

            // add all the data to the lists
            if (inputOffcut == null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Offcut data provided!");
            else if (inputOffcut.Count == 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The provided list is empty!");
            else
            {
                for (int i = 0; i < inputOffcut.Count; i++)
                {
                    indexList.Add(inputOffcut[i].Index);
                    xList.Add(inputOffcut[i].X);
                    yList.Add(inputOffcut[i].Y);
                    zList.Add(inputOffcut[i].Z);
                    volList.Add(inputOffcut[i].Vol);

                    if (inputOffcut[i].FabVol == null)
                        goto Offcut;
                    else
                        fvolList.Add(inputOffcut[i].FabVol);

                    Offcut:
                    if (inputOffcut[i].OffcutGeometry == null)
                        goto FirstPlane;
                    else
                        offcutList.Add(inputOffcut[i].OffcutGeometry);

                    FirstPlane:
                    if (inputOffcut[i].FirstPlane == null)
                        goto SecondPlane;
                    else
                        firstPlaneList.Add(inputOffcut[i].FirstPlane);

                    SecondPlane:
                    if (inputOffcut[i].SecondPlane == null)
                        goto AveragePlane;
                    else
                        secondPlaneList.Add(inputOffcut[i].SecondPlane);

                    AveragePlane:
                    if (inputOffcut[i].AveragePlane == null)
                        goto MovedAveragePlane;
                    else
                        averagePlaneList.Add(inputOffcut[i].AveragePlane);

                    MovedAveragePlane:
                    if (inputOffcut[i].MovedAveragePlane == null)
                        goto BasePosition;
                    else
                        movedAveragePlaneList.Add(inputOffcut[i].MovedAveragePlane);

                    BasePosition:
                    if (inputOffcut[i].PositionIndex == null)
                        continue;
                    else
                        basePositionList.Add(inputOffcut[i].PositionIndex);
                }

                // access output parameters
                DA.SetDataList(0, indexList);
                DA.SetDataList(1, xList);
                DA.SetDataList(2, yList);
                DA.SetDataList(3, zList);
                DA.SetDataList(4, volList);
                DA.SetDataList(5, fvolList);
                DA.SetDataList(6, offcutList);
                DA.SetDataList(7, firstPlaneList);
                DA.SetDataList(8, secondPlaneList);
                DA.SetDataList(9, averagePlaneList);
                DA.SetDataList(10, movedAveragePlaneList);
                DA.SetDataList(11, basePositionList);
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_Deconstruct_Offcut;

        // component giud
        public override Guid ComponentGuid => new Guid("1CA17605-92C8-4308-92FF-C949F8CCC34D");
    }
}