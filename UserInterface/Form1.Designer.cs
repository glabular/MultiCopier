namespace UserInterface;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        tabControl1 = new TabControl();
        tabPage1 = new TabPage();
        label2 = new Label();
        targetFolderComboBox = new ComboBox();
        pannelAddFilesToEncrypt = new Panel();
        label1 = new Label();
        tabPage2 = new TabPage();
        panel1 = new Panel();
        tabControl1.SuspendLayout();
        tabPage1.SuspendLayout();
        pannelAddFilesToEncrypt.SuspendLayout();
        tabPage2.SuspendLayout();
        SuspendLayout();
        // 
        // tabControl1
        // 
        tabControl1.Controls.Add(tabPage1);
        tabControl1.Controls.Add(tabPage2);
        tabControl1.Location = new Point(12, 12);
        tabControl1.Name = "tabControl1";
        tabControl1.SelectedIndex = 0;
        tabControl1.Size = new Size(420, 287);
        tabControl1.TabIndex = 0;
        // 
        // tabPage1
        // 
        tabPage1.Controls.Add(label2);
        tabPage1.Controls.Add(targetFolderComboBox);
        tabPage1.Controls.Add(pannelAddFilesToEncrypt);
        tabPage1.Location = new Point(4, 24);
        tabPage1.Name = "tabPage1";
        tabPage1.Padding = new Padding(3);
        tabPage1.Size = new Size(412, 259);
        tabPage1.TabIndex = 0;
        tabPage1.Text = "Добавить файлы";
        tabPage1.UseVisualStyleBackColor = true;
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(6, 3);
        label2.Name = "label2";
        label2.Size = new Size(91, 15);
        label2.TabIndex = 2;
        label2.Text = "Целевая папка:";
        // 
        // targetFolderComboBox
        // 
        targetFolderComboBox.FormattingEnabled = true;
        targetFolderComboBox.Location = new Point(3, 21);
        targetFolderComboBox.Name = "targetFolderComboBox";
        targetFolderComboBox.Size = new Size(231, 23);
        targetFolderComboBox.TabIndex = 1;
        targetFolderComboBox.SelectedIndexChanged += targetFolderComboBox_SelectedIndexChanged;
        // 
        // pannelAddFilesToEncrypt
        // 
        pannelAddFilesToEncrypt.AllowDrop = true;
        pannelAddFilesToEncrypt.BackColor = Color.Gray;
        pannelAddFilesToEncrypt.Controls.Add(label1);
        pannelAddFilesToEncrypt.Location = new Point(6, 141);
        pannelAddFilesToEncrypt.Name = "pannelAddFilesToEncrypt";
        pannelAddFilesToEncrypt.Size = new Size(400, 112);
        pannelAddFilesToEncrypt.TabIndex = 0;
        pannelAddFilesToEncrypt.DragDrop += pannelAddFilesToEncrypt_DragDrop;
        pannelAddFilesToEncrypt.DragEnter += pannelAddFilesToEncrypt_DragEnter;
        pannelAddFilesToEncrypt.DragLeave += pannelAddFilesToEncrypt_DragLeave;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(144, 52);
        label1.Name = "label1";
        label1.Size = new Size(101, 15);
        label1.TabIndex = 0;
        label1.Text = "Сбросить файлы";
        // 
        // tabPage2
        // 
        tabPage2.Controls.Add(panel1);
        tabPage2.Location = new Point(4, 24);
        tabPage2.Name = "tabPage2";
        tabPage2.Padding = new Padding(3);
        tabPage2.Size = new Size(412, 259);
        tabPage2.TabIndex = 1;
        tabPage2.Text = "Расшифровать файлы";
        tabPage2.UseVisualStyleBackColor = true;
        // 
        // panel1
        // 
        panel1.AllowDrop = true;
        panel1.BackColor = Color.MediumSlateBlue;
        panel1.Location = new Point(6, 144);
        panel1.Name = "panel1";
        panel1.Size = new Size(400, 109);
        panel1.TabIndex = 0;
        panel1.DragDrop += panel1_DragDrop;
        panel1.DragEnter += panel1_DragEnter;
        panel1.DragLeave += panel1_DragLeave;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(444, 333);
        Controls.Add(tabControl1);
        Name = "Form1";
        Text = "Form1";
        tabControl1.ResumeLayout(false);
        tabPage1.ResumeLayout(false);
        tabPage1.PerformLayout();
        pannelAddFilesToEncrypt.ResumeLayout(false);
        pannelAddFilesToEncrypt.PerformLayout();
        tabPage2.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private TabControl tabControl1;
    private TabPage tabPage1;
    private TabPage tabPage2;
    private Panel pannelAddFilesToEncrypt;
    private Label label1;
    private Panel panel1;
    private ComboBox targetFolderComboBox;
    private Label label2;
}
