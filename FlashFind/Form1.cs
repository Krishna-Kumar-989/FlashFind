using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlashFind
{
    public partial class Form1 : Form
    {
        private Timer searchTimer;
        private const int debounceInterval = 500;

        public Form1()
        {
            InitializeComponent();
            this.Width = 348;
            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, 0);
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;

            txtSearch.TextChanged += txtSearch_TextChanged;
            lstResults.DoubleClick += lstResults_DoubleClick;
            btnClose.Click += btnClose_Click;
            btnModifyIndexing.Click += btnModifyIndexing_Click;

            searchTimer = new Timer();
            searchTimer.Interval = debounceInterval;
            searchTimer.Tick += SearchTimer_Tick;


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BackColor = Color.FromArgb(255, 255, 253, 208);

            this.BackgroundImage = CreateGradientBackground();

            IntPtr hRgn = CreateRoundRectRgn(0, 0, this.Width, this.Height, 30, 30);
            this.Region = Region.FromHrgn(hRgn);

            txtSearch.Left = (this.ClientSize.Width - txtSearch.Width) / 2;
            txtSearch.Top = (int)(this.ClientSize.Height * 0.15);
            txtSearch.Width = 340;
            txtSearch.Font = new Font("Segoe UI", 16);

            btnClose.Location = new Point(this.ClientSize.Width - btnClose.Width - 10, 10);
          

            lstResults.Left = 10;
            lstResults.Top = txtSearch.Bottom + 20;
            lstResults.Width = this.ClientSize.Width - 20;
            lstResults.Height = this.ClientSize.Height - lstResults.Top - 80;
            lstResults.View = View.Details;

            btnModifyIndexing.Left = 20;
            btnModifyIndexing.Top = this.ClientSize.Height - btnModifyIndexing.Height - 10;

            lblStatus1.Left = this.ClientSize.Width - lblStatus1.Width - 20;
            lblStatus1.Top = this.ClientSize.Height - lblStatus1.Height - 10;

            this.lblTitle.Location = new Point((this.ClientSize.Width - this.lblTitle.PreferredWidth) / 2, 15);
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {   
            searchTimer.Stop();
            searchTimer.Start();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            searchTimer.Stop();

            if (!string.IsNullOrEmpty(txtSearch.Text) && txtSearch.Text.Length >= 3)
            {
                lstResults.Items.Clear();
                lblStatus1.Text = "Searching...";

                var results = SearchIndexedFiles(txtSearch.Text);

                HashSet<string> uniqueResults = new HashSet<string>(results);
                foreach (var result in uniqueResults)
                {
                    string fileName = Path.GetFileName(result);
                    ListViewItem item = new ListViewItem(fileName);
                    item.SubItems.Add(result);
                    lstResults.Items.Add(item);
                }

                lblStatus1.Text = $"{uniqueResults.Count} result(s) found.";
            }
            else
            {
                lstResults.Items.Clear();
                lblStatus1.Text = "Please type at least 3 letters.";
            }
        }

        private void lstResults_DoubleClick(object sender, EventArgs e)
        {
            if (lstResults.SelectedItems.Count > 0)
            {
                string path = lstResults.SelectedItems[0].SubItems[1].Text;
                if (File.Exists(path) || Directory.Exists(path))
                {
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to open: " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("The selected item is not a valid file or directory.");
                }
            }
            else
            {
                MessageBox.Show("No item selected.");
            }
        }

        private List<string> SearchIndexedFiles(string keyword)
        {
            var results = new List<string>();
            string connectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows'";
            string query = $"SELECT System.ItemPathDisplay FROM SYSTEMINDEX WHERE System.FileName LIKE '%{keyword}%'";

            try
            {
                using (OleDbConnection connection = new OleDbConnection(connectionString))
                {
                    connection.Open();
                    using (OleDbCommand command = new OleDbCommand(query, connection))
                    {
                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string path = reader["System.ItemPathDisplay"] as string;
                                if (!string.IsNullOrEmpty(path))
                                    results.Add(path);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message);
            }

            return results;
        }

        private void btnModifyIndexing_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("control.exe", "/name Microsoft.IndexingOptions");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening Indexing Options: " + ex.Message);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse
        );

        private Image CreateGradientBackground()
        {
            int width = this.Width;
            int height = this.Height;

            Bitmap gradientBitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(gradientBitmap))
            {
                Color startColor = Color.FromArgb(230, 230, 255);  
                Color endColor = Color.FromArgb(255, 245, 230);    




                using (Brush brush = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    startColor,
                    endColor,
                    135F
                ))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillRectangle(brush, 0, 0, width, height);
                }
            }

            return gradientBitmap;
        }

    }
}
