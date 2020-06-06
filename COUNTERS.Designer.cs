namespace COUNTERS
{
    partial class COUNTERS
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
            this.components = new System.ComponentModel.Container();
            this.groupCategory = new System.Windows.Forms.GroupBox();
            this.comboCategory = new System.Windows.Forms.ComboBox();
            this.timerCnt = new System.Windows.Forms.Timer(this.components);
            this.groupCounter = new System.Windows.Forms.GroupBox();
            this.comboCounter = new System.Windows.Forms.ComboBox();
            this.groupInstance = new System.Windows.Forms.GroupBox();
            this.comboInstance = new System.Windows.Forms.ComboBox();
            this.progressCnt = new System.Windows.Forms.ProgressBar();
            this.labelValue = new System.Windows.Forms.Label();
            this.timerIcon = new System.Windows.Forms.Timer(this.components);
            this.timerMinimize = new System.Windows.Forms.Timer(this.components);
            this.textCntDesc = new System.Windows.Forms.TextBox();
            this.groupCategory.SuspendLayout();
            this.groupCounter.SuspendLayout();
            this.groupInstance.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupCategory
            // 
            this.groupCategory.Controls.Add(this.comboCategory);
            this.groupCategory.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupCategory.Location = new System.Drawing.Point(12, 12);
            this.groupCategory.Name = "groupCategory";
            this.groupCategory.Size = new System.Drawing.Size(309, 49);
            this.groupCategory.TabIndex = 0;
            this.groupCategory.TabStop = false;
            this.groupCategory.Text = "Category name";
            // 
            // comboCategory
            // 
            this.comboCategory.FormattingEnabled = true;
            this.comboCategory.Location = new System.Drawing.Point(7, 20);
            this.comboCategory.Name = "comboCategory";
            this.comboCategory.Size = new System.Drawing.Size(296, 21);
            this.comboCategory.TabIndex = 0;
            this.comboCategory.SelectedIndexChanged += new System.EventHandler(this.comboCategory_SelectedIndexChanged);
            // 
            // timerCnt
            // 
            this.timerCnt.Enabled = true;
            this.timerCnt.Interval = 25;
            this.timerCnt.Tick += new System.EventHandler(this.timerCnt_Tick);
            // 
            // groupCounter
            // 
            this.groupCounter.Controls.Add(this.comboCounter);
            this.groupCounter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupCounter.Location = new System.Drawing.Point(12, 122);
            this.groupCounter.Name = "groupCounter";
            this.groupCounter.Size = new System.Drawing.Size(309, 49);
            this.groupCounter.TabIndex = 1;
            this.groupCounter.TabStop = false;
            this.groupCounter.Text = "Counter name";
            // 
            // comboCounter
            // 
            this.comboCounter.FormattingEnabled = true;
            this.comboCounter.Location = new System.Drawing.Point(7, 20);
            this.comboCounter.Name = "comboCounter";
            this.comboCounter.Size = new System.Drawing.Size(296, 21);
            this.comboCounter.TabIndex = 0;
            this.comboCounter.SelectedIndexChanged += new System.EventHandler(this.comboCounter_SelectedIndexChanged);
            // 
            // groupInstance
            // 
            this.groupInstance.Controls.Add(this.comboInstance);
            this.groupInstance.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupInstance.Location = new System.Drawing.Point(12, 67);
            this.groupInstance.Name = "groupInstance";
            this.groupInstance.Size = new System.Drawing.Size(309, 49);
            this.groupInstance.TabIndex = 2;
            this.groupInstance.TabStop = false;
            this.groupInstance.Text = "Instance";
            // 
            // comboInstance
            // 
            this.comboInstance.FormattingEnabled = true;
            this.comboInstance.Location = new System.Drawing.Point(7, 20);
            this.comboInstance.Name = "comboInstance";
            this.comboInstance.Size = new System.Drawing.Size(296, 21);
            this.comboInstance.TabIndex = 0;
            this.comboInstance.SelectedIndexChanged += new System.EventHandler(this.comboInstance_SelectedIndexChanged);
            // 
            // progressCnt
            // 
            this.progressCnt.Location = new System.Drawing.Point(327, 142);
            this.progressCnt.Name = "progressCnt";
            this.progressCnt.Size = new System.Drawing.Size(379, 23);
            this.progressCnt.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressCnt.TabIndex = 3;
            // 
            // labelValue
            // 
            this.labelValue.AutoSize = true;
            this.labelValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelValue.Location = new System.Drawing.Point(652, 110);
            this.labelValue.Name = "labelValue";
            this.labelValue.Size = new System.Drawing.Size(55, 29);
            this.labelValue.TabIndex = 4;
            this.labelValue.Text = "100";
            this.labelValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // timerIcon
            // 
            this.timerIcon.Enabled = true;
            this.timerIcon.Interval = 50;
            this.timerIcon.Tick += new System.EventHandler(this.timerIcon_Tick);
            // 
            // timerMinimize
            // 
            this.timerMinimize.Enabled = true;
            this.timerMinimize.Interval = 1;
            this.timerMinimize.Tick += new System.EventHandler(this.timerMinimize_Tick);
            // 
            // textCntDesc
            // 
            this.textCntDesc.Location = new System.Drawing.Point(328, 12);
            this.textCntDesc.Multiline = true;
            this.textCntDesc.Name = "textCntDesc";
            this.textCntDesc.ReadOnly = true;
            this.textCntDesc.Size = new System.Drawing.Size(318, 124);
            this.textCntDesc.TabIndex = 5;
            // 
            // COUNTERS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(719, 183);
            this.Controls.Add(this.textCntDesc);
            this.Controls.Add(this.labelValue);
            this.Controls.Add(this.progressCnt);
            this.Controls.Add(this.groupInstance);
            this.Controls.Add(this.groupCounter);
            this.Controls.Add(this.groupCategory);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "COUNTERS";
            this.Text = "COUNTERS";
            this.groupCategory.ResumeLayout(false);
            this.groupCounter.ResumeLayout(false);
            this.groupInstance.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupCategory;
        private System.Windows.Forms.ComboBox comboCategory;
        private System.Windows.Forms.Timer timerCnt;
        private System.Windows.Forms.GroupBox groupCounter;
        private System.Windows.Forms.ComboBox comboCounter;
        private System.Windows.Forms.GroupBox groupInstance;
        private System.Windows.Forms.ComboBox comboInstance;
        private System.Windows.Forms.ProgressBar progressCnt;
        private System.Windows.Forms.Label labelValue;
        private System.Windows.Forms.Timer timerIcon;
        private System.Windows.Forms.Timer timerMinimize;
        private System.Windows.Forms.TextBox textCntDesc;
    }
}

