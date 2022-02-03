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
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;


namespace SpruceBeetle
{
    public class SpruceBeetleInfo : GH_AssemblyInfo
    {
        // name of gh library
        public override string Name => "Spruce Beetle";

        // icon of gh library
        public override Bitmap Icon => Properties.Resources._24x24_SpruceBeetle;

        // description of gh library
        public override string Description => "A toolkit for working with timber offcuts and reclaimed timber ";

        // guid of gh library
        public override Guid Id => new Guid("ED2527D8-505A-4566-A0FD-49166600227C");

        // author name
        public override string AuthorName => "Dominik Reisach";

        // author contact details
        public override string AuthorContact => "dominik.reisach@gmail.com";
    }


    public class SpruceBeetleCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            // icon of gh library
            Instances.ComponentServer.AddCategoryIcon("Spruce Beetle", Properties.Resources._24x24_SpruceBeetle);

            // name and abbreviation of gh library
            Instances.ComponentServer.AddCategorySymbolName("Spruce Beetle", 'S');
            return GH_LoadingInstruction.Proceed;
        }
    }
}