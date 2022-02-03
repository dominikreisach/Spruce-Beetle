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


namespace SpruceBeetle.Create
{
    public class ConstructOffcut_GH : GH_Component
    {
        public ConstructOffcut_GH()
          : base("Construct Offcut", "ConOffcut", "Construct Offcut(s) by providing the necessary data.", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("x-Dimension", "x", "x-Dimension of the Offcut", GH_ParamAccess.list);
            pManager.AddNumberParameter("y-Dimension", "y", "y-Dimension of the Offcut", GH_ParamAccess.list);
            pManager.AddNumberParameter("z-Dimension", "z", "z-Dimension of the Offcut", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut", "Oc", "Generated Offcut", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            List<double> zList = new List<double>();

            // access input parameters
            if (!DA.GetDataList(0, xList)) return;
            if (!DA.GetDataList(1, yList)) return;
            if (!DA.GetDataList(2, zList)) return;

            // initialise list to store the Offcuts
            List<SpruceBeetle.Offcut_GH> offcutGHList = new List<SpruceBeetle.Offcut_GH>();

            if (xList.Count == yList.Count && xList.Count == zList.Count)
            {
                for (int i = 0; i < xList.Count; i++)
                {
                    SpruceBeetle.Offcut offcut = new SpruceBeetle.Offcut(i, xList[i], yList[i], zList[i]);
                    SpruceBeetle.Offcut_GH offcutGH = new SpruceBeetle.Offcut_GH(offcut);
                    offcutGHList.Add(offcutGH);
                }
            }
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Different amount of x, y, and z values provided");

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
        public override Guid ComponentGuid => new Guid("6D5E4857-46B8-4031-8BBD-BCE54D1FF71D");
    }

}