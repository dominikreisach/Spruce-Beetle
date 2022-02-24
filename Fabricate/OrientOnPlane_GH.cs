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
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;


namespace SpruceBeetle.Fabricate
{
    public class OrientOnPlane_GH : GH_Component
    {
        public OrientOnPlane_GH()
          : base("Orient on Plane", "OrientOc", "Orients the Offcuts to a specified planee for fabrication purposes", "Spruce Beetle", "    Fabricate")
        {
        }

        
        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "Aligned Offcuts on the curve", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Target Plane", "TP", "The plane where the Offcuts shall be oriented at", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Plane Index", "PI", "0 = Avrg, 1 = First, 2 = Second", GH_ParamAccess.item, 0);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.AddBrepParameter("Oriented Offcuts", "OOc", "Offcuts oriented to the specified plane", GH_ParamAccess.tree);

            pManager.HideParameter(0);
            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> offcutData = new List<Offcut>();
            Plane targetPlane = new Plane();
            int planeIndex = 0;

            // access input parameter
            if (!DA.GetDataList(0, offcutData)) return;
            if (!DA.GetData(1, ref targetPlane)) return;
            if (!DA.GetData(2, ref planeIndex)) return;

            // initialise lists to store all the data
            DataTree<Brep> outputOriginOffcuts = new DataTree<Brep>();

            // iterate over aligned Offcuts to move them to the world origin
            for (int i = 0; i < offcutData.Count; i++)
            {
                // create path for outputOriginOffcuts
                GH_Path originPath = new GH_Path(i);
                outputOriginOffcuts.AddRange(OffcutToOrigin(offcutData[i], targetPlane, planeIndex), originPath);
            }

            // access output parameters
            DA.SetDataTree(0, outputOriginOffcuts);
        }


        //------------------------------------------------------------
        // OffcutToOrigin method
        //------------------------------------------------------------
        protected List<Brep> OffcutToOrigin(Offcut offcut, Plane targetPlane, int planeIndex)
        {
            // call CreateOffcutBrep method
            Brep originOffcut = Offcut.CreateOffcutBrep(offcut, offcut.AveragePlane, offcut.PositionIndex);

            // duplicate brep to avoid any problems
            Brep movedOffcut = offcut.OffcutGeometry.DuplicateBrep();

            // add return breps to list
            List<Brep> returnBreps = new List<Brep>
            {
                movedOffcut,
                originOffcut
            };

            // select a specific Offcut plane
            Plane initialPlane = new Plane();

            switch(planeIndex)
            {
                case 0:
                    {
                        initialPlane = new Plane(offcut.AveragePlane);
                    }
                    break;
                case 1:
                    {
                        initialPlane = new Plane(offcut.FirstPlane);
                    }
                    break;
                case 2:
                    {
                        initialPlane = new Plane(offcut.SecondPlane);
                    }
                    break;
                default:
                    {
                        initialPlane = new Plane(offcut.AveragePlane);
                    }
                    break;
            }

            // orient Offcuts
            Transform orientation = Transform.PlaneToPlane(initialPlane, targetPlane);

            // output transformed Offcut for fabrication
            if (movedOffcut.Transform(orientation) && originOffcut.Transform(orientation))
            {
                return returnBreps;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went wrong with the transformation!");
                return null;
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_MoveToOrigin;

        // component giud
        public override Guid ComponentGuid => new Guid("42799FCD-8288-43DA-BEAC-45E1333EC4C4");
    }
}