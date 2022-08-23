using System;
namespace CNO.BPA.FNP8
{
   public interface IDocSecurity
   {
      void SetLegalHold(IUserConnection userConn, IDocInfo docInfo);
      void SetLegalSecure(IUserConnection userConn, IDocInfo docInfo);
      void SetNormal(IUserConnection userConn, IDocInfo docInfo);
      void SetNormal(IUserConnection userConn, IDocInfo docInfo, bool CurrentVersionOnly);
      string GetCurrentSecurity(IUserConnection userConn, IDocInfo docInfo);
   }
}
