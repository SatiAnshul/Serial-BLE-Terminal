using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace BLE_SERIAL_TERMINAL
{
    partial class TerminalScreen
    {
        private System.ComponentModel.IContainer components = null;

        private Guna2ComboBox ddlDevices;
        private Guna2Button btnScan;
        private Guna2Button btnConnect;

        private Guna2TextBox txtSend;
        private Guna2Button btnSend;

        private Guna2CheckBox chkEcho;
        private Guna2CheckBox chkTimestamp;

        private Guna2ComboBox ddlLineBreak;

        private Guna2Button btnClearLog;
        private Guna2Button btnSaveLog;

        private Guna2TextBox txtLogs;

        // 🔥 NEW: Status Indicator
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ddlDevices = new Guna2ComboBox();
            btnScan = new Guna2Button();
            btnConnect = new Guna2Button();
            txtSend = new Guna2TextBox();
            btnSend = new Guna2Button();
            chkEcho = new Guna2CheckBox();
            chkTimestamp = new Guna2CheckBox();
            ddlLineBreak = new Guna2ComboBox();
            btnClearLog = new Guna2Button();
            btnSaveLog = new Guna2Button();
            txtLogs = new Guna2TextBox();
            lblStatus = new Label();

            SuspendLayout();

            // ddlDevices
            ddlDevices.Location = new Point(20, 20);
            ddlDevices.Size = new Size(350, 36);
            ddlDevices.DropDownStyle = ComboBoxStyle.DropDownList;
            ddlDevices.BorderRadius = 6;
            ddlDevices.FillColor = Color.White;
            ddlDevices.ForeColor = Color.Black;

            // btnScan
            btnScan.Location = new Point(380, 20);
            btnScan.Size = new Size(160, 36);
            btnScan.Text = "Scan Nearby Devices";
            btnScan.BorderRadius = 8;
            btnScan.FillColor = Color.FromArgb(0, 122, 204);
            btnScan.HoverState.FillColor = Color.FromArgb(0, 100, 180);
            btnScan.PressedColor = Color.FromArgb(0, 70, 130);
            btnScan.ForeColor = Color.White;

            // btnConnect
            btnConnect.Location = new Point(890, 20);
            btnConnect.Size = new Size(120, 36);
            btnConnect.Text = "Connect";
            btnConnect.BorderRadius = 8;
            btnConnect.FillColor = Color.FromArgb(0, 150, 136);
            btnConnect.HoverState.FillColor = Color.FromArgb(0, 100, 180);
            btnConnect.PressedColor = Color.FromArgb(0, 70, 130);
            btnConnect.ForeColor = Color.White;

            // 🔥 Status Label
            lblStatus.Location = new Point(1025, 25);
            lblStatus.Size = new Size(150, 20);
            lblStatus.Text = "● Disconnected";
            lblStatus.ForeColor = Color.Red;
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            // txtSend
            txtSend.Location = new Point(20, 70);
            txtSend.Size = new Size(500, 36);
            txtSend.BorderRadius = 6;
            txtSend.FillColor = Color.White;
            txtSend.ForeColor = Color.Black;

            // btnSend
            btnSend.Location = new Point(530, 70);
            btnSend.Size = new Size(80, 36);
            btnSend.Text = "Send";
            btnSend.BorderRadius = 8;
            btnSend.FillColor = Color.FromArgb(0, 122, 204);
            btnSend.HoverState.FillColor = Color.FromArgb(0, 100, 180);
            btnSend.PressedColor = Color.FromArgb(0, 70, 130);
            btnSend.ForeColor = Color.White;

            // chkEcho
            chkEcho.Location = new Point(560, 26);
            chkEcho.Text = "Local Echo";
            chkEcho.ForeColor = Color.Black;

            // chkTimestamp
            chkTimestamp.Location = new Point(680, 26);
            chkTimestamp.Text = "Time Stamp";
            chkTimestamp.ForeColor = Color.Black;

            // ddlLineBreak
            ddlLineBreak.Location = new Point(790, 20);
            ddlLineBreak.Size = new Size(80, 36);
            ddlLineBreak.BorderRadius = 6;
            ddlLineBreak.FillColor = Color.White;
            ddlLineBreak.ForeColor = Color.Black;
            ddlLineBreak.Items.AddRange(new object[] { "NONE", "CR", "LF", "CRLF" });

            // btnClearLog
            btnClearLog.Location = new Point(735, 70);
            btnClearLog.Size = new Size(110, 36);
            btnClearLog.Text = "Clear Log";
            btnClearLog.BorderRadius = 8;
            btnClearLog.FillColor = Color.FromArgb(255, 87, 34);
            btnClearLog.ForeColor = Color.White;

            // btnSaveLog
            btnSaveLog.Location = new Point(850, 70);
            btnSaveLog.Size = new Size(110, 36);
            btnSaveLog.Text = "Save Log";
            btnSaveLog.BorderRadius = 8;
            btnSaveLog.FillColor = Color.FromArgb(76, 175, 80);
            btnSaveLog.ForeColor = Color.White;

            // txtLogs
            txtLogs.Location = new Point(20, 150);
            txtLogs.Size = new Size(1150, 420);
            txtLogs.Multiline = true;
            txtLogs.ReadOnly = true;
            txtLogs.ScrollBars = ScrollBars.Vertical;
            txtLogs.BorderRadius = 6;
            txtLogs.FillColor = Color.White;
            txtLogs.ForeColor = Color.Black;

            // Terminal Screen Form
            ClientSize = new Size(1200, 600);
            BackColor = Color.White;

            Controls.Add(ddlDevices);
            Controls.Add(btnScan);
            Controls.Add(btnConnect);
            Controls.Add(lblStatus); 
            Controls.Add(chkEcho);
            Controls.Add(chkTimestamp);
            Controls.Add(ddlLineBreak);
            Controls.Add(txtSend);
            Controls.Add(btnSend);
            Controls.Add(btnClearLog);
            Controls.Add(btnSaveLog);
            Controls.Add(txtLogs);

            Name = "TerminalScreen";
            Text = "Serial BLE Terminal";

            ResumeLayout(false);
            PerformLayout();
        }
    }
}