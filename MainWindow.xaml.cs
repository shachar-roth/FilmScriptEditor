using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace FilmScriptEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
                    currentParagraph = new DialogCharName();
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
                        sceneBody = new SceneBody();
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
                        currentParagraph = new SceneBody();
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
                        currentParagraph = new DialogContent();
                        parent.Blocks.InsertAfter(charName, currentParagraph);
                    }
                    textBox.CaretPosition = currentParagraph.ContentEnd;
                    textBox.Selection.Select(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                    break;

                case DialogContent content:
                    if (content.NextBlock == null)
                    {
                        currentParagraph = new DialogCharName();
                        parent.Blocks.InsertAfter(content, currentParagraph);
                    }
                    textBox.CaretPosition = currentParagraph.ContentEnd;
                    textBox.Selection.Select(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                    break;
            }
        }

        private void CreateNewScene(object sender, ExecutedRoutedEventArgs e)
        {
            Paragraph p = new SceneHeader();
            ListItem currentScene = textBox.CaretPosition.Paragraph?.Parent as ListItem;
            Scences scenes = (Scences)document.Blocks.FirstBlock;
            if (scenes == null)
            {
                scenes = new Scences();
                document.Blocks.Add(scenes);
            }
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
    }

    internal class Scences : List
    {
        public Scences()
        {
            var converter = new LengthConverter();
            MarkerOffset = (double)converter.ConvertFrom("0.5cm");
            MarkerStyle = TextMarkerStyle.Decimal;
        }
    }

    internal abstract class ParagraphBase : Paragraph
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

    internal class SceneHeader : ParagraphBase
    {
        public SceneHeader()
        {
            TextDecorations.Add(System.Windows.TextDecorations.Underline);
            Margin = new Thickness { Bottom = 10, Top = 10 };
            FontWeight = FontWeights.Bold;
            Inlines.Add(@"פנים\חוץ. תיאור מקום - יום\לילה");
        }
    }

    internal class SceneBody : ParagraphBase
    {
    }

    internal class DialogCharName : ParagraphBase
    {
        private const string defaultContent = @"שם דמות";

        public DialogCharName()
        {
            var converter = new LengthConverter();
            Margin = new Thickness { Left = (double)converter.ConvertFrom("6cm"), Top = 10 };
            FontWeight = FontWeights.Bold;
            Inlines.Add(defaultContent);
        }

        public bool IsEmpty => Inlines.Count < 2 && Inlines.FirstInline is Run run && (run.Text.Equals(defaultContent) || string.IsNullOrWhiteSpace(run.Text));
    }

    internal class DialogContent : ParagraphBase
    {
        public DialogContent()
        {
            var converter = new LengthConverter();
            Margin = new Thickness { Left = (double)converter.ConvertFrom("3.5cm"), Right = (double)converter.ConvertFrom("4cm"), Bottom = 10 };
            Inlines.Add(@"מה היא אומרת");
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