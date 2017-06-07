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

namespace KSPExtensions.Refactoring
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


        private string newIDKey="";
        private string newValue = "";
        private async Task<Document> ReplaceStringWithLocalizerFormat(Document document, LiteralExpressionSyntax litDecl, CancellationToken cancellationToken)
        {
            //Get the details of the ID to use
            currentProject = ProjectsManager.projects[document.Project.Name];
            newIDKey = currentProject.LocalizerSettings.NextTag;
            newValue = litDecl.ToString();

            if (newValue.StartsWith("\"")) newValue = newValue.Substring(1);
            if (newValue.EndsWith("\"")) newValue = newValue.Substring(0,newValue.Length-1);

            //Get the document
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newroot = (CompilationUnitSyntax)root;

            //Set up the call to Localizer.Format
            var localizer = SyntaxFactory.IdentifierName("Localizer");
            var format = SyntaxFactory.IdentifierName("Format");
            var memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localizer, format);

            var arg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newIDKey)));
            var argList = SyntaxFactory.SeparatedList(new[] { arg });

            var syntaxAnnotation = new SyntaxAnnotation("LocalizerFormat");

            SyntaxNode writecall =
                SyntaxFactory.InvocationExpression(memberaccess,
                    SyntaxFactory.ArgumentList(argList)

            ).WithAdditionalAnnotations(syntaxAnnotation).WithTriviaFrom(litDecl);
            

            newroot = newroot.ReplaceNode(litDecl, (SyntaxNode)writecall);

            //get the changed node back from teh updated document root
            var replacedNode = newroot.GetAnnotatedNodes(syntaxAnnotation).Single();

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
                if(FindEOLAfter(objToCheck, replacedNode.FullSpan.End,ref objEOL))
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

            //Make sure the file has a usings so the short name works
            if (!newroot.Usings.Any(u => u.Name.GetText().ToString() == "KSP.Localization"))
            {
                newroot = newroot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("KSP"), SyntaxFactory.IdentifierName("Localization"))));
            }
                         
            //Now convert it to the document to send it back
            try
            {
                var result = document.WithSyntaxRoot(newroot);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LocalizerFormatRefactoring]: Failed in refactoring - " + ex.Message);
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
        bool FindEOLAfter(SyntaxNodeOrToken objToSearch,int endReplacedSpan, ref SyntaxTrivia foundEOL)
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
            //if this fires then we changed the actual file
            //Write the value to the cfg files...
            string[] langCodes = currentProject.LocalizerSettings.ProjectSettings.LanguageCodes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string langCode in langCodes)
            {
                string destinationFile = currentProject.LocalizerSettings.ProjectSettings.BaseCfgFile;
                if (currentProject.LocalizerSettings.ProjectSettings.UseMultiCfgFiles)
                {
                    destinationFile = currentProject.LocalizerSettings.ProjectSettings.MultiCfgFile.Replace("{LANGCODE}", langCode);
                }

                AddTagToFile(newIDKey, newValue, langCode, destinationFile);
            }

                

            //string cfgFile = currentProject.LocalizerSettings.ProjectSettings.BaseCfgFile;
            //if (!File.Exists(cfgFile))
            //{
            //    File.WriteAllText(cfgFile, "Localization\n{\n\ten-us\n\t{\n\t}\n}\n", Encoding.UTF8);
            //}

            //List<string> fileLinesFile = File.ReadAllLines(cfgFile, Encoding.UTF8).ToList();
            //fileLinesFile.Insert(
            //        fileLinesFile.Count - 2,
            //        string.Format("\t\t{0} = {1}", newIDKey, newValue)
            //    );
            //File.WriteAllLines(cfgFile, fileLinesFile.ToArray(), Encoding.UTF8);

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

        private void AddTagToFile(string key, string value, string LangCode, string filePath)
        {
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
                    System.Diagnostics.Debug.WriteLine("Opening:{0}-{1}", cfgLevel, CurrentSectionPath);
                }
                else if (lineKey == "}")
                {
                    System.Diagnostics.Debug.WriteLine("Closing:{0}-{1}", cfgLevel, CurrentSectionPath);

                    if (CurrentSectionPath == "/Localization/" + LangCode)
                    {
                        newLines.Add(string.Format("\t\t{0} = {1}", key, value));
                        AddedTag = true;
                    }
                    else if (CurrentSectionPath == "/Localization" && AddedTag == false)
                    {
                        newLines.Add(string.Format("\t{0}", LangCode));
                        newLines.Add(string.Format("\t{{"));
                        newLines.Add(string.Format("\t\t{0} = {1}", key, value));
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
                    System.Diagnostics.Debug.WriteLine("KEY FOUND - Updating it:{0}={1}", key,value);
                    newLines.Add(string.Format("\t\t{0} = {1}", key, value));
                    AddedTag = true;

                    //dont add the original line in this case
                    continue;
                }
                newLines.Add(line);
            }
            File.WriteAllLines(filePath, newLines.ToArray());
        }

        public delegate void RefactorComplete();
        public static event RefactorComplete OnRefactorComplete;
    }
}