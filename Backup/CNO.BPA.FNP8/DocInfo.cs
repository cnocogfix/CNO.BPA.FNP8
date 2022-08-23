using System;
using System.Collections.Generic;

namespace CNO.BPA.FNP8
{
   /// <summary>
   /// FNP8.DocInfo class is used to pass data back and forth with the caller.
   /// </summary> 
   public class DocInfo : CNO.BPA.FNP8.IDocInfo
   {
      #region Variables
      //booleans
      bool _isMulti = false;
      //strings
      string _documentClassName = String.Empty;
      string _documentGUID = String.Empty;
      string _extension = String.Empty;
      string _folderPath = String.Empty;      
      string _msarLocation = String.Empty;
      string _objectStore = String.Empty;
      string _retrievalName = String.Empty;
      string _versionSeriesID = String.Empty;
      string _fdocnumber = String.Empty;
      //objects
      Dictionary<string, string> _properties = null;

      #endregion

      #region Public Properties
      public bool IsMulti
      {
         get { return _isMulti; }
         set { _isMulti = value; }
      }
      public string DocumentClassName
      {
         get { return _documentClassName; }
         set { _documentClassName = value; }
      }
      public string DocumentGUID
      {
         get { return _documentGUID; }
         set { _documentGUID = value; }
      }
      public string Extension
      {
         get { return _extension; }
         set { _extension = value; }
      }
      public string F_DOCNUMBER
      {
         get { return _fdocnumber; }
         set { _fdocnumber = value; }
      }
      public string RetrievalName
      {
         get { return _retrievalName; }
         set { _retrievalName = value; }
      }
      public string FolderPath
      {
         get { return _folderPath; }
         set { _folderPath = value; }
      }
      public string MSARLocation
      {
         get { return _msarLocation; }
         set { _msarLocation = value; }
      }
      public string ObjectStore
      {
         get { return _objectStore; }
         set { _objectStore = value; }
      }      
      public Dictionary<string, string> Properties
      {
         get { return _properties; }
         set { _properties = value; }
      }
      public string VersionSeriesID
      {
         get { return _versionSeriesID; }
         set { _versionSeriesID = value; }
      }
      #endregion
   }
}
