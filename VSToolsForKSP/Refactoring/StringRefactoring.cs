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

using VSToolsForKSP.Managers;

namespace VSToolsForKSP.Refactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(LocalizerFormatRefactoring)), Shared]
    internal class StringRefactoring : RefactoringCodeProvider
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

            // Only offer a refactoring if the selected node is a string literal - be it an argument, standalone or attirbute value.
            var argument = node as ArgumentSyntax;
            if (argument != null)
            {
                node = argument.Expression;
            }
            else
            {
                var attributeArgument = node as AttributeArgumentSyntax;
                if(attributeArgument!= null)
                {
                    node = attributeArgument.Expression;
                }
            }

            var typeDecl = node as LiteralExpressionSyntax;
            if (typeDecl == null)
            {
                return;
            }

            // Create the action if we are all good
            var action = RefactoringCodeAction.Create("Replace String with KSP Localization Tag", c => ReplaceStringWithAutoLOC(context.Document, typeDecl, c));

            action.OnChangesWithNoPreview += Action_OnDocumentChanged;

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private ProjectDetails currentProject;

        private string newIDKey = "";
        private string newValue = "";
        private async Task<Document> ReplaceStringWithAutoLOC(Document document, LiteralExpressionSyntax litDecl, CancellationToken cancellationToken)
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
                    //IdentifierNameSyntax localizer = SyntaxFactory.IdentifierName("Localizer");
                    //IdentifierNameSyntax format = SyntaxFactory.IdentifierName("Format");
                    //MemberAccessExpressionSyntax memberaccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localizer, format);

                    //ArgumentSyntax arg = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newIDKey)));
                    //SeparatedSyntaxList<ArgumentSyntax> argList = SyntaxFactory.SeparatedList(new[] { arg });

                    SyntaxAnnotation syntaxAnnotation = new SyntaxAnnotation("LocalizedString");

                    SyntaxNode writecall =
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newIDKey)
                        //SyntaxFactory.InvocationExpression(memberaccess,
                        //    SyntaxFactory.ArgumentList(argList)

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
    }
}