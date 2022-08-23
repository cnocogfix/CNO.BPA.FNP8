using System;
namespace CNO.BPA.FNP8
{
   public interface IDocUpdate
   {
      string updateDocument(IUserConnection UserConn, IDocInfo DocInfo);
   }
}
