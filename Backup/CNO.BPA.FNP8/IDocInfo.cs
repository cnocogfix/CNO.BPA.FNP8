using System;
namespace CNO.BPA.FNP8
{
   public interface IDocInfo
   {
      string DocumentClassName { get; set; }
      string DocumentGUID { get; set; }
      string Extension { get; set; }
      string F_DOCNUMBER { get; set; }
      string FolderPath { get; set; }
      bool IsMulti { get; set; }
      string MSARLocation { get; set; }
      string ObjectStore { get; set; }
      System.Collections.Generic.Dictionary<string, string> Properties { get; set; }
      string RetrievalName { get; set; }
      string VersionSeriesID { get; set; }
   }
}
