﻿using System;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace PasswordHasher
{
    public partial class Form1 : Form
    {
        private static String magic = "$1$";
        private static String itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        private EventHandler sendPasswordToWindow;

        private ContextMenu trayMenu;
        private NotifyIcon trayIcon;

        public Form1()
        {
            InitializeComponent();

            sendPasswordToWindow = new EventHandler(onDeactivate);

            if (Properties.Settings.Default.Salts == null)
                Properties.Settings.Default.Salts = new StringCollection();

            // Load settings
            trayCheckBox.Checked = Properties.Settings.Default.Tray;
            enterCheckBox.Checked = Properties.Settings.Default.SendEnter;
            ontopCheckBox.Checked = Properties.Settings.Default.OnTop;

            trayIcon = new NotifyIcon();
            trayIcon.Text = "Password Hasher";
            trayIcon.BalloonTipText = "Password Hasher minimized to tray.";
            trayIcon.BalloonTipTitle = "Password Hasher";
            trayIcon.Icon = this.Icon;
            trayIcon.DoubleClick += (sender, e) =>
            {
                WindowState = FormWindowState.Normal;
                ShowInTaskbar = true;
                trayIcon.Visible = false;
            };
            trayMenu = new ContextMenu();
            GenerateTrayMenu();
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = false;

            foreach (String s in Properties.Settings.Default.Salts)
            {
                saltBox.Items.Add(s);
            }
        }

        private void generateButton_Click(object sender, EventArgs e)
        {
            if (!saltBox.Items.Contains(saltBox.Text))
                saltBox.Items.Add(saltBox.Text);

            SaveSalts();
                        
            outputBox.Text = crypt(passBox.Text, saltBox.Text);
            passBox.Clear();
        }

        private void SaveSalts()
        {
            GenerateTrayMenu();

            StringCollection stringcollection = new StringCollection();

            foreach (object o in saltBox.Items)
                stringcollection.Add(o.ToString());

            Properties.Settings.Default.Salts = stringcollection;
            Properties.Settings.Default.Save();
        }

        
        private void GenerateTrayMenu()
        {
            /*
            trayMenu.MenuItems.Clear();

            foreach(string s in Properties.Settings.Default.Salts)
            {
                trayMenu.MenuItems.Add( "Send: " + s, (sender, e) =>
                {
                    SetForegroundWindow(prevWindow);
                    SendKeys.SendWait(crypt(passBox.Text, s));
                });
            }*/

            trayMenu.MenuItems.Add("Exit", onExit);
        }

        private void passBox_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private static byte[] Concat(byte[] array1, byte[] array2)
        {
            byte[] concat = new byte[array1.Length + array2.Length];
            System.Buffer.BlockCopy(array1, 0, concat, 0, array1.Length);
            System.Buffer.BlockCopy(array2, 0, concat, array1.Length, array2.Length);
            return concat;
        }

        private static byte[] PartialConcat(byte[]array1, byte[] array2, int max)
        {
            byte[] concat = new byte[array1.Length + max];
            System.Buffer.BlockCopy(array1, 0, concat, 0, array1.Length);
            System.Buffer.BlockCopy(array2, 0, concat, array1.Length, max);
            return concat;
        }

        private static String to64(int value, int length)
        {
            StringBuilder result = new StringBuilder();

            while(--length >= 0)
            {
                result.Append(itoa64.Substring(value & 0x3f, 1));
                value >>= 6;
            }

            return result.ToString();
        }

        private String crypt(String password, String salt)
        {
            int saltEnd;
            int len;
            int value;
            int i;

            byte[] final;
            byte[] passwordBytes;
            byte[] saltBytes;
            byte[] ctx;

            StringBuilder result;
            HashAlgorithm x_hash_alg = HashAlgorithm.Create("MD5");

            // Skip magic if it exists
            if (salt.StartsWith(magic))
            {
                salt = salt.Substring(magic.Length);
            }

            // Remove password hash if present
            if ((saltEnd = salt.LastIndexOf('$')) != -1)
            {
                salt = salt.Substring(0, saltEnd);
            }

            // Shorten salt to 8 characters if it is longer
            if (salt.Length > 8)
            {
                salt = salt.Substring(0, 8);
            }

            ctx = Encoding.ASCII.GetBytes((password + magic + salt));
            final = x_hash_alg.ComputeHash(Encoding.ASCII.GetBytes((password + salt + password)));


            // Add as many characters of ctx1 to ctx
            for (len = password.Length; len > 0; len -= 16)
            {
                if (len > 16)
                {
                    ctx = Concat(ctx, final);
                }
                else
                {
                    ctx = PartialConcat(ctx, final, len);

                }

            }

            // Then something really weird...
            passwordBytes = Encoding.ASCII.GetBytes(password);

            for (i = password.Length; i > 0; i >>= 1)
            {
                if ((i & 1) == 1)
                {
                    ctx = Concat(ctx, new byte[] { 0 });
                }
                else
                {
                    ctx = Concat(ctx, new byte[] { passwordBytes[0] });
                }
            }

            final = x_hash_alg.ComputeHash(ctx);

            byte[] ctx1;

            // Do additional mutations
            saltBytes = Encoding.ASCII.GetBytes(salt);
            for (i = 0; i < 1000; i++)
            {
                ctx1 = new byte[] { };
                if ((i & 1) == 1)
                {
                    ctx1 = Concat(ctx1, passwordBytes);
                }
                else
                {
                    ctx1 = Concat(ctx1, final);
                }
                if (i % 3 != 0)
                {
                    ctx1 = Concat(ctx1, saltBytes);
                }
                if (i % 7 != 0)
                {
                    ctx1 = Concat(ctx1, passwordBytes);
                }
                if ((i & 1) != 0)
                {
                    ctx1 = Concat(ctx1, final);
                }
                else
                {
                    ctx1 = Concat(ctx1, passwordBytes);
                }
                final = x_hash_alg.ComputeHash(ctx1);

            }
            result = new StringBuilder();
            // Add the password hash to the result string
            value = ((final[0] & 0xff) << 16) | ((final[6] & 0xff) << 8)
                    | (final[12] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[1] & 0xff) << 16) | ((final[7] & 0xff) << 8)
                    | (final[13] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[2] & 0xff) << 16) | ((final[8] & 0xff) << 8)
                    | (final[14] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[3] & 0xff) << 16) | ((final[9] & 0xff) << 8)
                    | (final[15] & 0xff);
            result.Append(to64(value, 4));
            value = ((final[4] & 0xff) << 16) | ((final[10] & 0xff) << 8)
                    | (final[5] & 0xff);
            result.Append(to64(value, 4));
            value = final[11] & 0xff;
            result.Append(to64(value, 2));

            // Return result string
            return magic + salt + "$" + result.ToString();
        }

        private void removeSaltButton_Click(object sender, EventArgs e)
        {
            if (saltBox.Items.Contains(saltBox.Text))
            {
                saltBox.Items.Remove(saltBox.Text);
                SaveSalts();
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            Deactivate += sendPasswordToWindow;

            Cursor = Cursors.Cross;
        }

        private void onDeactivate(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(300);
            for (int i = 0; i < outputBox.Text.Length; i++)
            {
                SendKeys.SendWait(outputBox.Text[i].ToString());
                // Wait some time between sending each character
                System.Threading.Thread.Sleep(50);
            }

            if (enterCheckBox.Checked)
            {
                SendKeys.Send("{ENTER}");
            }

            Deactivate -= sendPasswordToWindow;
            Cursor = Cursors.Default;
            outputBox.Clear();
        }

        private void clearOutput(object sender, EventArgs e)
        {
            outputBox.Clear();
        }

        private void onExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void displayCheckBox_Click(object sender, EventArgs e)
        {
            outputBox.UseSystemPasswordChar = !displayCheckBox.Checked;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (trayCheckBox.Checked && WindowState == FormWindowState.Minimized)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(5000);
                ShowInTaskbar = false;
            }
        }

        private void trayCheckBox_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Tray = trayCheckBox.Checked;
            Properties.Settings.Default.Save();
        }

        private void ontopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = ontopCheckBox.Checked;
            Properties.Settings.Default.OnTop = ontopCheckBox.Checked;
            Properties.Settings.Default.Save();
        }

        private void enterCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SendEnter = enterCheckBox.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
