//Base
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using log4net;
//P8
using FileNet.Api.Authentication;
using FileNet.Api.Constants;
using FileNet.Api.Core;
using FileNet.Api.Exception;
using FileNet.Api.Util;
//Internal
using CNO.BPA.Framework;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]
namespace CNO.BPA.FNP8
{
   /// <summary>
   /// FNP8.Connection class allows for establishing a connection to P8
   /// </summary> 
   public class UserConnection : CNO.BPA.FNP8.IUserConnection
   {
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      private IConnection _conn = null;
      private IDomain _domain = null;
      private IObjectStore _objectStore = null;
      private Cryptography _crypto = new Cryptography();
      private string _userName = string.Empty;
      private string _password = string.Empty;
      private string _uri = string.Empty;
      private string _domainName = string.Empty;
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.Connection class allows for establishing a connection to P8
      /// </summary>    
      public UserConnection()
      {
         //initialize the logger
         FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CNO.BPA.FNP8.config"));
         log4net.Config.XmlConfigurator.Configure(fi);
      }
      #endregion

      #region Encryption Decription

      public string Encrypt(string plainText)
      {
         return _crypto.Encrypt(plainText);
      }
      public string Decrypt(string cypherText)
      {
         return _crypto.Decrypt(cypherText);
      }
      #endregion

      #region Connection Methods
      /// <summary>
      /// Obtains an Iconnection object and an IDomain object 
      /// required for interacting with the P8 repository
      /// </summary>     
      /// <param name="uri">The Universal Resource Identity (URI) of the P8 system to connect to</param>
      /// <param name="domain">The name of the domain to connect to</param>
      /// <param name="user">The encrypted user credential to login with</param>
      /// <param name="pass">The encrypted password for the user credential</param>  
      public void logon(string uri, string domain, string user, string pass)
      {
         try
         {
            //check that a user and pass were supplied
            if (user.Length > 0 && user.Length > 0)
            {
               log.Info("Preparing to decrypt user info");
               //first we want to grab a local copy of the login parameters
               _userName = _crypto.Decrypt(user);
               _password = _crypto.Decrypt(pass);
               log.Debug("User info successfully decrypted");
               //check that they sent a URI
               if (uri.Length > 0)
               {
                  _uri = uri;
               }
               else
               {
                  log.Info("The P8 URI is required and was not passed in.");
               }
               //check that they sent a Domain
               if (domain.Length > 0)
               {
                  _domainName = domain;
               }
               else
               {
                  log.Info("The P8 Domain is required and was not passed in.");
               }
               //make sure there is not currently a connection open
               if (null == _conn)
               {
                  //call the get connection method 
                  _conn = getConnection(_uri, _userName, _password);
                  //using the connection call the get domain method
                  _domain = getDomain(_conn, _domainName);
               }
            }
            else
            {
               log.Info("A LAN ID (" + user + ") and LAN password (" + pass + ") are required to connect to P8.");            
            }
         }
         catch (Exception ex)
         {
            log.Error("Logon: Error logging on to P8", ex);
            throw new Exception("CNO.BPA.FNP8.Connection.logon: " + ex.Message);
         }
      }
      /// <summary>
      /// This method requires the caller to pass in the elements neccessary  
      /// to create a connection to the P8 content engine
      /// </summary>
      /// <param name="uri">The Universal Resource Identity (URI) for this connection</param>
      /// <param name="user">The user to use for this connection</param>
      /// <param name="pword">The password to use for this connection</param>
      /// <returns> IConnection object</returns>
      private IConnection getConnection(string uri, string user, string pword)
      {
         try
         {
            log.Info("Preparing to connect to the following URI: " + uri);
            UsernameCredentials creds = new UsernameCredentials(user, pword);
            ClientContext.SetProcessCredentials(creds);

            IConnection conn = Factory.Connection.GetConnection(uri);
            log.Debug("Connection successful, returning connection to caller.");
            return conn;
         }
         catch (Exception ex)
         {
            log.Error("getConnection: Error connecting to P8", ex);
            throw new Exception("CNO.BPA.FNP8.UserConnection.getConnection: " + ex.Message);
         }
      }
      /// <summary>
      /// This method requires the caller to pass in the elements neccessary  
      /// to return the P8 domain to interact with
      /// </summary>
      /// <param name="conn">The handle to an IConnection</param>
      /// <param name="domainName">The domain to return</param>      
      /// <returns> IDomain object</returns>
      private IDomain getDomain(IConnection conn, string domainName)
      {
         try
         {
            log.Info("Preparing to fetch an instance of the domain (" + domainName + ").");
            IDomain domain = null;
            domain = Factory.Domain.FetchInstance(conn, domainName, null);
            log.Debug("Domain successfully returned.");
            return domain;
         }
         catch (Exception ex)
         {
            log.Error("getDomain: Error fetching an instance of the domain", ex);
            throw new Exception("CNO.BPA.FNP8.UserConnection.getDomain: " + ex.Message);
         }
      }
      #endregion

      #region Public Properties
      public IDomain Domain
      {
         get { return _domain; }
         set { _domain = value; }
      }
      public IConnection Conn
      {
         get { return _conn; }
         set { _conn = value; }
      }
      #endregion
   }
}
