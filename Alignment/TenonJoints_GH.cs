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
using Grasshopper.Kernel.Special;
using Rhino.Geometry;


namespace SpruceBeetle.Alignment
{
    public class TenonJoints_GH : GH_Component
    {
        public TenonJoints_GH()
          : base("Tenon Joints", "Tenon", "Create tenon joints between the aligned Offcuts", "Spruce Beetle", "    Alignment")
        {
        }


        // value list
        GH_ValueList valueList = null;
        IGH_Param parameter = null;


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "List of aligned Offcuts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Radius", "R", "Radius of the milling tool", GH_ParamAccess.item, 0.005);
            pManager.AddNumberParameter("Joint X", "JX", "Joint dimension in X direction", GH_ParamAccess.item, 0.02);
            pManager.AddNumberParameter("Joint Y", "JY", "Joint dimension in Y direction", GH_ParamAccess.item, 0.05);
            pManager.AddNumberParameter("Joint Z", "JZ", "Joint dimension in Z direction", GH_ParamAccess.item, 0.04);
            pManager.AddTextParameter("Joint Type", "JT", "Adds the specified joint type", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Tenon Count", "TC", "The number of tenons to be created", GH_ParamAccess.item, 1);

            //pManager.AddCurveParameter("Custom Shape", "CS", "Creates a custom tenon from the specifc shape of a closed planar curve", GH_ParamAccess.item);
            //pManager[7].Optional = true;
            
            // pManager[7].Optional = true;

            parameter = pManager[5];

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


        // create value list
        protected override void BeforeSolveInstance()
        {
            if (valueList == null)
            {
                if (parameter.Sources.Count == 0)
                    valueList = new GH_ValueList();
                else
                {
                    foreach (var source in parameter.Sources)
                    {
                        if (source is GH_ValueList)
                            valueList = source as GH_ValueList;

                        return;
                    }
                }

                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 200, this.Attributes.Pivot.Y - 0);
                valueList.ListItems.Clear();

                List<string> jointTypes = Joint.TenonTypes();

                foreach (string param in jointTypes)
                    valueList.ListItems.Add(new GH_ValueListItem(param, $"\"{param}\""));

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
            }
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> alignedOffcuts = new List<Offcut>();
            double toolRadius = 0.005;
            double jointX = 0.0;
            double jointY = 0.0;
            double jointZ = 0.0;
            string jointKey = "";
            int tenonCount = 1;
            Curve jointShape = null;

            // access input parameters
            if (!DA.GetDataList(0, alignedOffcuts)) return;
            if (!DA.GetData(1, ref toolRadius)) return;
            if (!DA.GetData(2, ref jointX)) return;
            if (!DA.GetData(3, ref jointY)) return;
            if (!DA.GetData(4, ref jointZ)) return;
            if (!DA.GetData(5, ref jointKey)) return;
            if (!DA.GetData(6, ref tenonCount)) return;
            //if (!DA.GetData(7, ref jointShape)) return;

            //// check if the curve is closed and planar
            //if (jointShape != null)
            //{
            //    if (!jointShape.IsClosed)
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve is not closed!");
            //    else if (!jointShape.IsPlanar())
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve is not planar!");
            //}

            // get joint type
            Dictionary<string, int> jointDict = Joint.GetJointType();
            int jointType = jointDict[jointKey];

            // initialise lists to store all the data
            Brep[] outputOffcuts = new Brep[alignedOffcuts.Count];
            Brep[,] outputJoints = new Brep[alignedOffcuts.Count, tenonCount];
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
                    CreateTenons(alignedOffcuts[i].SecondPlane, jointX, jointY, jointZ, toolRadius, secondMin, alignedOffcuts[i].PositionIndex, jointType, tenonCount, jointShape, out Brep[] joints);

                    // call CutOffcut method
                    Brep cutOffcut = Joint.CutOffcut(joints, alignedOffcuts[i].OffcutGeometry);

                    // add data to the Offcut
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);

                    // output joint data
                    outputJointVol[i] = joints[0].GetVolume(0.0001, 0.0001) * tenonCount;

                    for (int j = 0; j < joints.Length; j++)
                    {
                        outputJoints[i, j] = joints[j];
                    }
                }

                else if (i == alignedOffcuts.Count - 1)
                {
                    // call CreateJoints method
                    CreateTenons(alignedOffcuts[i].FirstPlane, jointX, jointY, jointZ, toolRadius, firstMin, alignedOffcuts[i].PositionIndex, jointType, tenonCount, jointShape, out Brep[] joints);

                    // call CutOffcut method
                    Brep cutOffcut = Joint.CutOffcut(joints, alignedOffcuts[i].OffcutGeometry);

                    // add data to the Offcut
                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }

                else
                {
                    // call CreateJoints method for both ends of the Offcuts
                    CreateTenons(alignedOffcuts[i].FirstPlane, jointX, jointY, jointZ, toolRadius, firstMin, alignedOffcuts[i].PositionIndex, jointType, tenonCount, jointShape, out Brep[] firstJoints);
                    CreateTenons(alignedOffcuts[i].SecondPlane, jointX, jointY, jointZ, toolRadius, secondMin, alignedOffcuts[i].PositionIndex, jointType, tenonCount, jointShape, out Brep[] secondJoints);

                    // add all joints to one single array
                    Brep[] cutterBreps = new Brep[tenonCount * 2];

                    // output joint data and add joints to brep array
                    outputJointVol[i] = secondJoints[0].GetVolume(0.0001, 0.0001) * tenonCount;

                    // add all joints to one single array
                    for (int j = 0; j < secondJoints.Length; j++)
                    {
                        cutterBreps[j] = firstJoints[j];
                        cutterBreps[j + tenonCount] = secondJoints[j];
                        outputJoints[i, j] = secondJoints[j];
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
        // CreateTenons method
        //------------------------------------------------------------
        protected void CreateTenons(Plane plane, double jointX, double jointY, double jointZ, double toolRadius, double[] minValue, int positionIndex, int jointType, int tenonCount, Curve jointShape, out Brep[] returnJoints)
        {
            // initialise new plane
            Plane basePlane = new Plane(plane);

            // setting new origin point for the planes
            Rectangle3d originRect = Offcut.GetOffcutBase(minValue[0], minValue[1], basePlane, positionIndex);

            // change origin of planes
            basePlane.Origin = originRect.Center;

            // create joints according to the joint type
            switch (jointType)
            {
                case 0:
                    {
                        RectJoint(basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;

                case 1:
                    {
                        CrossJoint(basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;

                case 2:
                    {
                        CreateCustomTenon(jointShape, basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;

                default:
                    {
                        RectJoint(basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;
            }
        }


        //------------------------------------------------------------
        // RectJoint method
        //------------------------------------------------------------
        protected void RectJoint(Plane plane, double jointX, double jointY, double jointZ, double[] minValue, double toolRadius, int tenonCount, out Brep[] joints)
        {
            // initialise empty brep variable
            joints = new Brep[tenonCount];

            // joint dimensions
            Interval dX = new Interval(-jointX / 2, jointX / 2);
            Interval dY = new Interval(-jointY / 2, jointY / 2);
            double dZ = jointZ;

            // assign plane
            Plane basePlane = plane;

            // move basePlane for centered extrusion
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * dZ / 2));

            // base points for the tenons
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], tenonCount);

            // create the tenon joints
            for (int i = 0; i < divPts.Length; i++)
            {
                // set new base for origin
                basePlane.Origin = divPts[i];

                // create first and second rectangle
                Rectangle3d baseRect = new Rectangle3d(basePlane, dY, dX);
                Curve baseCurve = Curve.CreateFilletCornersCurve(baseRect.ToNurbsCurve(), toolRadius, 0.0001, 0.0001);

                // extrude first base to create first joint
                Brep joint = Extrusion.Create(baseCurve, dZ, true).ToBrep();

                // clean-up
                joint.Faces.SplitKinkyFaces(0.0001);
                if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                    joint.Flip();

                joints[i] = joint;
            }
        }


        //------------------------------------------------------------
        // CrossJoint method
        //------------------------------------------------------------
        protected void CrossJoint(Plane plane, double jointX, double jointY, double jointZ, double[] minValue, double toolRadius, int tenonCount, out Brep[] joints)
        {
            // initialise empty brep variable
            joints = new Brep[tenonCount];

            // joint dimensions
            Interval dX = new Interval(-jointX / 2, jointX / 2);
            Interval dY1 = new Interval(-jointY / 2, jointY / 2);
            Interval dY2 = new Interval(-jointY / 3, jointY / 3);
            double dZ = jointZ;

            // assign plane
            Plane basePlane = new Plane(plane);

            // move basePlane for centered extrusion
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * dZ / 2));

            // base points for the tenons
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], tenonCount);

            // create the tenon joints
            for (int i = 0; i < divPts.Length; i++)
            {
                // set new base for origin
                basePlane.Origin = divPts[i];

                // create rectangles
                Rectangle3d firstRect = new Rectangle3d(basePlane, dY1, dX);
                Rectangle3d secondRect = new Rectangle3d(basePlane, dX, dY2);

                List<Curve> rectList = new List<Curve>
                {
                    firstRect.ToNurbsCurve(),
                    secondRect.ToNurbsCurve()
                };

                // join and fillet cross
                Curve cross = Curve.CreateBooleanUnion(rectList, 0.00001)[0];
                Curve baseCurve = Curve.CreateFilletCornersCurve(cross, toolRadius, 0.00001, 0.00001);

                // extrude first base to create first joint
                Brep joint = Extrusion.Create(baseCurve, dZ, true).ToBrep();

                // clean-up
                joint.Faces.SplitKinkyFaces(0.0001);
                if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                    joint.Flip();

                joints[i] = joint;
            }
        }


        //------------------------------------------------------------
        // CreateCustomTenon method
        //------------------------------------------------------------
        protected void CreateCustomTenon(Curve shape, Plane plane, double jointX, double jointY, double jointZ, double[] minValue, double toolRadius, int tenonCount, out Brep[] joints)
        {
            // initialise empty brep variable
            joints = new Brep[tenonCount];

            Curve jointShape = shape.DuplicateCurve();

            // get amp from curve
            var curveAMP = AreaMassProperties.Compute(jointShape);

            // new xy plane with curve centroid as origin
            Point3d curveCenter = curveAMP.Centroid;
            Plane curvePlane = new Plane(curveCenter, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));

            // assign new plane
            Plane basePlane = new Plane(plane);

            // scale jointShape curve down
            Box box = new Box(jointShape.GetBoundingBox(true));
            jointShape.Transform(Transform.Scale(curvePlane, 1 / box.X.Length * jointX / 2, 1 / box.Y.Length * jointY / 2, 1));

            // z-value
            double dZ = jointZ;

            // move plane a bit for extrusion
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * dZ / 2));

            // base points for the dovetails
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], tenonCount);

            // create the tenon joints
            for (int i = 0; i < divPts.Length; i++)
            {
                // set new base for origin
                basePlane.Origin = divPts[i];

                // orient curvePlane to the base plane
                jointShape.DuplicateCurve().Transform(Transform.PlaneToPlane(curvePlane, basePlane));

                if (!jointShape.IsClosed)
                    jointShape.MakeClosed(0.0001);

                // fillet base curve
                Curve baseCurve = Curve.CreateFilletCornersCurve(jointShape, toolRadius, 0.0001, 0.0001);

                // extrude first base to create first joint
                Brep joint = Extrusion.Create(baseCurve, dZ, true).ToBrep();

                // clean-up
                joint.Faces.SplitKinkyFaces(0.0001);
                if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                    joint.Flip();

                joints[i] = joint;
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_TenonJoints;

        // component giud
        public override Guid ComponentGuid => new Guid("0CAE8FA6-7CFE-4C2E-8E19-AE5F005EF8FB");
    }
}