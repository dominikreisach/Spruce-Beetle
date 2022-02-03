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

namespace SpruceBeetle.Fabricate
{
    public class GetCoordinates_GH : GH_Component
    {
        public GetCoordinates_GH() : base("Get Coordinates", "Coords", "Convert the data from an CSV file to point coordinates", "Spruce Beetle", "    Fabricate")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "file", "Provide a path to a CSV file", GH_ParamAccess.item);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Coordinates", "C", "The target coordinates as points", GH_ParamAccess.list);

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
                // read CSV file and add string data to list
                ReadCSV readCSV = new ReadCSV(filePath);
                List<string> coordinatesTxt = readCSV.GetCSVData();

                // initialise list of points
                List<Point3d> ptList = new List<Point3d>();

                for (int i = 0; i < coordinatesTxt.Count; i++)
                {
                    // split string into coordinate strings
                    string[] coordinates = coordinatesTxt[i].Split(';');

                    // convert strings to doubles
                    double x = Convert.ToDouble(coordinates[0]);
                    double y = Convert.ToDouble(coordinates[1]);
                    double z = Convert.ToDouble(coordinates[2]);

                    // create new point and add to list
                    ptList.Add(new Point3d(x, y, z));
                }

                // output coordinates
                DA.SetDataList(0, ptList);
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_CSVCoord;

        // component giud
        public override Guid ComponentGuid => new Guid("4D21D990-C212-4500-9B04-9DB6C9BB9BD8");
    }
}