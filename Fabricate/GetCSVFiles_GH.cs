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
using System.IO;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;

namespace SpruceBeetle.Fabricate
{
    public class GetCSVFiles_GH : GH_Component
    {
        public GetCSVFiles_GH() : base("Get CSV Files", "CSV", "Find all CSV files in a specific folder", "Spruce Beetle", "    Fabricate")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Folder Path", "F", "Provide a path to a folder", GH_ParamAccess.item);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("File Paths", "FP", "File paths of all CSV files in the folder", GH_ParamAccess.tree);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string folderPath = "";

            DA.GetData(0, ref folderPath);

            if (folderPath == null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No folder path provided!");
            else
            {
                // get directory information, i.e., a FileInfo array
                var dInfo = new DirectoryInfo(folderPath);
                var filePaths = dInfo.GetFiles("*.csv", SearchOption.TopDirectoryOnly);

                // initialise data tree to store the file paths
                DataTree<string> outputPaths = new DataTree<string>();

                for (int i = 0; i < filePaths.Length; i++)
                {
                    // create tree path
                    GH_Path treePath = new GH_Path(i);

                    // add string to path of the tree
                    outputPaths.Add(filePaths[i].FullName, treePath);
                }

                // output file paths
                DA.SetDataTree(0, outputPaths);
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_CSVFiles;

        // component giud
        public override Guid ComponentGuid => new Guid("6B9FED81-508C-4A33-9465-9757440A0AB5");
    }
}