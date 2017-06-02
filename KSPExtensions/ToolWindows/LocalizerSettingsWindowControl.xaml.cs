//------------------------------------------------------------------------------
// <copyright file="LocalizerSettingsWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace KSPExtensions.ToolWindows
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    using KSPExtensions.Settings;
    using System.Windows.Data;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for LocalizerSettingsWindowControl.
    /// </summary>
    public partial class LocalizerSettingsWindowControl : UserControl
    {

        public LocalizerSettings Settings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizerSettingsWindowControl"/> class.
        /// </summary>
        public LocalizerSettingsWindowControl()
        {
            this.InitializeComponent();

            ProjectsManager.ProjectsListChanged += ExtensionUtils_ProjectsListChanged;

            Settings = new LocalizerSettings();
            Settings.ProjectSettings.IDType = LocalizerProjectSettings.IDTypeEnum.ProjectBased;
            Settings.ProjectSettings.NextProjectID = 100000;
            Settings.ProjectSettings.TagAutoLocPortion = "#autoLOC";
            Settings.ProjectSettings.WriteXML("c:\\users\\dtregoning\\desktop\\test.xml");

            //Settings.ProjectSettings = LocalizerProjectSettings.CreateFromXML<LocalizerProjectSettings>("c:\\users\\dtregoning\\desktop\\test.xml");
            this.DataContext = this;

            UpdateProjectsDropdown();
            //UpdateSampleText();
        }

        private void UpdateProjectsDropdown()
        {
            projectsList.Items.Clear();

            foreach (ProjectDetails p in ProjectsManager.projects.Values)
            {
                projectsList.Items.Add(p.name);
            }

            if (projectsList.Items.Count > 0)
            {
                projectsList.IsEnabled = true;
            }
            else
            {
                ComboBoxItem none = new ComboBoxItem();
                TextBlock tb = new TextBlock();
                tb.Text = "No Projects Loaded";
                tb.FontStyle = FontStyles.Italic;
                tb.Foreground = new SolidColorBrush(Colors.DarkSlateGray);
                none.Content = tb;

                projectsList.Items.Add(none);
                projectsList.IsEnabled = false;
            }
            projectsList.SelectedItem = projectsList.Items[0];

            UpdateSampleText();

        }

        private void ExtensionUtils_ProjectsListChanged(object sender, System.EventArgs e)
        {
            UpdateProjectsDropdown();
        }

        private void TagTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSampleText();
        }

        private void TagComboChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSampleText();
        }

        private void UpdateSampleText()
        {
            if (tagFormatExample == null)
                return;

            BindingExpression b = tagFormatExample.GetBindingExpression(TextBlock.TextProperty);
                
            if(b!=null) b.UpdateTarget();
            
            //tagFormatExample.Text = tagAutoLocPortion.Text + "_";
            //if (tagProjectPortion.Text != "")
            //    tagFormatExample.Text += tagProjectPortion.Text + "_";

            //if ((idOptionsCombo.SelectedItem as ComboBoxItem).Content.ToString() == "Project Based")
            //{
            //    tagFormatExample.Text += tagProjectID.Text;
            //}
            //else
            //{
            //    tagFormatExample.Text += tagUserID.Text;
            //}
            //Settings.WriteXML(ProjectsManager.projects[projectsList.SelectedValue.ToString()].FolderPath + "/KSPExtensionsLocalizer.settings");
        }

        private void TagTextKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UpdateSampleText();
        }
    }
}