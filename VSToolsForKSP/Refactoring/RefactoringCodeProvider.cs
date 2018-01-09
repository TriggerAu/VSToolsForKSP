using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;
using VSToolsForKSP.Managers;

namespace VSToolsForKSP.Refactoring
{
    internal abstract class RefactoringCodeProvider : CodeRefactoringProvider
    {
        private ProjectDetails currentProject;

        private string newIDKey = "";
        private string newValue = "";

        #region Common Methods
        /// <summary>
        /// Search the Children of this object for an EOL Trivia after the position of the replacedNode
        /// </summary>
        /// <param name="objToSearch">The Node or Token to search the children of (and then itself) for a SyntaxKind.EndOfLineTrivia</param>
        /// <param name="endReplacedSpan">The character position at the end of the replacedNode - dont want EOLs from before the replacement</param>
        /// <param name="foundEOL">the object to get back to the top when we find it</param>
        /// <returns>True if we found  it</returns>
        protected bool FindEOLAfter(SyntaxNodeOrToken objToSearch, int endReplacedSpan, ref SyntaxTrivia foundEOL)
        {
            // Guard for the object is before the replacedNode
            if (objToSearch.FullSpan.End < endReplacedSpan)
                return false;

            // Search each child for the trivia
            foreach (SyntaxNodeOrToken n in objToSearch.ChildNodesAndTokens())
            {
                if (n.FullSpan.End < endReplacedSpan)
                    continue;

                if (FindEOLAfter(n, endReplacedSpan, ref foundEOL))
                {
                    return true;
                }
            }

            // Now search this object
            foreach (SyntaxTrivia item in objToSearch.GetLeadingTrivia())
            {
                //Dont bother if the start of the element is before the replaced Node
                if (objToSearch.FullSpan.Start < endReplacedSpan)
                    continue;

                if (item.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    foundEOL = item;
                    return true;
                }
            }
            foreach (SyntaxTrivia item in objToSearch.GetTrailingTrivia())
            {
                if (item.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    foundEOL = item;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a key/value pair to the cfg file value
        /// </summary>
        /// <param name="key">the autoLOC key for the string</param>
        /// <param name="value">the value portion of the pair</param>
        /// <param name="LangCode">the langcode thats in this file</param>
        /// <param name="filePath">the path to the file</param>
        /// <param name="addLangTagToValue">whether to prefix the value with [langcode]</param>
        /// <returns></returns>
        private bool AddTagToFile(string key, string value, string LangCode, string filePath, bool addLangTagToValue)
        {
            try
            {
                OutputManager.WriteLine("Adding key and value to {0} in {1}", LangCode, filePath);

                string langTag = "";
                if (addLangTagToValue && !LangCode.StartsWith("en"))
                {
                    langTag = "[" + LangCode.Split('-')[0] + "]";
                }

                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "Localization\n{\n\t" + LangCode + "\n\t{\n\t}\n}\n", Encoding.UTF8);
                }


                string[] oldLines = File.ReadAllLines(filePath);

                List<string> newLines = new List<string>();

                int cfgLevel = 0;
                string NextCfgSection = "", CurrentCfgSection = "", CurrentSectionPath = "";
                Stack<string> previousSectionStack = new Stack<string>();

                bool AddedTag = false;
                foreach (string line in oldLines)
                {
                    //If we already added the tag then just buildthe rest of the file
                    if (AddedTag)
                    {
                        newLines.Add(line);
                        continue;
                    }

                    string[] pairs = line.Split(new char[] { '=' }, 2);
                    string lineKey = pairs[0].Trim();
                    if (lineKey == "{")
                    {
                        cfgLevel++;
                        previousSectionStack.Push(CurrentCfgSection);
                        CurrentCfgSection = NextCfgSection;
                        CurrentSectionPath += "/" + NextCfgSection;
                        //System.Diagnostics.Debug.WriteLine("Opening:{0}-{1}", cfgLevel, CurrentSectionPath);
                    }
                    else if (lineKey == "}")
                    {
                        //System.Diagnostics.Debug.WriteLine("Closing:{0}-{1}", cfgLevel, CurrentSectionPath);

                        if (CurrentSectionPath == "/Localization/" + LangCode)
                        {
                            OutputManager.WriteLine("\tLanguage Code found - Adding new key: {0}={2}{1}", key, value, langTag);
                            newLines.Add(string.Format("\t\t{0} = {2}{1}", key, value, langTag));
                            AddedTag = true;
                        }
                        else if (CurrentSectionPath == "/Localization" && AddedTag == false)
                        {
                            OutputManager.WriteLine("\tLanguage Code missing - Adding new section: {0}={1}", key, value);
                            newLines.Add(string.Format("\t{0}", LangCode));
                            newLines.Add(string.Format("\t{{"));
                            newLines.Add(string.Format("\t\t{0} = {2}{1}", key, value, langTag));
                            newLines.Add(string.Format("\t}}"));
                            AddedTag = true;
                        }

                        CurrentCfgSection = previousSectionStack.Pop();
                        CurrentSectionPath = CurrentSectionPath.Substring(0, CurrentSectionPath.LastIndexOf("/"));
                        cfgLevel--;
                    }
                    else if (pairs.Length < 2)
                    {
                        NextCfgSection = lineKey;
                    }
                    else if (lineKey == key && CurrentSectionPath == "/Localization/" + LangCode)
                    {
                        OutputManager.WriteLine("\tKey found in file - Updating it: {0} = {2}{1}", key, value, langTag);
                        newLines.Add(string.Format("\t\t{0} = {1}", key, value));
                        AddedTag = true;

                        //dont add the original line in this case
                        continue;
                    }
                    newLines.Add(line);
                }
                File.WriteAllLines(filePath, newLines.ToArray());
            }
            catch (Exception ex)
            {
                OutputManager.WriteErrorEx(ex, "\nUnable to add tag to file\n\tTag:\t\t{0}\n\tLanguage:\t{1}\n\tFile:\t\t{2}", key, LangCode, filePath);
                return false;
            }
            return true;
        }
        #endregion

        #region Events
        protected void Action_OnDocumentChanged()
        {
            OutputManager.WriteLine("\nRefactor commited. Adding tags to language files...");

            //if this fires then we changed the actual file
            //Write the value to the cfg files...
            string[] langCodes = currentProject.LocalizerSettings.ProjectSettings.LanguageCodes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (langCodes.Length == 0)
            {
                OutputManager.WriteLine("Error Detected. Theres no language codes defined. CHECK THE CONSOLE, YOUR CFG AND SOURCE FOR ID MISMATCHES!!!!");
                return;
            }

            if (!currentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles || currentProject.LocalizerSettings.ProjectSettings.UseMultiAndBaseCfgFiles)
            {
                string destinationFile = currentProject.LocalizerSettings.ProjectSettings.BaseCfgFile;

                if (!AddTagToFile(newIDKey, newValue, langCodes[0], destinationFile, false))
                {
                    OutputManager.WriteLine("Error Detected. Skipping any other languages. CHECK THE CONSOLE, YOUR CFG AND SOURCE FOR ID MISMATCHES!!!!");
                    return;
                }
            }
            if (currentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles)
            {
                foreach (string langCode in langCodes)
                {
                    string destinationFile = currentProject.LocalizerSettings.ProjectSettings.MultiCfgFile.Replace("{LANGCODE}", langCode);

                    if (!AddTagToFile(newIDKey, newValue, langCode, destinationFile, currentProject.LocalizerSettings.ProjectSettings.AddLanguageCodePrefixToMultiFiles))
                    {
                        OutputManager.WriteLine("Error Detected. Skipping any other languages. CHECK THE CONSOLE, YOUR CFG AND SOURCE FOR ID MISMATCHES!!!!");
                        return;
                    }
                }
            }

            //update the settings...
            if (currentProject.LocalizerSettings.IDType == Settings.LocalizerProjectSettings.IDTypeEnum.ProjectBased)
            {
                currentProject.LocalizerSettings.ProjectSettings.NextProjectID++;
            }
            else
            {
                currentProject.LocalizerSettings.UserSettings.NextUserID++;
            }

            // and rewrite the xmls
            currentProject.LocalizerSettings.WriteAllXML(currentProject.FolderPath);

            OnRefactorComplete?.Invoke();
        }

        public delegate void RefactorComplete();
        public static event RefactorComplete OnRefactorComplete;
        #endregion
    }
}