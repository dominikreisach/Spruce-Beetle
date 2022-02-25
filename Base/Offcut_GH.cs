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
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using Rhino.Geometry;


namespace SpruceBeetle
{
    public class Offcut_GH : GH_Goo<Offcut>
    {
        //------------------------------------------------------------
        // constructors
        //------------------------------------------------------------
        public Offcut_GH() : this(null) {}
        
        public Offcut_GH(Offcut native)
        {
            this.Value = native;
        }

        public override IGH_Goo Duplicate()
        {
            if (Value == null)
                return new Offcut_GH();
            else
                return new Offcut_GH(Value.Duplicate());
        }

        public static Offcut ParseOffcut(object obj)
        {
            if (obj is Offcut_GH)
                return (obj as Offcut_GH).Value;
            else
                return obj as Offcut;
        }

        public override string ToString()
        {
            if (Value == null) return "Null Offcut";
            return Value.ToString();
        }

        public override string TypeName => "Offcut";
        public override string TypeDescription => "Offcut Data Type";
        public override object ScriptVariable() => Value;

        public override bool IsValid
        {
            get
            {
                if (Value == null) return false;
                return true;
            }
        }

        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null) return "No data";
                return string.Empty;
            }
        }


        //------------------------------------------------------------
        // casting
        //------------------------------------------------------------
        public override bool CastFrom(object source)
        {
            if (source == null)
                return false;

            if (source is Offcut offcut)
            {
                Value = offcut;
                return true;
            }

            if (source is Offcut_GH offcutGH)
            {
                Value = offcutGH.Value;
                return true;
            }

            return false;
        }


        public override bool CastTo<Q>(ref Q target)
        {
            if (Value == null)
                return false;

            if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
            {
                object brep = new GH_Brep(Value.OffcutGeometry);

                target = (Q)brep;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(Offcut)))
            {
                object offcut = Value;
                target = (Q)offcut;
                return true;
            }

            return false;
        }


        //------------------------------------------------------------
        // (de)serialization
        //------------------------------------------------------------
        public override bool Write(GH_IWriter writer)
        {
            if (Value == null) return false;

            byte[] brepBytes = GH_Convert.CommonObjectToByteArray(Value.OffcutGeometry);

            writer.SetByteArray("ocb", brepBytes);
            writer.SetDouble("i", Value.Index);
            writer.SetDouble("x", Value.X);
            writer.SetDouble("y", Value.Y);
            writer.SetDouble("z", Value.Z);
            writer.SetDouble("vol", Value.Vol);
            writer.SetDouble("fabvol", Value.FabVol);
            writer.SetPlane("fp", new GH_IO.Types.GH_Plane(Value.FirstPlane.OriginX, Value.FirstPlane.OriginY, Value.FirstPlane.OriginZ, Value.FirstPlane.XAxis.X, Value.FirstPlane.XAxis.Y, Value.FirstPlane.XAxis.Z, Value.FirstPlane.YAxis.X, Value.FirstPlane.YAxis.Y, Value.FirstPlane.YAxis.Z));
            writer.SetPlane("sp", new GH_IO.Types.GH_Plane(Value.SecondPlane.OriginX, Value.SecondPlane.OriginY, Value.SecondPlane.OriginZ, Value.SecondPlane.XAxis.X, Value.SecondPlane.XAxis.Y, Value.SecondPlane.XAxis.Z, Value.SecondPlane.YAxis.X, Value.SecondPlane.YAxis.Y, Value.SecondPlane.YAxis.Z));
            writer.SetPlane("ap", new GH_IO.Types.GH_Plane(Value.AveragePlane.OriginX, Value.AveragePlane.OriginY, Value.AveragePlane.OriginZ, Value.AveragePlane.XAxis.X, Value.AveragePlane.XAxis.Y, Value.AveragePlane.XAxis.Z, Value.AveragePlane.YAxis.X, Value.AveragePlane.YAxis.Y, Value.AveragePlane.YAxis.Z));
            writer.SetPlane("map", new GH_IO.Types.GH_Plane(Value.MovedAveragePlane.OriginX, Value.MovedAveragePlane.OriginY, Value.MovedAveragePlane.OriginZ, Value.MovedAveragePlane.XAxis.X, Value.MovedAveragePlane.XAxis.Y, Value.MovedAveragePlane.XAxis.Z, Value.MovedAveragePlane.YAxis.X, Value.MovedAveragePlane.YAxis.Y, Value.MovedAveragePlane.YAxis.Z));
            writer.SetPlane("bp", new GH_IO.Types.GH_Plane(Value.BasePlane.OriginX, Value.BasePlane.OriginY, Value.BasePlane.OriginZ, Value.BasePlane.XAxis.X, Value.BasePlane.XAxis.Y, Value.BasePlane.XAxis.Z, Value.BasePlane.YAxis.X, Value.BasePlane.YAxis.Y, Value.BasePlane.YAxis.Z));
            writer.SetInt32("pi", Value.PositionIndex);

            return true;
        }


        public override bool Read(GH_IReader reader)
        {
            if (!reader.ItemExists("ocb"))
            {
                Value = null;
                throw new Exception("Could not retrieve brepBytes!");
            }

            byte[] brepBytes = reader.GetByteArray("ocb");

            Brep offcutBrep = GH_Convert.ByteArrayToCommonObject<Brep>(brepBytes);
            if (offcutBrep == null)
                throw new Exception("Failed to convert to Brep!");

            double index = reader.GetDouble("i");
            double x = reader.GetDouble("x");
            double y = reader.GetDouble("y");
            double z = reader.GetDouble("z");
            double vol = reader.GetDouble("vol");
            double fabvol = reader.GetDouble("fabvol");

            GH_IO.Types.GH_Plane ghfp = reader.GetPlane("fp");
            Plane fp = new Plane(
                new Point3d(ghfp.Origin.x, ghfp.Origin.y, ghfp.Origin.z),
                new Vector3d(ghfp.XAxis.x, ghfp.XAxis.y, ghfp.XAxis.z),
                new Vector3d(ghfp.YAxis.x, ghfp.YAxis.y, ghfp.YAxis.z));

            GH_IO.Types.GH_Plane ghsp = reader.GetPlane("sp");
            Plane sp = new Plane(
                new Point3d(ghsp.Origin.x, ghsp.Origin.y, ghsp.Origin.z),
                new Vector3d(ghsp.XAxis.x, ghsp.XAxis.y, ghsp.XAxis.z),
                new Vector3d(ghsp.YAxis.x, ghsp.YAxis.y, ghsp.YAxis.z));

            GH_IO.Types.GH_Plane ghap = reader.GetPlane("ap");
            Plane ap = new Plane(
                new Point3d(ghap.Origin.x, ghap.Origin.y, ghap.Origin.z),
                new Vector3d(ghap.XAxis.x, ghap.XAxis.y, ghap.XAxis.z),
                new Vector3d(ghap.YAxis.x, ghap.YAxis.y, ghap.YAxis.z));

            GH_IO.Types.GH_Plane ghmap = reader.GetPlane("map");
            Plane map = new Plane(
                new Point3d(ghmap.Origin.x, ghmap.Origin.y, ghmap.Origin.z),
                new Vector3d(ghmap.XAxis.x, ghap.XAxis.y, ghmap.XAxis.z),
                new Vector3d(ghmap.YAxis.x, ghmap.YAxis.y, ghmap.YAxis.z));

            GH_IO.Types.GH_Plane ghbp = reader.GetPlane("bp");
            Plane bp = new Plane(
                new Point3d(ghbp.Origin.x, ghbp.Origin.y, ghbp.Origin.z),
                new Vector3d(ghbp.XAxis.x, ghbp.XAxis.y, ghbp.XAxis.z),
                new Vector3d(ghbp.YAxis.x, ghbp.YAxis.y, ghbp.YAxis.z));

            int pi = reader.GetInt32("pi");

            Value = Offcut.CreateOffcut(offcutBrep, index, x, y, z, vol, fabvol, fp, sp, ap, map, bp, pi);

            if (Value == null)
                throw new Exception("Something went wrong down the road :(");
            else
                return true;
        }
    }
}