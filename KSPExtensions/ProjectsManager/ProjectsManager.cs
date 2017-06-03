using EnvDTE;
using KSPExtensions.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSLangProj;

namespace KSPExtensions
{
    internal static class ProjectsManager
    {

        #region Initialization
        private static DTE dte;
        private static SolutionEvents solutionEvents;

        public static bool ready = false;

        internal static void Initialize(DTE newDTE)
        {
            dte = newDTE;
            solutionEvents = dte.Events.SolutionEvents;

            solutionEvents.Opened += SolutionChanged;
            solutionEvents.ProjectAdded += ProjectsChanged;
            solutionEvents.ProjectRemoved += ProjectsChanged;
            solutionEvents.AfterClosing += SolutionChanged;
        }

        #endregion

        #region Projects List
        internal static Dictionary<string, ProjectDetails> projects = new Dictionary<string, ProjectDetails>();

        internal static event EventHandler ProjectsListChanged;

        internal static void OnProjectsListChanged(EventArgs e)
        {
            ProjectsListChanged?.Invoke(null, e);
        }

        private static void ProjectsChanged(Project project)
        {
            SolutionChanged();
        }
        private static void SolutionChanged()
        {
            projects.Clear();

            ProjectDetails newProj;
            foreach (Project p in ExtensionsGlobal.dte.Solution.Projects)
            {
                //if theres no saved project then skip this one
                if (p.Object == null || string.IsNullOrEmpty(p.FileName))
                    continue;

                //Create a new Project Details object
                newProj = new ProjectDetails();
                newProj.name = p.Name;
                newProj.fileName = p.FileName;

                //Get the references - well use this later to see if the KSP Assembly is there
                var vsproject = p.Object as VSProject;

                foreach (Reference reference in vsproject.References)
                {
                    if (reference.SourceProject == null)
                    {
                        // This is an assembly reference
                        newProj.references.Add(reference.Name);
                        //System.Diagnostics.Debug.WriteLine(reference.Name);
                    }
                    else
                    {
                        // This is a project reference
                    }
                }

                //Now check we have the two files
                if (File.Exists(newProj.FolderPath + "/.ksplocalizer.settings") && File.Exists(newProj.FolderPath + "/.ksplocalizer.settings"))
                {
                    newProj.LocalizerSettings = new LocalizerSettings(newProj.name);
                    newProj.LocalizerSettings.ProjectSettings = LocalizerSettings.CreateFromXML<LocalizerProjectSettings>(newProj.FolderPath + "/.ksplocalizer.settings");
                    newProj.LocalizerSettings.UserSettings = LocalizerSettings.CreateFromXML<LocalizerUserSettings>(newProj.FolderPath + "/.ksplocalizer.settings.user");
                }
                else
                {
                    newProj.LocalizerSettings = null;
                }

                projects.Add(newProj.name, newProj);
            }
            OnProjectsListChanged(null);
        }
        #endregion

        #region Projects Methods
        public static bool ProjectHasLocalizerSettings(string projectName)
        {
            ProjectDetails p;
            if(projects.TryGetValue(projectName, out p))
            {
                return p.HasLocalizerSettings;
            }
            return false;
        }

        #endregion

    }
}
