using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DCT.Classes.Extensions
{
    public static class ListBoxExtensions
    {
        /// <summary>
        /// Checks if the ListBox has an item selected, and if that item's string representation 
        /// is not an empty string.
        /// </summary>
        /// <param name="listBox">The ListBox instance.</param>
        /// <returns>True if an item is selected and is not an empty string; otherwise, false.</returns>
        public static bool NotNullAndNotEmpty(this ListBox listBox)
        {
            // 1. Check if the ListBox control is null (handled implicitly by C# calling convention)
            // 2. Check if an item is selected (SelectedItem is not null)
            if (listBox.SelectedItem == null)
            {
                return false;
            }

            // 3. Convert the selected item (object) to a string and check if it's not null/empty
            // We use the safe string.IsNullOrEmpty() method for the check.
            string selectedValue = listBox.SelectedItem.ToString();

            return !string.IsNullOrEmpty(selectedValue);
        }
    }
}