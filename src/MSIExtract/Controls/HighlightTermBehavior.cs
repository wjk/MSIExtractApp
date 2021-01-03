// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MSIExtract.Controls
{
    /// <summary>
    /// Allows a TextBlock to display the highlighted text.
    /// Modified from https://stackoverflow.com/a/60474831/4928207 .
    /// </summary>
    public static class HighlightTermBehavior
    {
        private static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text",
            typeof(string),
            typeof(HighlightTermBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

        private static readonly DependencyProperty TermToBeHighlightedProperty = DependencyProperty.RegisterAttached(
            "TermToBeHighlighted",
            typeof(string),
            typeof(HighlightTermBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnTextChanged2));

        /// <summary>
        /// Accessor for the Text.
        /// </summary>
        /// <param name="frameworkElement">The active element.</param>
        /// <returns>The highlight string.</returns>
        public static string GetText(FrameworkElement frameworkElement) => (string)frameworkElement.GetValue(TextProperty);

        /// <summary>
        /// Accessor for the Text.
        /// </summary>
        /// <param name="frameworkElement">The active element.</param>
        /// <param name="value">The highlight string.</param>
        public static void SetText(FrameworkElement frameworkElement, string value) => frameworkElement.SetValue(TextProperty, value);

        /// <summary>
        /// Accessor for the TermToBeHighlighted.
        /// </summary>
        /// <param name="frameworkElement">The active element.</param>
        /// <returns>The full text to be highlighted.</returns>
        public static string GetTermToBeHighlighted(FrameworkElement frameworkElement) => (string)frameworkElement.GetValue(TermToBeHighlightedProperty);

        /// <summary>
        /// Accessor for the TermToBeHighlighted.
        /// </summary>
        /// <param name="frameworkElement">The active element.</param>
        /// <param name="value">The new full text to be highlighted.</param>
        public static void SetTermToBeHighlighted(FrameworkElement frameworkElement, string value) => frameworkElement.SetValue(TermToBeHighlightedProperty, value);

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                SetTextBlockTextAndHighlightTerm(textBlock, GetText(textBlock), GetTermToBeHighlighted(textBlock));
            }
        }

        private static void OnTextChanged2(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                SetTextBlockTextAndHighlightTerm(textBlock, GetText(textBlock), GetTermToBeHighlighted(textBlock));
            }
        }

        private static void SetTextBlockTextAndHighlightTerm(TextBlock textBlock, string text, string termToBeHighlighted)
        {
            textBlock.Text = string.Empty;

            if (TextIsEmpty(text))
            {
                return;
            }

            if (string.IsNullOrEmpty(termToBeHighlighted) ||
                TextIsNotContainingTermToBeHighlighted(text, termToBeHighlighted))
            {
                AddPartToTextBlock(textBlock, text);
                return;
            }

            List<string>? textParts = SplitTextIntoTermAndNotTermParts(text, termToBeHighlighted);

            foreach (var textPart in textParts)
            {
                AddPartToTextBlockAndHighlightIfNecessary(textBlock, termToBeHighlighted, textPart);
            }
        }

        private static bool TextIsEmpty(string text)
        {
            return text.Length == 0;
        }

        private static bool TextIsNotContainingTermToBeHighlighted(string text, string termToBeHighlighted)
        {
            return text.Contains(termToBeHighlighted, StringComparison.OrdinalIgnoreCase) == false;
        }

        private static void AddPartToTextBlockAndHighlightIfNecessary(TextBlock textBlock, string termToBeHighlighted, string textPart)
        {
            if (textPart.Equals(termToBeHighlighted, StringComparison.OrdinalIgnoreCase))
            {
                AddHighlightedPartToTextBlock(textBlock, textPart);
            }
            else
            {
                AddPartToTextBlock(textBlock, textPart);
            }
        }

        private static void AddPartToTextBlock(TextBlock textBlock, string part)
        {
            textBlock.Inlines.Add(new Run { Text = part });
        }

        private static void AddHighlightedPartToTextBlock(TextBlock textBlock, string part)
        {
            var style = (Style)Application.Current.Resources["HightlightColors"];
            textBlock.Inlines.Add(new Run { Text = part, Style = style });
        }

        private static List<string> SplitTextIntoTermAndNotTermParts(string text, string term)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>() { string.Empty };
            }

            return Regex.Split(text, $@"({Regex.Escape(term)})", RegexOptions.IgnoreCase)
                        .Where(p => p != string.Empty)
                        .ToList();
        }
    }
}
