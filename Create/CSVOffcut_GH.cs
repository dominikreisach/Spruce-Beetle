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
    public class CSVOffcut_GH : GH_Component
    {
        public CSVOffcut_GH() : base("CSV to Offcut", "CSV 2 Oc", "Convert the data from an CSV file to Offcuts", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "file", "Provide a path to an CSV file", GH_ParamAccess.item);
            pManager.AddTextParameter("Delimiter", "D", "Provide a delimiter", GH_ParamAccess.item, ";");

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
            string delimiter = ";";

            DA.GetData(0, ref filePath);
            DA.GetData(1, ref delimiter);

            if (filePath == null)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No file path provided!");
            else
            {
                // read CSV file and add string data to list
                ReadCSV readCSV = new ReadCSV(filePath);
                List<string> coordinatesTxt = readCSV.GetCSVData();

                // initialise new list of Offcuts
                List<Offcut> offcutList = new List<Offcut>();

                // iterate over strings from csv file, convert to Offcut and add to list
                for (int i = 0; i < coordinatesTxt.Count; i++)
                {
                    // split string into coordinate strings
                    string[] coordinates = coordinatesTxt[i].Split(delimiter.ToCharArray()[0]);

                    // convert strings to doubles
                    double index = Convert.ToDouble(coordinates[0]);
                    double x = Convert.ToDouble(coordinates[1]);
                    double y = Convert.ToDouble(coordinates[2]);
                    double z = Convert.ToDouble(coordinates[3]);

                    // initialise new Offcut instance
                    Offcut offcut = new Offcut(index, x, y, z);
                    
                    // add Offcut to list
                    offcutList.Add(offcut);
                }

                // change data type for output to GH
                List<Offcut_GH> offcutGHList = new List<Offcut_GH>();
                foreach (Offcut offcut in offcutList)
                {
                    Offcut_GH offcutGH = new Offcut_GH(offcut);
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_CSVOffcut;

        // component giud
        public override Guid ComponentGuid => new Guid("6B75A8EE-F73C-4C56-BD2A-8B8C8EA57CE9");
    }
}