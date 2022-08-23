//Base
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using log4net;
using System.Data;
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
//Internal
using CNO.BPA.Framework;
using CNO.BPA.FNP8.DataHandler;


namespace CNO.BPA.FNP8
{      
   /// <summary>
   /// FNP8.DocCreate class allows for committing documents to P8 
   /// </summary>  
   public class DocCreate : CNO.BPA.FNP8.IDocCreate
   {
      
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      private IFolder _folder = null;
      DataSet _appConfig = null;
      private string _userName = string.Empty;
      private string _password = string.Empty;
      private string _uri = string.Empty;
      private string _domainName = string.Empty;
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.DocCreate class allows for committing documents to P8 
      /// </summary>    
      public DocCreate()
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
            throw new Exception("CNO.BPA.FNP8.DocCreate.getObjectStore: " + ex.Message);
         }
      }
      #endregion

      #region Foldering
      private IFolder getFolder(IObjectStore store, string folderName)
      {
         IFolder folder = null;
         try
         {
            folder = Factory.Folder.FetchInstance(store, folderName, null);
            folderName = folder.FolderName;
         }
         catch (EngineRuntimeException ere)
         {
            //fetch instance failed.  See if it's because the folder does not exist.
            ExceptionCode code = ere.GetExceptionCode();
            if (code == ExceptionCode.E_OBJECT_NOT_FOUND)
            {
               try
               {
                  folder = createFolder(store, folderName);
               }
               catch (EngineRuntimeException erex)
               {
                  ExceptionCode code2 = erex.GetExceptionCode();
                  if (code2 == ExceptionCode.E_ACCESS_DENIED)
                  {
                     //the user does not have rights to create the folder
                     return null;
                  }
               }
            }
         }

         return folder;

      }
      private IFolder createFolder(IObjectStore store, string folderName)
      {
         IFolder parentFolder = null;
         IFolder newFolder = null;
         try
         {
            string pfolder = getParentFolderName(folderName);
            string nfolder = getChildFolderName(folderName);
            parentFolder = getFolder(store, pfolder);
            newFolder = parentFolder.CreateSubFolder(nfolder);
            newFolder.Save(RefreshMode.NO_REFRESH);
         }
         catch (EngineRuntimeException ere)
         {
            ExceptionCode code = ere.GetExceptionCode();
           // System.Windows.Forms.MessageBox.Show(code.ToString());
         }

         return newFolder;
      }
      private string getParentFolderName(string folderName)
      {
         int LastSlash = folderName.LastIndexOf("/");
         string pfolder = folderName.Substring(0, LastSlash);
         if (pfolder.Length == 0)
         {
            pfolder = "/";
         }
        
         return pfolder;
      }
      private string getChildFolderName(string folderName)
      {
         int LastSlash = folderName.LastIndexOf("/");
         string cfolder = folderName.Substring(LastSlash + 1);
         return cfolder;
      }
      #endregion

      #region Document Creation
      public void createDocument(Stream Document, IUserConnection UserConn, IDocInfo DocInfo)
      {       
         try
         {

            MemoryStream[] newDoc = new MemoryStream[1];
            MemoryStream oldDoc = new MemoryStream();            
            if (DocInfo.IsMulti == false & (DocInfo.Extension.ToLower() == "tif" || DocInfo.Extension.ToLower() == "tiff"))
            {
               TiffUtility tu = new TiffUtility();
               //we need to commit as single pages
               newDoc = tu.SplitTiffImage(Document, System.Drawing.Imaging.EncoderValue.CompressionCCITT4);

            }
            else
            {
               oldDoc.SetLength(Document.Length);
               Document.Read(oldDoc.GetBuffer(), 0, (int)Document.Length);

               oldDoc.Flush();
               newDoc[0] = oldDoc; 
            }
            createDocument(newDoc, UserConn, DocInfo);

         }
         catch (Exception ex)
         {
            log.Error("createDocument: Error creating the document", ex);
            throw new Exception("CNO.BPA.FNP8.DocCreate.createDocument: " + ex.Message);

         }
      }
      public void createDocument(MemoryStream[] Document,  IUserConnection UserConn, IDocInfo DocInfo)
      {
         IDocument newDoc = null;
         IDocument newDocRev = null;
         try
         {
            // we need to start out by getting an object store reference
            IObjectStore objectStore = getObjectStore(DocInfo.ObjectStore, UserConn);
            // we need a place to store the retrieval name
            string fileName = String.Empty;
            // if the caller passes us a version series id, 
            //assume they want a new version of an existing document
            if (DocInfo.VersionSeriesID.Length != 0)
            {
               log.Info("A version series id was passed indicating the caller desires a new version be added to an existing document.");
               #region Create new version
               log.Debug("Preparing to fetch an instance of the version series using the following id: " + DocInfo.VersionSeriesID);
               //retrieve the document series
               IVersionSeries docSeries = Factory.VersionSeries.FetchInstance(objectStore, new Id(DocInfo.VersionSeriesID), null);
               log.Debug("Preparing to checkout the document prior to adding the new version.");
               //check out the document series
               docSeries.Checkout(ReservationType.OBJECT_STORE_DEFAULT, null, null, null);
               docSeries.Save(RefreshMode.REFRESH);
               log.Debug("Preparing to create the new document by calling the docSeries Reservation and casting as an IDocument.");
               //now create the new document version
               newDoc = (IDocument)docSeries.Reservation;
               log.Debug("Preparing to create a new content transfer list object.");
               //prepare the content element
               IContentElementList contentList1 = Factory.ContentTransfer.CreateList();
               log.Debug("Preparing to loop through each memory stream in the array of memory streams passed in."); 
               //now loop through all pages and send to the content list
               int pageCount1 = 1;
               foreach (MemoryStream file in Document)
               {
                  IContentTransfer content1 = Factory.ContentTransfer.CreateInstance();
                  file.Position = 0;
                  fileName = DocInfo.RetrievalName.Substring(0, DocInfo.RetrievalName.Length - 4) + pageCount1.ToString() + "." + DocInfo.Extension;
                  content1.SetCaptureSource(file);
                  content1.RetrievalName = fileName; //needed for downloading with name from workplace
                  contentList1.Add(content1);
                  pageCount1++;
               }
               log.Debug("Adding the content list object to the document reservation content elements.");
               //add the content to the new document version
               newDoc.ContentElements = contentList1;
               newDoc.MimeType = getMimeType(DocInfo.Extension);
               log.Debug("Checking in the new version.");
               newDoc.Checkin(AutoClassify.DO_NOT_AUTO_CLASSIFY, CheckinType.MAJOR_VERSION);
               log.Debug("Saving the new version.");
               newDoc.Save(RefreshMode.REFRESH);
               log.Debug("Returning the document guid (" + newDoc.Id.ToString() + ") and also the version series id (" + newDoc.VersionSeries.Id.ToString() + ") to the caller.");
               //return both guid and version guid to consumer
               DocInfo.DocumentGUID = newDoc.Id.ToString();
               DocInfo.VersionSeriesID = newDoc.VersionSeries.Id.ToString();
               #endregion
            }
            else
            {
               log.Info("A new document will be created based on the information provided.");
               #region Create new document
               newDoc = Factory.Document.CreateInstance(objectStore, DocInfo.DocumentClassName);
               log.Debug("A new document has been created within the document class, " + DocInfo.DocumentClassName);
               IContentElementList contentList = null;
               log.Debug("Preparing to create a new content transfer list object.");
               contentList = Factory.ContentElement.CreateList();
               IContentTransfer content = null;
               log.Debug("Preparing to loop through each memory stream in the array of memory streams passed in.");
               int pageCount = 1;
               foreach (MemoryStream file in Document)
               {
                  content = Factory.ContentTransfer.CreateInstance();
                  file.Position = 0;
                  fileName = DocInfo.RetrievalName + pageCount.ToString() + "." + DocInfo.Extension;
                  content.SetCaptureSource(file);
                  content.RetrievalName = fileName; //needed for downloading with name from workplace
                  contentList.Add(content);
                  pageCount++;
               }
               log.Debug("Adding the content list object to the document reservation content elements.");
               newDoc.ContentElements = contentList;
               newDoc.Checkin(AutoClassify.DO_NOT_AUTO_CLASSIFY, CheckinType.MAJOR_VERSION);
               log.Debug("New document has been checked in.");
               IClassDescription myClassDesc = null;
               myClassDesc = Factory.ClassDescription.FetchInstance(objectStore, DocInfo.DocumentClassName, null);               
               //add indexes to properties
               log.Debug("Pulling back a properties object from the new doc.");
               IProperties properties = newDoc.Properties;
               log.Debug("Creating a dictionary of the property name value pairs that were passed in");
               Dictionary<string, string> DocProperties = DocInfo.Properties;
               log.Debug("Looping through the dictionary to setup each property passed in.");
               foreach (string propName in DocProperties.Keys)
               {
                  object propValue = DocProperties[propName];
                  Cardinality propCardinality = new FileNet.Api.Constants.Cardinality();
                  log.Debug("Calling the validProperty method for the property name of " + propName + " and the property value of " + propValue);
                  bool validIndex = validProperty(propName, ref propValue, myClassDesc.PropertyDescriptions, ref propCardinality);
                  if (validIndex == true)
                  {
                     log.Debug("The property was valid, so now we are able to add it to the properties object.");
                     if (propCardinality == Cardinality.LIST)
                     {
                        IStringList listValue = Factory.StringList.CreateList();
                        //assume indexValue is a pipe delimited string
                        string[] values = propValue.ToString().Split(new Char[] { '|' });
                        foreach (string value in values)
                        {
                           listValue.Add(value);
                        }
                        properties[propName] = listValue;
                        log.Debug(propName + " was successfully populated with a value of " + propValue.ToString());
                     }
                     else
                     {
                        //Index value passed validation
                        properties[propName] = propValue;
                        log.Debug(propName + " was successfully populated with a value of " + propValue);
                     }

                  }
               }
               //determine mimetype
               newDoc.MimeType = getMimeType(DocInfo.Extension);               
               log.Debug("Now we can save the new document.");
               //now we save the document changes
               newDoc.Save(RefreshMode.REFRESH);
               log.Debug("Once saved, we can check to see if we need to file the document in a folder.");
               //file to folder if needed
               if (DocInfo.FolderPath.Length != 0)
               {
                  _folder = getFolder(objectStore, DocInfo.FolderPath);
               }
               else
               {
                  log.Debug ("No folder was passed in.");
                  _folder = null;
               }
               if (null != _folder)
               {
                  IReferentialContainmentRelationship rel = null;
                  rel = _folder.File(newDoc, AutoUniqueName.AUTO_UNIQUE, fileName, DefineSecurityParentage.DEFINE_SECURITY_PARENTAGE);
                  rel.Save(RefreshMode.NO_REFRESH);
               }
               log.Debug("Returning the document guid (" + newDoc.Id.ToString() + ") and also the version series id (" + newDoc.VersionSeries.Id.ToString() + ") to the caller.");
               //return both guid and version guid to consumer
               DocInfo.DocumentGUID = newDoc.Id.ToString();
               DocInfo.VersionSeriesID = newDoc.VersionSeries.Id.ToString();
               #endregion
            }               
         }
         catch (Exception ex)
         {
            try
            {
               log.Debug("An exception has occurred check for and delete the new doc if already established.");
               if (null != newDoc)
               {
                  newDoc.Delete();
               }
            }
            catch { }
            log.Error("createDocument: the document creation failed ", ex);
            throw new Exception("CNO.BPA.FNP8.DocCreate.createDocument: " + ex.Message);

         }
      }
      private bool validProperty(string indexName, ref object indexValue, IPropertyDescriptionList propertyDescs, ref Cardinality propCardinality)
      {
         try
         {
            bool foundProperty = false;
            foreach (IPropertyDescription propDesc in propertyDescs)
            {
               if (propDesc.SymbolicName == indexName)
               {
                  foundProperty = true;
                  propCardinality = propDesc.Cardinality;
                  switch (propDesc.DataType)
                  {                        
                     #region DataTypes
                     case TypeID.DATE:
                        {
                           #region DataType = Date
                           DateTime parsedDate;
                           if (DateTime.TryParse(indexValue.ToString(), out parsedDate))
                           {
                              parsedDate = parsedDate.ToUniversalTime();
                              indexValue = parsedDate;
                              return true;
                           }
                           else
                           {
                              return false;
                           }
                           #endregion
                        }

                     case TypeID.BOOLEAN:
                        {
                           #region DataType = boolean
                           bool parsedBool;
                           if (Boolean.TryParse(indexValue.ToString(), out parsedBool))
                           {
                              indexValue = parsedBool;
                              return true;
                           }
                           else
                           {
                              return false;
                           }
                           #endregion
                        }
                     case TypeID.DOUBLE:
                        {
                           #region DataType = double
                           double parsedDouble;
                           if (Double.TryParse(indexValue.ToString(), out parsedDouble))
                           {
                              indexValue = parsedDouble;
                              return true;
                           }
                           else
                           {
                              return false;
                           }
                           #endregion
                        }
                     case TypeID.LONG:
                        {
                           #region DataType = long
                           //although FileNet indicates the data type as long, 
                           //it expects a java.lang.integer which is and int in C#   
                           int parsedOut;
                           if (int.TryParse(indexValue.ToString(), out parsedOut))
                           {
                              indexValue = parsedOut;
                              return true;
                           }
                           else
                           {
                              return false;
                           }
                           #endregion
                        }
                     case TypeID.STRING:
                        {
                           #region DataType = string
                           //trim value to size if needed
                           IPropertyDescriptionString propDescString = (IPropertyDescriptionString)propDesc;
                           int maxLength = 0;
                           int.TryParse(propDescString.MaximumLengthString.ToString(), out maxLength);
                           if (maxLength > 0 && indexValue.ToString().Length > maxLength)
                           {
                              indexValue = indexValue.ToString().Substring(0, maxLength);
                           }
                           //check if it is a menu...
                           if (propDesc.ChoiceList != null)
                           {
                              //if a choice list exists we should ensure the value is contained within the list
                              for (int i = 0; i < propDesc.ChoiceList.ChoiceValues.Count; i++)
                              {
                                 IChoice choice = (IChoice)propDesc.ChoiceList.ChoiceValues[i];
                                 if (propCardinality == Cardinality.LIST)
                                 //assume the value will be pipe delimited
                                 {
                                    string[] values = indexValue.ToString().Split(new Char[] { '|' });
                                    foreach (string value in values)
                                    {
                                       if (choice.ChoiceStringValue == value)
                                       {
                                          return true;
                                       }
                                    }
                                 }
                                 else
                                 {
                                    if (choice.ChoiceStringValue == indexValue.ToString())
                                    {
                                       return true;
                                    }
                                 }
                              }
                              //if we make it here we did not find a match
                              return false;
                           }
                           else
                           {
                              return true;
                           }
                           #endregion
                        }
                     #endregion
                  }
                  break;
               }
            }

            if (foundProperty == false)
            {
               return false;
            }

            return true;

         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.validProperty: " + ex.Message);

         }

      }
      private string getMimeType(string Extension)
      {
         try
         {
            DataRow[] dr = _appConfig.Tables["CONFIG"].Select("CONFIG_TYPE = 'MIMETYPE' AND CONFIG_NAME = '" + Extension + "'");
            string value = dr[0]["CONFIG_VALUE"].ToString();
            return value;
         }
         catch (Exception ex)
         {
            return String.Empty;
         }
      }
      #endregion
   }
}
