using System.Windows;
using System.Windows.Documents;

namespace FilmScriptEditor
{
    /// <summary>
    /// Interaction logic for PrintPreview.xaml
    /// </summary>
    public partial class PrintPreview : Window
    {
        public PrintPreview(FixedDocumentSequence document)
        {
            InitializeComponent();
            PreviewD.Document = document;
        }
    }
}