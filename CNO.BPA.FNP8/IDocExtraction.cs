using System;
namespace CNO.BPA.FNP8
{
   public interface IDocExtraction
   {
      System.IO.MemoryStream[] getDocument(IUserConnection UserConn, IDocInfo DocInfo);
   }
}
