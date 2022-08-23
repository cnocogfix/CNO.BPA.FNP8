//Base
using System;
using System.IO;
using System.Linq;
using System.Reflection;
//P8
using FileNet.Api.Core;
using FileNet.Api.Util;
//Perficient
using MigrateODService;
using MigrateODService.Pages;
using FileNet.Api.Property;
using FileNet.Api.Collection;
using FileNet.Api.Constants;
//Internal


namespace CNO.BPA.FNP8
{
   /// <summary>
   /// FNP8.DocDelete class allows for deleting content elements from P8 
   /// </summary>   
   public class DocDelete : CNO.BPA.FNP8.IDocDelete
   {
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.DocDelete class allows for deleting content elements from P8 
      /// </summary>   
      public DocDelete()
      {
         //initialize the logger
         FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CNO.BPA.FNP8.config"));
         log4net.Config.XmlConfigurator.Configure(fi);
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
            throw new Exception("CNO.BPA.FNP8.DocDelete.getObjectStore: " + ex.Message);
         }
      }
      #endregion

      #region Deletion
      /// <summary>
      /// Deletes the content element specified.
      /// </summary>
      /// <param name="UserConn">A user connection object.</param>
      /// <param name="DocInfo">A document info object.</param>
      /// <returns></returns>
      public void deleteContentElement(IUserConnection UserConn, IDocInfo DocInfo)
      {
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (DocInfo.DocumentGUID.Length > 0 & DocInfo.ObjectStore.Length > 0)
            {
               log.Info("A document GUID of '" + DocInfo.DocumentGUID + "' was passed in for deletion.");
               //get a handle to the object store
               IObjectStore objectStore = getObjectStore(DocInfo.ObjectStore, UserConn);
               log.Debug("Preparing to fetch an instance of the document guid.");
               IDocument doc = Factory.Document.FetchInstance(objectStore, new Id(DocInfo.DocumentGUID),null );
               log.Debug("Preparing to call the delete on the document object.");
               doc.Delete();
               doc.Save(RefreshMode.REFRESH);
               log.Info("The document was successfully deleted from the " + DocInfo.ObjectStore + " ObjectStore.");
            }            
            else
            {
               log.Info("A Document GUID (" + DocInfo.DocumentGUID + ") and an ObjectStore (" + DocInfo.ObjectStore + ") are required to delete a content element.");
            }
         }
         catch (Exception e)
         {
            log.Error("deleteContentElement: Error deleting content element", e);
            throw new Exception("CNO.BPA.FNP8.DocDelete.deleteContentElement: Error deleting content element; " + e.Message, e);
         }
      }
      #endregion
   }
}

