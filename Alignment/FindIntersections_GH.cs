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

/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017 Tom Svilans
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/*
 * The base code is derived from the following repository:
 * https://github.com/tsvilans/tas
 * Made some minor adjustments to fit the needs of this project.
 */


using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;


namespace SpruceBeetle.Alignment
{
    public class FindIntersections_GH : GH_Component
    {
        public FindIntersections_GH()
          : base("Find Intersections", "Intersect", "Find intersections of the alignment curves", "Spruce Beetle", "    Alignment")
        {
        }

        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Base curves of the alignments", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "Intersection tolerance, default = 0.05", GH_ParamAccess.item, 0.05);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }

        
        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Indices", "I", "Indicies of connected beams", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Intersection Type", "IT", "Intersection type: 0 = V, 1 = X, 2 = T", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Parameter", "t", "t on curve where connection occurs", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curves", "C", "Curves that were included in the calculation", GH_ParamAccess.list);
            pManager.AddPointParameter("Intersection Points", "IntP", "Points at curve intersections", GH_ParamAccess.tree);

            pManager.HideParameter(3);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Curve> curveList = new List<Curve>();
            double tolerance = 0.05;

            // access input parameters
            if (!DA.GetDataList(0, curveList)) return;
            if (!DA.GetData(1, ref tolerance)) return;

            // find connections from curve list
            FindIntersection(curveList, tolerance, out DataTree<int> indices, out DataTree<int> intersectionTypes, out DataTree<double> parameters, out DataTree<Point3d> interPts);

            DA.SetDataTree(0, indices);
            DA.SetDataTree(1, intersectionTypes);
            DA.SetDataTree(2, parameters);
            DA.SetDataList(3, curveList);
            DA.SetDataTree(4, interPts);
        }


        //------------------------------------------------------------
        // FindIntersection method
        //------------------------------------------------------------
        protected void FindIntersection(List<Curve> curves, double tolerance, out DataTree<int> indices, out DataTree<int> intersectionTypes, out DataTree<double> parameters, out DataTree<Point3d> interPts)
        {
            // create new empty data trees to store output
            indices = new DataTree<int>();
            intersectionTypes = new DataTree<int>();
            parameters = new DataTree<double>();
            interPts = new DataTree<Point3d>();

            // set intersection count to 0, will be updated
            int intersectionCount = 0;

            // initialise empty int and double variables
            int type0, type1, jointType;
            double t0, t1;


            for (int i = 0; i < curves.Count; i++)
            {
                // first path
                GH_Path firstPath = new GH_Path(i);

                for (int j = i + 1; j < curves.Count; j++)
                {
                    // second path
                    GH_Path secondPath = new GH_Path(j);

                    // intersection event between the curves
                    CurveIntersections intersections = Intersection.CurveCurve(curves[i], curves[j], tolerance, 0.00001);

                    // check if there is any intersection at all
                    if (intersections == null || intersections.Count < 1)
                        continue;

                    foreach (var curveInt in intersections)
                    {
                        // increment intersection count
                        intersectionCount++;

                        // check intersection type
                        type0 = CheckVXT(curves[i], curveInt.ParameterA);
                        type1 = CheckVXT(curves[j], curveInt.ParameterB);

                        // get parameters t for each curve
                        t0 = curveInt.ParameterA;
                        t1 = curveInt.ParameterB;

                        // find intersection point on each curve
                        Point3d firstPt = curves[i].PointAt(t0);
                        Point3d secondPt = curves[j].PointAt(t1);

                        // if curves have a V-intersection
                        if (type0 == 0 && type1 == 0)
                            jointType = 0;

                        // if curves have an X-intersection
                        else if (type0 == 1 && type1 == 1)
                            jointType = 1;

                        // if curves have a T-intersection
                        else
                            jointType = 2;

                        // add data to respective path of the data trees
                        indices.Add(j, firstPath);
                        parameters.Add(t0, firstPath);
                        intersectionTypes.Add(jointType, firstPath);
                        interPts.Add(firstPt, firstPath);

                        indices.Add(i, secondPath);
                        parameters.Add(t1, secondPath);
                        intersectionTypes.Add(jointType, secondPath);
                        interPts.Add(secondPt, secondPath);
                    }
                }
            }
            // message of how many intersection have been found
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Found {intersectionCount} intersections.");
        }


        //------------------------------------------------------------
        // CheckVXT method
        //------------------------------------------------------------
        protected int CheckVXT(Curve crv, double t, double endOffset = 0.05)
        {
            if (!crv.Domain.IncludesParameter(t))
                return -1;

            double length = crv.GetLength(new Interval(crv.Domain.Min, t));
            double crvLength = crv.GetLength();

            // return V
            if (length < endOffset || (crvLength - length) < endOffset)
                return 0;
            // return X
            else
                return 1;
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_FindIntersections;

        // component giud
        public override Guid ComponentGuid => new Guid("6A455267-5097-446F-97FC-BEEDB5E4724E");
    }
}