using System.Windows.Controls;
using System.Windows.Input;

namespace ShapefileEditor
{
    public class AsciiTextBox : TextBox
    {
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
                if (c > 127)
                    e.Handled = true;
            base.OnPreviewTextInput(e);
        }
    }
}
