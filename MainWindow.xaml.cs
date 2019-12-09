using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;

namespace FilmScriptEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public const double pageSizeCm = 25.4;
        public const double topPaddingCm = 0;
        public const double cm2px = 37.795275590551178;

        private double pageCount;

        public double PageCount
        {
            get => pageCount;
            set
            {
                if (pageCount == value || double.IsInfinity(value))
                    return;

                pageCount = value;
                AddPageLines();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PageCountText"));
            }
        }

        public string PageCountText => $"מספר עמודים כולל: {PageCount}";

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            textBox.TextChanged += (_, __) => RefreshPageCount();
        }

        private void DocumentPaginator_ComputePageCountCompleted(object sender, AsyncCompletedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ChangeMode_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ChangeMode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Paragraph currentParagraph = textBox.CaretPosition.Paragraph;
            ListItem parent = currentParagraph?.Parent as ListItem;
            switch (currentParagraph)
            {
                case SceneBody body:
                    currentParagraph = DialogCharName.Create();
                    parent.Blocks.InsertAfter(body, currentParagraph);
                    textBox.CaretPosition = currentParagraph.ContentEnd;
                    textBox.Selection.Select(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                    break;

                case DialogCharName charName:
                case DialogContent content:
                    SceneBody sceneBody;
                    if (currentParagraph.PreviousBlock is SceneBody prevSceneBody)
                    {
                        sceneBody = prevSceneBody;
                    }
                    else
                    {
                        sceneBody = SceneBody.Create();
                        parent.Blocks.InsertBefore(currentParagraph, sceneBody);
                    }
                    parent.Blocks.Remove(currentParagraph);
                    textBox.CaretPosition = sceneBody.ContentEnd;
                    break;
            }
        }

        private void OnEnter_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OnEnter_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Paragraph currentParagraph = textBox.CaretPosition.Paragraph;
            ListItem parent = currentParagraph?.Parent as ListItem;
            switch (currentParagraph)
            {
                case SceneHeader header:

                    if (parent.Blocks.LastBlock != header)
                    {
                        currentParagraph = (Paragraph)parent.Blocks.LastBlock;
                    }
                    else
                    {
                        currentParagraph = SceneBody.Create();
                        parent.Blocks.Add(currentParagraph);
                    }
                    textBox.CaretPosition = currentParagraph.ContentEnd;
                    break;

                case SceneBody body:
                    EditingCommands.EnterLineBreak.Execute(null, currentParagraph);
                    break;

                case DialogCharName charName:
                    if (charName.NextBlock == null)
                    {
                        currentParagraph = DialogContent.Create();
                        parent.Blocks.InsertAfter(charName, currentParagraph);
                    }
                    textBox.CaretPosition = currentParagraph.ContentEnd;
                    textBox.Selection.Select(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                    break;

                case DialogContent content:
                    if (content.NextBlock == null)
                    {
                        currentParagraph = DialogCharName.Create();
                        parent.Blocks.InsertAfter(content, currentParagraph);
                    }
                    textBox.CaretPosition = currentParagraph.ContentEnd;
                    textBox.Selection.Select(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                    break;
            }
        }

        private void CreateNewScene(object sender, ExecutedRoutedEventArgs e)
        {
            Scences scenes = document.Blocks.FirstBlock as Scences;
            if (scenes == null)
            {
                scenes = Scences.Create();
                document.Blocks.Add(scenes);
            }
            Paragraph p = SceneHeader.Create();
            ListItem currentScene = textBox.CaretPosition.Paragraph?.Parent as ListItem;
            if (currentScene != null)
            {
                scenes.ListItems.InsertAfter(currentScene, new ListItem(p));
            }
            else
            {
                scenes.ListItems.Add(new ListItem(p));
            }
            p.Focus();
            textBox.Selection.Select(p.ContentStart, p.ContentEnd);
        }

        public void RefreshPageCount()
        {
            PageCount = Math.Ceiling((textBox.Document.ContentEnd.GetCharacterRect(LogicalDirection.Forward).Y / cm2px - topPaddingCm) / pageSizeCm);
        }

        private int DrawnLines = 0;

        public void AddPageLines()
        {
            for (; DrawnLines < PageCount; DrawnLines++)
            {
                double top = (topPaddingCm + DrawnLines * pageSizeCm) * cm2px;

                TextBlock leftText = new TextBlock { Foreground = Brushes.White, FontSize = 16 };
                TextBlock rightText = new TextBlock { Foreground = Brushes.White, FontSize = 16 };
                Line line = new Line { Stroke = Brushes.Black, StrokeThickness = 0.1, StrokeDashArray = DoubleCollection.Parse("5, 10") };

                leftText.Text = $"עמוד {DrawnLines + 1} \u2193 ";
                rightText.Text = $" \u2193 עמוד {DrawnLines + 1}";
                line.Y1 = line.Y2 = top;
                line.X2 = 23 * cm2px;
                canvas.Children.Add(leftText);
                canvas.Children.Add(rightText);
                Canvas.SetTop(leftText, top);
                Canvas.SetTop(rightText, top);
                Canvas.SetLeft(rightText, 22 * cm2px);
                Canvas.SetRight(leftText, 22 * cm2px);
                canvas.Children.Add(line);
            }
        }

        public void Print()
        {
            String copyString = XamlWriter.Save(document);
            FlowDocument copy = XamlReader.Parse(copyString) as FlowDocument;
            using (MemoryStream stream = new MemoryStream())
            {
                Package package = Package.Open(stream, FileMode.Create, FileAccess.ReadWrite);
                var packUri = new Uri("pack://temp.xps");
                PackageStore.RemovePackage(packUri);
                PackageStore.AddPackage(packUri, package);
                XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.SuperFast, packUri.ToString());
                var paginator = ((IDocumentPaginatorSource)copy).DocumentPaginator;
                var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                writer.Write(paginator);
                //new XpsSerializationManager(new XpsPackagingPolicy(xpsDocument), false).SaveAsXaml(paginator);
                var docSequence = xpsDocument.GetFixedDocumentSequence();
                xpsDocument.Close();
                var window = new PrintPreview(docSequence);
                window.ShowDialog();
            }
            /*            PrintDialog printDialog = new PrintDialog();
                        var paginatorSource = (IDocumentPaginatorSource)document;
                        printDialog.ShowDialog();
            */            //printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Film Script");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Print();
        }
    }

    public class Scences : List
    {
        public static Scences Create()
        {
            var converter = new LengthConverter();
            var result = new Scences
            {
                MarkerOffset = (double)converter.ConvertFrom("0.5cm"),
                MarkerStyle = TextMarkerStyle.Decimal
            };
            return result;
        }
    }

    public abstract class ParagraphBase : Paragraph
    {
        public ParagraphBase()
        {
            Unloaded += OnUnLoaded;
        }

        private void OnUnLoaded(object sender, RoutedEventArgs e)
        {
            if (Parent is FlowDocument documnet)
            {
                documnet.Blocks.Remove(this);
                if (documnet.Blocks.Count != 1)
                {
                    List scenes = (List)documnet.Blocks.FirstBlock;
                    var otherLists = documnet.Blocks.OfType<List>().Skip(1).ToList();
                    scenes.ListItems.AddRange(otherLists.SelectMany(list => list.ListItems).ToList());
                    foreach (List list in otherLists)
                    {
                        documnet.Blocks.Remove(list);
                    }
                }
            }
        }
    }

    public class SceneHeader : ParagraphBase
    {
        public static SceneHeader Create()
        {
            var result = new SceneHeader
            {
                Margin = new Thickness { Bottom = 10, Top = 10 },
                FontWeight = FontWeights.Bold
            };
            result.TextDecorations.Add(System.Windows.TextDecorations.Underline);
            result.Inlines.Add(@"פנים\חוץ. תיאור מקום - יום\לילה");
            return result;
        }
    }

    public class SceneBody : ParagraphBase
    {
        public static SceneBody Create() => new SceneBody();
    }

    public class DialogCharName : ParagraphBase
    {
        private const string defaultContent = @"שם דמות";

        public static DialogCharName Create()
        {
            var converter = new LengthConverter();
            DialogCharName result = new DialogCharName
            {
                Margin = new Thickness { Left = (double)converter.ConvertFrom("6cm"), Top = 10 },
                FontWeight = FontWeights.Bold,
            };
            result.Inlines.Add(defaultContent);
            return result;
        }

        public bool IsEmpty => Inlines.Count < 2 && Inlines.FirstInline is Run run && (run.Text.Equals(defaultContent) || string.IsNullOrWhiteSpace(run.Text));
    }

    public class DialogContent : ParagraphBase
    {
        public static DialogContent Create()
        {
            var converter = new LengthConverter();
            var result = new DialogContent
            {
                Margin = new Thickness { Left = (double)converter.ConvertFrom("3.5cm"), Right = (double)converter.ConvertFrom("4cm"), Bottom = 10 }
            };
            result.Inlines.Add(@"מה היא אומרת");
            return result;
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand
            (
                "Exit",
                "Exit",
                typeof(CustomCommands),
                new InputGestureCollection()
                {
                    new KeyGesture(Key.F4, ModifierKeys.Alt)
                }
            );

        public static readonly RoutedUICommand NewScene = new RoutedUICommand(
            "סצנה חדשה",
            "סצנה חדשה",
            typeof(CustomCommands),
            new InputGestureCollection()
                {
                    new KeyGesture(Key.Enter, ModifierKeys.Control)
                }
            );

        public static readonly RoutedUICommand OnEnter = new RoutedUICommand(
            "OnEnter",
            "OnEnter",
            typeof(CustomCommands));

        public static readonly RoutedUICommand ChangeMode = new RoutedUICommand(
            "ChangeMode",
            "ChangeMode",
            typeof(CustomCommands),
            new InputGestureCollection()
                {
                    new KeyGesture(Key.Tab)
                }
            );

        //Define more commands here, just like the one above
    }
}