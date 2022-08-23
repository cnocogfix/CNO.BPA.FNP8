using System;
namespace CNO.BPA.FNP8
{
   public interface IDocDelete
   {
      void deleteContentElement(IUserConnection UserConn, IDocInfo DocInfo);
   }
}
