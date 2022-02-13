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
    public class SplineJoints_GH : GH_Component
    {
        public SplineJoints_GH()
          : base("Spline Joints", "Spline", "Create spline joints between the aligned Offcuts", "Spruce Beetle", "    Alignment")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "List of aligned Offcuts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Radius", "R", "Radius of the milling tool", GH_ParamAccess.item, 0.005);
            pManager.AddNumberParameter("Joint X", "JX", "Joint dimension in X direction", GH_ParamAccess.item, 0.02);
            pManager.AddNumberParameter("Joint Y", "JY", "Joint dimension in Y direction", GH_ParamAccess.item, 0.05);
            pManager.AddIntegerParameter("Spline Count", "SC", "The number of splines to be created", GH_ParamAccess.item, 1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after joint intersection", GH_ParamAccess.list);
            pManager.AddBrepParameter("Joints", "J", "The cutting and joining geometry", GH_ParamAccess.list);
            pManager.AddNumberParameter("Joint Volume", "JV", "Volume of the joints", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> alignedOffcuts = new List<Offcut>();
            double toolRadius = 0.005;
            double jointX = 0.0;
            double jointY = 0.0;
            int splineCount = 1;

            // access input parameters
            if (!DA.GetDataList(0, alignedOffcuts)) return;
            if (!DA.GetData(1, ref toolRadius)) return;
            if (!DA.GetData(2, ref jointX)) return;
            if (!DA.GetData(3, ref jointY)) return;
            if (!DA.GetData(4, ref splineCount)) return;

            // initialise lists to store all the data
            Brep[] outputOffcuts = new Brep[alignedOffcuts.Count];
            Brep[,] outputJoints = new Brep[alignedOffcuts.Count, splineCount];
            double[] outputOffcutVol = new double[alignedOffcuts.Count];
            double[] outputJointVol = new double[alignedOffcuts.Count];

            // parallel computed joints with boolean difference
            System.Threading.Tasks.Parallel.For(0, alignedOffcuts.Count, (i, state) =>
            {
                // get the base for the joint position of each Offcut
                List<double[]> minimumDimensions = Utility.GetMinimumDimension(alignedOffcuts, i);
                double[] firstMin = minimumDimensions[0];
                double[] secondMin = minimumDimensions[1];

                // create joints with their respective volumes according to the joint type
                if (i == 0)
                {
                    // call CreateJoints method
                    CreateSplines(alignedOffcuts[i].SecondPlane, jointX, jointY, toolRadius, secondMin, alignedOffcuts[i].Y, alignedOffcuts[i].PositionIndex, splineCount, out Brep[] joints, out Brep[] display);

                    // call CutOffcut method
                    Brep cutOffcut = Joint.CutOffcut(joints, alignedOffcuts[i].OffcutGeometry);

                    // add data to the Offcut
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);

                    // output joint data
                    outputJointVol[i] = display[0].GetVolume(0.0001, 0.0001) * splineCount;

                    for (int j = 0; j < joints.Length; j++)
                    {
                        outputJoints[i, j] = display[j];
                    }
                }

                else if (i == alignedOffcuts.Count - 1)
                {
                    // call CreateJoints method
                    CreateSplines(alignedOffcuts[i].FirstPlane, jointX, jointY, toolRadius, firstMin, alignedOffcuts[i].Y, alignedOffcuts[i].PositionIndex, splineCount, out Brep[] joints, out Brep[] display);

                    // call CutOffcut method
                    Brep cutOffcut = Joint.CutOffcut(joints, alignedOffcuts[i].OffcutGeometry);

                    // add data to the Offcut
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }

                else
                {
                    // call CreateJoints method for both ends of the Offcuts
                    CreateSplines(alignedOffcuts[i].FirstPlane, jointX, jointY, toolRadius, firstMin, alignedOffcuts[i].Y, alignedOffcuts[i].PositionIndex, splineCount, out Brep[] firstJoints, out Brep[] firstDisplay);
                    CreateSplines(alignedOffcuts[i].SecondPlane, jointX, jointY, toolRadius, secondMin, alignedOffcuts[i].Y, alignedOffcuts[i].PositionIndex, splineCount, out Brep[] secondJoints, out Brep[] secondDisplay);

                    // add all joints to one single array
                    Brep[] cutterBreps = new Brep[splineCount * 2];

                    // output joint data and add joints to brep array
                    outputJointVol[i] = secondDisplay[0].GetVolume(0.0001, 0.0001) * splineCount;

                    for (int j = 0; j < secondJoints.Length; j++)
                    {
                        cutterBreps[j] = firstJoints[j];
                        cutterBreps[j + splineCount] = secondJoints[j];
                        outputJoints[i, j] = secondDisplay[j];
                    }

                    // call CutOffcut method
                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, alignedOffcuts[i].OffcutGeometry);

                    // add data to the Offcut
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }

                // stop parallel loop
                if (i >= alignedOffcuts.Count)
                {
                    state.Stop();
                    return;
                }

                if (state.IsStopped)
                    return;
            });

            // output data to new Offcut_GH list
            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();

            for (int i = 0; i < alignedOffcuts.Count; i++)
            {
                Offcut localOffcut = new Offcut(alignedOffcuts[i])
                {
                    OffcutGeometry = outputOffcuts[i],
                    FabVol = outputOffcutVol[i]
                };

                Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                offcutGHList.Add(offcutGH);
            }

            // access output parameters
            DA.SetDataList(0, offcutGHList);
            DA.SetDataList(1, outputJoints);
            DA.SetDataList(2, outputJointVol);
        }


        //------------------------------------------------------------
        // CreateSplines method
        //------------------------------------------------------------
        protected void CreateSplines(Plane plane, double jointX, double jointY, double toolRadius, double[] minValue, double yDim, int positionIndex, int splineCount, out Brep[] returnJoint, out Brep[] displayJoint)
        {
            // initialise empty brep variable
            returnJoint = new Brep[splineCount];
            displayJoint = new Brep[splineCount];

            // initialise new plane
            Plane basePlane = new Plane(plane);

            // setting new origin point for the planes
            Rectangle3d originRect = Offcut.GetOffcutBase(minValue[0], minValue[1], basePlane, positionIndex);

            // change origin of planes
            basePlane.Origin = originRect.Center;

            // create joints according to the joint type
            // joint dimensions
            Interval dX = new Interval(-jointX / 2, jointX / 2);
            Interval dY = new Interval(-jointY / 2, jointY / 2);
            double dZ = yDim * 2;

            // rotate base plane
            double angle = Utility.ConvertToRadians(90);
            basePlane.Rotate(angle, basePlane.XAxis, basePlane.Origin);

            // move basePlane for centered extrusion
            basePlane.Transform(Transform.Translation(basePlane.ZAxis * minValue[1]));

            // base points for the splines
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], splineCount);

            // create the spline joints
            for (int i = 0; i < divPts.Length; i++)
            {
                if (positionIndex == 0 || positionIndex == 3 || positionIndex == 4)
                {
                    // set new base for origin
                    basePlane.Origin = divPts[i];

                    // create rectangle
                    Rectangle3d baseRect = new Rectangle3d(basePlane, dX, dY);
                    Curve baseCurve = DovetailRect(baseRect, basePlane, toolRadius);

                    // extrude first base to create first joint
                    Brep joint = Extrusion.Create(baseCurve, -dZ, true).ToBrep();

                    // cutter clean-up
                    joint.Faces.SplitKinkyFaces(0.0001);
                    if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                        joint.Flip();

                    // create display joints
                    basePlane.Transform(Transform.Translation(basePlane.ZAxis * -minValue[1] / 2));
                    Brep display = Extrusion.Create(DovetailRect(new Rectangle3d(basePlane, dX, dY), basePlane, toolRadius), -minValue[1], true).ToBrep();

                    // display clean-up
                    display.Faces.SplitKinkyFaces(0.0001);
                    if (BrepSolidOrientation.Inward == display.SolidOrientation)
                        display.Flip();

                    returnJoint[i] = joint;
                    displayJoint[i] = display;
                }
                else
                {
                    // set new base for origin
                    basePlane.Origin = divPts[i];

                    // create rectangle
                    Rectangle3d baseRect = new Rectangle3d(basePlane, dX, dY);
                    Curve baseCurve = DovetailRect(baseRect, basePlane, toolRadius);

                    // extrude first base to create first joint
                    Brep joint = Extrusion.Create(baseCurve, -minValue[1] * 2, true).ToBrep();

                    // cutter clean-up
                    joint.Faces.SplitKinkyFaces(0.0001);
                    if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                        joint.Flip();

                    // create display joints
                    basePlane.Transform(Transform.Translation(basePlane.ZAxis * -minValue[1] / 2));
                    Brep display = Extrusion.Create(DovetailRect(new Rectangle3d(basePlane, dX, dY), basePlane, toolRadius), -minValue[1], true).ToBrep();

                    // display clean-up
                    display.Faces.SplitKinkyFaces(0.0001);
                    if (BrepSolidOrientation.Inward == display.SolidOrientation)
                        display.Flip();

                    returnJoint[i] = joint;
                    displayJoint[i] = display;
                }
            }
        }


        //------------------------------------------------------------
        // DovtailRectangle
        //------------------------------------------------------------
        protected Curve DovetailRect(Rectangle3d rect, Plane plane, double toolRadius)
        {
            // list for all corner points of the rectangle
            List<Point3d> rectangleCorners = new List<Point3d>
            {
                rect.Corner(0),
                rect.Corner(1),
                rect.Corner(2),
                rect.Corner(3)
            };

            // first list for the dovetail
            List<Point3d> firstPoints = new List<Point3d>
            {
                rect.Corner(0),
                plane.Origin,
                rect.Corner(3)
            };

            // second list for the dovetail
            List<Point3d> secondPoints = new List<Point3d>
            {
                rect.Corner(1),
                plane.Origin,
                rect.Corner(2)
            };

            // create all lines and curves out of the points
            Line firstLine = new Line(rectangleCorners[0], rectangleCorners[1]);
            Curve firstCurve = firstLine.ToNurbsCurve();

            Curve secondCurve = Curve.CreateControlPointCurve(firstPoints);

            Line secondLine = new Line(rectangleCorners[2], rectangleCorners[3]);
            Curve thirdCurve = secondLine.ToNurbsCurve();

            Curve fourthCurve = Curve.CreateControlPointCurve(secondPoints);

            // append all curves to a list
            List<Curve> curveList = new List<Curve>
            {
                firstCurve,
                secondCurve,
                thirdCurve,
                fourthCurve
            };

            // join all curves and fillet them
            Curve joinedCurves = Curve.JoinCurves(curveList, 0.0001)[0];
            Curve filletCurve = Curve.CreateFilletCornersCurve(joinedCurves, toolRadius, 0.0001, 0.0001);

            // return curve
            return filletCurve;
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_SplineJoints;

        // component giud
        public override Guid ComponentGuid => new Guid("A5482B02-5A24-4A18-9BCB-0E0F92C7121C");
    }
}