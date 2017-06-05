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

            //find the Trivial that marks the end of this line
            bool foundEOL = false;
            SyntaxNode objToCheck = litDecl;
            SyntaxTrivia objEOL = SyntaxFactory.Comment(" ");
            while (!foundEOL)
            {
                objToCheck = objToCheck.Parent;

                if (objToCheck.ChildNodesAndTokens().Last().HasTrailingTrivia)
                {
                    foreach (SyntaxTrivia item in objToCheck.ChildNodesAndTokens().Last().GetTrailingTrivia())
                    {
                        if(item.Kind() == SyntaxKind.EndOfLineTrivia)
                        {
                            foundEOL = true;
                            objEOL = item;
                            break;
                        }
                    }
                }
                if(objToCheck == root)
                {
                    break;
                }
            }

            var tabs = SyntaxFactory.Whitespace("\t\t");
            var comment = SyntaxFactory.Comment(newIDKey + " = " + newValue);
            List<SyntaxTrivia> lineComment = new List<SyntaxTrivia>();
            lineComment.Add(tabs);
            lineComment.Add(comment);


            var newroot = (CompilationUnitSyntax)root;

            //Set up the call to Localizer.Format
            var localizer = SyntaxFactory.IdentifierName("Localizer");
            var format = SyntaxFactory.IdentifierName("Format");
            var memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localizer, format);

            var arg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newIDKey)));
            var argList = SyntaxFactory.SeparatedList(new[] { arg });

            SyntaxNode writecall =
                SyntaxFactory.InvocationExpression(memberaccess,
                    SyntaxFactory.ArgumentList(argList)

            );

            try
            {
                if (newroot.Usings.Any(u => u.Name.GetText().ToString() == "KSP.Localization"))
                {
                    newroot = newroot.ReplaceNode(litDecl, (SyntaxNode)writecall).InsertTriviaBefore(objEOL,lineComment);
                }
                else
                {
                    newroot = newroot.ReplaceNode(litDecl, (SyntaxNode)writecall).InsertTriviaBefore(objEOL, lineComment).AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("KSP"), SyntaxFactory.IdentifierName("Localization"))));
                }

                var result = document.WithSyntaxRoot(newroot);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LocalizerFormatRefactoring]: Failed in refactoring - " + ex.Message);
            }

            return null;
        }

        private void Action_OnDocumentChanged()
        {
            //if this fires then we changed the actual file
            //Write the value to the cfg files...
            string cfgFile = currentProject.LocalizerSettings.ProjectSettings.BaseCfgFile;
            if (!File.Exists(cfgFile))
            {
                File.WriteAllText(cfgFile, "Localization\n{\n\ten-us\n\t{\n\t}\n}\n", Encoding.UTF8);
            }

            List<string> fileLinesFile = File.ReadAllLines(cfgFile, Encoding.UTF8).ToList();
            fileLinesFile.Insert(
                    fileLinesFile.Count - 2,
                    string.Format("\t\t{0} = {1}", newIDKey, newValue)
                );
            File.WriteAllLines(cfgFile, fileLinesFile.ToArray(), Encoding.UTF8);


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
    }
}