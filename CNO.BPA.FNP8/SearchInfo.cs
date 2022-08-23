using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace CNO.BPA.FNP8
{
   public class SearchInfo : CNO.BPA.FNP8.ISearchInfo
   {
      #region Variables
      //booleans
      //integers
      int _maxRecords = 10000;
      //strings     
      string[] _objectStores;
      string[] _selectProperties;
      string[] _documentClasses;
      string[] _orderByList;
      string _directSQLQuery = String.Empty;
      //objects
      List<ConditionalProperty> _conditionalProperties = null;      
      DataTable _returnData = new DataTable();
      #endregion

      #region Public Properties  
      public List<ConditionalProperty> ConditionalProperties
      {
         get { return _conditionalProperties; }
         set { _conditionalProperties = value; }
      }
      public string DirectSQLQuery
      {
         get { return _directSQLQuery; }
         set { _directSQLQuery = value; }
      }
      public string[] DocumentClasses
      {
         get { return _documentClasses; }
         set { _documentClasses = value; }
      }
      public int MaxRecords
      {
         get { return _maxRecords; }
         set { _maxRecords = value; }
      }
      public string[] ObjectStores
      {
         get { return _objectStores; }
         set { _objectStores = value; }
      }
      public string[] OrderByList
      {
         get { return _orderByList; }
         set { _orderByList = value; }
      }
      public DataTable ReturnData
      {
         get { return _returnData; }
         set { _returnData = value; }
      }
      public string[] SelectProperties
      {
         get { return _selectProperties; }
         set { _selectProperties = value; }
      }      
      #endregion
   }
}
