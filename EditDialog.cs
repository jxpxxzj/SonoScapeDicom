using Dicom;
using System;
using System.Windows.Forms;

namespace SonoScapeDicom
{
    public partial class EditDialog : Form
    {
        public EditDialog()
        {
            InitializeComponent();
        }

        DicomElement element;

        public string Value
        {
            get => textBox1.Text;
        }

        public EditDialog(DicomElement element)
        {
            this.element = element;
            InitializeComponent();
            textBox1.Text = element.Get<string>();
            Text = $"Edit - {element.Tag.DictionaryEntry.Name} - {element.ValueRepresentation.Name}";
            AcceptButton = button1;
            CancelButton = button2;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
