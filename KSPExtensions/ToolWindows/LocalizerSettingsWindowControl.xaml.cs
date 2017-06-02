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

        public LocalizerSettings Settings { get; set; }
        private ProjectDetails currentProject;
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizerSettingsWindowControl"/> class.
        /// </summary>
        public LocalizerSettingsWindowControl()
        {
            this.InitializeComponent();

            ProjectsManager.ProjectsListChanged += ExtensionUtils_ProjectsListChanged;

            Settings = null;
            currentProject = null;

            Settings = new LocalizerSettings("");
            Settings.ProjectSettings.IDType = LocalizerProjectSettings.IDTypeEnum.ProjectBased;
            Settings.ProjectSettings.NextProjectID = 100000;
            Settings.ProjectSettings.TagAutoLocPortion = "#autoLOC";
            Settings.ProjectSettings.WriteXML("c:\\users\\dtregoning\\desktop\\test.xml");

            //Settings.ProjectSettings = LocalizerProjectSettings.CreateFromXML<LocalizerProjectSettings>("c:\\users\\dtregoning\\desktop\\test.xml");
            this.DataContext = this;

            Style s = new Style();
            s.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            Settingstabs.ItemContainerStyle = s;
            
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
                currentProject = ProjectsManager.projects[projectsList.Items[0].ToString()];
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

                currentProject = null;
            }
            projectsList.SelectedItem = projectsList.Items[0];
        }

        private void ProjectsComboChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentProject == null)
                return;

            //Now check we have the two files
            if(File.Exists(currentProject.FolderPath + "/.ksplocalizer.settings") && File.Exists(currentProject.FolderPath + "/.ksplocalizer.settings"))
            {
                Settings = new LocalizerSettings(currentProject.name);
                Settings.ProjectSettings = LocalizerSettings.CreateFromXML<LocalizerProjectSettings>(currentProject.FolderPath + "/.ksplocalizer.settings");
                Settings.UserSettings = LocalizerSettings.CreateFromXML<LocalizerUserSettings>(currentProject.FolderPath + "/.ksplocalizer.settings.user");

                //reset these two
                this.DataContext = this;
                tagProjectPortion.Text = Settings.ProjectSettings.TagProjectPortion;

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
            Settings.ProjectSettings.IDType = (LocalizerProjectSettings.IDTypeEnum)idOptionsCombo.SelectedValue;
            UpdateSampleText();
        }

        private void UpdateSampleText(bool dontSaveChanges = false)
        {
            if (currentProject== null || tagFormatExample == null)
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

            if (!dontSaveChanges)
            {
                Settings.WriteAllXML(currentProject.FolderPath);
            }
        }

        private void TagTextKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UpdateSampleText();
        }

        private void CreateSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings = new LocalizerSettings(currentProject.name);
            Settings.WriteAllXML(currentProject.FolderPath);
            UpdateProjectsDropdown();
        }
    }
}