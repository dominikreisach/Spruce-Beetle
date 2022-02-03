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
    public class ExcelOffcut_GH : GH_Component
    {
        public ExcelOffcut_GH() : base("Excel to Offcut", "XLS 2 Oc", "Convert the data from an Excel sheet to Offcuts", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "file", "Provide a path to an Excel file", GH_ParamAccess.item);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "All the necessary data of the Offcuts", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = "";

            DA.GetData(0, ref filePath);

            if (filePath == null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No file path provided!");
            else
            {
                ExcelOffcut pathOffcut = new ExcelOffcut(filePath);
                List<SpruceBeetle.Offcut> offcutList = pathOffcut.ExcelFile();

                // change data type for output to GH
                List<SpruceBeetle.Offcut_GH> offcutGHList = new List<SpruceBeetle.Offcut_GH>();
                foreach (SpruceBeetle.Offcut offcut in offcutList)
                {
                    SpruceBeetle.Offcut_GH offcutGH = new SpruceBeetle.Offcut_GH(offcut);
                    offcutGHList.Add(offcutGH);
                }

                DA.SetDataList(0, offcutGHList);
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_ExcelOffcut;

        // component giud
        public override Guid ComponentGuid => new Guid("FDFD931D-359F-4CF6-ACA5-3911CF71A76C");
    }
}