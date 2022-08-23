using System;
namespace CNO.BPA.FNP8
{
   public interface IDocSearch
   {
      string Search(IUserConnection UserConn, ISearchInfo searchInfo);
   }
}
