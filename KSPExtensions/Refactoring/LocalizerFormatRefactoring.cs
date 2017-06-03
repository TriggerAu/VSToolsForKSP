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
using Microsoft.VisualStudio.Shell.Interop;

namespace KSPExtensions.Refactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(LocalizerFormatRefactoring)), Shared]
    [Export(typeof(IVsHierarchyRefactorNotify))]
    internal class LocalizerFormatRefactoring : CodeRefactoringProvider,  Microsoft.VisualStudio.Shell.Interop.IVsHierarchyRefactorNotify
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            //If the documents project has no localizer settings dont give the option
            if (!ProjectsManager.ProjectHasLocalizerSettings(context.Document.Project.Name))
                return;

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
            var action = CodeAction.Create("Replace String with KSP Localizer.Format", c => ReplaceStringWithLocalizerFormat(context.Document, typeDecl, c));

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private static string cfgFile = "localization.cfg";
        private async Task<Document> ReplaceStringWithLocalizerFormat(Document document, LiteralExpressionSyntax litDecl, CancellationToken cancellationToken)
        {
            //Get the details of the ID to use
            ProjectDetails p = ProjectsManager.projects[document.Project.Name];
            string newIDKey = p.LocalizerSettings.NextTag;

            var root = await document.GetSyntaxRootAsync(cancellationToken);
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
                    newroot = newroot.ReplaceNode(litDecl, (SyntaxNode)writecall);
                }
                else
                {
                    newroot = newroot.ReplaceNode(litDecl, (SyntaxNode)writecall).AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("KSP"), SyntaxFactory.IdentifierName("Localization"))));
                }

                var result = document.WithSyntaxRoot(newroot);

                //if (p.LocalizerSettings.IDType == Settings.LocalizerProjectSettings.IDTypeEnum.ProjectBased)
                //{
                //    p.LocalizerSettings.ProjectSettings.NextProjectID++;
                //} else
                //{
                //    p.LocalizerSettings.UserSettings.NextUserID++;
                //}
                //p.LocalizerSettings.WriteAllXML(p.FolderPath);

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("AAA:" + ex.Message);
            }

            return null;
        }



        public int OnBeforeGlobalSymbolRenamed(uint cItemsAffected, uint[] rgItemsAffected, uint cRQNames, string[] rglpszRQName, string lpszNewName, int promptContinueOnFail)
        {
            return 1;
        }

        public int OnGlobalSymbolRenamed(uint cItemsAffected, uint[] rgItemsAffected, uint cRQNames, string[] rglpszRQName, string lpszNewName)
        {
                        return 1;
        }

        public int OnBeforeReorderParams(uint itemid, string lpszRQName, uint cParamIndexes, uint[] rgParamIndexes, int promptContinueOnFail)
        {
                        return 1;
        }

        public int OnReorderParams(uint itemid, string lpszRQName, uint cParamIndexes, uint[] rgParamIndexes)
        {
                        return 1;
        }

        public int OnBeforeRemoveParams(uint itemid, string lpszRQName, uint cParamIndexes, uint[] rgParamIndexes, int promptContinueOnFail)
        {
                        return 1;
        }

        public int OnRemoveParams(uint itemid, string lpszRQName, uint cParamIndexes, uint[] rgParamIndexes)
        {
                        return 1;
        }

        public int OnBeforeAddParams(uint itemid, string lpszRQName, uint cParams, uint[] rgszParamIndexes, string[] rgszRQTypeNames, string[] rgszParamNames, int promptContinueOnFail)
        {
                        return 1;
        }

        public int OnAddParams(uint itemid, string lpszRQName, uint cParams, uint[] rgszParamIndexes, string[] rgszRQTypeNames, string[] rgszParamNames)
        {
                        return 1;
        }
    }
}