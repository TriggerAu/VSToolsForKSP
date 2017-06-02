﻿using EnvDTE;
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
            if (ProjectsListChanged != null)
            {
                ProjectsListChanged(null, e);
            }
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
                newProj = new ProjectDetails();
                newProj.name = p.Name;
                newProj.fileName = p.FileName;

                //System.Diagnostics.Debug.WriteLine(p.FullName);
                var vsproject = p.Object as VSProject;
                foreach (Reference reference in vsproject.References)
                {
                    if (reference.SourceProject == null)
                    {
                        // This is an assembly reference
                        newProj.references.Add(reference.Name);
                        System.Diagnostics.Debug.WriteLine(reference.Name);
                    }
                    else
                    {
                        // This is a project reference
                    }
                }
                projects.Add(newProj.name, newProj);
            }
            OnProjectsListChanged(null);
        }
        #endregion

    }
}