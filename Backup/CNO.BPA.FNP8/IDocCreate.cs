using System;
namespace CNO.BPA.FNP8
{
   public interface IDocCreate
   {
      void createDocument(System.IO.MemoryStream[] Document, IUserConnection UserConn, IDocInfo DocInfo);
      void createDocument(System.IO.Stream Document, IUserConnection UserConn, IDocInfo DocInfo);
   }
}
