//Base
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using log4net;
//P8
using FileNet.Api.Admin;
using FileNet.Api.Authentication;
using FileNet.Api.Collection;
using FileNet.Api.Constants;
using FileNet.Api.Core;
using FileNet.Api.Exception;
using FileNet.Api.Meta;
using FileNet.Api.Property;
using FileNet.Api.Util;
using FileNet.Api.Security;
//Perficient
using MigrateODService;
using MigrateODService.Pages;
//Internal
using CNO.BPA.Framework;
using CNO.BPA.FNP8.DataHandler;

namespace CNO.BPA.FNP8
{  
   /// <summary>
   /// FNP8.DocSecurity class allows for applying the appropriate security template for legalhold and legalsecure documents
   /// </summary> 
   public class DocSecurity : CNO.BPA.FNP8.IDocSecurity
   {
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      DataSet _appConfig = null;
      private string LEGALHOLD = String.Empty;
      private string LEGALSECURE = String.Empty;
      private string NORMAL = String.Empty;
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.DocSecurity class allows for applying the appropriate security template for legalhold and legalsecure documents
      /// </summary>  
      public DocSecurity()
      {
         log.Debug("Preparing to instantiate a new DataAccess object");
         DataAccess dataAccess = new DataAccess();
         log.Debug("Preparing to call selectAppConfigValues");
         _appConfig = dataAccess.selectAppConfigValues("CNO.BPA.FNP8");
         if (null != _appConfig && null != _appConfig.Tables["CONFIG"])
         {
            log.Debug("selectAppConfigValues returned with " + _appConfig.Tables["CONFIG"].Rows.Count + " rows returned.");
         }
         //we need to set the values for the security template names
         SetTemplateNameDefaults();

      }
      #endregion

      #region Working with the ObjectStore
      /// <summary>
      /// This method accepts the name of an object store  
      /// and returns an instance of that object store
      /// </summary>
      /// <param name="objectStoreName">The name of the ObjectStore to return</param>   
      /// <param name="userConn">A user connection object</param>   
      /// <returns> IObjectStore object</returns>
      private IObjectStore getObjectStore(string objectStoreName, IUserConnection userConn)
      {
         try
         {
            log.Debug("Preparing to fetch an instance of the object store, '" + objectStoreName + "'.");
            //extablish and internal objectstore 
            IObjectStore store = null;
            //pull back a reference to the desired object store
            store = Factory.ObjectStore.FetchInstance(userConn.Domain, objectStoreName, null);
            log.Debug("ObjectStore retrieved successfully.");
            //returne the internal object store to the caller
            return store;
         }
         catch (Exception ex)
         {
            log.Error("getObjectStore: Error connecting to the ObjectStore", ex);
            throw new Exception("CNO.BPA.FNP8.DocSecurity.getObjectStore: " + ex.Message);
         }
      }
      #endregion

      #region Public Methods
      /// <summary>
      /// This method will change the security of all versions of the supplied document to be
      /// on legal hold. The document info object must contain either an F_DOCNUMBER or the
      /// Version Series ID and the object store name.
      /// </summary>
      /// <param name="userConn">A user connection object</param>
      /// <param name="docInfo">A document info object</param>
      public void SetLegalHold(IUserConnection userConn, IDocInfo docInfo)
      {
         //handle null parameters
         if (userConn == null) throw new ArgumentNullException("userConn");
         if (docInfo == null) throw new ArgumentNullException("docInfo");
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (docInfo.F_DOCNUMBER.Length > 0 && docInfo.VersionSeriesID.Length == 0)
            {
               //call the local method to return the vs id
               GetVersionSeriesId(userConn, docInfo);
            }
            if (docInfo.VersionSeriesID.Length > 0 && docInfo.ObjectStore.Length > 0)
            {
               //first get a handle to the object store
               IObjectStore objectStore = getObjectStore(docInfo.ObjectStore, userConn);
               log.Debug("Preparing to fetch an instance of the document with version series id of " + docInfo.VersionSeriesID);
               //retreive the document            
               IVersionSeries docRev = Factory.VersionSeries.FetchInstance(objectStore, new Id(docInfo.VersionSeriesID), null);
               log.Debug("Version series has been retrieved, preparing to loop through all versions to change their security.");
               //we need to loop through all of the versions of the doc
               foreach (IVersionable doc in docRev.Versions)
               {
                  //casting the iversionable object into a document object is required.
                  IDocument myDoc = (IDocument)doc;
                  log.Debug("preparing to pull a list of security templates for the current doc (" + myDoc.Id.ToString() + ").");
                  //pull back a list of available security templates
                  ISecurityTemplateList availableTemplates = myDoc.SecurityPolicy.SecurityTemplates;
                  log.Debug("Preparing to loop through the security template list looking for a template named '" + LEGALHOLD + "'.");
                  //and then loop through them looking for the correct one
                  foreach (ISecurityTemplate template in availableTemplates)
                  {
                     if (template.DisplayName == LEGALHOLD)
                     {
                        log.Debug("Preparing to apply the security template.");
                        myDoc.ApplySecurityTemplate(template.ApplyStateID);
                        myDoc.Save(RefreshMode.REFRESH);
                        log.Debug("Security template successfully applied.");
                     }
                  }
                  //check to see if this doc is the current released version and if so, return the doc guid
                  if ((bool)myDoc.IsCurrentVersion)
                  {
                     docInfo.DocumentGUID = myDoc.Id.ToString();
                  }
               }
            }
            else
            {
               log.Info("A Version Series ID (" + docInfo.VersionSeriesID + ") and an ObjectStore (" + docInfo.ObjectStore + ") are required.");                             
            }
         }
         catch (Exception e)
         {
            log.Error("setLegalHold: Error setting document security to LegalHold", e); 
            throw new Exception("CNO.BPA.FNP8.DocSecurity.setLegalHold: Error setting document security to LegalHold; " + e.Message, e);
         }
      }
      /// <summary>
      /// This method will change the security of all versions of the supplied document to the
      /// normal state of security within the document's document class. The document info object
      /// must contain either an F_DOCNUMBER or the Version Series ID and the object store name.
      /// </summary>
      /// <param name="userConn">A user connection object</param>
      /// <param name="docInfo">A document info object</param>
      public void SetNormal(IUserConnection userConn, IDocInfo docInfo)
      {
         SetNormal(userConn, docInfo, false);
      }
      public void SetNormal(IUserConnection userConn, IDocInfo docInfo, bool CurrentVersionOnly)
      {
         //handle null parameters
         if (userConn == null) throw new ArgumentNullException("userConn");
         if (docInfo == null) throw new ArgumentNullException("docInfo");
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (docInfo.F_DOCNUMBER.Length > 0 & docInfo.VersionSeriesID.Length == 0)
            {
               //call the local method to return the vs id
               GetVersionSeriesId(userConn, docInfo);
            }
            if (docInfo.VersionSeriesID.Length > 0 && docInfo.ObjectStore.Length > 0)
            {
               //first get a handle to the object store
               IObjectStore objectStore = getObjectStore(docInfo.ObjectStore, userConn);
               log.Debug("Preparing to fetch an instance of the document with version series id of " + docInfo.VersionSeriesID);
               //retreive the document            
               IVersionSeries docRev = Factory.VersionSeries.FetchInstance(objectStore, new Id(docInfo.VersionSeriesID), null);
               log.Debug("Version series has been retrieved");

               if (CurrentVersionOnly == false)
               {
                  log.Debug("Preparing to loop through all versions to change their security.");
                  //we need to loop through all of the versions of the doc
                  foreach (IVersionable doc in docRev.Versions)
                  {
                     //casting the iversionable object into a document object is required.
                     IDocument myDoc = (IDocument)doc;
                     log.Debug("preparing to pull a list of security templates for the current doc (" + myDoc.Id.ToString() + ").");
                     //pull back a list of available security templates
                     ISecurityTemplateList availableTemplates = myDoc.SecurityPolicy.SecurityTemplates;
                     log.Debug("Preparing to loop through the security template list looking for a template named '" + NORMAL + "'.");
                     //and then loop through them looking for the correct one
                     foreach (ISecurityTemplate template in availableTemplates)
                     {
                        if (template.DisplayName == NORMAL)
                        {
                           log.Debug("Preparing to apply the security template.");
                           myDoc.ApplySecurityTemplate(template.ApplyStateID);
                           myDoc.Save(RefreshMode.REFRESH);
                           log.Debug("Security template successfully applied.");
                        }
                     }
                     //check to see if this doc is the current released version and if so, return the doc guid
                     if ((bool)myDoc.IsCurrentVersion)
                     {
                        docInfo.DocumentGUID = myDoc.Id.ToString();
                     }
                  }
               }
               else
               {
                  foreach (IVersionable doc in docRev.Versions)
                  {
                     log.Debug("Preparing to loop through all versions to locate the current version.");
                     //casting the iversionable object into a document object is required.
                     IDocument myDoc2 = (IDocument)doc;
                     //check to see if this doc is the current released version and if so, return the doc guid
                     if ((bool)myDoc2.IsCurrentVersion)
                     {
                        log.Debug("Current version has been found.");
                        log.Debug("Preparing to pull a list of security templates for the document's current version (" + myDoc2.Id.ToString() + ").");
                        //pull back a list of available security templates
                        ISecurityTemplateList availableTemplates = myDoc2.SecurityPolicy.SecurityTemplates;
                        log.Debug("Preparing to loop through the security template list looking for a template named '" + NORMAL + "'.");
                        //and then loop through them looking for the correct one
                        foreach (ISecurityTemplate template in availableTemplates)
                        {
                           if (template.DisplayName == NORMAL)
                           {
                              log.Debug("Preparing to apply the security template to current version.");
                              myDoc2.ApplySecurityTemplate(template.ApplyStateID);
                              myDoc2.Save(RefreshMode.REFRESH);
                              log.Debug("Security template successfully applied to current version.");
                              break;
                           }
                        }
                        break;
                     }
                  }
               }
            }
            else
            {
               log.Info("A Version Series ID (" + docInfo.VersionSeriesID + ") and an ObjectStore (" + docInfo.ObjectStore + ") are required.");
            }
         }
         catch (Exception e)
         {
            log.Error("setNormal: Error setting document security to Normal state", e); 
            throw new Exception("CNO.BPA.FNP8.DocSecurity.setNormal: Error setting document security to Normal state; " + e.Message, e);
         }
      }
      /// <summary>
      /// This method will change the security of all versions of the supplied document to be
      /// unsecured and available to all users to view. The document info object must contain 
      /// either an F_DOCNUMBER or the Version Series ID and the object store name.
      /// </summary>
      /// <param name="userConn">A user connection object</param>
      /// <param name="docInfo">A document info object</param>
      public void SetLegalSecure(IUserConnection userConn, IDocInfo docInfo)
      {
         //handle null parameters
         if (userConn == null) throw new ArgumentNullException("userConn");
         if (docInfo == null) throw new ArgumentNullException("docInfo");
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (docInfo.F_DOCNUMBER.Length > 0 & docInfo.VersionSeriesID.Length == 0)
            {
               //call the local method to return the vs id
               GetVersionSeriesId(userConn, docInfo);
            }
            if (docInfo.VersionSeriesID.Length > 0 && docInfo.ObjectStore.Length > 0)
            {
               //get a handle to the object store
               IObjectStore objectStore = getObjectStore(docInfo.ObjectStore, userConn);
               log.Debug("Preparing to fetch an instance of the document with version series id of " + docInfo.VersionSeriesID);
               //retreive the document            
               IVersionSeries docRev = Factory.VersionSeries.FetchInstance(objectStore, new Id(docInfo.VersionSeriesID), null);
               log.Debug("Version series has been retrieved, preparing to loop through all versions to change their security.");               
               //we need to loop through all of the versions of the doc
               foreach (IVersionable doc in docRev.Versions)
               {
                  //casting the iversionable object into a document object is required.
                  IDocument myDoc = (IDocument)doc;
                  log.Debug("preparing to pull a list of security templates for the current doc (" + myDoc.Id.ToString() + ").");
                  //pull back a list of available security templates
                  ISecurityTemplateList availableTemplates = myDoc.SecurityPolicy.SecurityTemplates;
                  log.Debug("Preparing to loop through the security template list looking for a template named '" + LEGALSECURE + "'.");
                  //and then loop through them looking for the correct one
                  foreach (ISecurityTemplate template in availableTemplates)
                  {
                     if (template.DisplayName == LEGALSECURE)
                     {
                        log.Debug("Preparing to apply the security template.");
                        myDoc.ApplySecurityTemplate(template.ApplyStateID);
                        myDoc.Save(RefreshMode.REFRESH);
                        log.Debug("Security template successfully applied.");                        
                     }
                  }
                  //check to see if this doc is the current released version and if so, return the doc guid
                  if ((bool)myDoc.IsCurrentVersion)
                  {
                     docInfo.DocumentGUID = myDoc.Id.ToString();
                  }
               }
            }
            else
            {
               log.Info("A Version Series ID (" + docInfo.VersionSeriesID + ") and an ObjectStore (" + docInfo.ObjectStore + ") are required.");
            }
         }
         catch (Exception e)
         {
            log.Error("setLegalSecure: Error setting document security to LegalSecure", e); 
            throw new Exception("CNO.BPA.FNP8.DocSecurity.setLegalSecure: Error setting document security to LegalSecure; " + e.Message, e);
         }
      }
      /// <summary>
      /// This method will get the current security of the current version of the supplied
      /// document. The document info object must contain either an F_DOCNUMBER or the 
      /// Version Series ID and the object store name.
      /// </summary>
      /// <param name="userConn">A user connection object</param>
      /// <param name="docInfo">A document info object</param>
      public string GetCurrentSecurity(IUserConnection userConn, IDocInfo docInfo)
      {
         //handle null parameters
         if (userConn == null) throw new ArgumentNullException("userConn");
         if (docInfo == null) throw new ArgumentNullException("docInfo");

         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (docInfo.F_DOCNUMBER.Length > 0 && docInfo.VersionSeriesID.Length == 0)
            {
               //call the local method to return the vs id
               GetVersionSeriesId(userConn, docInfo);
            }
            if (docInfo.VersionSeriesID.Length > 0 && docInfo.ObjectStore.Length > 0)
            {
               //first get a handle to the object store
               IObjectStore objectStore = getObjectStore(docInfo.ObjectStore, userConn);
               log.Debug("Preparing to fetch an instance of the document with version series id of " + docInfo.VersionSeriesID);
               //retreive the document            
               IVersionSeries docSeries = Factory.VersionSeries.FetchInstance(objectStore, new Id(docInfo.VersionSeriesID), null);
               log.Debug("Version series has been retrieved, preparing to loop through all versions to change their security.");
               //we need to loop through all of the versions of the doc
               
                  //casting the iversionable object into a document object is required.
                  IDocument myDoc = (IDocument)docSeries.CurrentVersion;
                  log.Debug("preparing to pull a list of security templates for the current doc (" + myDoc.Id.ToString() + ").");
                  //pull back a list of available security templates
              
               
               ISecurityPolicy securityPolicy = myDoc.SecurityPolicy;

               foreach (IPermission permission in myDoc.Permissions)
               {
                  string value = permission.GranteeName;
               }

               return securityPolicy.DisplayName;
               
            }
            else
            {
               log.Info("A Version Series ID (" + docInfo.VersionSeriesID + ") and an ObjectStore (" + docInfo.ObjectStore + ") are required.");
               return "A Version Series ID (" + docInfo.VersionSeriesID + ") and an ObjectStore (" + docInfo.ObjectStore +
                      ") are required.";
            }
         }
         catch (Exception e)
         {
            log.Error("GetCurrentSecurity: Error getting current document security", e);
            throw new Exception("CNO.BPA.FNP8.DocSecurity.GetCurrentSecurity: Error getting current document security; " + e.Message, e);
         }
      }

      #endregion

      #region Private Methods
      private void SetTemplateNameDefaults()
      {
         DataRow[] dr = null;
         //pull back the value for legal hold
         dr = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'SECURITY' AND CONFIG_NAME = 'LEGALHOLD'");
         if (null != dr && dr.Any())
         {
            LEGALHOLD = dr[0]["CONFIG_VALUE"].ToString();
         }
         //pull back the value for legal secure
         dr = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'SECURITY' AND CONFIG_NAME = 'LEGALSECURE'");
         if (null != dr && dr.Any())
         {
            LEGALSECURE = dr[0]["CONFIG_VALUE"].ToString();
         }
         //pull back the value for normal
         dr = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'SECURITY' AND CONFIG_NAME = 'NORMAL'");
         if (null != dr && dr.Any())
         {
            NORMAL = dr[0]["CONFIG_VALUE"].ToString();
         }
      }
      private void GetVersionSeriesId(IUserConnection UserConn, IDocInfo DocInfo)
      {
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (DocInfo.F_DOCNUMBER.Length > 0 && DocInfo.ObjectStore.Length > 0)
            {
               log.Info("An F_DOCNUMBER was passed of '" + DocInfo.F_DOCNUMBER + "' so a search must first be performed");
               //we need to search for and locate the version series id
               SearchInfo sInfo = new SearchInfo();
               //we need to build an array of the object stores to pass in to the dll
               string[] oStores = new string[1];
               oStores[0] = DocInfo.ObjectStore;
               //for query we will simply build the query string to use
               string simpleQuery = "SELECT d.VersionSeries FROM Document d "
                  + "WHERE d.F_DOCNUMBER = " + DocInfo.F_DOCNUMBER;
               //once we have everything we can assign the values to the search info object
               sInfo.ObjectStores = oStores;
               sInfo.DirectSQLQuery = simpleQuery;
               //now we're ready to perform the search
               DocSearch mysearch = new DocSearch();
               mysearch.Search(UserConn, sInfo);
               //once found, we'll just assign the version series id and continue normally
               if (sInfo.ReturnData.Rows.Count >= 1)
               {
                  DocInfo.VersionSeriesID = sInfo.ReturnData.Rows[0][0].ToString();
               }
               else
               {
                  log.Error("No rows were returned from the search using F_DOCNUMBER '" + DocInfo.F_DOCNUMBER + "' within the ObjectStore '"
                     + DocInfo.ObjectStore + "'.");
               }
            }
            else
            {
               log.Error("Both F_DOCNUMBER('" + DocInfo.F_DOCNUMBER + "') and ObjectStore('" + DocInfo.ObjectStore + "') are required.");
            }
         }
         catch (Exception e)
         {
            log.Error("getVersionSeriesID: Error retrieving version series ID", e);            
         }
      }
      #endregion

   }
}
