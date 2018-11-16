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
using Microsoft.CodeAnalysis.Editing;

namespace Analyzer1
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(HttpClientFactoryCodeFixProvider)), Shared]
    public class HttpClientFactoryCodeFixProvider : CodeFixProvider
    {
        // TODO: Replace with actual diagnostic id that should trigger this fix.
       // public const string DiagnosticId = "EnforceSingletonHttpClientInstance";
        private const string title = "Use IHttpClientFactory";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(HttpClientCreationAnalyzer.BlockHttpClientInstantiationRule);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {

            var diagnostic = context.Diagnostics.First();

            context.RegisterCodeFix(
              CodeAction.Create(
                  title: title,
                  createChangedDocument: c => UseHttpClientFactory(context.Document, diagnostic, c),
                  equivalenceKey: title),
              diagnostic);

           

          
        }

        private async Task<Document> UseHttpClientFactory(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            try
            {
                var root = await document.GetSyntaxRootAsync();
                var semanticModel = await document.GetSemanticModelAsync();
                var httpClientCreationExpression = root.FindNode(diagnostic.Location.SourceSpan);

                var generator = SyntaxGenerator.GetGenerator(document);



                //var field = generator.FieldDeclaration("httpClientFactory", ParseTypeName("System.Net.Http.IHttpClientFactory"));
                //var newRoot = generator.InsertMembers(root, 0, new[] { field });


                var operation = semanticModel.GetOperation(httpClientCreationExpression);




                var @class = operation.Syntax.Ancestors().OfType<ClassDeclarationSyntax>().First();

                FieldDeclarationSyntax aField = FieldDeclaration(
    VariableDeclaration(
        ParseTypeName("System.Net.Http.IHttpClientFactory"),
        SeparatedList(new[] { VariableDeclarator(Identifier("httpClientFactory")) })
    ))
    .AddModifiers(Token(SyntaxKind.PrivateKeyword));

                var firstNode = @class.ChildNodes().First();

                

                var newClass = @class.InsertNodesBefore(firstNode, new[] { aField });

                var invocationNode =  ParseExpression("httpClientFactory.CreateClient()");

                var newRoot = root.ReplaceNode(@class, newClass);

                var newRoot2 =
                    httpClientCreationExpression.ReplaceNode(httpClientCreationExpression.ChildNodes().First(),
                        invocationNode);
                //      var arrayTypeSymbol = (IArrayTypeSymbol) operation.Type;
                //     var elementType = arrayTypeSymbol.ElementType;

                // genericname expression = Empty<int> (typeExpr, element type)
                //     var genericName = generator.GenericName("Empty", elementType);
                // type expression = Array
                // member access expression = Array.Empty<int>
                // invocation expression = Array.Empty<int>()

              //  var newDocument = document.WithSyntaxRoot(newRoot2);




                var editor = await DocumentEditor.CreateAsync(document);
              //  editor.RemoveNode(variableDeclaration);
                editor.ReplaceNode(httpClientCreationExpression, newRoot2);
                editor.InsertBefore(firstNode,
                     new[] { aField });

                var newDocument = editor.GetChangedDocument();

                return newDocument;
            }
            catch (Exception ex)
            {

                throw;
            }
        
        }
    }
}
