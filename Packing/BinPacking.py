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


from flask import Flask
import ghhops_server as hs
import rhinoinside
import py3dbp as py3

rhinoinside.load()

import System
import Rhino
import Rhino.Geometry as rg

hops = hs.Hops(app=rhinoinside)

# create a new hops component
@hops.component(
    "/BinPack",
    name="Bin Packing BestFit",
    nickname="PackBin",
    description="The BestFit algorithm supports full item rotation and has excellent runtime performance and bin utilization",
    category="Spruce Beetle",
    subcategory="   Packing",
    icon="D:/Dominik/architecture/miscellaneous/c#/SpruceBeetle/Resources/24x24_BinPacking.png",
    inputs=[
        hs.HopsBrep("Box", "B", "Box container to fill with Offcuts"),
        hs.HopsNumber("X", "X", "X dimension of the Offcuts", list),
        hs.HopsNumber("Y", "Y", "Y dimension of the Offcuts", list),
        hs.HopsNumber("Z", "Z", "Z dimension of hte Offcuts", list)
    ],
    outputs=[
        hs.HopsBrep("Container", "C", "The container where the Offcuts are packed into"),
        hs.HopsBrep("Packed Offcuts", "POc", "List of packed Offcuts", list)
    ]
)

def BinPack(Box, X, Y, Z):
    # create Packer
    packer = py3.Packer()

    # create bin / container
    bin_container = py3.Bin('Container', Box.X, Box.Y, Box.Z, 1000)

    packer.add_bin(bin_container)

    # create Offcuts
    for i in range(X):
        packer.add_item(py3.Item(str(i), X[i], Y[i], Z[i], (X * Y * Z * 470)))
        
    packer.pack()

    packed_wall = System.Collection.Generic.List[rg.Brep()]

    for bin in packer.bins:
        
        xb = bin.width
        yb = bin.height
        zb = bin.depth
        
        rect_b = rg.Rectangle3d.ToNurbsCurve(rg.Rectangle3d(rg.Plane.WorldXY, xb, zb))
        srf_b = rg.Surface.ToBrep(rg.Surface.CreateExtrusion(rect_b, rg.Vector3d(0, 0, yb)))
        bin_b = rg.Brep.CapPlanarHoles(srf_b, 0.01)
        
        BIN = bin_b

    # collection of unused Offcuts
    for bin in packer.bins:
        for item in bin.unfitted_items:
            print(item.string())

    # packed Offcuts
    for bin in packer.bins:    
        for item in bin.items:
            x = 0
            y = 0
            z = 0
            
            if item.rotation_type == 0:
                x = item.width
                y = item.height
                z = item.depth
            elif item.rotation_type == 1:
                y = item.width
                x = item.height
                z = item.depth
            elif item.rotation_type == 2:
                z = item.width
                x = item.height
                y = item.depth
            elif item.rotation_type == 3:
                z = item.width
                y = item.height
                x = item.depth
            elif item.rotation_type == 4:
                y = item.width
                z = item.height
                x = item.depth
            elif item.rotation_type == 5:
                x = item.width
                z = item.height
                y = item.depth

            # construct Box 
            rect = rg.Rectangle3d.ToNurbsCurve(rg.Rectangle3d(rg.Plane.WorldXY, x, z))
            srf = rg.Surface.ToBrep(rg.Surface.CreateExtrusion(rect, rg.Vector3d(0, 0, y)))
            offcut = rg.Brep.CapPlanarHoles(srf, 0.01)
            
            move_vec = rg.Vector3d(item.position[0], item.position[2], item.position[1])
            move = rg.Transform.Translation(move_vec)
            
            offcut.Transform(move)
            packed_wall.append(offcut)


if __name__ == "__main__":
    hops.start(debug=True)
