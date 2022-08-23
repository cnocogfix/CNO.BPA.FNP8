using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using CNO.BPA.FNP8;

namespace FNP8ControlPanel
{
   public partial class Form1 : Form
   {
      public Form1()
      {
         InitializeComponent();
      }

      private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (tabControl1.SelectedTab.Text == "Search")
         {
            InitializeGrid();
         }
      }

      #region Create Tab
      private void btnCreate_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));
            //myconn.logon("http://ntp8s02:9080/wsi/FNCEWS40MTOM/", "P8Domain", "ucFzv0Zb78woH1Wh+Sh9bA==", "TXeapXqEe+NriCsv8YpqWg==");
            //we need to populate the document info class with the necessary details/instructions
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtObjectStore.Text;
            mydocinfo.DocumentClassName = txtDocumentClass.Text;
            mydocinfo.FolderPath = txtFolder.Text;
            //retrieval name must be the name only without extension
            mydocinfo.RetrievalName = txtSourceFile.Text.Substring(txtSourceFile.Text.LastIndexOf("\\") + 1);
            mydocinfo.RetrievalName = mydocinfo.RetrievalName.Substring(0, mydocinfo.RetrievalName.Length - 4);
            mydocinfo.Extension = txtSourceExtension.Text;
            mydocinfo.IsMulti = rbtnMulti.Checked;
            mydocinfo.VersionSeriesID = txtCurrentVersionID.Text;
            mydocinfo.Properties = new Dictionary<string, string>();
            mydocinfo.Properties.Add(txtPropName1.Text, txtPropValue1.Text);
            mydocinfo.Properties.Add(txtPropName2.Text, txtPropValue2.Text);
            mydocinfo.Properties.Add(txtPropName3.Text, txtPropValue3.Text);
            mydocinfo.Properties.Add(txtPropName4.Text, txtPropValue4.Text);
            //now open the file and read it in
            System.IO.Stream myfile = System.IO.File.OpenRead(txtSourceFile.Text);
            IDocCreate mydoc = new DocCreate();
            mydoc.createDocument(myfile, myconn, mydocinfo);
            //once we have a connection and all other info populated, we can call the create document
            txtVersionSeriesID.Text = mydocinfo.VersionSeriesID;
            txtDocumentID.Text = mydocinfo.DocumentGUID;
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }

      }
      private void btnMultiFileCreate_Click(object sender, EventArgs e)
      {
         try
         {
            string input = String.Empty;
            DialogResult dr = ShowInputDialog(ref input);

            if (dr == DialogResult.OK)
            {
               
               //create an array of files to send in
               MemoryStream[] documents = MultiDocArray(input);

               //establish a connection to the desired P8 domain
               IUserConnection myconn = new UserConnection();
               myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));
               //we need to populate the document info class with the necessary details/instructions
               IDocInfo mydocinfo = new DocInfo();
               mydocinfo.ObjectStore = txtObjectStore.Text;
               mydocinfo.DocumentClassName = txtDocumentClass.Text;
               mydocinfo.FolderPath = txtFolder.Text;
               //retrieval name must be the name only without extension
               mydocinfo.RetrievalName = txtSourceFile.Text.Substring(txtSourceFile.Text.LastIndexOf("\\") + 1);
               mydocinfo.RetrievalName = mydocinfo.RetrievalName.Substring(0, mydocinfo.RetrievalName.Length - 4);
               mydocinfo.Extension = txtSourceExtension.Text;
               mydocinfo.IsMulti = rbtnMulti.Checked;
               mydocinfo.VersionSeriesID = txtCurrentVersionID.Text;
               mydocinfo.Properties = new Dictionary<string, string>();
               mydocinfo.Properties.Add(txtPropName1.Text, txtPropValue1.Text);
               mydocinfo.Properties.Add(txtPropName2.Text, txtPropValue2.Text);
               mydocinfo.Properties.Add(txtPropName3.Text, txtPropValue3.Text);
               mydocinfo.Properties.Add(txtPropName4.Text, txtPropValue4.Text);
               IDocCreate mydoc = new DocCreate();
               mydoc.createDocument(documents, myconn, mydocinfo);
               //once we have a connection and all other info populated, we can call the create document
               txtVersionSeriesID.Text = mydocinfo.VersionSeriesID;
               txtDocumentID.Text = mydocinfo.DocumentGUID;
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }
      private MemoryStream[] MultiDocArray(string FilesLocation)
      {
         int fileCount = 0;
         MemoryStream[] mstream = new MemoryStream[Directory.GetFiles(FilesLocation).Count()];
         

         foreach (string fileName in Directory.GetFiles(FilesLocation))
         {
            using (FileStream fileStream = File.OpenRead(fileName))
            {
               MemoryStream memStream = new MemoryStream();
               memStream.SetLength(fileStream.Length);
               fileStream.Read(memStream.GetBuffer(), 0, (int)fileStream.Length);
               mstream[fileCount] = memStream;
               fileCount++;
            }            
         }
         return mstream;
      }
      private static DialogResult ShowInputDialog(ref string input)
      {
         System.Drawing.Size size = new System.Drawing.Size(300, 70);
         Form inputBox = new Form();

         inputBox.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
         inputBox.ClientSize = size;
         inputBox.Text = "Directory containing files for import.";

         System.Windows.Forms.TextBox textBox = new TextBox();
         textBox.Size = new System.Drawing.Size(size.Width - 10, 23);
         textBox.Location = new System.Drawing.Point(5, 5);
         textBox.Text = input;
         inputBox.Controls.Add(textBox);

         Button okButton = new Button();
         okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
         okButton.Name = "okButton";
         okButton.Size = new System.Drawing.Size(75, 23);
         okButton.Text = "&OK";
         okButton.Location = new System.Drawing.Point(size.Width - 80 - 80, 39);
         inputBox.Controls.Add(okButton);

         Button cancelButton = new Button();
         cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         cancelButton.Name = "cancelButton";
         cancelButton.Size = new System.Drawing.Size(75, 23);
         cancelButton.Text = "&Cancel";
         cancelButton.Location = new System.Drawing.Point(size.Width - 80, 39);
         inputBox.Controls.Add(cancelButton);


         DialogResult result = inputBox.ShowDialog();
         input = textBox.Text;
         return result;
      }

      #endregion

      #region Extract Tab
      private void btnExtract_Click(object sender, EventArgs e)
      {
         try
         {

            string startTime = System.DateTime.Now.ToString();

            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            //myconn.logon("http://sit.p8ce.cnoinc.com:80/wsi/FNCEWS40MTOM/", "P8_SIT", "qkDLnnIKG6uUWOkiqxBPkg==", "ySxl+hMmmXmw9bkMdUEP/g==");
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));
            //we need to populate the document info class with the necessary details/instructions
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtExtObjectStore.Text;
            mydocinfo.IsMulti = rbtnExtMulti.Checked;
            mydocinfo.VersionSeriesID = txtExtVersionID.Text;
            mydocinfo.F_DOCNUMBER = txtFDocnumber.Text;
            // mydocinfo.MSARLocation = "\\v_blc_t_filenetsnaplock.nfs.conseco.stg\vol\v_blc_t_filenetsnaplock";
            mydocinfo.MSARLocation = @"D:\FileNet\Storage";
            IDocExtraction myextract = new DocExtraction();

            //get the document
            MemoryStream[] documentPages = myextract.getDocument(myconn, mydocinfo);
            int pageCount = 1;
            if (null != documentPages)
            {
               if (documentPages.Count() > 1)
               {
                  foreach (MemoryStream page in documentPages)
                  {
                     //setup the file name               
                     string fileName = txtDestinationLocation.Text + "\\" + mydocinfo.RetrievalName.Substring(0, (mydocinfo.RetrievalName.Length - 4)) + "_" + pageCount.ToString() + "." + mydocinfo.Extension;
                     //write it to a file
                     using (FileStream file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                     {
                        page.WriteTo(file);
                        file.Flush();
                     }
                     pageCount++;
                  }
               }
               else
               {
                  string fileName = txtDestinationLocation.Text + "\\" + mydocinfo.RetrievalName;
                  //write it to a file
                  using (FileStream file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                  {
                     MemoryStream page = documentPages[0];
                     page.WriteTo(file);
                     file.Flush();
                  }
               }
            }
            else
            {
               MessageBox.Show("Document Not Returned");
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }
      #endregion

      #region Security Tab
      private void btnSetLegalHold_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));            
            //pass in the values for lookup
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtSecObjectStore.Text;
            mydocinfo.VersionSeriesID = txtSecVersID.Text;
            mydocinfo.F_DOCNUMBER = txtSecFDocnumber.Text;
            DocSecurity mySecurity = new DocSecurity();
            mySecurity.SetLegalHold(myconn, mydocinfo);
            txtSecDocGUID.Text = mydocinfo.DocumentGUID;
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }

      private void btnSetNormal_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));            
            //pass in the values for lookup
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtSecObjectStore.Text;
            mydocinfo.VersionSeriesID = txtSecVersID.Text;
            mydocinfo.F_DOCNUMBER = txtSecFDocnumber.Text;
            DocSecurity mySecurity = new DocSecurity();
            mySecurity.SetNormal(myconn, mydocinfo);
            txtSecDocGUID.Text = mydocinfo.DocumentGUID;
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }
      
      private void btnSetPopDoc_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));
            //pass in the values for lookup
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtSecObjectStore.Text;
            mydocinfo.VersionSeriesID = txtSecVersID.Text;
            mydocinfo.F_DOCNUMBER = txtSecFDocnumber.Text;
            DocSecurity mySecurity = new DocSecurity();
            mySecurity.SetNormal(myconn, mydocinfo, true);
            txtSecDocGUID.Text = mydocinfo.DocumentGUID;
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }

      private void btnSetLegalSecure_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));            
            //pass in the values for lookup
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtSecObjectStore.Text;
            mydocinfo.VersionSeriesID = txtSecVersID.Text;
            mydocinfo.F_DOCNUMBER = txtSecFDocnumber.Text;
            IDocSecurity mySecurity = new DocSecurity();
            mySecurity.SetLegalSecure(myconn, mydocinfo);
            txtSecDocGUID.Text = mydocinfo.DocumentGUID;
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }

      private void btnGetCurrentSecurity_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));            
            //pass in the values for lookup
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtSecObjectStore.Text;
            mydocinfo.VersionSeriesID = txtSecVersID.Text;
            mydocinfo.F_DOCNUMBER = txtSecFDocnumber.Text;
            IDocSecurity mySecurity = new DocSecurity();
            string currentSecurity = mySecurity.GetCurrentSecurity(myconn, mydocinfo);
            txtCurrentSecurity.Text = currentSecurity;
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }

      #endregion

      #region Update Tab
      private void btnUpdate_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));
            //myconn.logon("http://sit.p8ce.cnoinc.com:80/wsi/FNCEWS40MTOM/", "P8_SIT", "qkDLnnIKG6uUWOkiqxBPkg==", "ySxl+hMmmXmw9bkMdUEP/g==");
            //pass in the values for lookup
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtUpdtOS.Text;
            mydocinfo.VersionSeriesID = txtUpdtVersID.Text;
            mydocinfo.F_DOCNUMBER = txtUPDTFDocnumber.Text;
            mydocinfo.Properties = new Dictionary<string, string>();
            if (txtUpdtPropValue1.Text.Length > 0)
            {
               mydocinfo.Properties.Add(txtUpdtPropName1.Text, txtUpdtPropValue1.Text);
            }
            if (txtUpdtPropValue2.Text.Length > 0)
            {
               mydocinfo.Properties.Add(txtUpdtPropName2.Text, txtUpdtPropValue2.Text);
            }
            if (txtUpdtPropValue3.Text.Length > 0)
            {
               mydocinfo.Properties.Add(txtUpdtPropName3.Text, txtUpdtPropValue3.Text);
            }
            if (txtUpdtPropValue4.Text.Length > 0)
            {
               mydocinfo.Properties.Add(txtUpdtPropName4.Text, txtUpdtPropValue4.Text);
            }
            IDocUpdate myUpdate = new DocUpdate();
            //myUpdate.versionProperties(myconn, mydocinfo);
            string result = myUpdate.updateDocument(myconn, mydocinfo);
            MessageBox.Show(result);
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }

      }
      #endregion

      #region Search Tab
      private void btnQuerySearch_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));

            //we need to populate the document info class with the necessary details/instructions
            SearchInfo mysearchinfo = new SearchInfo();

            //somehow we need to build an array of the object stores to pass in to the dll
            string[] myobjectstores = new string[1];
            myobjectstores[0] = txtSearchObjectStore.Text;

            //we also need to prepare an integer of the max records to return
            int maxRecords;
            int.TryParse(txtSearchMaxRecords.Text, out maxRecords);

            //for this example we need to build the query string to use
            //string myquery = "SELECT d.VersionSeries, d.Application_Number, d.Doc_Type FROM Document d "
            //   + "WHERE IsClass(d,WN_Health_Policy) AND (d.Company_Code = 'CAF' OR (d.Company_Code = 'WNIC' AND d.Target_System = 'AWD') OR "
            //   + "(d.Company_Code = 'WNIC' AND d.Target_System = 'SOD') OR (d.Company_Code = 'CHIC' AND d.Target_System = 'AWD') OR "
            //   + "(d.Company_Code = 'CHIC' AND d.Target_System = 'SOD') OR (d.Company_Code = 'CIC' AND d.Target_System = 'AWD') OR "
            //   + "(d.Company_Code = 'CIC' AND d.Target_System = 'SOD')) AND d.Doc_Type = 'NBAP' AND d.Policy_Number IS NULL "
            //   + "AND d.Application_Number LIKE '___________'";

            //string myquery = "SELECT d.Policy_Number, d.Last_Name, d.First_Name, d.Soc_Sec_No, "
            //   + "d.Group_Number, d.Agent_Number, d.VersionSeries, d.Received_Date, d.Doc_Type, "
            //   + "d.Batch_Number, d.Box_Number FROM Document d WHERE d.VersionSeries = object({2345219D-DA6A-415A-A673-F19D3F0CCEED})";
            string myquery = String.Empty;

            Form2 searchDialog = new Form2();
            if (searchDialog.ShowDialog(this) == DialogResult.OK)
            {
               myquery = searchDialog.txtQueryString.Text;
            }
            searchDialog.Dispose();

            //once we have everything we can assign the values to the search info object
            mysearchinfo.MaxRecords = maxRecords;
            mysearchinfo.ObjectStores = myobjectstores;
            mysearchinfo.DirectSQLQuery = myquery;

            //now we're ready to perform the search
            DocSearch mysearch = new DocSearch();
            string result = mysearch.Search(myconn, mysearchinfo);

            //now pull back the data table returned
            dataGridView1.DataSource = mysearchinfo.ReturnData;
            txtRecordCount.Text = mysearchinfo.ReturnData.Rows.Count.ToString();
            MessageBox.Show(result);
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }
      private void btnSearch_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));

            //we need to populate the document info class with the necessary details/instructions
            SearchInfo mysearchinfo = new SearchInfo();

            string[] myobjectstores = new string[1];

            myobjectstores[0] = txtSearchObjectStore.Text;
            int maxRecords;
            int.TryParse(txtSearchMaxRecords.Text, out maxRecords);
            mysearchinfo.MaxRecords = maxRecords;
            mysearchinfo.ObjectStores = myobjectstores;
            mysearchinfo.DocumentClasses = txtSearchDocClasses.Lines;
            mysearchinfo.SelectProperties = txtSearchSelectProps.Lines;
            mysearchinfo.OrderByList = txtSearchOrderBy.Lines;

            List<ConditionalProperty> myProps = new List<ConditionalProperty>();

            foreach (DataGridViewRow row in dgConditionalProps.Rows)
            {
               ConditionalProperty myprop = new ConditionalProperty();
               string value = String.Empty;
               if (row.Cells[0].Value != null)
               {
                  myprop.Name = row.Cells[0].Value.ToString();
                  if (row.Cells[2].Value != null)
                  {
                     myprop.Value = row.Cells[2].Value.ToString();
                  }
                  else
                  {
                     myprop.Value = "";
                  }
                  myprop.ConditionalOperator = (ConditionalProperty.COperator)Enum.Parse(typeof(ConditionalProperty.COperator), row.Cells[1].Value.ToString());
                  if (row.Cells[3].Value != null)
                  {
                     myprop.RelationalOperator = (ConditionalProperty.ROperator)Enum.Parse(typeof(ConditionalProperty.ROperator), row.Cells[3].Value.ToString());
                  }
                  myProps.Add(myprop);
               }
            }
            mysearchinfo.ConditionalProperties = myProps;

            //now we're ready to perform the search
            IDocSearch mysearch = new DocSearch();
            string result = mysearch.Search(myconn, mysearchinfo);

            //now pull back the data table returned
            dataGridView1.DataSource = mysearchinfo.ReturnData;
            txtRecordCount.Text = mysearchinfo.ReturnData.Rows.Count.ToString();

            MessageBox.Show(result);
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }
      private void InitializeGrid()
      {
         //start by clearing the grid
         dgConditionalProps.Rows.Clear();
         dgConditionalProps.Columns.Clear();
         dgConditionalProps.Dock = DockStyle.Fill;
         dataGridView1.Rows.Clear();
         dataGridView1.Columns.Clear();
         dataGridView1.Dock = DockStyle.Fill;
         //define and create the db parameter column
         DataGridViewTextBoxColumn colP8DocPropName = new DataGridViewTextBoxColumn();
         colP8DocPropName.Name = "P8DocProp";
         colP8DocPropName.HeaderText = "P8 Prop Name";
         colP8DocPropName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
         dgConditionalProps.Columns.Add(colP8DocPropName);
         //condional prop selection
         DataGridViewComboBoxColumn colConditionalProp = new DataGridViewComboBoxColumn();
         colConditionalProp.Name = "ConditionalProp";
         colConditionalProp.HeaderText = "Condition";
         colConditionalProp.Items.Add("Equals");
         colConditionalProp.Items.Add("NotEquals");
         colConditionalProp.Items.Add("GreaterThan");
         colConditionalProp.Items.Add("LessThan");
         colConditionalProp.Items.Add("Like");
         colConditionalProp.Items.Add("Null");
         colConditionalProp.Items.Add("NotNull");
         colConditionalProp.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
         dgConditionalProps.Columns.Add(colConditionalProp);
         //define and create the ia value column
         DataGridViewTextBoxColumn colP8DocPropValue = new DataGridViewTextBoxColumn();
         colP8DocPropValue.HeaderText = "P8 Prop Value";
         colP8DocPropValue.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
         dgConditionalProps.Columns.Add(colP8DocPropValue);
         //relational prop selection
         DataGridViewComboBoxColumn colRelationalProp = new DataGridViewComboBoxColumn();
         colRelationalProp.Name = "RelationalProp";
         colRelationalProp.HeaderText = "Relational";
         colRelationalProp.Items.Add("And");
         colRelationalProp.Items.Add("Or");
         colRelationalProp.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
         dgConditionalProps.Columns.Add(colRelationalProp);
         dgConditionalProps.RowHeadersVisible = false;
         pnlDGConditionalProps.Visible = true;

      }


      #endregion

      #region Delete Tab
      private void btnDeleteDocument_Click(object sender, EventArgs e)
      {
         try
         {
            //establish a connection to the desired P8 domain
            IUserConnection myconn = new UserConnection();
            myconn.logon(txtP8URI.Text, txtP8Domain.Text, myconn.Encrypt(txtP8UID.Text), myconn.Encrypt(txtP8PWD.Text));
            //create and populate the doc info object
            IDocInfo mydocinfo = new DocInfo();
            mydocinfo.ObjectStore = txtDelObjectStore.Text;
            mydocinfo.DocumentGUID = txtDelDocGUID.Text;
            DocDelete myDocDelete = new DocDelete();
            myDocDelete.deleteContentElement(myconn, mydocinfo);
         }
         catch (Exception ex)
         {
            MessageBox.Show("An Error Occurred: " + ex.Message);
         }
         finally
         {
            MessageBox.Show("Process Complete");
         }
      }
      #endregion








   }

}
