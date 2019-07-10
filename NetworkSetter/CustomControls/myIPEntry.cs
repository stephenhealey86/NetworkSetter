using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkSetter.CustomControls
{
    class myIPEntry : TextBox
    {
        private string oldText;

        /// <summary>
        /// Only allow certain keys
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            //Store text before update
            oldText = this.Text;

            var key = e.Key;
            if (key >= Key.D0 && key <= Key.D9)
            {
                return;
            }
            else if (key == Key.OemPeriod)
            {
                return;
            }
            else if (key == Key.Delete || key == Key.Back)
            {
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
            // Get new text
            var text = this.Text;
            // Compare new text
            if (!Regex.IsMatch(text, @"\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b"))
            {
                // Set text back to previous value
                this.Text = oldText;
                // Set cursor to end
                this.SelectionStart = this.Text.Length;
                this.SelectionLength = 0;
            }
        }
    }
}
