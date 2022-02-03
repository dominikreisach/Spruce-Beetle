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
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;


namespace SpruceBeetle.Create
{
    class ExcelOffcut
    {
        // global variable
        public string FilePath;


        // ExcelOffcut method
        public ExcelOffcut(string filePath)
        {
            FilePath = filePath;
        }


        // ExcelFile method
        public List<Offcut> ExcelFile()
        {
            // create COM objects
            Excel.Application xlsApp = new Excel.Application();
            Excel.Workbook xlsWB = xlsApp.Workbooks.Open(@FilePath);
            Excel._Worksheet xlsWS = xlsWB.Sheets[1];
            Excel.Range xlsRange = xlsWS.UsedRange;

            int rowCount = xlsRange.Rows.Count;
            int columnCount = xlsRange.Columns.Count;

            List<Offcut> offcutList = new List<Offcut>();

            // iterate over the rows to extract data
            for (int i = 2; i <= rowCount; i++)
            {
                if (i == 0 || i == 1 || xlsRange.Cells[i, 1] == null)
                    continue;

                double index = xlsRange.Cells[i, 1].Value2;
                double x = Convert.ToDouble(xlsRange.Cells[i, 2].Value2);
                double y = Convert.ToDouble(xlsRange.Cells[i, 3].Value2);
                double z = Convert.ToDouble(xlsRange.Cells[i, 4].Value2);

                Offcut offcut = new Offcut(index, x, y, z);

                offcutList.Add(offcut);
            }

            // cleanup
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // release COM objects to fully kill Excel process from running in the background
            Marshal.ReleaseComObject(xlsRange);
            Marshal.ReleaseComObject(xlsWS);

            // close and release
            xlsWB.Close();
            Marshal.ReleaseComObject(xlsWB);

            // quit and release
            xlsApp.Quit();
            Marshal.ReleaseComObject(xlsApp);

            return offcutList;
        }
    }
}
