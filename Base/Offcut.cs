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


using System.Collections.Generic;
using Rhino.Geometry;


namespace SpruceBeetle
{
    public class Offcut
    {
        public Brep OffcutGeometry { get; set; }
        public double Index { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Vol { get; set; }
        public double FabVol { get; set; }
        public Plane FirstPlane { get; set; }
        public Plane SecondPlane { get; set; }
        public Plane AveragePlane { get; set; }
        public Plane MovedAveragePlane { get; set; }
        public int PositionIndex { get; set; }


        //------------------------------------------------------------
        // Offcut constructors
        //------------------------------------------------------------

        // constructor with no argument
        protected Offcut() {}

        // constructor with offcut
        public Offcut(Offcut offcut)
        {
            OffcutGeometry = offcut.OffcutGeometry;
            Index = offcut.Index;
            X = offcut.X;
            Y = offcut.Y;
            Z = offcut.Z;
            Vol = offcut.Vol;
            FabVol = offcut.FabVol;
            FirstPlane = offcut.FirstPlane;
            SecondPlane = offcut.SecondPlane;
            AveragePlane = offcut.AveragePlane;
            MovedAveragePlane = offcut.MovedAveragePlane;
            PositionIndex = offcut.PositionIndex;
        }

        public Offcut(Brep offcut)
        {
            OffcutGeometry = offcut;
        }

        // constructor with index and dimensions
        public Offcut(double index, double x, double y, double z)
        {
            Index = index;
            X = x;
            Y = y;
            Z = z;
            Vol = x * y * z;
        }

        // constructor with all data
        public Offcut (Brep offcut, double index, double x, double y, double z, double vol, double fabvol, Plane fp, Plane sp, Plane ap, Plane map, int pi)
        {
            OffcutGeometry = offcut;
            Index = index;
            X = x;
            Y = y;
            Z = z;
            Vol = vol;
            FabVol = fabvol;
            FirstPlane = fp;
            SecondPlane = sp;
            AveragePlane = ap;
            MovedAveragePlane = map;
            PositionIndex = pi;
        }


        //------------------------------------------------------------
        // CreateOffcut methods
        //------------------------------------------------------------
        public static Offcut CreateOffcut(Brep offcut, double index, double x, double y, double z, double vol, double fabvol, Plane fp, Plane sp, Plane ap, Plane map, int pi)
        {
            Offcut createdOffcut = new Offcut(offcut, index, x, y, z, vol, fabvol, fp, sp, ap, map, pi);
            
            return createdOffcut;
        }


        public static Offcut CreateOffcutFromData(double index, double x, double y, double z)
        {
            Offcut createdOffcut = new Offcut(index, x, y, z);

            return createdOffcut;
        }


        //------------------------------------------------------------
        // Duplicate method
        //------------------------------------------------------------
        public Offcut Duplicate()
        {
            Offcut offcut = new Offcut
            {
                OffcutGeometry = OffcutGeometry,
                Index = Index,
                X = X,
                Y = Y,
                Z = Z,
                Vol = Vol,
                FabVol = FabVol,
                FirstPlane = FirstPlane,
                SecondPlane = SecondPlane,
                AveragePlane = AveragePlane,
                MovedAveragePlane = MovedAveragePlane,
                PositionIndex = PositionIndex
            };

            return offcut;
        }

        //------------------------------------------------------------
        // GetOffcutBase method
        //------------------------------------------------------------
        public static Rectangle3d GetOffcutBase(double X, double Y, Plane BasePlane, int BasePosition)
        {
            Rectangle3d[] ocBase = new Rectangle3d[1];

            switch (BasePosition)
            {
                case 0:
                    {
                        Interval iX = new Interval(-X / 2, X / 2);
                        Interval iY = new Interval(-Y / 2, Y / 2);

                        ocBase[0] = new Rectangle3d(BasePlane, iX, iY);
                    }
                    break;

                case 1:
                    {
                        Interval iX = new Interval(-X / 2, X / 2);
                        Interval iY = new Interval(0, Y);

                        ocBase[0] = new Rectangle3d(BasePlane, iX, iY);
                    }
                    break;

                case 2:
                    {
                        Interval iX = new Interval(-X / 2, X / 2);
                        Interval iY = new Interval(-Y, 0);

                        ocBase[0] = new Rectangle3d(BasePlane, iX, iY);
                    }
                    break;

                case 3:
                    {
                        Interval iX = new Interval(0, X);
                        Interval iY = new Interval(-Y / 2, Y / 2);

                        ocBase[0] = new Rectangle3d(BasePlane, iX, iY);
                    }
                    break;

                case 4:
                    {
                        Interval iX = new Interval(-X, 0);
                        Interval iY = new Interval(-Y / 2, Y / 2);

                        ocBase[0] = new Rectangle3d(BasePlane, iX, iY);
                    }
                    break;

                case 5:
                    {
                        ocBase[0] = new Rectangle3d(BasePlane, X, Y);
                    }
                    break;

                case 6:
                    {
                        ocBase[0] = new Rectangle3d(BasePlane, X, -Y);
                    }
                    break;

                case 7:
                    {
                        ocBase[0] = new Rectangle3d(BasePlane, -X, -Y);
                    }
                    break;

                case 8:
                    {
                        ocBase[0] = new Rectangle3d(BasePlane, -X, Y);
                    }
                    break;

                default:
                    {
                        Interval iX = new Interval(-X / 2, X / 2);
                        Interval iY = new Interval(-Y / 2, Y / 2);

                        ocBase[0] = new Rectangle3d(BasePlane, iX, iY);
                    }
                    break;
            }

            // return values
            return ocBase[0];
        }


        //------------------------------------------------------------
        // CreateOffcutBrep method
        //------------------------------------------------------------
        public static Brep CreateOffcutBrep(Offcut offcutDim, Plane averagePlane, int ocBaseIndex)
        {
            // copy and move average plane as offcut base (avoiding extrusion in two directions and boolean union calculation)
            Plane basePlane = new Plane(averagePlane);
            basePlane.Origin -= (basePlane.ZAxis * offcutDim.Z / 2);
            
            // creating rectangle
            Rectangle3d baseRect = GetOffcutBase(offcutDim.X, offcutDim.Y, basePlane, ocBaseIndex);
            NurbsCurve nurbsRect = baseRect.ToNurbsCurve();

            // extrude rectangle to create Offcut
            Brep brepOffcut = Extrusion.Create(nurbsRect, offcutDim.Z, true).ToBrep();

            /*  Because kinked surfaces can cause problems down stream, Rhino always splits kinked surfaces when adding Breps to the document.
                Sometimes, we have to do it.    */
            brepOffcut.Faces.SplitKinkyFaces(0.0001);

            // check solid orientation and return
            if (BrepSolidOrientation.Inward == brepOffcut.SolidOrientation)
            {
                brepOffcut.Flip();
                return brepOffcut;
            }
            else
                return brepOffcut;
        }


        //------------------------------------------------------------
        // List of Offcut base orientations as reference
        //------------------------------------------------------------
        public static List<string> BasePosition()
        {
            return new List<string>
            {
                "mid-mid",
                "mid-top",
                "mid-bottom",
                "right-mid",
                "left-mid",
                "right-top",
                "right-bottom",
                "left-bottom",
                "left-top"
            };
        }


        //------------------------------------------------------------
        // Dictionary of Offcut base orientations
        //------------------------------------------------------------
        public static Dictionary<string, int> GetBasePosition()
        {
            Dictionary<string, int> baseOrientation = new Dictionary<string, int>
            {
                { "mid-mid", 0 },
                { "mid-top", 1 },
                { "mid-bottom", 2 },
                { "right-mid", 3 },
                { "left-mid", 4 },
                { "right-top", 5 },
                { "right-bottom", 6 },
                { "left-bottom", 7 },
                { "left-top", 8 }
            };

            return baseOrientation;
        }
    }
}
