///*
// * MIT License
// * 
// * Copyright (c) 2022 Dominik Reisach
// * 
// * Permission is hereby granted, free of charge, to any person obtaining a copy
// * of this software and associated documentation files (the "Software"), to deal
// * in the Software without restriction, including without limitation the rights
// * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// * copies of the Software, and to permit persons to whom the Software is
// * furnished to do so, subject to the following conditions:
// * 
// * The above copyright notice and this permission notice shall be included in all
// * copies or substantial portions of the Software.
// * 
// * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// * SOFTWARE.
// */


//using System;
//using System.Collections.Generic;
//using Grasshopper.Kernel;
//using Rhino.Geometry;


//namespace SpruceBeetle.Alignment
//{
//    public class CustomJoints : GH_Component
//    {
//        public CustomJoints()
//          : base("Custom Joints", "MyJoints", "Create custom tenon joints between the aligned Offcuts", "Spruce Beetle", "    Alignment")
//        {
//        }


//        // parameter inputs
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "List of aligned Offcuts", GH_ParamAccess.list);
//            pManager.AddNumberParameter("Tool Radius", "R", "Radius of the milling tool", GH_ParamAccess.item, 0.005);
//            pManager.AddNumberParameter("Joint X", "JX", "Joint dimension in X direction", GH_ParamAccess.item, 0.04);
//            pManager.AddNumberParameter("Joint Y", "JY", "Joint dimension in Y direction", GH_ParamAccess.item, 0.02);
//            pManager.AddNumberParameter("Joint Z", "JZ", "Joint dimension in Z direction", GH_ParamAccess.item, 0.04);
//            pManager.AddCurveParameter("Joint Shape", "JS", "Creates a joint from the specifc shape of a closed planar curve", GH_ParamAccess.item);

//            for (int i = 0; i < pManager.ParamCount; i++)
//                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
//        }


//        // parameter outputs
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after joint intersection", GH_ParamAccess.list);
//            pManager.AddBrepParameter("Joints", "J", "The cutting and joining geometry", GH_ParamAccess.list);
//            pManager.AddNumberParameter("Joint Volume", "JV", "Volume of the joints", GH_ParamAccess.list);

//            pManager.HideParameter(1);

//            for (int i = 0; i < pManager.ParamCount; i++)
//                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
//        }


//        // main
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            // variables to reference the input parameters to
//            List<Offcut> alignedOffcuts = new List<Offcut>();
//            double toolRadius = 0.005;
//            double jointX = 0.0;
//            double jointY = 0.0;
//            double jointZ = 0.0;
//            Curve jointShape = null;

//            // access input parameters
//            if (!DA.GetDataList(0, alignedOffcuts)) return;
//            if (!DA.GetData(1, ref toolRadius)) return;
//            if (!DA.GetData(2, ref jointX)) return;
//            if (!DA.GetData(3, ref jointY)) return;
//            if (!DA.GetData(4, ref jointZ)) return;
//            if (!DA.GetData(5, ref jointShape)) return;

//            // check if the curve is closed and planar
//            if (!jointShape.IsClosed)
//                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve is not closed!");
//            else if (!jointShape.IsPlanar())
//                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve is not planar!");

//            // initialise lists to store all the data
//            Brep[] outputOffcuts = new Brep[alignedOffcuts.Count];
//            Brep[] outputJoints = new Brep[alignedOffcuts.Count];
//            double[] outputOffcutVol = new double[alignedOffcuts.Count];
//            double[] outputJointVol = new double[alignedOffcuts.Count];

//            // parallel computed joints with boolean difference
//            System.Threading.Tasks.Parallel.For(0, alignedOffcuts.Count, (i, state) =>
//            {
//                // get the base for the joint position of each Offcut
//                List<double[]> minimumDimensions = Utility.GetMinimumDimension(alignedOffcuts, i);
//                double[] firstMin = minimumDimensions[0];
//                double[] secondMin = minimumDimensions[1];

//                // create joints with their respective volumes according to the joint type
//                if (i == 0)
//                {
//                    // call CreateCustomTenon method
//                    CreateCustomTenon(jointShape, alignedOffcuts[i].SecondPlane, jointX, jointY, jointZ, toolRadius, secondMin, alignedOffcuts[i].PositionIndex, out Brep joint);

//                    // add all joints to one single array
//                    Brep[] cutterBreps = new Brep[] { joint };

//                    // call CutOffcut method
//                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, alignedOffcuts[i].OffcutGeometry);

//                    // add data to the Offcut
//                    outputOffcuts[i] = cutOffcut;
//                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
//                    outputJoints[i] = joint;
//                    outputJointVol[i] = joint.GetVolume(0.0001, 0.0001);
//                }

//                else if (i == alignedOffcuts.Count - 1)
//                {
//                    // call CreateCustomTenon method
//                    CreateCustomTenon(jointShape, alignedOffcuts[i].FirstPlane, jointX, jointY, jointZ, toolRadius, firstMin, alignedOffcuts[i].PositionIndex, out Brep joint);

//                    // add all joints to one single array
//                    Brep[] cutterBreps = new Brep[] { joint };

//                    // call CutOffcut method
//                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, alignedOffcuts[i].OffcutGeometry);

//                    // add data to the Offcut
//                    outputOffcuts[i] = cutOffcut;
//                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
//                }

//                else
//                {
//                    // call CreateCustomTenon method
//                    CreateCustomTenon(jointShape, alignedOffcuts[i].FirstPlane, jointX, jointY, jointZ, toolRadius, firstMin, alignedOffcuts[i].PositionIndex, out Brep firstJoint);
//                    CreateCustomTenon(jointShape, alignedOffcuts[i].SecondPlane, jointX, jointY, jointZ, toolRadius, secondMin, alignedOffcuts[i].PositionIndex, out Brep secondJoint);

//                    // add all joints to one single array
//                    Brep[] cutterBreps = new Brep[] { firstJoint, secondJoint };

//                    // call CutOffcut method
//                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, alignedOffcuts[i].OffcutGeometry);

//                    // add data to the Offcut
//                    outputOffcuts[i] = cutOffcut;
//                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
//                    outputJoints[i] = secondJoint;
//                    outputJointVol[i] = secondJoint.GetVolume(0.0001, 0.0001);
//                }

//                // stop parallel loop
//                if (i >= alignedOffcuts.Count)
//                {
//                    state.Stop();
//                    return;
//                }

//                if (state.IsStopped)
//                    return;
//            });

//            // output data to new Offcut_GH list
//            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();

//            for (int i = 0; i < alignedOffcuts.Count; i++)
//            {
//                Offcut localOffcut = new Offcut(alignedOffcuts[i])
//                {
//                    OffcutGeometry = outputOffcuts[i],
//                    FabVol = outputOffcutVol[i]
//                };

//                Offcut_GH offcutGH = new Offcut_GH(localOffcut);
//                offcutGHList.Add(offcutGH);
//            }

//            // access output parameters
//            DA.SetDataList(0, offcutGHList);
//            DA.SetDataList(1, outputJoints);
//            DA.SetDataList(2, outputJointVol);
//        }


//        //------------------------------------------------------------
//        // CreateCustomTenon method
//        //------------------------------------------------------------
//        protected void CreateCustomTenon(Curve shape, Plane basePlane, double jointX, double jointY, double jointZ, double toolRadius, double[] minValue, int positionIndex, out Brep joint)
//        {
//            Curve jointShape = shape.DuplicateCurve();

//            // get amp from curve
//            var curveAMP = AreaMassProperties.Compute(jointShape);

//            // new xy plane with curve centroid as origin
//            Point3d curveCenter = curveAMP.Centroid;
//            Plane curvePlane = new Plane(curveCenter, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));

//            // assign new plane
//            Plane plane = new Plane(basePlane);

//            // scale jointShape curve down
//            Box box = new Box(jointShape.GetBoundingBox(true));
//            jointShape.Transform(Transform.Scale(curvePlane, 1 / box.X.Length * jointX / 2, 1 / box.Y.Length * jointY / 2, 1));

//            // z-value
//            double dZ = jointZ;

//            // setting new origin point for the plane
//            Rectangle3d baseRect = Offcut.GetOffcutBase(minValue[0], minValue[1], plane, positionIndex);
//            plane.Origin = baseRect.Center;

//            // move plane a bit for extrusion
//            plane.Transform(Transform.Translation(plane.ZAxis * dZ / 2));

//            // orient curvePlane to the base plane
//            jointShape.Transform(Transform.PlaneToPlane(curvePlane, plane));

//            if (!jointShape.IsClosed)
//                jointShape.MakeClosed(0.0001);

//            // fillet base curve
//            Curve baseCurve = Curve.CreateFilletCornersCurve(jointShape, toolRadius, 0.0001, 0.0001);

//            // extrude first base to create first joint
//            joint = Extrusion.Create(baseCurve, -dZ, true).ToBrep();

//            // clean-up
//            joint.Faces.SplitKinkyFaces(0.0001);
//            if (BrepSolidOrientation.Inward == joint.SolidOrientation)
//                joint.Flip();
//        }


//        //------------------------------------------------------------
//        // Else
//        //------------------------------------------------------------

//        // exposure property
//        public override GH_Exposure Exposure => GH_Exposure.secondary;

//        // add icon
//        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_CustomJoints;

//        // component giud
//        public override Guid ComponentGuid => new Guid("BA1B39A4-FAC4-4DCD-B929-369F339C8CA1");
//    }
//}