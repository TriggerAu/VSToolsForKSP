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
    using System.IO;

    /// <summary>
    /// Interaction logic for LocalizerSettingsWindowControl.
    /// </summary>
    public partial class LocalizerSettingsWindowControl : UserControl
    {
        public ProjectDetails CurrentProject { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizerSettingsWindowControl"/> class.
        /// </summary>
        public LocalizerSettingsWindowControl()
        {
            this.InitializeComponent();

            //Hide the tab header - only use this in designer
            Style s = new Style();
            s.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            Settingstabs.ItemContainerStyle = s;


            ProjectsManager.ProjectsListChanged += ExtensionUtils_ProjectsListChanged;
            CurrentProject = null;

            UpdateProjectsDropdown();
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
                CurrentProject = ProjectsManager.projects[projectsList.Items[0].ToString()];
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
                Settingstabs.SelectedItem = tabNoProject;

                CurrentProject = null;
            }
            projectsList.SelectedItem = projectsList.Items[0];
        }

        private void ProjectsComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentProject == null)
                return;

            if (CurrentProject.HasLocalizerSettings)
            {
                //load the text boxes
                this.DataContext = this.CurrentProject;
                //tagProjectPortion.Text = CurrentProject.localizerSettings.ProjectSettings.TagProjectPortion;

                Settingstabs.SelectedItem = tabSettings;

                UpdateSampleText(true);
            }
            else
            {
                Settingstabs.SelectedItem = tabNoSettings;

            }
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
            CurrentProject.LocalizerSettings.ProjectSettings.IDType = (LocalizerProjectSettings.IDTypeEnum)idOptionsCombo.SelectedValue;
            UpdateSampleText();
        }

        private void UpdateSampleText(bool dontSaveChanges = false)
        {
            if (CurrentProject== null || tagFormatExample == null)
                return;

            BindingExpression b = tagFormatExample.GetBindingExpression(TextBlock.TextProperty);
                
            if(b!=null) b.UpdateTarget();

            if (!dontSaveChanges)
            {
                CurrentProject.LocalizerSettings.WriteAllXML(CurrentProject.FolderPath);
            }
        }

        private void TagTextKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UpdateSampleText();
        }

        private void CreateSettings_Click(object sender, RoutedEventArgs e)
        {
            CurrentProject.LocalizerSettings = new LocalizerSettings(CurrentProject.name);
            CurrentProject.LocalizerSettings.WriteAllXML(CurrentProject.FolderPath);
            UpdateProjectsDropdown();
        }
    }
}