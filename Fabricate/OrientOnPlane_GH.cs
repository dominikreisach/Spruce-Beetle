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
    public class OrientOnPlane_GH : GH_Component
    {
        public OrientOnPlane_GH()
          : base("Orient on Plane", "OrientOc", "Orients the Offcuts to a specified plane for fabrication purposes", "Spruce Beetle", "    Fabricate")
        {
        }

        
        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "Aligned Offcuts on the curve", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Target Plane", "TP", "The plane where the Offcuts shall be oriented at", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddIntegerParameter("Plane Index", "PI", "0 = Base, 1 = First, 2 = Second, 3 = Avrg", GH_ParamAccess.item, 0);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

            pManager.AddGenericParameter("Oriented Offcuts", "OOc", "Offcuts oriented to the specified plane", GH_ParamAccess.list);
            pManager.AddBrepParameter("Offcut Stock", "OcS", "The stock model of each Offcut", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> offcutData = new List<Offcut>();
            Plane targetPlane = Plane.WorldXY;
            int planeIndex = 0;

            // access input parameter
            if (!DA.GetDataList(0, offcutData)) return;
            if (!DA.GetData(1, ref targetPlane)) return;
            if (!DA.GetData(2, ref planeIndex)) return;

            // initialise lists to store all the data
            List<Brep> outputStock = new List<Brep>();
            List<Offcut_GH> orientedOffcuts = new List<Offcut_GH>();

            // iterate over aligned Offcuts to move them to the world origin
            for (int i = 0; i < offcutData.Count; i++)
            {
                // Call OrientOffcut method
                OrientOffcut(offcutData[i], targetPlane, planeIndex, out Brep offcutStock, out Offcut orientedOffcut);

                // add stock model to list
                outputStock.Add(offcutStock);

                // add oriented Offcut to list
                orientedOffcuts.Add(new Offcut_GH(orientedOffcut));
            }

            // access output parameters
            DA.SetDataList(0, orientedOffcuts);
            DA.SetDataList(1, outputStock);
        }


        //------------------------------------------------------------
        // OrientOffcut method
        //------------------------------------------------------------
        protected void OrientOffcut(Offcut offcut, Plane targetPlane, int planeIndex, out Brep offcutStock, out Offcut orientedOffcut)
        {
            // call CreateOffcutBrep method
            offcutStock = Offcut.CreateOffcutBrep(offcut, offcut.AveragePlane, offcut.PositionIndex);

            // duplicate Offcut data to avoid any problems
            Offcut localOffcut = new Offcut(offcut);

            Brep localBrep = localOffcut.OffcutGeometry.DuplicateBrep();
            Plane fP = new Plane(localOffcut.FirstPlane);
            Plane sP = new Plane(localOffcut.SecondPlane);
            Plane aP = new Plane(localOffcut.AveragePlane);
            Plane maP = new Plane(localOffcut.MovedAveragePlane);
            Plane bp = new Plane(localOffcut.BasePlane);

            // select a specific Offcut plane
            Plane initialPlane = new Plane();

            switch(planeIndex)
            {
                case 0:
                    {
                        initialPlane = new Plane(offcut.BasePlane);
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
                case 3:
                    {
                        initialPlane = new Plane(offcut.AveragePlane);
                    }
                    break;
                default:
                    {
                        initialPlane = new Plane(offcut.BasePlane);
                    }
                    break;
            }

            // orient Offcuts
            Transform orientation = Transform.PlaneToPlane(initialPlane, targetPlane);

            // transform stock model
            offcutStock.Transform(orientation);

            // transform geometrical Offcut data
            localBrep.Transform(orientation);
            fP.Transform(orientation);
            sP.Transform(orientation);
            aP.Transform(orientation);
            maP.Transform(orientation);
            bp.Transform(orientation);

            orientedOffcut = new Offcut(localOffcut)
            {
                OffcutGeometry = localBrep,
                FirstPlane = fP,
                SecondPlane = sP,
                AveragePlane = aP,
                MovedAveragePlane = maP,
                BasePlane = bp
            };
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