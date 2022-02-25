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


namespace SpruceBeetle.Alignment
{
    public class Unification_GH : GH_Component
    {
        public Unification_GH()
          : base("Unification", "Unify", "Unify the alignment by trimming all elements to the smallest x and y dimensions", "Spruce Beetle", "    Alignment")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to align Offcuts to", GH_ParamAccess.item);
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "Aligned Offcuts on the curve", GH_ParamAccess.list);
            pManager.AddNumberParameter("Scale", "S", "Scale of the unification", GH_ParamAccess.item, 0.975);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Unified Offcuts", "UOc", "Unified Offcuts aligned on the curve", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            Curve curve = null;
            List<Offcut> offcutList = new List<Offcut>();
            double scale = 0.975;

            // access input parameters
            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetDataList(1, offcutList)) return;
            if (!DA.GetData(2, ref scale)) return;

            // call CreateMinShape methods
            Brep minShape = CreateMinShape(curve, offcutList, scale, out List<Plane> movedAveragePlanes, out double[] minValues);

            // initialise array to store the parallel loop data
            Brep[] trimmedOffcuts = new Brep[offcutList.Count];

            // parallel computed intersection
            System.Threading.Tasks.Parallel.For(0, offcutList.Count, (i, state) =>
            {
                Brep trimmedOffcut = OffcutIntersection(offcutList[i].OffcutGeometry, minShape);
                trimmedOffcuts[i] = trimmedOffcut;

                // stop parallel loop
                if (i >= offcutList.Count)
                {
                    state.Stop();
                    return;
                }

                if (state.IsStopped)
                    return;
            });

            // output data to new Offcut_GH list
            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();

            for (int i = 0; i < offcutList.Count; i++)
            {
                Offcut localOffcut = new Offcut(offcutList[i])
                {
                    OffcutGeometry = trimmedOffcuts[i],
                    FabVol = trimmedOffcuts[i].GetVolume(0.0001, 0.0001),
                    MovedAveragePlane = movedAveragePlanes[i]
                };

                Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                offcutGHList.Add(offcutGH);
            }

            // access output parameters
            DA.SetDataList(0, offcutGHList);
        }


        //------------------------------------------------------------
        // OffcutIntersection method
        //------------------------------------------------------------
        protected Brep OffcutIntersection(Brep offcut, Brep minShape)
        {
            // check if Offcut orientation is outward and create a boolean intersection
            if (offcut.SolidOrientation == BrepSolidOrientation.Outward)
            {
                Brep trimmedOffcut = Brep.CreateBooleanIntersection(offcut, minShape, 0.0001)[0];
                trimmedOffcut.Faces.SplitKinkyFaces(0.0001);

                return trimmedOffcut;
            }
            else
            {
                offcut.Flip();
                Brep trimmedOffcut = Brep.CreateBooleanIntersection(offcut, minShape, 0.0001)[0];
                trimmedOffcut.Faces.SplitKinkyFaces(0.0001);

                return trimmedOffcut;
            }
        }


        //------------------------------------------------------------
        // CreateMinShape method
        //------------------------------------------------------------
        protected Brep CreateMinShape(Curve crv, List<Offcut> offcutData, double scale, out List<Plane> movedAveragePlanes, out double[] dimensions)
        {
            // call MinimumDimensions method
            dimensions = Utility.MinimumDimensions(offcutData);

            // get new moved average planes
            movedAveragePlanes = GetMinAvergePlane(offcutData, dimensions);

            // store all the curves (sweep cross-sections) in a list
            List<Curve> baseCurves = new List<Curve>();

            // store all the planes in a list
            List<Plane> planeList = new List<Plane>();

            for (int i = 0; i < offcutData.Count; i++)
            {
                if (i == 0)
                {
                    planeList.Add(offcutData[i].FirstPlane);
                    planeList.Add(offcutData[i].AveragePlane);
                }
                else if (i == offcutData.Count - 1)
                {
                    planeList.Add(offcutData[i].AveragePlane);
                    planeList.Add(offcutData[i].SecondPlane);
                }
                else
                {
                    planeList.Add(offcutData[i].AveragePlane);
                }
            }

            // create all the rectangles based on the planes
            for (int i = 0; i < planeList.Count; i++)
            {
                // create rectangle at start of the curve
                Rectangle3d baseRect = Offcut.GetOffcutBase(dimensions[0], dimensions[1], planeList[i], offcutData[0].PositionIndex);

                // scale the rectangle base a bit down to avoid coincidial faces
                baseRect.Transform(Transform.Scale(baseRect.Center, scale));   /// scaled down so that the faces are not coincident, since this
                                                                              /// might cause the boolean operation to fail; it also ensures a smooth shape
                baseCurves.Add(baseRect.ToNurbsCurve());
            }

            // sweep rectangle along curve and close Brep
            SweepOneRail newSweep = new SweepOneRail();
            Brep minShape = newSweep.PerformSweep(crv, baseCurves)[0];

            // close brep and split kinky faces
            Brep closedMinShape = minShape.CapPlanarHoles(0.0001);
            closedMinShape.Faces.SplitKinkyFaces(0.0001);

            // check if solid orientation is fine and return
            if (BrepSolidOrientation.Outward == closedMinShape.SolidOrientation)
                return closedMinShape;
            else
            {
                closedMinShape.Flip();
                return closedMinShape;
            }
        }


        //------------------------------------------------------------
        // GetMinAveragePlane method
        //------------------------------------------------------------
        protected List<Plane> GetMinAvergePlane(List<Offcut> offcutData, double[] dimensions)
        {
            List<Plane> planeList = new List<Plane>();
            
            // adjust the origin of the average planes
            for (int i = 0; i < offcutData.Count; i++)
            {
                Plane movedAvrgPlane = new Plane(offcutData[i].AveragePlane);
                Rectangle3d avrgPosition = Offcut.GetOffcutBase(dimensions[0], dimensions[1], movedAvrgPlane, offcutData[i].PositionIndex);

                movedAvrgPlane.Origin = avrgPosition.Center;

                planeList.Add(movedAvrgPlane);
            }

            // return list of planes
            return planeList;
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_Unification;

        // component giud
        public override Guid ComponentGuid => new Guid("154DE293-E401-4190-AC9C-6225BA2726E0");
    }
}