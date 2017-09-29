using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SonoScapeDicom
{
    public partial class Form1 : Form
    {
        public static string PROGRAM_NAME = "SonoScape Dicom Editor";

        public Form1()
        {
            InitializeComponent();
            Text = PROGRAM_NAME;
            statusStrip1.CanOverflow = true;
            statusStrip1.Renderer = new CustomRenderer();
        }
        private void setToolstripText(string text)
        {
            Application.DoEvents();
            toolStripStatusLabel1.Text = text;
        }

        #region dcmfile

        DicomFile file;
        DicomImage img;
        ImageList imglist = new ImageList();
        List<DicomItem> itemarray = new List<DicomItem>();
        private async Task openFile(string filename)
        {
            setToolstripText("Loading file...");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            file = await DicomFile.OpenAsync(filename);
            initImage();
            loadDataset();
            initFrameList();
            render(0);
            stopwatch.Stop();
            setToolstripText($"File loaded in {Math.Round(stopwatch.ElapsedMilliseconds / 1000.0, 2)}s");
        }

        private void initImage()
        {
            img = new DicomImage(file.Dataset);
            label1.Text = $"{img.NumberOfFrames} frames @ {img.Width}x{img.Height}";
        }
        private void loadDataset(int defaultSelect = -1)
        {
            itemarray.Clear();
            listBox1.Items.Clear();
            listView1.Items.Clear();
            foreach (var item in file.Dataset)
            {
                listBox1.Items.Add(item.Tag.DictionaryEntry.Name);
                itemarray.Add(item);
            }
            if (defaultSelect != -1)
            {
                listBox1.SelectedIndex = defaultSelect;
            }
            if (string.IsNullOrEmpty(file.Dataset.InternalTransferSyntax.LossyCompressionMethod))
            {
                toolStripStatusLabel2.Text = $"Transfer Syntax: {file.Dataset.InternalTransferSyntax.UID.Name} [{file.Dataset.InternalTransferSyntax.UID.UID}]";
            }
            else
            {
                toolStripStatusLabel2.Text = $"Transfer Syntax: {file.Dataset.InternalTransferSyntax.LossyCompressionMethod} - {file.Dataset.InternalTransferSyntax.UID.Name} [{file.Dataset.InternalTransferSyntax.UID.UID}]";
            }
        }

        private void initFrameList()
        {
            trackBar1.Value = 0;
            listView2.Items.Clear();
            imglist.Images.Clear();
            listView2.SmallImageList = imglist;
            imglist.ImageSize = new Size(100, 100);
            listView2.BeginUpdate();
            for (int i = 0; i < img.NumberOfFrames; i++)
            {
                imglist.Images.Add(img.RenderImage(i).AsBitmap());
                var lvi = new ListViewItem
                {
                    ImageIndex = i,
                    Text = $"Frame {i}"
                };
                listView2.Items.Add(lvi);
            }
            listView2.EndUpdate();
            trackBar1.Maximum = img.NumberOfFrames - 1;
        }

        private void render(int frame)
        {
            setToolstripText("Rendering...");
            try
            {
                var image = img.RenderImage(frame).AsBitmap();
                Image imgs = Image.FromHbitmap(image.GetHbitmap());
                pictureBox1.Image = imgs;
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Refresh();
                setToolstripText($"Current Frame: {frame}");
            }
            catch (Exception)
            {
                pictureBox1.Image = null;
                pictureBox1.Refresh();
            }
        }

        #endregion

        #region menu
        OpenFileDialog fdialog = new OpenFileDialog()
        {
            InitialDirectory = Application.StartupPath,
            AutoUpgradeEnabled = true,
            CheckFileExists = true,
            CheckPathExists = true
        };
        private async void 打开OToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = fdialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var path = fdialog.FileName;
                Text = $"{PROGRAM_NAME} - {path}";
                await openFile(path);
            }
        }

        private void 退出EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        SaveFileDialog saveFileDialog = new SaveFileDialog()
        {
            InitialDirectory = Application.StartupPath,
            AutoUpgradeEnabled = true
        };

        private async void 保存SToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (file == null) return;
            var result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var fname = saveFileDialog.FileName;
                await file.SaveAsync(fname);
                MessageBox.Show($"File successfully saved in {fname}", PROGRAM_NAME, MessageBoxButtons.OK, MessageBoxIcon.Information);
                setToolstripText($"File saved in {Path.GetFileName(fname)}.");
                Text = $"{PROGRAM_NAME} - {fname}";
            }
        }

        private void 关于AToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"{PROGRAM_NAME} v1.0.0\nSchool Of Software, Central South University.", "SonoScapeDicom", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region FrameSequence
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            listView2.Items[trackBar1.Value].Selected = true;
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedIndices.Count != 0)
            {
                trackBar1.Value = listView2.SelectedIndices[0];
                render(trackBar1.Value);
            }
        }

        OpenFileDialog bmDialog = new OpenFileDialog()
        {
            AutoUpgradeEnabled = true,
            CheckFileExists = true,
            CheckPathExists = true,
            InitialDirectory = Application.StartupPath
        };

        private void insertFrame()
        {
            try
            {
                var result = bmDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    var bitmap = new Bitmap(bmDialog.FileName);
                    if (bitmap.Width != img.Width || bitmap.Height != img.Height)
                    {
                        result = MessageBox.Show($"The size of selected image is not matched with current frame sequence, continue inserting?\nImage source: {bitmap.Width}x{bitmap.Height}\nFrame sequence: {img.Width}x{img.Height}", PROGRAM_NAME, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result == DialogResult.Yes)
                        {
                            result = MessageBox.Show("Scale image to fit the frame sequence?\nIf choose no, the frame will looks strange.", PROGRAM_NAME, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (result == DialogResult.Yes)
                            {
                                bitmap = ImageUtil.ResizeImage(bitmap, img.Width, img.Height);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }

                    byte[] pixels = ImageUtil.GetPixels(bitmap);

                    //TODO: very bad perfomance, needs improvement
                    var fileDecode = file.Clone(DicomTransferSyntax.ExplicitVRLittleEndian);
                    var pixelData = DicomPixelData.Create(fileDecode.Dataset);
                    var buffer = new MemoryByteBuffer(pixels);
                    pixelData.AddFrame(buffer);
                    file = fileDecode.Clone(file.Dataset.InternalTransferSyntax);

                    initImage();
                    initFrameList();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Image invaild, insertion failed.", PROGRAM_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (file == null) return;
            insertFrame();
        }
        #endregion

        #region Dataset
        private void addItem<T>(string name, T type, string value)
        {
            var listviewItem = new ListViewItem(name);
            listviewItem.SubItems.Add(type.GetType().FullName);
            listviewItem.SubItems.Add(value);
            listView1.Items.Add(listviewItem);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = listBox1.SelectedIndex;
            try
            {
                var item = (DicomElement)itemarray[index];
                listView1.Items.Clear();
                addItem("Tag", item.Tag, $"{item.Tag.ToString()} - {item.Tag.DictionaryEntry.Name}");
                addItem("Value Multiplicity", item.Count, item.Count.ToString());
                addItem("Value Representation", item.ValueRepresentation, $"{item.ValueRepresentation.Code} - {item.ValueRepresentation.Name}");
                addItem("Length", item.Length, item.Length.ToString());
                addItem("Value", item, item.Get<String>());
            }
            catch (Exception)
            {
                listView1.Items.Clear();
                var item = itemarray[index];
                var listviewItem = new ListViewItem("Value Representation");
                listviewItem.SubItems.Add(item.ValueRepresentation.GetType().FullName);
                listviewItem.SubItems.Add(item.ValueRepresentation.Name);
                listView1.Items.Add(listviewItem);
            }

        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;

            var index = listBox1.SelectedIndex;
            if (itemarray[index] is DicomElement item)
            {
                var dialog = new EditDialog(item);
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    file.Dataset.AddOrUpdate(item.Tag, dialog.Value);
                    setToolstripText($"Field edited: {item.Tag.DictionaryEntry.Name}");
                    loadDataset(index);
                }
            }
            else
            {
                MessageBox.Show("This item can't be edited as string.", PROGRAM_NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
