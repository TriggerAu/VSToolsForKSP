﻿//------------------------------------------------------------------------------
// <copyright file="LocalizerSettingsWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace VSToolsForKSP.ToolWindows
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    using VSToolsForKSP.Settings;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.IO;
    using Microsoft.Win32;
    using VSToolsForKSP.Managers;
    using System.Linq;

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
            UpdateTemplatesDropdown();

            VSToolsForKSP.Refactoring.LocalizerFormatRefactoring.OnRefactorComplete += LocalizerFormatRefactoring_OnRefactorComplete;
        }

        private void LocalizerFormatRefactoring_OnRefactorComplete()
        {

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
                OutputManager.WriteLine("No projects in current solution.");
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
            if(ProjectsManager.projects.Count < 1 || projectsList.SelectedValue == null)
                return;

            CurrentProject = ProjectsManager.projects[projectsList.SelectedValue.ToString()];

            if (CurrentProject.HasLocalizerSettings)
            {
                OutputManager.WriteLine("Selected project has Localizer settings. Setting up the databind...");
                //load the text boxes
                this.DataContext = this.CurrentProject;
                //tagProjectPortion.Text = CurrentProject.localizerSettings.ProjectSettings.TagProjectPortion;

                Settingstabs.SelectedItem = tabSettings;

                MultiFile.IsEnabled = CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles;
                BaseFile.IsEnabled = !CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles;
                BaseFileSelector.IsEnabled = !CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles;

                UpdateSampleText(true);
            }
            else
            {
                OutputManager.WriteLine("Selected project has no Localizer settings.");
                Settingstabs.SelectedItem = tabNoSettings;

            }
        }


        private void ExtensionUtils_ProjectsListChanged(object sender, System.EventArgs e)
        {
            UpdateProjectsDropdown();
            UpdateTemplatesDropdown();
        }

        private void TagTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSampleText();
        }

        private void TagComboChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentProject.LocalizerSettings.ProjectSettings.IDType = (LocalizerProjectSettings.IDTypeEnum)idOptionsCombo.SelectedValue;

            tagProjectID.IsEnabled = CurrentProject.LocalizerSettings.ProjectSettings.IDType == LocalizerProjectSettings.IDTypeEnum.ProjectBased;
            tagUserID.IsEnabled = CurrentProject.LocalizerSettings.ProjectSettings.IDType == LocalizerProjectSettings.IDTypeEnum.UserBased;

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
            UpdateTemplatesDropdown();
        }

        private void BaseFileSelector_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = BaseFile.Text;
            dlg.DefaultExt = ".cfg";
            dlg.Filter = "Config Files (*.cfg)|*.cfg|All Files (*.*)|*.*";

            if (dlg.ShowDialog()==true)
            {
                BaseFile.Text = dlg.FileName;
            }
        }

        private void cfgLanguageMultiFile_Changed(object sender, RoutedEventArgs e)
        {
            MultiFile.IsEnabled = CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles;
            cfgLanguageMultiAndBaseFile.IsEnabled = CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles;
            BaseFile.IsEnabled = !CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles || CurrentProject.LocalizerSettings.ProjectSettings.UseMultiAndBaseCfgFiles;
            BaseFileSelector.IsEnabled = !CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles || CurrentProject.LocalizerSettings.ProjectSettings.UseMultiAndBaseCfgFiles; ;
            cfgLangPrefixMultiFile.IsEnabled = CurrentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles;

            UpdateSampleText();
        }


        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            CurrentProject.LocalizerSettings.ProjectSettings.SaveTemplate(templateName.Text);
            UpdateTemplatesDropdown(templateName.Text);
        }

        private void UpdateTemplatesDropdown(string value = "")
        {
            if (CurrentProject == null)
            {
                return;
            }

            int valueIndex = 0;
            ddlTemplates.Items.Clear();

            CurrentProject.LocalizerSettings.ProjectSettings.Templates = CurrentProject.LocalizerSettings.ProjectSettings.Templates.OrderBy(x => x.name).ToList();

            foreach (LocalizerTemplateSettings t in CurrentProject.LocalizerSettings.ProjectSettings.Templates)
            {
                ddlTemplates.Items.Add(t.name);
                if(value != "" && t.name == value)
                {
                    valueIndex = ddlTemplates.Items.IndexOf(t.name);
                }
            }
            ddlTemplates.SelectedItem = ddlTemplates.Items[valueIndex];
        }

        private void ApplyTemplate_Click(object sender, RoutedEventArgs e)
        {
            CurrentProject.LocalizerSettings.ProjectSettings.ApplyTemplate(ddlTemplates.SelectedItem.ToString());
        }

    }
}