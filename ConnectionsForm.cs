using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IERAX_MissionControl
{
    public class ConnectionsForm : Form
    {
        private readonly MPIeraxMain _main;
        private readonly FlowLayoutPanel _listPanel;
        private readonly Button _refreshButton;

        public ConnectionsForm(MPIeraxMain main)
        {
            _main = main ?? throw new ArgumentNullException(nameof(main));

            Text = "Available Connections";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 520;
            Height = 420;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var topBar = new Panel { Dock = DockStyle.Fill, Height = 40 };
            _refreshButton = new Button { Text = "Refresh", AutoSize = true, Dock = DockStyle.Right };
            _refreshButton.Click += async (s, e) => await PopulateAsync();
            topBar.Controls.Add(_refreshButton);
            root.Controls.Add(topBar, 0, 0);

            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            _listPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8)
            };
            scroll.Controls.Add(_listPanel);
            root.Controls.Add(scroll, 0, 1);

            Load += async (s, e) => await PopulateAsync();
        }

        private async Task PopulateAsync()
        {
            _listPanel.Controls.Clear();
            var items = _main.GetAvailableConnections();
            foreach (var conn in items.Distinct())
            {
                _listPanel.Controls.Add(CreateRow(conn));
            }
        }

        private Control CreateRow(string connection)
        {
            var row = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 6)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            var icon = new PictureBox
            {
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Margin = new Padding(6, 3, 6, 3)
            };
            try
            {
                icon.Image = connection.Contains(":") ? SystemIcons.Application.ToBitmap() : SystemIcons.Information.ToBitmap();
            }
            catch { }

            var label = new Label
            {
                AutoSize = true,
                Text = connection,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 8, 6, 0)
            };

            var btn = new Button
            {
                Text = "Connect",
                AutoSize = true,
                BackColor = Color.LightGray,
                Margin = new Padding(6)
            };

            btn.Click += async (s, e) =>
            {
                btn.Enabled = false;
                btn.Text = "Connecting...";
                try
                {
                    bool ok = await _main.TryConnectToAsync(connection);
                    if (ok)
                    {
                        btn.Text = "Connected";
                        btn.BackColor = Color.Red; // per request
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.Text = "Connect";
                        btn.BackColor = Color.LightGray;
                        btn.ForeColor = Color.Black;
                    }
                }
                finally
                {
                    btn.Enabled = true;
                }
            };

            row.Controls.Add(icon, 0, 0);
            row.Controls.Add(label, 1, 0);
            row.Controls.Add(btn, 2, 0);
            return row;
        }
    }
}
