using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NotesRichTextGUI
{
    public partial class Form1 : Form
    {
        private string _currentPath = null;   // full path of current doc (null = new/unsaved)
        private bool _dirty = false;          // unsaved changes?

        public Form1()
        {
            InitializeComponent();

            // Populate font sizes
            comboFontSize.Items.AddRange(new object[]
            {
                "8","9","10","11","12","14","16","18","20","24","28","32"
            });
            comboFontSize.Text = "12";

            rtb.Font = new Font("Segoe UI", 12f, FontStyle.Regular);

            UpdateTitle();
            UpdateStatus();
        }

        // ------------- UI EVENTS -------------

        private void rtb_TextChanged(object sender, EventArgs e)
        {
            _dirty = true;
            UpdateStatus();
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            if (!ConfirmSaveIfDirty()) return;

            rtb.Clear();
            rtb.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            _currentPath = null;
            _dirty = false;
            UpdateTitle();
            UpdateStatus();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (!ConfirmSaveIfDirty()) return;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Rich Text (*.rtf)|*.rtf|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                dlg.Title = "Open Note";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LoadDocument(dlg.FileName);
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveDocument(false);
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            SaveDocument(true);
        }

        private void btnBold_Click(object sender, EventArgs e)
        {
            ToggleStyle(FontStyle.Bold);
            btnBold.Checked = IsStyleActive(FontStyle.Bold);
        }

        private void btnItalic_Click(object sender, EventArgs e)
        {
            ToggleStyle(FontStyle.Italic);
            btnItalic.Checked = IsStyleActive(FontStyle.Italic);
        }

        private void btnUnderline_Click(object sender, EventArgs e)
        {
            ToggleStyle(FontStyle.Underline);
            btnUnderline.Checked = IsStyleActive(FontStyle.Underline);
        }

        private void comboFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            int size;
            if (int.TryParse(Convert.ToString(comboFontSize.SelectedItem), out size))
            {
                ApplyFontSize(size);
            }
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                dlg.FullOpen = true;
                dlg.Color = rtb.SelectionColor;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    rtb.SelectionColor = dlg.Color;
                    _dirty = true;
                }
            }
        }

        private void btnBullet_Click(object sender, EventArgs e)
        {
            rtb.SelectionBullet = !rtb.SelectionBullet;
            rtb.Focus();
            _dirty = true;
        }

        private void btnClearFormatting_Click(object sender, EventArgs e)
        {
            var def = rtb.Font;
            if (def == null) def = new Font("Segoe UI", 12f, FontStyle.Regular);

            rtb.SelectionFont = new Font(def, FontStyle.Regular);
            rtb.SelectionColor = Color.Black;
            rtb.SelectionBullet = false;
            _dirty = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmSaveIfDirty())
            {
                e.Cancel = true;
            }
        }

        // ------------- HELPERS -------------

        private void UpdateTitle()
        {
            var name = _currentPath == null ? "Untitled" : Path.GetFileName(_currentPath);
            this.Text = (_dirty ? "*" : "") + name + " - Notes (Day 18)";
        }

        private void UpdateStatus()
        {
            int chars = rtb.TextLength;
            lblStatus.Text = "Chars: " + chars + (_dirty ? " • Unsaved" : "");
            UpdateTitle();
        }

        private bool ConfirmSaveIfDirty()
        {
            if (!_dirty) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Save now?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.Yes) return SaveDocument(false);
            return true; // No
        }

        private void LoadDocument(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".rtf")
                {
                    rtb.LoadFile(path, RichTextBoxStreamType.RichText);
                }
                else
                {
                    rtb.Text = File.ReadAllText(path);
                }

                _currentPath = path;
                _dirty = false;
                UpdateTitle();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open file: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool SaveDocument(bool saveAs)
        {
            try
            {
                string path = _currentPath;

                if (saveAs || string.IsNullOrEmpty(path))
                {
                    using (var dlg = new SaveFileDialog())
                    {
                        dlg.Filter = "Rich Text (*.rtf)|*.rtf|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                        dlg.Title = "Save Note";
                        if (!string.IsNullOrEmpty(_currentPath))
                            dlg.FileName = Path.GetFileName(_currentPath);
                        if (dlg.ShowDialog(this) != DialogResult.OK) return false;
                        path = dlg.FileName;
                    }
                }

                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".rtf")
                {
                    rtb.SaveFile(path, RichTextBoxStreamType.RichText);
                }
                else
                {
                    File.WriteAllText(path, rtb.Text);
                }

                _currentPath = path;
                _dirty = false;
                UpdateTitle();
                UpdateStatus();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ToggleStyle(FontStyle style)
        {
            Font sel = rtb.SelectionFont;
            if (sel == null) sel = rtb.Font; // mixed selection → use base font

            FontStyle newStyle;

            if ((sel.Style & style) == style)
            {
                // Turn OFF this style
                newStyle = sel.Style & ~style;
            }
            else
            {
                // Turn ON this style
                newStyle = sel.Style | style;
            }

            rtb.SelectionFont = new Font(sel, newStyle);
            _dirty = true;
            rtb.Focus();
        }

        private bool IsStyleActive(FontStyle style)
        {
            Font sel = rtb.SelectionFont;
            if (sel == null) sel = rtb.Font;
            return (sel.Style & style) == style;
        }

        private void ApplyFontSize(int size)
        {
            Font sel = rtb.SelectionFont;
            if (sel == null) sel = rtb.Font;
            rtb.SelectionFont = new Font(sel.FontFamily, size, sel.Style);
            _dirty = true;
            rtb.Focus();
        }
    }
}
