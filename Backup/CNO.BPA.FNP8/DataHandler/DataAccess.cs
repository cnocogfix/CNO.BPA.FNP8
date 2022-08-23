//Base
using System;
using System.Data;
using System.Data.OracleClient;
using System.Xml;
using System.Reflection;
using System.IO;
//Internal
using CNO.BPA.Framework;


namespace CNO.BPA.FNP8.DataHandler
{
   public class DataAccess : IDisposable
   {
      #region Procedure Names
      private string INSERT_CONFIG = "BPA_APPS.PKG_APP_CONFIG.INSERT_CONFIG";
      private string SELECT_CONFIG = "BPA_APPS.PKG_APP_CONFIG.SELECT_CONFIG";
      #endregion

      #region Variables
      private log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      private OracleConnection _connection = null;
      private OracleTransaction _transaction = null;
      private CNO.BPA.Framework.XML.Parser _xmlParser = null;
      private Cryptography crypto = new Cryptography();
      private string _appConfigLocation = null;      
      private string _connectionString = null;
      private string _activeRegion = String.Empty;
      private string _DSN = String.Empty;
      private string _DBUser = String.Empty;
      private string _DBPass = String.Empty;

      #endregion

      #region Constructors
      public DataAccess()
      {
         //locate the app config
         _appConfigLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CNO.BPA.FNP8.config");
         //create a new instance of the parser class
         _xmlParser = new CNO.BPA.Framework.XML.Parser(_appConfigLocation, "configuration");
         //now we need to pull the active region
         _activeRegion = _xmlParser.GetCustomAttribute(
            "Database/Connections", "active");
         //next grab a copy of each of the db values
         _DSN = _xmlParser.GetCustomAttribute(
            "Database/Connections/" + _activeRegion, "dsn");
         _DBUser = crypto.Decrypt(_xmlParser.GetCustomAttribute(
            "Database/Connections/" + _activeRegion, "usr"));
         _DBPass = crypto.Decrypt(_xmlParser.GetCustomAttribute(
            "Database/Connections/" + _activeRegion, "pwd"));

         //check to see that we have values for the db info
         if (_DSN.Length != 0 & _DBUser.Length != 0 &
             _DBPass.Length != 0)
         {
            //build the connection string
            _connectionString = "Data Source=" + _DSN + ";Persist Security Info=True;User ID="
               + _DBUser + ";Password=" + _DBPass + "";
         }
         else
         {
            throw new ArgumentNullException("-266007825; Database information was "
               + "not found in the configuration file.");
         }
         //we need to pull in the list of fully qualified procedures
         populateProcedureVariables();

      }
      #endregion

      #region Private Methods
      internal void populateProcedureVariables()
      {
         //we need to pull in the list of fully qualified procedures
         XmlNodeList procList = _xmlParser.GetNodeList("Database/Procedures");
         foreach (XmlNode nodeProcedure in procList)
         {
            switch (nodeProcedure.Name)
            {
               case "INSERT_CONFIG":
                  INSERT_CONFIG = _xmlParser.GetCustomAttribute(nodeProcedure, "value");
                  break;
               case "SELECT_CONFIG":
                  SELECT_CONFIG = _xmlParser.GetCustomAttribute(nodeProcedure, "value");
                  break;
            }
         }
      }
      /// <summary>
      /// Connects and logs in to the database, and begins a transaction.
      /// </summary>
      public void Connect()
      {
         _connection = new OracleConnection();
         _connection.ConnectionString = _connectionString;
         try
         {
            _connection.Open();
            _transaction = _connection.BeginTransaction();
         }
         catch (Exception ex)
         {
            throw new Exception("An error occurred while connecting to the database.", ex);
         }
      }
      /// <summary>
      /// Commits the current transaction and disconnects from the database.
      /// </summary>
      public void Disconnect()
      {
         try
         {
            if (null != _connection)
            {
               _transaction.Commit();
               _connection.Close();
               _connection = null;
               _transaction = null;
            }
         }
         catch (Exception ex)
         {
            throw new Exception("CNO.BPA.FNP8.DataHandler.DataAccess.Disconnect: " + ex.Message);
         }
      }
      /// <summary>
      /// Commits all of the data changes to the database.
      /// </summary>
      internal void Commit()
      {
         _transaction.Commit();
      }
      /// <summary>
      /// Cancels the transaction and voids any changes to the database.
      /// </summary>
      public void Cancel()
      {
         _transaction.Rollback();
         _connection.Close();
         _connection = null;
         _transaction = null;
      }
      /// <summary>
      /// Generates the command object and associates it with the current transaction object
      /// </summary>
      /// <param name="commandText"></param>
      /// <param name="commandType"></param>
      /// <returns></returns>
      internal OracleCommand GenerateCommand(string commandText, System.Data.CommandType commandType)
      {
         OracleCommand cmd = new OracleCommand(commandText, _connection);
         cmd.Transaction = _transaction;
         cmd.CommandType = commandType;
         return cmd;
      }
      #endregion

      #region Public Methods
      public DataSet selectAppConfigValues(string AppName)
      {
         try
         {
            DataSet DataSetResults = new DataSet();
            Connect();
            OracleCommand cmd = GenerateCommand(SELECT_CONFIG, CommandType.StoredProcedure);
            DBUtilities.CreateAndAddParameter("p_in_app_name",
              AppName, OracleType.VarChar, ParameterDirection.Input, cmd);
            DBUtilities.CreateAndAddParameter("p_out_cursor",
               DBNull.Value, OracleType.Cursor, ParameterDirection.Output,
               cmd);
            DBUtilities.CreateAndAddParameter("p_out_result",
               OracleType.VarChar, ParameterDirection.Output, 255, cmd);
            DBUtilities.CreateAndAddParameter("p_out_error_message",
               OracleType.VarChar, ParameterDirection.Output, 4000, cmd);

            using (OracleDataReader dataReader = cmd.ExecuteReader())
            {
               if (cmd.Parameters["p_out_result"].Value.ToString()
                  .ToUpper() != "SUCCESSFUL")
               {
                  throw new Exception("-266088529; Procedure Error: " +
                     cmd.Parameters["p_out_result"].Value.ToString() +
                     "; Oracle Error: " + cmd.Parameters[
                     "p_out_error_message"].Value.ToString());
               }
               else
               {
                  if (dataReader.HasRows)
                  {
                     DataTable dt = new DataTable("CONFIG");
                     DataSetResults.Tables.Add(dt);
                     DataSetResults.Load(dataReader, LoadOption.PreserveChanges, DataSetResults.Tables[0]);
                     Disconnect();
                     return DataSetResults;
                  }
                  else
                  {
                     Disconnect();
                     return null;
                  }
               }
            }
         }
         catch (Exception ex)
         {
            throw new Exception("CNO.BPA.FNP8.DataHandler.DataAccess.selectAppConfigValues: " + ex.Message);
         }
      }
 
      public DataSet selectAppConfigValues(string AppName, string ConfigType)
      {
         try
         {
            DataSet DataSetResults = new DataSet();
            Connect();
            OracleCommand cmd = GenerateCommand(SELECT_CONFIG, CommandType.StoredProcedure);
            DBUtilities.CreateAndAddParameter("p_in_app_name",
              AppName, OracleType.VarChar, ParameterDirection.Input, cmd);
            DBUtilities.CreateAndAddParameter("p_in_config_type",
              ConfigType, OracleType.VarChar, ParameterDirection.Input, cmd);
            DBUtilities.CreateAndAddParameter("p_out_cursor",
               DBNull.Value, OracleType.Cursor, ParameterDirection.Output,
               cmd);
            DBUtilities.CreateAndAddParameter("p_out_result",
               OracleType.VarChar, ParameterDirection.Output, 255, cmd);
            DBUtilities.CreateAndAddParameter("p_out_error_message",
               OracleType.VarChar, ParameterDirection.Output, 4000, cmd);

            using (OracleDataReader dataReader = cmd.ExecuteReader())
            {
               if (cmd.Parameters["p_out_result"].Value.ToString()
                  .ToUpper() != "SUCCESSFUL")
               {
                  throw new Exception("-266088529; Procedure Error: " +
                     cmd.Parameters["p_out_result"].Value.ToString() +
                     "; Oracle Error: " + cmd.Parameters[
                     "p_out_error_message"].Value.ToString());
               }
               else
               {
                  if (dataReader.HasRows)
                  {
                     DataTable dt = new DataTable("CONFIG");
                     DataSetResults.Tables.Add(dt);
                     DataSetResults.Load(dataReader, LoadOption.PreserveChanges, DataSetResults.Tables[0]);
                     Disconnect();
                     return DataSetResults;
                  }
                  else
                  {
                     Disconnect();
                     return null;
                  }
               }
            }
         }
         catch (Exception ex)
         {
            throw new Exception("CNO.BPA.FNP8.DataHandler.DataAccess.selectAppConfigValues: " + ex.Message);
         }
      }
 
      #endregion

      #region IDisposable Members

      public void Dispose()
      {
         crypto = null;
         _connection = null;
         _connectionString = null;
         _transaction = null;
         _appConfigLocation = null;
         _xmlParser = null;
         _activeRegion = String.Empty;
         _DSN = String.Empty;
         _DBUser = String.Empty;
         _DBPass = String.Empty;
      }

      #endregion
   }

}
