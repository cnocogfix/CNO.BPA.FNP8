using System;

namespace CNO.BPA.FNP8
{
   public interface IUserConnection
   {
      FileNet.Api.Core.IConnection Conn { get; set; }
      string Decrypt(string cypherText);
      FileNet.Api.Core.IDomain Domain { get; set; }
      string Encrypt(string plainText);
      void logon(string uri, string domain, string user, string pass);
   }
}
