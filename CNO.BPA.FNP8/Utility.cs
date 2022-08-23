//Base
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Data;
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
//Internal
using CNO.BPA.Framework;
using FileNet.Api.Query;

namespace CNO.BPA.FNP8
{
   /// <summary>
   /// FNP8.DocSearch class allows for searching for documents in P8 
   /// </summary> 
   internal class Utility
   {
      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      #endregion

      #region Constructor
      /// <summary>
      /// FNP8.DocSearch class allows for searching for documents in P8 
      /// </summary>   
      public Utility()
      {
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
            throw new Exception("CNO.BPA.FNP8.DocSearch.getObjectStore: " + ex.Message);
         }
      }
      #endregion

      #region Public Methods
      public List<string> getChoiceList(IUserConnection UserConn, ISearchInfo searchInfo)
      {
         try
         {
            if (null != searchInfo.ObjectStores)
            {
               //obtain a handle to the object store
               IObjectStore objectStore = getObjectStore(searchInfo.ObjectStores[0], UserConn);

               //retrieve the class description
               IClassDescription myClassDesc = null;
               myClassDesc = Factory.ClassDescription.FetchInstance(objectStore, "Document", null);

               //create the SearchSQL object.
               SearchSQL sqlObject = new SearchSQL();
               //set the maximum number of records to be returned. 
               sqlObject.SetMaxRecords(searchInfo.MaxRecords);

               if (searchInfo.DirectSQLQuery.Length == 0)
               {
                  #region Select Helper
                  // Specify the SELECT list using the setSelectList method.
                  string select = String.Empty;
                  foreach (string property in searchInfo.SelectProperties)
                  {
                     select += "d." + property + ", ";
                  }
                  //remove the final comma
                  select = select.Substring(0, (select.Length - 2));

                  //now we can send the select parameters into the helper
                  sqlObject.SetSelectList(select);
                  #endregion

                  #region From Helper
                  // Specify the FROM clause using the setFromClauseInitialValue method.
                  // Symbolic name of class.
                  string myClassName1 = "Document";
                  // Alias name.
                  string myAlias1 = "d";
                  // Indicates whether subclasses are included.
                  bool subclassesToo = true;
                  //now use the helper object to set it
                  sqlObject.SetFromClauseInitialValue(myClassName1, myAlias1, subclassesToo);
                  #endregion

                  #region Where Helper
                  //next let's build the where clause
                  string whereClause = buildWhereClause(searchInfo, myClassDesc);
                  //now use the helper object to set it
                  sqlObject.SetWhereClause(whereClause);
                  #endregion

                  #region OrderBy Helper
                  // Specify the ORDER BY clause using the setOrderByClause method.
                  if (null != searchInfo.OrderByList && searchInfo.OrderByList.Count() > 0)
                  {
                     string orderClause = String.Empty; // "d.F_DOCNUMBER";            
                     foreach (string orderbyprop in searchInfo.OrderByList)
                     {
                        orderClause += "d." + orderbyprop + ", ";
                     }
                     //remove the final comma
                     orderClause = orderClause.Substring(0, (orderClause.Length - 2));
                     //now use the helper object to set it
                     sqlObject.SetOrderByClause(orderClause);
                  }
                  #endregion
               }
               else
               {
                  //just use whatever the caller passed in
                  sqlObject.SetQueryString(searchInfo.DirectSQLQuery);
               }
               // Check the SQL statement constructed.
               System.Console.WriteLine("SQL: " + sqlObject.ToString());

               #region Create and Perform Search
               // Create a SearchScope instance and test the SQL statement.
               SearchScope searchScope = new SearchScope(objectStore);

               // Uses fetchRows to test the SQL statement.
               IRepositoryRowSet rowSet = searchScope.FetchRows(sqlObject, null, null, true);
               #endregion

               #region Search Results
               // You can then iterate through the collection of rows to access the properties.
               int rowCount = 0;
               DataTable searchResults = new DataTable("Results");
               foreach (IRepositoryRow row in rowSet)
               {
                  DataRow workingRow = searchResults.NewRow();

                  foreach (IProperty rowprop in row.Properties)
                  {
                     string propname = rowprop.GetPropertyName();
                     if (!searchResults.Columns.Contains(propname))
                     {
                        searchResults.Columns.Add(propname);
                     }
                     System.Console.WriteLine(rowprop.GetType());
                     string value = String.Empty;
                     string DataType = rowprop.GetType().ToString();
                     //now find the type based on the name

                     switch (DataType)
                     {
                        case "FileNet.Apiimpl.Property.PropertyIdImpl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              value = rowprop.GetIdValue().ToString();
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyEngineObjectImpl":
                           if (rowprop.GetState() == PropertyState.REFERENCE)
                           {
                              IVersionSeries vsID = (IVersionSeries)rowprop.GetObjectValue();
                              value = vsID.Id.ToString();
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyStringImpl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              value = rowprop.GetStringValue().ToString();
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyStringListImpl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              foreach (string val in rowprop.GetStringListValue())
                              {
                                 value += val + "|";
                              }
                              value = value.Substring(0, (value.Length - 1));
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyInteger32Impl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              value = "" + rowprop.GetInteger32Value().ToString();
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyDateTimeImpl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              value = rowprop.GetDateTimeValue().ToString();
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyDoubleImpl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              value = rowprop.GetFloat64Value().ToString();
                           }
                           break;
                        case "FileNet.Apiimpl.Property.PropertyBooleanImpl":
                           if (rowprop.GetState() == PropertyState.VALUE)
                           {
                              value = rowprop.GetBooleanValue().ToString();
                           }
                           break;
                     }
                     workingRow[propname] = value;

                  }
                  searchResults.Rows.Add(workingRow);
               }
               searchInfo.ReturnData = searchResults;
               #endregion

               return "SUCCESS";
            }
            else
            {
               return "An Object Store is required to perform a search";
            }
         }
         catch (Exception ex)
         {
            return "EXCEPTION ENCOUNTERED: " + ex.Message;
         }
      }
      #endregion

      #region Private Methods
      private string buildWhereClause(ISearchInfo searchInfo, IClassDescription classDescription)
      {
         string whereClause = String.Empty;
         string conditionalOperator = String.Empty;
         string searchValue = String.Empty;
         string relationalOperator = String.Empty;
         //loop through all of the conditional properties
         foreach (ConditionalProperty cproperty in searchInfo.ConditionalProperties)
         {
            whereClause += "d." + cproperty.Name + " ";

            conditionalOperator = GetConditionalOperator(cproperty.ConditionalOperator);
            whereClause += conditionalOperator + " ";

            if (cproperty.ConditionalOperator != ConditionalProperty.COperator.NotNull & cproperty.ConditionalOperator != ConditionalProperty.COperator.Null)
            {
               searchValue = GetSearchValue(cproperty, classDescription);
               whereClause += searchValue + " ";
            }
            relationalOperator = GetRelationalOperator(cproperty.RelationalOperator);
            whereClause += relationalOperator + " ";
         }
         //remove the final comma and relational value 
         whereClause = whereClause.Substring(0, (whereClause.LastIndexOf(relationalOperator) - 1));

         //next determine if they want to limit their query by 1 or more document classes
         if (null != searchInfo.DocumentClasses && searchInfo.DocumentClasses.Count() > 0)
         {
            //start by adding an and to the end of the where clause
            whereClause += " AND ";
            foreach (string docClass in searchInfo.DocumentClasses)
            {
               whereClause += "IsClass(d," + docClass + ") OR ";
            }
            //remove the final relational value 
            whereClause = whereClause.Substring(0, (whereClause.LastIndexOf("OR") - 1));
         }
         return whereClause;

      }
      private string GetSearchValue(ConditionalProperty cProperty, IClassDescription classDescription)
      {
         //start by looping through the property descriptions
         foreach (IPropertyDescription prop in classDescription.ProperSubclassPropertyDescriptions)
         {
            if (prop.SymbolicName == cProperty.Name)
            {
               switch (prop.DataType)
               {
                  case TypeID.GUID:
                     return "Id(" + cProperty.Value + ")";
                  case TypeID.OBJECT:
                     return "object(" + cProperty.Value + ")";
                  case TypeID.STRING:
                     return "'" + cProperty.Value + "'";
                  case TypeID.LONG:
                     return cProperty.Value;
                  case TypeID.DATE:
                     return cProperty.Value;
                  case TypeID.DOUBLE:
                     return cProperty.Value;
                  case TypeID.BOOLEAN:
                     return cProperty.Value;
                  default:
                     return cProperty.Value;
               }
            }
         }
         //if we end up here... the property searched for does not appear to be part of the class 
         //description so just return the value they are looking for
         if (cProperty.Name == "VersionSeries")
         {
            return "object(" + cProperty.Value + ")";
         }
         else
         {
            return "'" + cProperty.Value + "'";
         }
      }
      private string GetRelationalOperator(ConditionalProperty.ROperator rOperator)
      {
         switch (rOperator)
         {
            case ConditionalProperty.ROperator.And:
               return "AND";
            case ConditionalProperty.ROperator.Or:
               return "OR";
            default:
               return "AND";
         }
      }
      private string GetConditionalOperator(ConditionalProperty.COperator cOperator)
      {
         switch (cOperator)
         {
            case ConditionalProperty.COperator.Equals:
               return "=";
            case ConditionalProperty.COperator.NotEquals:
               return "!=";
            case ConditionalProperty.COperator.Like:
               return "LIKE";
            case ConditionalProperty.COperator.GreaterThan:
               return ">";
            case ConditionalProperty.COperator.LessThan:
               return "<";
            case ConditionalProperty.COperator.Null:
               return "is null";
            case ConditionalProperty.COperator.NotNull:
               return "is not null";
            default:
               return "=";
         }
      }
      #endregion
   }
}
