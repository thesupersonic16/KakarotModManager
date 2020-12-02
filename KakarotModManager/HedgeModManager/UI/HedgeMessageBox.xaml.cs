﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Markdig;
using TheArtOfDev.HtmlRenderer.WPF;

namespace HedgeModManager
{
    /// <summary>
    /// Interaction logic for HedgeMessageBox.xaml
    /// </summary>
    public partial class HedgeMessageBox : Window
    {
        public HedgeMessageBox()
        {
            InitializeComponent();
        }

        public HedgeMessageBox(string header, string message,
            HorizontalAlignment buttonAlignment = HorizontalAlignment.Right, TextAlignment textAlignment = TextAlignment.Center, InputType type = InputType.Basic)
        {
            InitializeComponent();
            Header.Text = header;

            if (type == InputType.Basic)
            {
                Message.Child = new ScrollViewer()
                {
                    Content = new TextBlock()
                    {
                        IsEnabled = false,
                        Text = message,
                        TextAlignment = textAlignment,
                        FontSize = 25,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
            }
            else if (type == InputType.HTML || type == InputType.MarkDown)
            {
                Message.Child = new HtmlPanel()
                {
                    Text = $@"
<html>
    <body>
        <style>
            {KakarotModManager.Properties.Resources.GBStyleSheet}
        </style>
        {(type == InputType.MarkDown ? Markdown.ToHtml(message, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build()) : message)}
    </body>
</html>",
                    Background = new SolidColorBrush(Colors.Transparent),
                    Width = double.NaN,
                    Height = double.NaN
                };
            }

            Stack.HorizontalAlignment = buttonAlignment;
        }

        public void SetWindowSize(double width, double height)
        {
            SetWindowSize(new Size(width, height));
        }

        public void SetWindowSize(Size size)
        {
            Width = size.Width;
            Height = size.Height;
        }

        public void AddButton(string text, Action onClick)
        {
            var btn = new Button()
            {
                Content = text,
                Width = double.NaN,
                Height = 23,
                Margin = new Thickness(5, 0, 5, 0),
                Padding = new Thickness(25, 0, 25, 0)
            };

            btn.Click += (caller, args) => { onClick.Invoke(); };
            Stack.Children.Add(btn);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            InvalidateVisual();
        }
    }

    public enum InputType
    {
        Basic,
        HTML,
        MarkDown
    }
}
