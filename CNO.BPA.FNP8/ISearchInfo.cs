using System;
namespace CNO.BPA.FNP8
{
   public interface ISearchInfo
   {
      System.Collections.Generic.List<ConditionalProperty> ConditionalProperties { get; set; }
      string DirectSQLQuery { get; set; }
      string[] DocumentClasses { get; set; }
      int MaxRecords { get; set; }
      string[] ObjectStores { get; set; }
      string[] OrderByList { get; set; }
      System.Data.DataTable ReturnData { get; set; }
      string[] SelectProperties { get; set; }
   }
}
