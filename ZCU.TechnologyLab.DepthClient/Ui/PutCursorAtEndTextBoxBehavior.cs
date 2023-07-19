using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace ZCU.TechnologyLab.DepthClient.Ui
{
    /// <summary>
    /// Put cursor at the end of the text box behavior - every time text changes, scroll down
    /// </summary>
    class PutCursorAtEndTextBoxBehavior : Behavior<UIElement>
    {
        private TextBox _textBox;

        protected override void OnAttached()
        {
            base.OnAttached();

            _textBox = AssociatedObject as TextBox;

            if (_textBox == null)
            {
                return;
            }
            _textBox.TextChanged += ScrollDown;
        }

        private void ScrollDown(object sender, TextChangedEventArgs e)
        {
            _textBox.ScrollToEnd();
        }

        protected override void OnDetaching()
        {
            if (_textBox == null)
            {
                return;
            }
            _textBox.TextChanged -= ScrollDown;

            base.OnDetaching();
        }

    }
}
