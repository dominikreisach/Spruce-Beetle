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
    public class GetBrep_GH : GH_Component
    {
        public GetBrep_GH()
          : base("Get Brep", "GetB", "Deconstruct an Offcut and get all the Breps", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Provide one or more Offcuts", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Breps", "B", "Offcuts as Breps", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // list to reference the input parameter to
            List<SpruceBeetle.Offcut> inputOffcut = new List<SpruceBeetle.Offcut>();

            // access input parameter
            DA.GetDataList(0, inputOffcut);

            // initialise a list of Breps to store all the Brep data
            List<Brep> offcutList = new List<Brep>();

            // add all the data to the lists
            if (inputOffcut == null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Offcut data provided!");
            else if (inputOffcut.Count == 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The provided list is empty!");
            else
            {
                for (int i = 0; i < inputOffcut.Count; i++)
                {
                    if (inputOffcut[i].OffcutGeometry == null)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Brep found in the Offcut data!");
                    else
                        offcutList.Add(inputOffcut[i].OffcutGeometry);
                }

                // access output parameter
                DA.SetDataList(0, offcutList);
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_GetBrep;

        // component giud
        public override Guid ComponentGuid => new Guid("6BE96E20-4576-4E8A-A7C0-ABF527263A15");
    }
}