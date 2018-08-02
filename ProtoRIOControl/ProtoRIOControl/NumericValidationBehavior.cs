using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace ProtoRIOControl {
    // based on https://stackoverflow.com/questions/44475667/is-it-possible-specify-xamarin-forms-entry-numeric-keyboard-without-comma-or-dec
    // Only allows positive integers to be entered to a Xamarin.Forms Entry
    public class NumericValidationBehavior : Behavior<Entry> {
        protected override void OnAttachedTo(Entry entry) {
            entry.TextChanged += OnEntryTextChanged;
            base.OnAttachedTo(entry);
        }
        protected override void OnDetachingFrom(Entry entry) {
            entry.TextChanged -= OnEntryTextChanged;
            base.OnDetachingFrom(entry);
        }
        private static void OnEntryTextChanged(object sender, TextChangedEventArgs args) {
            if (!string.IsNullOrWhiteSpace(args.NewTextValue)) {
                //Make sure all characters are numbers
                bool isValid = args.NewTextValue.ToCharArray().All(x => char.IsDigit(x));
                ((Entry)sender).Text = isValid ? args.NewTextValue : args.NewTextValue.Remove(args.NewTextValue.Length - 1);
            }
        }


    }
}
