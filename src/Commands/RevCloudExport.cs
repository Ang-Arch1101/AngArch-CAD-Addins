using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;

[assembly: CommandClass(typeof(AngArchCADAddins.Commands.RevCloudExportCommand))]
[assembly: ExtensionApplication(null)]

namespace AngArchCADAddins.Commands
{
    public class RevCloudExportCommand
    {
        [CommandMethod("REVEXPORT")]
        public void RevExport()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var pso = new PromptStringOptions("\nEnter layer name to scan: ")
            {
                AllowSpaces = false
            };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;
            string layerName = psr.StringResult;

            var clouds = new List<Polyline>();
            var triangles = new List<Polyline>();
            var allTexts = new List<TextEntry>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                int idx = 0;
                foreach (ObjectId id in btr)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;
                    if (!ent.Layer.Equals(layerName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (ent is Polyline pl)
                    {
                        if (IsRevCloud(pl)) clouds.Add(pl);
                        else if (IsTriangle(pl)) triangles.Add(pl);
                    }
                    else if (ent is DBText dt)
                        allTexts.Add(new TextEntry(idx++, dt.Position, dt.TextString));
                    else if (ent is MText mt)
                        allTexts.Add(new TextEntry(idx++, mt.Location, mt.Text));
                }

                tr.Commit();
            }

            var rows = new List<RevRow>();

            foreach (var cloud in clouds)
            {
                Point3d cloudCentroid = GetCentroid(cloud);

                // Nearest triangle to cloud centroid
                Polyline nearestTri = null;
                double minTriDist = double.MaxValue;
                foreach (var tri in triangles)
                {
                    double d = GetCentroid(tri).DistanceTo(cloudCentroid);
                    if (d < minTriDist) { minTriDist = d; nearestTri = tri; }
                }

                // Text nearest to triangle centroid = revision number
                string revNum = "";
                int matchedRevIdx = -1;
                if (nearestTri != null && allTexts.Count > 0)
                {
                    Point3d triCentroid = GetCentroid(nearestTri);
                    double minDist = double.MaxValue;
                    foreach (var t in allTexts)
                    {
                        double d = t.Pos.DistanceTo(triCentroid);
                        if (d < minDist) { minDist = d; matchedRevIdx = t.Idx; revNum = t.Val; }
                    }
                }

                // Text nearest to cloud centroid (excluding the revision number text) = description
                string description = "";
                double minDescDist = double.MaxValue;
                foreach (var t in allTexts)
                {
                    if (t.Idx == matchedRevIdx) continue;
                    double d = t.Pos.DistanceTo(cloudCentroid);
                    if (d < minDescDist) { minDescDist = d; description = t.Val; }
                }

                string status;
                bool hasTri = nearestTri != null;
                bool hasDesc = !string.IsNullOrEmpty(description);
                if (hasTri && hasDesc) status = "OK";
                else if (hasTri) status = "漏字";
                else if (hasDesc) status = "漏△";
                else status = "漏△漏字";

                rows.Add(new RevRow
                {
                    No = rows.Count + 1,
                    CloudX = Math.Round(cloudCentroid.X, 3),
                    CloudY = Math.Round(cloudCentroid.Y, 3),
                    RevisionNumber = revNum,
                    Description = description,
                    HasTriangle = hasTri,
                    Status = status
                });
            }

            string outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "RevisionExport.xlsx");

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Revisions");

                ws.Cell(1, 1).Value = "No./編號";
                ws.Cell(1, 2).Value = "Cloud X/雲線X";
                ws.Cell(1, 3).Value = "Cloud Y/雲線Y";
                ws.Cell(1, 4).Value = "Revision/版次";
                ws.Cell(1, 5).Value = "Description/說明";
                ws.Cell(1, 6).Value = "Has △/有△";
                ws.Cell(1, 7).Value = "Status/狀態";

                var hdr = ws.Row(1);
                hdr.Style.Font.Bold = true;
                hdr.Style.Fill.BackgroundColor = XLColor.LightBlue;

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    int row = i + 2;
                    ws.Cell(row, 1).Value = r.No;
                    ws.Cell(row, 2).Value = r.CloudX;
                    ws.Cell(row, 3).Value = r.CloudY;
                    ws.Cell(row, 4).Value = r.RevisionNumber;
                    ws.Cell(row, 5).Value = r.Description;
                    ws.Cell(row, 6).Value = r.HasTriangle ? "Yes" : "No";
                    ws.Cell(row, 7).Value = r.Status;

                    if (r.Status != "OK")
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(outputPath);
            }

            ed.WriteMessage(
                $"\nREVEXPORT: {rows.Count} cloud(s) found on layer '{layerName}'. " +
                $"Exported to {outputPath}");
        }

        private static bool IsRevCloud(Polyline pl)
        {
            if (!pl.Closed || pl.NumberOfVertices < 3) return false;
            for (int i = 0; i < pl.NumberOfVertices; i++)
                if (Math.Abs(pl.GetBulgeAt(i)) < 1e-6) return false;
            return true;
        }

        private static bool IsTriangle(Polyline pl)
        {
            if (!pl.Closed || pl.NumberOfVertices != 3) return false;
            for (int i = 0; i < 3; i++)
                if (Math.Abs(pl.GetBulgeAt(i)) > 1e-6) return false;
            return true;
        }

        private static Point3d GetCentroid(Polyline pl)
        {
            double x = 0, y = 0;
            int n = pl.NumberOfVertices;
            for (int i = 0; i < n; i++)
            {
                var pt = pl.GetPoint3dAt(i);
                x += pt.X;
                y += pt.Y;
            }
            return new Point3d(x / n, y / n, 0);
        }
    }

    internal class TextEntry
    {
        public int Idx { get; }
        public Point3d Pos { get; }
        public string Val { get; }
        public TextEntry(int idx, Point3d pos, string val) { Idx = idx; Pos = pos; Val = val; }
    }

    internal class RevRow
    {
        public int No { get; set; }
        public double CloudX { get; set; }
        public double CloudY { get; set; }
        public string RevisionNumber { get; set; }
        public string Description { get; set; }
        public bool HasTriangle { get; set; }
        public string Status { get; set; }
    }
}
