"""
MIT License

Copyright (c) 2019 Enzo Ruiz
Copyright (c) 2022 Dominik Reisach

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

The Python libary used here can be found in this github repository:
https://github.com/enzoruiz/3dbinpacking
"""


from matplotlib.pyplot import box
import rhinoinside
import ghhops_server as hs
import py3dbp as py3

rhinoinside.load()

import System
import Rhino
import Rhino.Geometry as rg

hops = hs.Hops(app= rhinoinside)

# create a new hops component
@hops.component(
    "/BinPack",
    name="Bin Packing",
    nickname="PackBin",
    description="The BestFit algorithm supports full item rotation and has excellent runtime performance and bin utilization",
    category="Spruce Beetle",
    subcategory="   Packing",
    icon="D:/Dominik/architecture/miscellaneous/c#/SpruceBeetle/Resources/24x24_BinPacking.png",
    inputs=[
        hs.HopsBrep("Bin", "B", "Bin to fill with Offcuts"),
        hs.HopsNumber("X", "X", "X dimension of the Offcuts", list),
        hs.HopsNumber("Y", "Y", "Y dimension of the Offcuts", list),
        hs.HopsNumber("Z", "Z", "Z dimension of the Offcuts", list),
    ],
    outputs=[
        hs.HopsBrep("Packed Bin", "PB", "The bin where the Offcuts are packed into"),
        hs.HopsBrep("Packed Offcuts", "POc", "List of packed Offcuts", list),
    ],
)

def BinPack(Bin, X, Y, Z):
    # generate box out of brep
    bbox = rg.Box(Bin.GetBoundingBox(True))
    
    # create Packer
    packer = py3.Packer()

    # create bin / container
    bin_container = py3.Bin('Bin', bbox.X.T1, bbox.Y.T1, bbox.Z.T1, 1000.0)

    packer.add_bin(bin_container)

    # create Offcuts
    for i in range(len(X)):
        packer.add_item(py3.Item(str(i), X[i], Y[i], Z[i], (X[i] * Y[i] * Z[i] * 470.0)))
        
    packer.pack()

    packed_wall = System.Collections.Generic.List[rg.Brep]()

    for bin in packer.bins:
        
        xb = float(bin.width)
        yb = float(bin.height)
        zb = float(bin.depth)
        
        rect_b = rg.Rectangle3d.ToNurbsCurve(rg.Rectangle3d(rg.Plane.WorldXY, xb, zb))
        srf_b = rg.Surface.ToBrep(rg.Surface.CreateExtrusion(rect_b, rg.Vector3d(0.0, 0.0, yb)))
        bin_b = rg.Brep.CapPlanarHoles(srf_b, 0.01)
        bin_b.Faces.SplitKinkyFaces(0.01)
        
        BIN = bin_b

    # collection of unused Offcuts
    for bin in packer.bins:
        for item in bin.unfitted_items:
            print(item.string())

    # packed Offcuts
    for bin in packer.bins:    
        for item in bin.items:
            x = 0.0
            y = 0.0
            z = 0.0
            
            if item.rotation_type == 0:
                x = float(item.width)
                y = float(item.height)
                z = float(item.depth)
            elif item.rotation_type == 1:
                y = float(item.width)
                x = float(item.height)
                z = float(item.depth)
            elif item.rotation_type == 2:
                z = float(item.width)
                x = float(item.height)
                y = float(item.depth)
            elif item.rotation_type == 3:
                z = float(item.width)
                y = float(item.height)
                x = float(item.depth)
            elif item.rotation_type == 4:
                y = float(item.width)
                z = float(item.height)
                x = float(item.depth)
            elif item.rotation_type == 5:
                x = float(item.width)
                z = float(item.height)
                y = float(item.depth)

            # construct Box 
            rect = rg.Rectangle3d(rg.Plane.WorldXY, x, z)
            open_offcut = rg.Surface.CreateExtrusion(rect.ToNurbsCurve(), rg.Vector3d(0.0, 0.0, y)).ToBrep()
            offcut = open_offcut.CapPlanarHoles(0.001)
            offcut.Faces.SplitKinkyFaces(0.001)
            
            move_vec = rg.Vector3d(float(item.position[0]), float(item.position[2]), float(item.position[1]))
            move = rg.Transform.Translation(move_vec)
            
            offcut.Transform(move)
            packed_wall.Add(offcut)

            return bin_b, packed_wall

if __name__ == "__main__":
    hops.start(debug=False)
