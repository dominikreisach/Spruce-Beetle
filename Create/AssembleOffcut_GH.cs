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
    public class AssembleOffcut_GH : GH_Component
    {
        public AssembleOffcut_GH()
          : base("Assemble Offcut", "Asmbl", "Assemble Offcut(s) by providing the necessary data.", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
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
            pManager.AddPlaneParameter("Base Plane", "bp", "Base plane of the Offcut", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Position Index", "pi", "Index of the base position", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut", "Oc", "Assembled Offcut", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<double> indexList = new List<double>();
            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            List<double> zList = new List<double>();
            List<double> volList = new List<double>();
            List<double> fabvolList = new List<double>();
            List<Brep> brepList = new List<Brep>();
            List<Plane> fpList = new List<Plane>();
            List<Plane> spList = new List<Plane>();
            List<Plane> avrgpList = new List<Plane>();
            List<Plane> mavrgpList = new List<Plane>();
            List<Plane> bpList = new List<Plane>();
            List<int> posList = new List<int>();

            // access input parameters
            if (!DA.GetDataList(0, indexList)) return;
            if (!DA.GetDataList(1, xList)) return;
            if (!DA.GetDataList(2, yList)) return;
            if (!DA.GetDataList(3, zList)) return;
            if (!DA.GetDataList(4, volList)) return;
            if (!DA.GetDataList(5, fabvolList)) return;
            if (!DA.GetDataList(6, brepList)) return;
            if (!DA.GetDataList(7, fpList)) return;
            if (!DA.GetDataList(8, spList)) return;
            if (!DA.GetDataList(9, avrgpList)) return;
            if (!DA.GetDataList(10, mavrgpList)) return;
            if (!DA.GetDataList(11, bpList)) return;
            if (!DA.GetDataList(12, posList)) return;

            // initialise list to store the Offcuts
            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();

            if (indexList.Count == xList.Count && indexList.Count == yList.Count && indexList.Count == zList.Count && indexList.Count == volList.Count && indexList.Count == fabvolList.Count
                && indexList.Count == brepList.Count && indexList.Count == fpList.Count && indexList.Count == spList.Count && indexList.Count == avrgpList.Count && indexList.Count == mavrgpList.Count
                && indexList.Count == bpList.Count && indexList.Count == posList.Count)
            {
                for (int i = 0; i < xList.Count; i++)
                {
                    Offcut offcut = new Offcut(brepList[i], indexList[i], xList[i], yList[i], zList[i], volList[i], fabvolList[i], fpList[i], spList[i], avrgpList[i],
                        mavrgpList[i], bpList[i], posList[i]);
                    Offcut_GH offcutGH = new Offcut_GH(offcut);
                    offcutGHList.Add(offcutGH);
                }
            }
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Different amount of data provided");

            // access output parameters
            DA.SetDataList(0, offcutGHList);
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_ConstructOffcut;

        // component giud
        public override Guid ComponentGuid => new Guid("17F06CE5-B12C-48FB-8E00-E35DA303FE52");
    }

}