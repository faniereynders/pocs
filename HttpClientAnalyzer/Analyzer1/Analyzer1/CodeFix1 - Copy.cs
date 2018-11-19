using System;
using System.Composition;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CodeFix2)), Shared]
    public class CodeFix2 : CodeFixProvider
    {
        // TODO: Replace with actual diagnostic id that should trigger this fix.
        public const string DiagnosticId = "EnforceSingletonHttpClientInstance";
        private const string title = @"¯\_(ツ)_/¯ Convert to static";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(DiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root =
              await context.Document.GetSyntaxRootAsync(context.CancellationToken)
              .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the invocation expression identified by the diagnostic.
            var fieldDeclaration =
              root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
              .OfType<FieldDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
              CodeAction.Create(title, c =>
              FixRegexAsync(context.Document, fieldDeclaration, c), equivalenceKey: title), diagnostic);
        }

        private async Task<Document> FixRegexAsync(Document document,
          FieldDeclarationSyntax syntax,
          CancellationToken cancellationToken)
        {
            try
            {
                var modifiers = syntax.Modifiers;
                var staticToken = Token(SyntaxKind.StaticKeyword);
                var newModifiers = modifiers.Add(staticToken);

                var newSyntax = syntax.WithModifiers(newModifiers);
                var root = await document.GetSyntaxRootAsync();

                var newRoot = root.ReplaceNode(syntax, newSyntax).NormalizeWhitespace();

                var newDocument = document.WithSyntaxRoot(newRoot);

                return newDocument;
            }
            catch 
            {
                return document;
                
            }
        
        }
    }
}
