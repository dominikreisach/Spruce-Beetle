///*
// * MIT License
// * 
// * Copyright (c) 2022 Dominik Reisach
// * 
// * Permission is hereby granted, free of charge, to any person obtaining a copy
// * of this software and associated documentation files (the "Software"), to deal
// * in the Software without restriction, including without limitation the rights
// * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// * copies of the Software, and to permit persons to whom the Software is
// * furnished to do so, subject to the following conditions:
// * 
// * The above copyright notice and this permission notice shall be included in all
// * copies or substantial portions of the Software.
// * 
// * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// * SOFTWARE.
// */


//using System;
//using System.Linq;
//using System.Collections.Generic;
//using Grasshopper.Kernel;


//namespace SpruceBeetle.Create
//{
//    public class ListUpdate_GH : GH_Component
//    {
//        public ListUpdate_GH()
//          : base("List Update", "Update", "Update the list of Offcuts to remove all used Offucts", "Spruce Beetle", "     Create")
//        {
//        }


//        // parameter inputs
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
//            pManager.AddGenericParameter("Offcuts", "Oc", "Used Offcuts", GH_ParamAccess.list);

//            for (int i = 0; i < pManager.ParamCount; i++)
//                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
//        }


//        // parameter outputs
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Offcuts", "Oc", "Updated list with unused Offcuts only", GH_ParamAccess.list);

//            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
//        }


//        // main
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            // variables to reference the input parameters to
//            List<SpruceBeetle.Offcut> offcutData = new List<SpruceBeetle.Offcut>();
//            List<SpruceBeetle.Offcut> usedOffcuts = new List<SpruceBeetle.Offcut>();

//            // access input parameters
//            if (!DA.GetDataList(0, offcutData)) return;
//            if (!DA.GetDataList(1, usedOffcuts)) return;

//            // remove used Offcuts from initial list
//            List<SpruceBeetle.Offcut_GH> outputOffcuts = new List<SpruceBeetle.Offcut_GH>();

//            for (int i = 0; i < usedOffcuts.Count; i++)
//            {
//                SpruceBeetle.Offcut removeOffcut = offcutData.Single(offcut => offcut.Index == usedOffcuts[i].Index);
//                offcutData.Remove(removeOffcut);
//            }

//            // add Offcuts to Offcut_GH list for output
//            for (int i = 0; i < offcutData.Count; i++)
//            {
//                SpruceBeetle.Offcut_GH offcutGH = new SpruceBeetle.Offcut_GH(offcutData[i]);
//                outputOffcuts.Add(offcutGH);
//            }

//            // access output parameter
//            DA.SetDataList(0, outputOffcuts);
//        }


//        //------------------------------------------------------------
//        // Else
//        //------------------------------------------------------------

//        // exposure property
//        public override GH_Exposure Exposure => GH_Exposure.primary;

//        // add icon
//        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_UpdateList;

//        // component giud
//        public override Guid ComponentGuid => new Guid("0E664939-157A-4C82-BD80-483714CC8E1B");
//    }
//}