using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NetworkSetter.CustomControls
{
    class myIPEntry : TextBox
    {
        Brush brush;

        public myIPEntry()
        {
            brush = Foreground;
        }

        /// <summary>
        /// Only allow certain keys
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            var key = e.Key;
            if (key >= Key.D0 && key <= Key.D9)
            {
                return;
            }
            else if (key == Key.OemPeriod)
            {
                var cursorPosition = SelectionStart;
                for (int i = cursorPosition; i < Text.Length; i++)
                {
                    if (Text[i] == '.')
                    {
                        SelectionStart = i + 1;
                        SelectionLength = 0;
                        break;
                    }
                }
                e.Handled = true;
                return;
            }
            else if (key == Key.Delete || key == Key.Back)
            {
                var backSelection = SelectionStart == 0 ? SelectionStart : SelectionStart - 1;
                if (Text[backSelection] == '.' && key == Key.Back)
                {
                    e.Handled = true;
                }
                else if (Text.Length != SelectionStart && Text[SelectionStart] == '.' && key == Key.Delete)
                {
                    e.Handled = true;
                }
                return;
            }
            else if (key == Key.Left || key == Key.Right)
            {
                return;
            }
            else if (key == Key.Tab || key == Key.Enter)
            {
                return;
            }

            e.Handled = true;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            // Compare new text
            if (!Regex.IsMatch(Text, @"\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b"))
            {
                Foreground = Brushes.Red;
            }
            else
            {
                Foreground = brush;
            }
        }
    }
}
