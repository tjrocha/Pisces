using System;
using System.Collections.Generic;
using System.Text;
using Reclamation.Core;
using System.IO;
using Hec.Dss;

namespace Reclamation.TimeSeries.Hec
{
  public class HecDssTree
  {
    public static void AddDssFileToDatabase(string dssFilename, PiscesFolder parent,
        TimeSeriesDatabase db)
    {
      var tsPaths = new List<DssPath>();
      using (DssReader r = new DssReader(dssFilename))
      {
        var c = r.GetCatalog();
        foreach (DssPath p in c.CondensedPaths)
        {
          if (p.RecordType == RecordType.RegularTimeSeries
         || p.RecordType == RecordType.IrregularTimeSeries)
          {
            tsPaths.Add(p);
          }
        }
      }

      if (parent == null)
        parent = db.RootFolder;

      PiscesFolder root = parent;
      try
      {
        var paths = tsPaths.ToArray();
        root = db.AddFolder(parent, Path.GetFileName(dssFilename));
        var sc = db.GetSeriesCatalog();
        int folderID = root.ID;
        string previousA = "";
        for (int i = 0; i < paths.Length; i++)
        {
          var p = paths[i];
          if (i == 0 || p.Apart != previousA)
          {
            folderID = sc.AddFolder(p.Apart, root.ID);
            previousA = p.Apart;
          }

          HecDssSeries s = new HecDssSeries(dssFilename, paths[i].PathWithoutDate);
          sc.AddSeriesCatalogRow(s, sc.NextID(), folderID);
        }
        db.Server.SaveTable(sc);
      }
      catch (Exception e)
      {
        System.Windows.Forms.MessageBox.Show(e.Message);
      }
      finally
      {
        //db.ResumeTreeUpdates();
        //db.RefreshFolder(parent);
      }
    }

  }
}
