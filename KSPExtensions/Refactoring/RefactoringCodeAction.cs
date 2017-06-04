using Microsoft.CodeAnalysis.CodeActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace KSPExtensions.Refactoring
{
    /// <summary>
    /// This class is an overridden CodeAction so we can detect when a change actually occured and then process items on that event
    /// </summary>
    class RefactoringCodeAction : CodeAction
    {
        public override string Title { get; }
        public override string EquivalenceKey { get; }

        private readonly Func<CancellationToken, Task<Document>> createChangedDocument;
        private readonly Func<CancellationToken, Task<Solution>> createChangedSolution;

        #region Event Structures
        public delegate void ChangesWithNoPreview();
        public event ChangesWithNoPreview OnChangesWithNoPreview;
        #endregion

        /// <summary>
        /// This flag tells us if the ComputePreviewOperationsAsync has been run.
        /// Use it to tell if we get to post without having a preview - ie the thing actually happened!
        /// </summary>
        private bool HasPreviewed;

        #region Constructors
        public RefactoringCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey = null)
        {
            //store the Task in the local fields
            this.createChangedDocument = createChangedDocument;

            Title = title;
            HasPreviewed = false;
            EquivalenceKey = equivalenceKey;
        }

        public RefactoringCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
        {
            //store the Task in the local fields
            this.createChangedSolution = createChangedSolution;

            Title = title;
            HasPreviewed = false;
            EquivalenceKey = equivalenceKey;
        }
        #endregion

        #region Creation Helpers
        //to keep it in line with the base CodeAction
        public static new RefactoringCodeAction Create(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey = null)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (createChangedDocument == null)
            {
                throw new ArgumentNullException(nameof(createChangedDocument));
            }

            return new RefactoringCodeAction(title, createChangedDocument, equivalenceKey);
        }

        public static new RefactoringCodeAction Create(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey = null)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (createChangedSolution == null)
            {
                throw new ArgumentNullException(nameof(createChangedSolution));
            }

            return new RefactoringCodeAction(title, createChangedSolution, equivalenceKey);
        }
        #endregion


        #region Overrides of base class
        protected override Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
        {
            //System.Diagnostics.Debug.WriteLine("Post:{0}",HasPreviewed);
            if (!HasPreviewed)
            {
                OnChangesWithNoPreview?.Invoke();
            }
            HasPreviewed = false;
            return base.PostProcessChangesAsync(document, cancellationToken);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            //System.Diagnostics.Debug.WriteLine("Compute:{0}", HasPreviewed);
            return base.ComputeOperationsAsync(cancellationToken);
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            //System.Diagnostics.Debug.WriteLine("ComputePrev:{0}", HasPreviewed);
            HasPreviewed = true;
            return base.ComputePreviewOperationsAsync(cancellationToken);
        }

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            //System.Diagnostics.Debug.WriteLine("Changed:{0}", HasPreviewed);
            if (createChangedDocument == null)
                return base.GetChangedDocumentAsync(cancellationToken);
            else
                return createChangedDocument(cancellationToken);
        }

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            if (createChangedSolution == null)
                return base.GetChangedSolutionAsync(cancellationToken);
            else 
                return createChangedSolution(cancellationToken);
        }
        #endregion
    }
    }
