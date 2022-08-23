//Base
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Data;
//P8
using FileNet.Api.Core;
using FileNet.Api.Util;
//Perficient
using MigrateODService;
using MigrateODService.Pages;
//Internal
using CNO.BPA.FNP8.DataHandler;

namespace CNO.BPA.FNP8
{
   /// <summary>
   /// FNP8.DocExtraction class allows for extracting documents from P8 
   /// </summary> 
   public class DocExtraction : CNO.BPA.FNP8.IDocExtraction
   {
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      DataSet _appConfig = null;
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.DocExtraction class allows for extracting documents from P8 
      /// </summary>   
      public DocExtraction()
      {
         log.Debug("Preparing to instantiate a new DataAccess object");
         DataAccess dataAccess = new DataAccess();
         log.Debug("Preparing to call selectAppConfigValues");
         _appConfig = dataAccess.selectAppConfigValues("CNO.BPA.FNP8");
         if (null != _appConfig && null != _appConfig.Tables["CONFIG"])
         {
            log.Debug("selectAppConfigValues returned with " + _appConfig.Tables["CONFIG"].Rows.Count + " rows returned.");
         }
      }
      #endregion

      #region Working with the ObjectStore
      /// <summary>
      /// This method accepts the name of an object store  
      /// and returns an instance of that object store
      /// </summary>
      /// <param name="objectStoreName">The name of the ObjectStore to return</param>         
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
            throw new Exception("CNO.BPA.FNP8.DocExtraction.getObjectStore: " + ex.Message);
         }
      }
      #endregion

      #region Extraction
      /// <summary>
      /// Returns an array of memory streams containing the document.
      /// </summary>
      /// <param name="UserConn">A user connection object.</param>
      /// <param name="DocInfo">A document info object.</param>
      /// <returns>A memorty stream containing the document.</returns>
      public MemoryStream[] getDocument(IUserConnection UserConn, IDocInfo DocInfo)
      {
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (DocInfo.F_DOCNUMBER.Length > 0 & DocInfo.VersionSeriesID.Length == 0)
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
                  log.Error("No rows were returned from the search using F_DOCNUMBER '" + DocInfo.F_DOCNUMBER + "'.");
               }
            }
            if (DocInfo.VersionSeriesID.Length > 0 & DocInfo.ObjectStore.Length > 0)
            {
               log.Info("Preparing to extract version series id of '" + DocInfo.VersionSeriesID + "' from the object store '" + DocInfo.ObjectStore + "'.");
               //first get a handle to the object store
               IObjectStore objectStore = getObjectStore(DocInfo.ObjectStore, UserConn);
               log.Debug("Object store reference has been obtained, preparing to fetch an instance of the version series.");
               //retreive the document            
               IVersionSeries docRev = Factory.VersionSeries.FetchInstance(objectStore, new Id(DocInfo.VersionSeriesID), null);
               log.Debug("Version Series reference has been obtained, preparting to call the getDocumentContent method.");
               //now get the actual content
               MemoryStream[] documentPages = getDocumentContent((IDocument)docRev.ReleasedVersion, DocInfo.MSARLocation, DocInfo);
               log.Debug("getDocumentContent has returned.");
               //once we have the content, determine if the caller wants single or multi returned.
               TiffUtility tu = new TiffUtility();
               if (DocInfo.Extension.ToLower() == "tif" || DocInfo.Extension.ToLower() == "tiff")
               {
                  if (DocInfo.IsMulti == true)
                  {
                     log.Debug("The document content is a TIFF and the caller requested the content be returned as a multi-page file.");
                     if (documentPages.Count() > 1)
                     {
                        MemoryStream document = tu.JoinTiffImages(ref documentPages);
                        documentPages = tu.StreamToStreamArray(document);
                        log.Debug("The document content has been joined using the JoinTiffImages method.");
                     }
                  }
                  else
                  {
                     log.Debug("The document content is a TIFF and the caller requested the content be returned as an array of single page files.");
                     if (documentPages.Count() == 1)
                     {
                        Stream doc = documentPages[0];
                        MemoryStream[] document2 = tu.SplitTiffImage(doc, System.Drawing.Imaging.EncoderValue.CompressionCCITT4);
                        documentPages = document2;
                        log.Debug("The document content was stored as a multi-page file and has been burst to singles using the SplitTiffImage method.");
                     }
                  }
               }
               if (null != documentPages)
               {
                  log.Info("Extraction complete returning an array of " + documentPages.Count().ToString() + " file(s).");
               }
               return documentPages;
            }
            else
            {
               log.Info("A Version Series ID (" + DocInfo.VersionSeriesID + ") and an ObjectStore (" + DocInfo.ObjectStore + ") are required to extract a document.");                             
               return null;
            }
         }
         catch (Exception e)
         {
            log.Error("getDocument: Error retrieving document", e);
            throw new Exception("CNO.BPA.FNP8.DocExtraction.getDocument: Error retrieving document; " + e.Message, e);
         }
      }
      /// <summary>
      /// Returns the content of each page of CE document. 
      /// The document content elements can be of type IContentTransfer 
      /// or IContentReference.
      /// In the case of IContentReference, the method calls 
      /// the MOD DLL to decode the reference and retrieve the content 
      /// referenced 
      /// </summary>
      /// <param name="document">A CE document object.</param>
      /// <param name="DocInfo">A document info object.</param>
      /// <returns>The binary representation of the pages of the document.</returns>
      private MemoryStream[] getDocumentContent(IDocument document, string location, IDocInfo docInfo)
      {
         string reference = String.Empty;
         string msarLocations = String.Empty;
         string snaplockLocations = String.Empty;

         try
         {
            int pageCount = 0;
            MemoryStream[] documentPages = new MemoryStream[document.ContentElements.Count];
            log.Debug("Preparing to loop through the content elements of the document.");
            foreach (IContentElement ce in document.ContentElements)
            {
               if (ce is IContentTransfer)
               {
                  log.Debug("The document being extracted is an IContentTransfer element.");
                  docInfo.RetrievalName = ce.Properties["RetrievalName"].ToString();
                  docInfo.Extension = getExtension(ce.Properties["ContentType"].ToString());                  
                  IContentTransfer ct = ce as IContentTransfer;
                  byte[] b = new byte[(int)ct.ContentSize];
                  using (Stream s = ct.AccessContentStream())
                  {
                     int r, offset = 0;
                     while ((r = s.Read(b, offset, b.Length - offset)) > 0)
                        offset += r;
                  }
                  MemoryStream page = new MemoryStream(b);
                  documentPages[pageCount] = page;
               }
               else if (ce is IContentReference)
               {
                  log.Debug("The document being extracted is an IContentReference element.");
                  IContentReference cr = ce as IContentReference;
                  reference = cr.ContentLocation;
                  //Retrieve the content referenced by the ContentReference object 
                  RetrieveDocumentContent rdo = new RetrieveDocumentContent();
                  PageRef prf = null;
                  //we need to determine the storage type so we can build the appropriate path locations
                  string storageType = rdo.GetStorageType(reference);                  
                  if ("MSAR".Equals(storageType))
                  {
                     msarLocations = getMSARLocations();
                     log.Debug("Preparing to call the RetrievePage method for reference: " + reference + ".");
                     log.Debug("MSAR locations to be searched include: " + msarLocations + ".");
                     //Retrieve the content referenced by the ContentReference object
                     prf = rdo.RetrievePage(reference, msarLocations);
                  }
                  else
                  {
                     snaplockLocations = getSnapLockLocations();
                     log.Debug("Preparing to call the RetrievePage method for reference: " + reference + ".");
                     log.Debug("SnapLock locations to be searched include: " + snaplockLocations + ".");
                     //Retrieve the content referenced by the ContentReference object
                     prf = rdo.RetrievePage(reference, snaplockLocations);
                  }                  
                  //pull back the retrieval name and the extension
                  docInfo.RetrievalName = prf.DocInfo.FileName;
                  docInfo.Extension = getExtension(prf.DocInfo.MimeType);
                  //Save the content to the specified location
                  byte[] b = prf.Bytes;
                  MemoryStream pageIS = new MemoryStream(b);
                  documentPages[pageCount] = pageIS;
               }
               pageCount++;
            }
            log.Debug("There were " + pageCount.ToString() + " content elements extracted.");
            return documentPages;
         }
         
         catch (Exception ex)
         {
            log.Error("getDocumentContent: Error retrieving document content", ex);
            //we should check to see if the path indicates it was not found
            if ( ex.Message.Contains("path1"))
            {
               log.Error("The document referenced by: " + reference + " could not be found at either: \r\n MSAR Location:" 
                  + msarLocations + "\r\n OR \r\n SnapLock Location:" + snaplockLocations);
            }
            return null;            
         }
      }
      #endregion

      #region Private Methods
      private string getExtension(string MimeType)
      {
         DataRow[] dr = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'EXTENSION' AND CONFIG_NAME = '" + MimeType + "'");
         string value = dr[0]["CONFIG_VALUE"].ToString();
         return value;
      }
      private string getMSARLocations()
      {
         string value = String.Empty;

         DataRow[] rowsReturned = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'UNC' AND CONFIG_NAME = 'MSAR'");
         foreach (DataRow dr in rowsReturned)
         {
            value += dr["CONFIG_VALUE"].ToString() + ", ";
         }
         //strip last comma and space
         value = value.Substring(0, (value.Length - 2));
         return value;
      }
      private string getSnapLockLocations()
      {
         string value = String.Empty;

         DataRow[] rowsReturned = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'UNC' AND CONFIG_NAME = 'SNAPLOCK'");
         foreach (DataRow dr in rowsReturned)
         {
            value += dr["CONFIG_VALUE"].ToString() + ", ";
         }
         //strip last comma and space
         value = value.Substring(0, (value.Length - 2));
         return value;
      }
      #endregion

   }
}
