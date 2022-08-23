//Base
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
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
//Perficient
using MigrateODService;
using MigrateODService.Pages;
//Internal
using CNO.BPA.Framework;


namespace CNO.BPA.FNP8
{
   /// <summary>
   /// FNP8.DocUpdate class allows for updating documents in P8 
   /// </summary>  
   public class DocUpdate : CNO.BPA.FNP8.IDocUpdate 
   {
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.DocUpdate class allows for updating documents in P8 
      /// </summary>   
      public DocUpdate()
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
            throw new Exception("CNO.BPA.FNP8.DocUpdate.getObjectStore: " + ex.Message);
         }
      }
      #endregion

      #region Update
      /// <summary>
      /// Updates the document properties.
      /// </summary>
      /// <param name="UserConn">A user connection object.</param>
      /// <param name="DocInfo">A document info object.</param>
      /// <returns>A string indicating success or reason for failure.</returns>
      public string updateDocument(IUserConnection UserConn, IDocInfo DocInfo)
      {
         try
         {
            //if f docnumber is passed in and no version series id do a search
            if (DocInfo.F_DOCNUMBER.Length > 0 & DocInfo.VersionSeriesID.Length == 0)
            {
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
            }
            if (DocInfo.VersionSeriesID.Length > 0 & DocInfo.ObjectStore.Length > 0)
            {
               //first get a handle to the object store
               IObjectStore objectStore = getObjectStore(DocInfo.ObjectStore, UserConn);
               //setup the property filter to pull back the released version
               PropertyFilter propFilter = new PropertyFilter();
               propFilter.AddIncludeProperty(new FilterElement(null, null, null, PropertyNames.RELEASED_VERSION, null));
               //retreive the document            
               IVersionSeries docRev = Factory.VersionSeries.FetchInstance(objectStore, new Id(DocInfo.VersionSeriesID), propFilter);
               IDocument releasedDoc = (IDocument)docRev.ReleasedVersion;
               //pull the props
               IProperties properties = releasedDoc.Properties;
               IClassDescription myClassDesc = releasedDoc.ClassDescription;
               // Change property value.
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
                        properties[propName] =  propValue;
                     }
                  }
               }
               // Save and update property cache.
               releasedDoc.Save(RefreshMode.REFRESH);
               return "SUCCESS";
            }
            else
            {
               return "Version Series ID and Objectstore are missing";
            }
         }
         catch (Exception e)
         {
            log.Error("CNO.BPA.FNP8.DocExtraction.getDocument: Error retrieving document; ",e);
            return "CNO.BPA.FNP8.DocExtraction.getDocument: Error retrieving document; " + e.Message;
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

      #endregion
   }
}
