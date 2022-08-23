namespace FNP8ControlPanel
{
   partial class Form2
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.btnSearch = new System.Windows.Forms.Button();
         this.label1 = new System.Windows.Forms.Label();
         this.txtQueryString = new System.Windows.Forms.TextBox();
         this.SuspendLayout();
         // 
         // btnSearch
         // 
         this.btnSearch.Location = new System.Drawing.Point(338, 126);
         this.btnSearch.Name = "btnSearch";
         this.btnSearch.Size = new System.Drawing.Size(75, 23);
         this.btnSearch.TabIndex = 0;
         this.btnSearch.Text = "Search";
         this.btnSearch.UseVisualStyleBackColor = true;
         this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(8, 8);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(141, 13);
         this.label1.TabIndex = 1;
         this.label1.Text = "Enter Custom Search Query:";
         // 
         // txtQueryString
         // 
         this.txtQueryString.Location = new System.Drawing.Point(11, 24);
         this.txtQueryString.Multiline = true;
         this.txtQueryString.Name = "txtQueryString";
         this.txtQueryString.Size = new System.Drawing.Size(402, 96);
         this.txtQueryString.TabIndex = 2;
         // 
         // Form2
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(421, 153);
         this.Controls.Add(this.txtQueryString);
         this.Controls.Add(this.label1);
         this.Controls.Add(this.btnSearch);
         this.Name = "Form2";
         this.Text = "Custom Search Query";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Button btnSearch;
      private System.Windows.Forms.Label label1;
      internal System.Windows.Forms.TextBox txtQueryString;
   }
}