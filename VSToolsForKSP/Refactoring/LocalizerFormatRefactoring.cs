﻿using System;
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
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(LocalizerFormatRefactoring)), Shared]
    internal class LocalizerFormatRefactoring : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            //If the documents project has no localizer settings dont give the option
            if (!ProjectsManager.ProjectHasLocalizerSettings(context.Document.Project.Name))
                return;

            //Need to make sure a file is configured before here too!!!!

            //Get the doc we need to work with
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a string literal - be it an argument or standalone.
            var argument = node as ArgumentSyntax;
            if (argument != null)
            {
                node = argument.Expression;
            }

            var typeDecl = node as LiteralExpressionSyntax;
            if (typeDecl == null)
            {
                return;
            }

            // Create the action if we are all good
            var action = RefactoringCodeAction.Create("Replace String with KSP Localizer.Format", c => ReplaceStringWithLocalizerFormat(context.Document, typeDecl, c));

            action.OnChangesWithNoPreview += Action_OnDocumentChanged;

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private ProjectDetails currentProject;

        private string newIDKey = "";
        private string newValue = "";
        private async Task<Document> ReplaceStringWithLocalizerFormat(Document document, LiteralExpressionSyntax litDecl, CancellationToken cancellationToken)
        {
            try
            {
                //Get the details of the ID to use
                currentProject = ProjectsManager.projects[document.Project.Name];
                newIDKey = currentProject.LocalizerSettings.NextTag;
                newValue = litDecl.ToString();

                if (newValue.StartsWith("\"")) newValue = newValue.Substring(1);
                if (newValue.EndsWith("\"")) newValue = newValue.Substring(0, newValue.Length - 1);

                //Get the document
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                CompilationUnitSyntax newroot = (CompilationUnitSyntax)root;

                SyntaxNode replacedNode;
                try
                {
                    //Set up the call to Localizer.Format
                    IdentifierNameSyntax localizer = SyntaxFactory.IdentifierName("Localizer");
                    IdentifierNameSyntax format = SyntaxFactory.IdentifierName("Format");
                    MemberAccessExpressionSyntax memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localizer, format);

                    ArgumentSyntax arg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newIDKey)));
                    SeparatedSyntaxList<ArgumentSyntax> argList = SyntaxFactory.SeparatedList(new[] { arg });

                    SyntaxAnnotation syntaxAnnotation = new SyntaxAnnotation("LocalizerFormat");

                    SyntaxNode writecall =
                        SyntaxFactory.InvocationExpression(memberaccess,
                            SyntaxFactory.ArgumentList(argList)

                    ).WithAdditionalAnnotations(syntaxAnnotation).WithTriviaFrom(litDecl);


                     newroot = newroot.ReplaceNode(litDecl, (SyntaxNode)writecall);

                    //get the changed node back from teh updated document root
                    replacedNode = newroot.GetAnnotatedNodes(syntaxAnnotation).Single();
                }
                catch (Exception ex)
                {
                    OutputManager.WriteErrorEx(ex, "Unable to compile the Localizer call. Refactor cancelled.");
                    return null;
                }

                try
                {
                    //find the Trivial that marks the end of this line
                    bool foundEOL = false;
                    SyntaxNode objToCheck = replacedNode;
                    SyntaxTrivia objEOL = SyntaxFactory.Comment(" ");

                    //This look works upwards through the structure by parent to get bigger bits of the 
                    // syntax tree to find the first EOL after the replaced Node
                    while (!foundEOL)
                    {
                        //Go up one level
                        objToCheck = objToCheck.Parent;

                        //If we found it get out
                        if (FindEOLAfter(objToCheck, replacedNode.FullSpan.End, ref objEOL))
                        {
                            foundEOL = true;
                        }

                        //If we just checked the whole document then stop looping
                        if (objToCheck == root)
                        {
                            break;
                        }
                    }

                    //If we found the EOL Trivia then insert the new comment before it
                    if (foundEOL)
                    {
                        var tabs = SyntaxFactory.Whitespace("\t\t");
                        var comment = SyntaxFactory.Comment("// " + newIDKey + " = " + newValue);
                        List<SyntaxTrivia> lineComment = new List<SyntaxTrivia>();
                        lineComment.Add(tabs);
                        lineComment.Add(comment);

                        newroot = newroot.InsertTriviaBefore(objEOL, lineComment);
                    }
                }
                catch (Exception ex)
                {
                    OutputManager.WriteErrorEx(ex, "Unable to add comment to end of line. Add it manually if you like.");
                }


                try { 
                //Make sure the file has a usings so the short name works
                if (!newroot.Usings.Any(u => u.Name.GetText().ToString() == "KSP.Localization"))
                {
                    newroot = newroot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("KSP"), SyntaxFactory.IdentifierName("Localization"))));
                }
            }
            catch (Exception ex)
            {
                OutputManager.WriteErrorEx(ex, "Unable to add usings line to head of file. Add it manually.");
            }

            //Now convert it to the document to send it back
            try
            {
                    var result = document.WithSyntaxRoot(newroot);

                    return result;
                }
                catch (Exception ex)
                {
                    OutputManager.WriteErrorEx(ex, "Unable to rewrite the document. Refactor cancelled.");
                    return null;
                }

            }
            catch (Exception ex)
            {
                OutputManager.WriteErrorEx(ex, "General error refactoring the token. Refactor cancelled.");
            }

            return null;
        }

        /// <summary>
        /// Search the Children of this object for an EOL Trivia after the position of the replacedNode
        /// </summary>
        /// <param name="objToSearch">The Node or Token to search the children of (and then itself) for a SyntaxKind.EndOfLineTrivia</param>
        /// <param name="endReplacedSpan">The character position at the end of the replacedNode - dont want EOLs from before the replacement</param>
        /// <param name="foundEOL">the object to get back to the top when we find it</param>
        /// <returns>True if we found  it</returns>
        bool FindEOLAfter(SyntaxNodeOrToken objToSearch, int endReplacedSpan, ref SyntaxTrivia foundEOL)
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

        private void Action_OnDocumentChanged()
        {
            OutputManager.WriteLine("\nRefactor commited. Adding tags to language files...");

            //if this fires then we changed the actual file
            //Write the value to the cfg files...
            string[] langCodes = currentProject.LocalizerSettings.ProjectSettings.LanguageCodes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if(langCodes.Length == 0)
            {
                OutputManager.WriteLine("Error Detected. Theres no language codes defined. CHECK THE CONSOLE, YOUR CFG AND SOURCE FOR ID MISMATCHES!!!!");
                return;
            }

            if (!currentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles || currentProject.LocalizerSettings.ProjectSettings.UseMultiAndBaseCfgFiles)
            {
                string destinationFile = currentProject.LocalizerSettings.ProjectSettings.BaseCfgFile;

                if (!AddTagToFile(newIDKey, newValue, langCodes[0], destinationFile,false))
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

        public delegate void RefactorComplete();
        public static event RefactorComplete OnRefactorComplete;
    }
}