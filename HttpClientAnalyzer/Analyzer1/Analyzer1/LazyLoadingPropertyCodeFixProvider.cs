using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LazyLoadingPropertyCodeFixProvider)), Shared]
    public class LazyLoadingPropertyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(LazyLoadingPropertyAnalyzer.DiagnosticId);
            }
        }
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync();
            var diagnosticNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie:true);
            context.RegisterCodeFix(CodeAction.Create(
                title: $"Initialize a local with the value of '{diagnosticNode.ToString()}'",
                createChangedDocument:c => IntroduceLocal(context.Document, diagnosticNode)
                ), diagnostic);
        }

        private static async Task<Document> IntroduceLocal(Document originalDocument, SyntaxNode diagnosticSyntaxNode)
        {
            ExpressionSyntax diagnosticExpression = diagnosticSyntaxNode as ExpressionSyntax;
            if (diagnosticExpression == null)
            {
                return originalDocument;
            }
            SemanticModel model = await originalDocument.GetSemanticModelAsync();
            IOperation diagnosticOperation = model.GetOperation(diagnosticSyntaxNode);
            ITypeSymbol type = diagnosticOperation.Type;
            string newVariableName = GetNewLocalName(model, diagnosticSyntaxNode);
            LocalDeclarationStatementSyntax newLocal =
                GetVariableDeclaration(type, newVariableName, diagnosticExpression);
            ExpressionSyntax identifier = IdentifierName(newVariableName);
            StatementSyntax methodInvocation = GetMethodInvocation(diagnosticSyntaxNode);
            var editor = await DocumentEditor.CreateAsync(originalDocument);
            editor.InsertBefore(methodInvocation, newLocal);
            editor.ReplaceNode(diagnosticExpression, identifier);
            return editor.GetChangedDocument();
        }

        private static StatementSyntax GetMethodInvocation(SyntaxNode diagnosticSyntaxNode)
        {
            return diagnosticSyntaxNode.FirstAncestorOrSelf<InvocationExpressionSyntax>()
                .FirstAncestorOrSelf<StatementSyntax>();
        }

        private static string GetNewLocalName(SemanticModel model, SyntaxNode diagnosticSyntaxNode)
        {
            var localNames = model.LookupSymbols(diagnosticSyntaxNode.GetLocation().SourceSpan.Start)
                .OfType<ILocalSymbol>()
                .Select(s => s.ToString())
                .ToList();
            string newName = diagnosticSyntaxNode.ToString().Replace(".", "");
            while (localNames.Contains(newName))
            {
                newName = "_" + newName;
            }
            return newName;
        }

        private static LocalDeclarationStatementSyntax GetVariableDeclaration(ITypeSymbol type, string name, ExpressionSyntax value)
        {
            return LocalDeclarationStatement(
                VariableDeclaration(
                    IdentifierName(type.ToString()))
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(Identifier(name))
                    .WithInitializer(EqualsValueClause(value))
                    )));
        }
    }
}
