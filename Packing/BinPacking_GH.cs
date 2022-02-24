/*
 * MIT License
 * 
 * Copyright (c) 2019 Enzo Ruiz
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

/*
 * The Python libary used here can be found in this github repository:
 * https://github.com/enzoruiz/3dbinpacking
 */


using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Python.Runtime;


namespace SpruceBeetle.Packing
{
    public class BinPacking_GH : GH_Component
    {
        public BinPacking_GH()
          : base("Bin Packing BestFit", "PackBin", "The BestFit algorithm supports full item rotation and has excellent runtime performance and bin utilization",
              "Spruce Beetle", "   Packing")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "Box container to fill with Offcuts", GH_ParamAccess.item);

            for (int i = 0; i < 2; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Packed Offcuts", "POc", "List of packed Offcuts", GH_ParamAccess.list);
            pManager.AddBrepParameter("Container", "C", "The container where the Offcuts are packed into", GH_ParamAccess.item);

            for (int i = 0; i < 2; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> offcutData = new List<Offcut>();
            Box boundingBox = new Box();

            // access input parameters
            if (!DA.GetDataList(0, offcutData)) return;
            if (!DA.GetData(1, ref boundingBox)) return;

            // create bounding box at origin
            Box originBox = new Box(Plane.WorldXY, boundingBox.BoundingBox);
           
            // call PackOffcuts method
            List<Brep> packedOffcuts = PackOffcuts(boundingBox, offcutData);

            // access output parameters
            DA.SetDataList(0, packedOffcuts);
            DA.SetData(1, originBox);
        }


        //------------------------------------------------------------
        // PackOffcuts
        //------------------------------------------------------------
        protected List<Brep> PackOffcuts(Box boundingBox, List<Offcut> offcutData)
        {
            // initialise python
            PythonEngine.Initialize();
            
            // using Python.Net
            using (Py.GIL())
            {
                using (dynamic scope = Py.CreateScope())
                {
                    // import Python library
                    dynamic py3 = Py.Import("py3dbp");

                    // create Packer
                    dynamic packer = py3.Packer();

                    // create bin
                    dynamic binContainer = py3.Bin("Container", boundingBox.X.T1, boundingBox.Y.T1, boundingBox.Z.T1, 470);
                    packer.add_bin(binContainer);

                    // create Offcuts
                    foreach (Offcut offcut in offcutData)
                        packer.add_item(py3.Item(offcut.Index.ToString(), offcut.X.ToString(), offcut.Y.ToString(), offcut.Z.ToString(), (offcut.X * offcut.Y * offcut.Z * 470).ToString()));

                    // pack Offcuts
                    packer.pack();

                    // create empty list as output
                    List<Brep> packedOffcuts = new List<Brep>();
                    List<string> unpackedOffcuts = new List<string>();
                    Brep containerBin;

                    foreach (dynamic bin in packer.bins)
                    {
                        double xBin = (double)bin.width;
                        double yBin = (double)bin.height;
                        double zBin = (double)bin.depth;

                        Vector3d normalVec = new Vector3d(0, 0, yBin);
                        Rectangle3d baseRect = new Rectangle3d(Plane.WorldXY, xBin, zBin);
                        Brep openBin = Surface.CreateExtrusion(baseRect.ToNurbsCurve(), normalVec).ToBrep();
                        containerBin = openBin.CapPlanarHoles(0.0001);
                        containerBin.Faces.SplitKinkyFaces(0.0001);
                    }

                    // collection of unused Offcuts
                    foreach (dynamic bin in packer.bins)
                        foreach (dynamic item in bin.unfitted_items)
                            unpackedOffcuts.Add(item.ToString);

                    // packed Offcuts
                    foreach (dynamic bin in packer.bins)
                        foreach (dynamic item in bin.items)
                        {
                            double x = 0;
                            double y = 0;
                            double z = 0;

                            // check rotation type
                            switch (item.rotation_type)
                            {
                                case 0:
                                    {
                                        x = (double)item.width;
                                        y = (double)item.height;
                                        z = (double)item.depth;
                                    }
                                    break;
                                case 1:
                                    {
                                        y = (double)item.width;
                                        x = (double)item.height;
                                        z = (double)item.depth;
                                    }
                                    break;
                                case 2:
                                    {
                                        z = (double)item.width;
                                        x = (double)item.height;
                                        y = (double)item.depth;
                                    }
                                    break;
                                case 3:
                                    {
                                        z = (double)item.width;
                                        y = (double)item.height;
                                        x = (double)item.depth;
                                    }
                                    break;
                                case 4:
                                    {
                                        y = (double)item.width;
                                        z = (double)item.height;
                                        x = (double)item.depth;
                                    }
                                    break;
                                case 5:
                                    {
                                        x = (double)item.width;
                                        z = (double)item.height;
                                        y = (double)item.depth;
                                    }
                                    break;
                            }

                            // construct Offcut as Brep
                            Vector3d normalVec = new Vector3d(0, 0, y);
                            Rectangle3d baseRect = new Rectangle3d(Plane.WorldXY, x, z);
                            Brep openOffcut = Surface.CreateExtrusion(baseRect.ToNurbsCurve(), normalVec).ToBrep();
                            Brep closedOffcut = openOffcut.CapPlanarHoles(0.0001);
                            closedOffcut.Faces.SplitKinkyFaces(0.0001);

                            // move Offcut to correct packing location
                            Vector3d moveVec = new Vector3d(item.position[0], item.position[2], item.position[1]);
                            Transform move = Transform.Translation(moveVec);

                            closedOffcut.Transform(move);

                            // add Offcut to list
                            packedOffcuts.Add(closedOffcut);
                        }

                    // return values
                    return packedOffcuts;
                }
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BinPacking;

        // component giud
        public override Guid ComponentGuid => new Guid("7B0B4CCB-681F-4999-A184-5A118A13046E");
    }
}