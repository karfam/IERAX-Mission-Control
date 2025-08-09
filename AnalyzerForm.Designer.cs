namespace IERAX_MissionControl
{
    partial class AnalyzerForm
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
            this.calculationDataInputButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // calculationDataInputButton
            // 
            this.calculationDataInputButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.calculationDataInputButton.Location = new System.Drawing.Point(1176, 830);
            this.calculationDataInputButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.calculationDataInputButton.Name = "calculationDataInputButton";
            this.calculationDataInputButton.Size = new System.Drawing.Size(288, 147);
            this.calculationDataInputButton.TabIndex = 0;
            this.calculationDataInputButton.Text = "Update Background";
            this.calculationDataInputButton.UseVisualStyleBackColor = true;
            this.calculationDataInputButton.Click += new System.EventHandler(this.calculationDataInputButton_Click);
            // 
            // AnalyzerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1482, 1008);
            this.Controls.Add(this.calculationDataInputButton);
            this.Name = "AnalyzerForm";
            this.Text = "AnalyzerForm";
            this.Load += new System.EventHandler(this.AnalyzerForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button calculationDataInputButton;
    }
}