using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FileNet.Api.Collection;
using FileNet.Api.Constants;
using FileNet.Api.Core;
using FileNet.Api.Property;
using FileNet.Api.Admin;
using FileNet.Api.Meta;
using FileNet.Api.Util;
using FileNet.Api.Authentication;
using FileNet.Api.Exception;
using CNO.BPA.FNP8.Framework;

namespace CNO.BPA.FNP8
{
   public class FileNetP8 : CNO.BPA.FNP8.IFileNetP8
   {
      #region Variables
      private IConnection _conn = null;
      private IDomain _domain = null;
      private IObjectStore _objectStore = null;
      private IFolder _folder = null;
      private Cryptography _crypto = new Cryptography();
      private string _userName = string.Empty;
      private string _password = string.Empty;
      private string _uri = string.Empty;
      private string _domainName = string.Empty;
      #endregion

      #region Constructor
      /// <summary>
      /// FileNet P8 class allows for interaction with P8 
      /// </summary>    
      public FileNetP8()
      {
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
            //first we want to grab a local copy of the login parameters
            _userName = _crypto.Decrypt(user);
            _password = _crypto.Decrypt(pass);
            _uri = uri;
            _domainName = domain;

            //make sure there is not currently a connection open
            if (null == _conn)
            {
               //call the get connection method 
               _conn = getConnection(_uri, _userName, _password);
               //using the connection call the get domain method
               _domain = getDomain(_conn, _domainName);
            }
         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.logon: " + ex.Message);
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
            UsernameCredentials creds = new UsernameCredentials(user, pword);
            ClientContext.SetProcessCredentials(creds);

            IConnection conn = Factory.Connection.GetConnection(uri);
            return conn;
         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.getConnectionEDU: " + ex.Message);
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
            IDomain domain = null;
            domain = Factory.Domain.FetchInstance(conn, domainName, null);
            return domain;
         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.getDomainEDU: " + ex.Message);
         }
      }
      #endregion

      #region Working with the ObjectStore
      /// <summary>
      /// This method uses the already created domain and returns  
      /// a list of all of the object stores
      /// </summary>      
      /// <returns>List</returns>
      public List<string> getObjectStoreList()
      {
         try
         {
            //establish an internal list to store the object stores in
            List<string> storeList = new List<string>();
            //looop through all of the object stores within the current domain
            foreach (IObjectStore store in _domain.ObjectStores)
            {
               //add the store name to the internal list
               storeList.Add(store.Name);
            }
            //now return the internal list to the caller
            return storeList;
         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.getObjectStoreList: " + ex.Message);
         }
      }
      /// <summary>
      /// This method accepts the name of an object store  
      /// and returns an instance of that object store
      /// </summary>
      /// <param name="objectStoreName">The name of the ObjectStore to return</param>         
      /// <returns> IObjectStore object</returns>
      private IObjectStore getObjectStore(string objectStoreName)
      {
         try
         {
            //extablish and internal objectstore 
            IObjectStore store = null;
            //pull back a reference to the desired object store
            store = Factory.ObjectStore.FetchInstance(_domain, objectStoreName, null);
            //returne the internal object store to the caller
            return store;
         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.getObjectStoreEDU: " + ex.Message);
         }
      }
      #endregion

      #region Working with Document Classes
      public Dictionary<string, string> getDocumentClassProperties(string DocClass)
      {
         PropertyFilter pf = new PropertyFilter();
         Dictionary<string, string> pd = new Dictionary<string, string>();
         IClassDescription doc = Factory.ClassDescription.FetchInstance(_objectStore, DocClass, pf);
         IClassDefinition cd = Factory.ClassDefinition.FetchInstance(_objectStore, DocClass, pf);

         if (cd.Properties["IsSystemOwned"].ToString().ToUpper() == "FALSE")
         {

            foreach (IPropertyDefinition prop in cd.PropertyDefinitions)
            {
               //eliminate system owned, hidden and those that cannot be written to
               if (prop.IsSystemOwned == false && prop.IsHidden == false
                  && prop.Settability == PropertySettability.READ_WRITE)
               {
                  if (prop.IsValueRequired == true)
                  {
                     pd.Add(prop.SymbolicName, "REQUIRED");
                  }
                  else if (prop.ChoiceList != null && prop.Cardinality == Cardinality.LIST)
                  {
                     pd.Add(prop.SymbolicName, "MULTIVALUE");
                  }
                  else
                  {
                     pd.Add(prop.SymbolicName, "OPTIONAL");
                  }
               }
            }
         }
         return pd;
      }
      private IClassDefinition getDocumentClass(string DocClass)
      {
         try
         {
            PropertyFilter pf = new PropertyFilter();
            IClassDefinition cd = Factory.ClassDefinition.FetchInstance(_objectStore, DocClass, pf);
            return cd;
         }
         catch (EngineRuntimeException ere)
         {
            //fetch instance failed.  See if it's because the folder does not exist.
            ExceptionCode code = ere.GetExceptionCode();
            if (code == ExceptionCode.E_OBJECT_NOT_FOUND)
            {
               return null;
            }
            else
               return null;
         }
      }
      public List<string> getDocumentClassList(string ObjectStoreName)
      {
         try
         {
            //establish an internal list to store the object stores in
            List<string> classList = new List<string>();
            _objectStore = getObjectStore(ObjectStoreName);
            //looop through all of the object stores within the current doma`n
            foreach (IClassDescription docClass in _objectStore.ClassDescriptions)
            {
               IClassDefinition cd = getDocumentClass(docClass.Name);
               //add the store name to the internal list
               if (null != cd && cd.Properties["IsSystemOwned"].ToString().ToUpper() == "FALSE")
               {
                  classList.Add(docClass.SymbolicName);
               }
            }
            //now return the internal list to the caller              
            return classList;
         }
         catch (Exception ex)
         {
            throw new Exception("FileNetP8.getObjectStoreList: " + ex.Message);
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
                     //System.Windows.Forms.MessageBox.Show(erex.Message);
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
      public string createDocument(MemoryStream[] Document,  DocInfo DocInfo)
      {
         IDocument newDoc = null;
         try
         {
            _objectStore = getObjectStore(DocInfo.ObjectStore);
            newDoc = Factory.Document.CreateInstance(_objectStore, DocInfo.DocumentClassName);
            IContentElementList contentList = null;
            contentList = Factory.ContentElement.CreateList();
            IContentTransfer content = null;


            string fileName = String.Empty;

            int pageCount = 1;
            foreach (MemoryStream file in Document)
            {
               content = Factory.ContentTransfer.CreateInstance();
               file.Position = 0;
               fileName = DocInfo.FileName + pageCount.ToString() + "." + DocInfo.Extension;
               content.SetCaptureSource(file);
               content.RetrievalName = fileName; //needed for downloading with name from workplace
               contentList.Add(content);

               pageCount++;
            }
            newDoc.ContentElements = contentList;
            newDoc.Checkin(AutoClassify.DO_NOT_AUTO_CLASSIFY, CheckinType.MAJOR_VERSION);


            IClassDescription myClassDesc = null;
            myClassDesc = Factory.ClassDescription.FetchInstance(_objectStore, DocInfo.DocumentClassName, null);
            //add indexes to properties
            IProperties properties = newDoc.Properties;
            //             properties["DocumentTitle"] = fileName;
            Dictionary<string, string> DocProperties = DocInfo.Properties;
            foreach (string propName in DocProperties.Keys)
            {
               object propValue = DocProperties[propName];
               Cardinality propCardinality = new FileNet.Api.Constants.Cardinality();
               bool validIndex = validProperty(propName, ref propValue, myClassDesc.PropertyDescriptions, ref propCardinality);
               if (validIndex == true)
               {

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
                  }
                  else
                  {
                     //Index value passed validation
                     properties[propName] = propValue;
                  }

               }
            }

            //determine mimetype
            newDoc.MimeType = getMimeType(DocInfo.Extension);

            //"image/tiff";
            newDoc.Save(RefreshMode.REFRESH);



            //file to folder if needed
            if (null != _folder)
            {
               IReferentialContainmentRelationship rel = null;
               rel = _folder.File(newDoc, AutoUniqueName.AUTO_UNIQUE, fileName, DefineSecurityParentage.DEFINE_SECURITY_PARENTAGE);
               rel.Save(RefreshMode.NO_REFRESH);
            }
            //current value being returned to consumer
            //return newDoc.Id.ToString();

            //new value that needs to be returned
            return newDoc.VersionSeries.Id.ToString();


         }
         catch (Exception ex)
         {
            try
            {
               if (null != newDoc)
               {
                  newDoc.Delete();
               }
            }
            catch { }
            throw new Exception("FileNetP8.createDocument: " + ex.Message);

         }
      }
      public string createDocument(Dictionary<string, string> DocumentDetails, Dictionary<string, string> DocumentProperties, List<Stream> docStream)
      {
         IDocument myDoc = null;
         try
         {

            //myDoc = Factory.Document.CreateInstance(_objectStore, DocClass);
            //IContentElementList contentList = null;
            //contentList = Factory.ContentElement.CreateList();
            //IContentTransfer content = null;
            //content = Factory.ContentTransfer.CreateInstance();
            //string fileName = DocName + ".tif";

            //content.SetCaptureSource(docStream[0]);
            //content.RetrievalName = fileName; //needed for downloading with name from workplace
            //contentList.Add(content);

            //myDoc.ContentElements = contentList;
            //myDoc.Checkin(AutoClassify.DO_NOT_AUTO_CLASSIFY, CheckinType.MAJOR_VERSION);


            //IClassDescription myClassDesc = null;
            //myClassDesc = Factory.ClassDescription.FetchInstance(_objectStore, DocClass, null);
            ////add indexes to properties
            //IProperties properties = myDoc.Properties;
            //properties["DocumentTitle"] = fileName;
            //foreach (string indexName in DocumentProperties.Keys)
            //{
            //   object indexValue = properties[indexName];
            //   Cardinality propCardinality = new FileNet.Api.Constants.Cardinality();
            //   bool validIndex = validProperty(indexName, ref indexValue, myClassDesc.PropertyDescriptions, ref propCardinality);
            //   if (validIndex == true)
            //   {

            //      if (propCardinality == Cardinality.LIST)
            //      {
            //         IStringList listValue = Factory.StringList.CreateList();
            //         //assume indexValue is a pipe delimited string
            //         string[] values = indexValue.ToString().Split(new Char[] { '|' });
            //         foreach (string value in values)
            //         {
            //            listValue.Add(value);
            //         }
            //         properties[indexName] = listValue;
            //      }
            //      else
            //      {
            //         //Index value passed validation
            //         properties[indexName] = indexValue;
            //      }

            //   }
            //}


            //myDoc.MimeType = "image/tiff";
            //myDoc.Save(RefreshMode.REFRESH);



            ////file to folder if needed
            //if (null != _folder)
            //{
            //   IReferentialContainmentRelationship rel = null;
            //   rel = _folder.File(myDoc, AutoUniqueName.AUTO_UNIQUE, fileName, DefineSecurityParentage.DEFINE_SECURITY_PARENTAGE);
            //   rel.Save(RefreshMode.NO_REFRESH);
            //}
            return myDoc.Id.ToString();

         }
         catch (Exception ex)
         {
            try
            {
               if (null != myDoc)
               {
                  myDoc.Delete();
               }
            }
            catch { }
            throw new Exception("FileNetP8.createDocument: " + ex.Message);

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
                           long parsedLong;
                           if (long.TryParse(indexValue.ToString(), out parsedLong))
                           {
                              indexValue = parsedLong;
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
      private string getMimeType(string p)
      {
         switch (p.ToLower())
         {
            case "tif":
               return "IMAGE/TIFF";
            case "png":
               return "IMAGE/PNG";
            case "bmp":
               return "IMAGE/BMP";
            case "jpg":
               return "IMAGE/JPEG";
            case "gif":
               return "IMAGE/GIF";         
            case "pdf":
               return "APPLICATION/PDF";
            case "xls":
               return "APPLICATION/MS-EXCEL";
            case "doc":
               return "APPLICATION/MSWORD";
            case "msg":
               return "APPLICATION/MSG";
            case "ppt":
               return "APPLICATION/VND.MS-POWERPOINT";
            case "txt":
               return "TEXT/PLAIN";
            case "htm":
               return "TEXT/HTML";
            case "xml":
               return "TEXT/XML";
            case "mpg":
               return "VIDEO/MPEG";
            case "mpeg":
               return "VIDEO/MPEG";
            case "wmv":
               return "VIDEO/X-MS-WMV";
            case "avi":
               return "VIDEO/MSVIDEO";
            case "mp4":
               return "VIDEO/MP4";
            case "mp3":
               return "AUDIO/MPEG";
            case "wav":
               return "AUDIO/X-WAV";
            case "fml":
               return "application/x-file-mirror-list";             
            case "fni":
               return "application/x-FileNETNavigate";
            case "ics":
               return "text/calendar";
            case "css":
               return "text/css";
            case "mid":
               return "audio/x-mid";
            case "m3u":
               return "audio/x-mpegurl";
            case "ram":
               return "audio/x-pn-realaudio";
            case "pptx":
               return "application/vnd.openxmlformats-officedocument.presentationml.presentation";            
            case "ppsx":
               return "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
            case "potx":
               return "application/vnd.openxmlformats-officedocument.presentationml.template";
            case "xlsx":
               return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            case "xltx":
               return "application/vnd.openxmlformats-officedocument.spreadsheetml.template";                  
            case "docx":
               return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            case "dotx":
               return "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
            default:
               return null;
         }     
      }
      #endregion

   }
}
