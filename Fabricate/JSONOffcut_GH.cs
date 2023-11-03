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
using System.Collections.Generic;
using Newtonsoft.Json;
using Grasshopper.Kernel;

namespace SpruceBeetle.Fabricate
{
    public class JSONOffcut_GH : GH_Component
    {
        public JSONOffcut_GH() : base("JSON to Offcut", "To Offcut", "Deserialize the Offcut data from a JSON file", "Spruce Beetle", "    Fabricate")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "file", "File path to a JSON file with Offcut data", GH_ParamAccess.item);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "Deserialized Offcut data", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variable to reference the input parameters to
            string filePath = "";

            // access input parameters
            if (!DA.GetData(0, ref filePath)) return;

            // read the JSON file
            string json = File.ReadAllText(filePath);

            // deserialize JSON
            List<Offcut> offcutData = JsonConvert.DeserializeObject<List<Offcut>>(json);

            // return offcut data
            DA.SetDataList(0, offcutData);
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_JSONOffcut;

        // component giud
        public override Guid ComponentGuid => new Guid("DC0F4AFA-363F-4363-8837-8A9A2983116A");
    }
}