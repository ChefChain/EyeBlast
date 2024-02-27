using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    // Parses a limited subset of HLSL into a Dictionary<string, NodeDef> suitable for use in QuickNode.CompoundOp
    // using a relatively basic implementation of the shunting yard algorithm.  This is useful both in order to
    // implement node adapters based on the nodes' reference implementations in HLSL and to support the Custom Function
    // Node, which allows shader graph authors to define node functionality in terms of HLSL snippets.
    internal static class CompoundOpParser
    {
        // Describes the location of a character in the parsed source string,
        // including its context (the line containing the character).
        internal readonly struct Location
        {
            internal readonly int row;
            internal readonly int col;
            internal readonly string line;

            internal char character => line[col];
            internal char nextCharacter => (col + 1 < line.Length) ? line[col + 1] : '\0';
            internal char nextNextCharacter => (col + 2 < line.Length) ? line[col + 2] : '\0';

            internal Location(int row, int col, string line)
            {
                this.row = row;
                this.col = col;
                this.line = line;
            }

            public override string ToString()
            {
                return $"row {row + 1}, col {col + 1}";
            }
        }

        // Describes a span of multiple characters within a single line of the parsed source string.
        internal readonly struct Span
        {
            internal readonly Location start;
            internal readonly int length;

            internal string contents => start.line.Substring(start.col, length);

            internal Span(Location start, Location end) : this(start, end.col - start.col + 1)
            {
            }

            internal Span(Location start, int length)
            {
                this.start = start;
                this.length = length;
            }

            public override string ToString()
            {
                var before = start.line.Substring(0, start.col);
                var after = start.line.Substring(start.col + length);
                return $"{start}: {before}<color=magenta>{contents}</color>{after}";
            }
        }

        // An exception thrown for any error in lexing, parsing, or compilation.
        // Includes the location of the error in its text.
        internal class ParseException : Exception
        {
            internal string ShortMessage { get; private set; }

            internal ParseException(string message, Span span) : base($"{message} at {span}")
            {
                ShortMessage = $"{message} at {span.start}";
            }
        }

        // Abstract base class for parser inputs: the symbols accessible to the parsed expression.
        internal abstract class ParserInput
        {
            internal string InputType { get; private set; }

            internal ParserInput(string inputType)
            {
                InputType = inputType;
            }
        }

        // A parser input representing an externally-defined input.
        internal class ExternalInput : ParserInput
        {
            internal ExternalInput(string inputType) : base(inputType)
            {
            }
        }

        // A parser input representing a sampler state.
        internal class SamplerStateInput : ParserInput
        {
            internal TextureSamplerState SamplerState { get; private set; }

            internal SamplerStateInput(TextureSamplerState samplerState) : base("samplerstate")
            {
                SamplerState = samplerState;
            }
        }

        // Abstract base class for lexemes: the tokens emitted by the lexer.
        internal abstract class Lexeme
        {
            internal Span Span { get; private set; }

            // Whether or not the presence of this lexeme before an operator implies that it is a prefix
            // (unary +/-, prefix ++/--).
            internal virtual bool PrecedesPrefix => false;

            protected Lexeme(Span span)
            {
                Span = span;
            }

            internal abstract InputDef Compile(
                SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output);
        }

        // A lexeme representing an identifier (keyword, variable name, or function name).
        internal class Symbol : Lexeme
        {
            internal Symbol(Span span) : base(span)
            {
            }

            internal override InputDef Compile(
                SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
            {
                return CompoundOpFunctions.CompileSymbol(node, inputs, output);
            }
        }

        // A lexeme representing a literal numeric value.
        internal class Literal : Lexeme
        {
            internal Literal(Span span) : base(span)
            {
            }

            internal override InputDef Compile(
                SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
            {
                var contents = Span.contents;
                if ("fF".Contains(contents.Last()))
                    contents = contents.Substring(0, contents.Length - 1);
                return new FloatInputDef(MtlxDataTypes.Float, float.Parse(contents));
            }
        }

        // A lexeme representing an operator, including functions and variable
        // definitions that have been promoted from symbols.
        internal class Operator : Lexeme
        {
            internal enum VariantType
            {
                Default,
                Nullary,
                Prefix,
                FunctionCall,
                VariableDefinition,
            }

            internal VariantType Variant { get; private set; }

            internal override bool PrecedesPrefix => Span.contents switch
            {
                ")" or "]" or "}" => false,
                "++" or "--" => Variant == VariantType.Prefix,
                _ => true,
            };

            // HLSL follows C operator precedence/associativity rules:
            // https://en.cppreference.com/w/c/language/operator_precedence
            internal int Precedence => Span.contents switch
            {
                "." => 1,
                "++" or "--" => (Variant == VariantType.Prefix) ? 2 : 1,
                "!" or "~" => 2,
                "+" or "-" => (Variant == VariantType.Prefix) ? 2 : 4,
                "*" or "/" or "%" => 3,
                "<<" or ">>" => 5,
                "<" or "<=" or ">" or ">=" => 6,
                "==" or "!=" => 7,
                "&" => 8,
                "^" => 9,
                "|" => 10,
                "&&" => 11,
                "||" => 12,
                "?" or ":" => 13,
                "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "<<=" or ">>=" or "&=" or "|=" or "^=" => 14,
                "," => 15,
                ";" => 16,
                "(" or "{" or "[" => int.MaxValue, // Braces are handled as special cases.
                _ => 1,
            };

            internal bool IsRightAssociative => Span.contents switch
            {
                "++" or "--" => Variant == VariantType.Prefix,
                "+" or "-" => Variant == VariantType.Prefix,
                "!" or "~" => true,
                "?" or ":" => true,
                "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "<<=" or ">>=" or "&=" or "|=" or "^=" => true,
                _ => false,
            };

            internal Operator(Span span, Lexeme lastLexeme) : base(span)
            {
                var lastPrecedesPrefix = lastLexeme == null || lastLexeme.PrecedesPrefix;
                Variant = Span.contents switch
                {
                    "+" or "-" or "++" or "--" => lastPrecedesPrefix ? VariantType.Prefix : VariantType.Default,
                    _ => VariantType.Default,
                };
            }

            internal Operator(Span span, VariantType variant = VariantType.Default) : base(span)
            {
                Variant = variant;
            }

            internal bool TakesPrecedenceOver(Operator other)
            {
                var thisPrecedence = this.Precedence;
                var otherPrecedence = other.Precedence;
                return thisPrecedence < otherPrecedence ||
                    thisPrecedence == otherPrecedence && !other.IsRightAssociative;
            }

            internal int GetArity(int operandCount)
            {
                return Span.contents switch
                {
                    "(" or "[" or "{" => (Variant == VariantType.Nullary) ? 0 : 1,
                    "++" or "--" => 1,
                    "!" or "~" => 1,
                    "+" or "-" => (Variant == VariantType.Prefix) ? 1 : 2,
                    ";" => operandCount == 1 ? 1 : 2,
                    _ => (Variant == VariantType.FunctionCall || Variant == VariantType.VariableDefinition) ? 1 : 2,
                };
            }

            internal override InputDef Compile(
                SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
            {
                return CompoundOpFunctions.CompileOperator(node, inputs, output);
            }
        }

        // A node in the abstract syntax tree consisting of an identifying lexeme and zero or more children
        // (zero children for a literal value, one for a unary operation, two for a binary operation, etc.)
        internal class SyntaxNode
        {
            internal Lexeme Lexeme { get; private set; }
            internal List<SyntaxNode> Children { get; private set; }

            internal SyntaxNode(Lexeme lexeme, List<SyntaxNode> children = null)
            {
                Lexeme = lexeme;
                if (children == null)
                {
                    Children = new();
                    return;
                }
                Children = children
                    .SelectMany<SyntaxNode, SyntaxNode>(child =>
                    {
                        // Collapse contents of parentheses, separators.
                        if (child.Lexeme is Operator childOp && "(,;".Contains(childOp.Span.contents))
                            return child.Children;
                        else
                            return new[] { child };
                    })
                    .ToList();                
            }

            internal InputDef Compile(Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
            {
                return Lexeme.Compile(this, inputs, output);
            }

            internal void RequireChildCount(int count)
            {
                if (Children.Count != count)
                    throw new ParseException($"Expected {count} operands, found {Children.Count}", Lexeme.Span);
            }
        }

        // Creates a function that will act as a lexical analyzer for the supplied expression.  Each time the function
        // is called, it will return the next lexeme in the sequence.  When there are no more lexemes to return,
        // it will return null.
        static Func<Lexeme> CreateLexer(string expr)
        {
            var row = -1;
            var col = -1;
            string line = null;
            var nextLineIndex = 0;

            // The getNextLocation function returns a location struct containing the position and context of the next
            // character to be examined, or null if the end of the string has been reached.
            Func<Location?> getNextLocation = () =>
            {
                while (true)
                {
                    if (line == null)
                    {
                        if (nextLineIndex >= expr.Length)
                            return null;
                        
                        var newlineIndex = expr.IndexOf('\n', nextLineIndex);
                        var lineEndIndex = (newlineIndex == -1) ? expr.Length : newlineIndex;

                        ++row;
                        col = 0;
                        line = expr.Substring(nextLineIndex, lineEndIndex - nextLineIndex);
                        nextLineIndex = lineEndIndex + 1;
                    }
                    if (col < line.Length)
                        return new Location(row, col++, line);
                    else
                        line = null;
                }
            };

            // We keep track of the last lexeme returned in order to disambiguate modes of usage
            // (prefix versus postfix increment/decrement, unary versus binary plus/minus).
            Lexeme lastLexeme = null;
            return () =>
            {
                while (true)
                {
                    if (getNextLocation() is not Location location)
                        return (lastLexeme = null);
                    
                    // All whitespace is ignored.
                    if (char.IsWhiteSpace(location.character))
                        continue;
                    
                    var start = location;
                    if (location.character == '/')
                    {
                        switch (location.nextCharacter)
                        {
                            case '/':
                                // Single line comment; skip the rest of the current line.
                                line = null;
                                continue;
                            
                            case '*':
                                // Multi-line comment; consume characters until end.
                                getNextLocation();
                                while (true)
                                {
                                    if (getNextLocation() is not Location nextLocation)
                                        throw new ParseException("Unmatched /*", new(start, 2));

                                    if (nextLocation.character == '*' && nextLocation.nextCharacter == '/')
                                    {
                                        getNextLocation();
                                        break;
                                    }
                                }
                                continue;
                        }
                    }

                    if (location.character == '_' || char.IsLetter(location.character))
                    {
                        // Symbols: [_a-zA-z][_a-zA-Z0-9]*
                        while (location.nextCharacter == '_' || char.IsLetterOrDigit(location.nextCharacter))
                        {
                            location = getNextLocation().Value;
                        }
                        return (lastLexeme = new Symbol(new(start, location)));
                    }

                    if (char.IsDigit(location.character) ||
                        "+-".Contains(location.character) &&
                            (char.IsDigit(location.nextCharacter) || location.nextCharacter == '.') ||
                        location.character == '.' && char.IsDigit(location.nextCharacter))
                    {
                        // Numeric literals: [+-]?([0-9]+(.[0-9]*)?|.[0-9]+)([eE][+-]?[0-9]+)?[fF]?
                        if ("+-".Contains(location.character))
                            location = getNextLocation().Value;

                        while (char.IsDigit(location.nextCharacter))
                        {
                            location = getNextLocation().Value;
                        }
                        if (location.nextCharacter == '.')
                        {
                            location = getNextLocation().Value;
                            while (char.IsDigit(location.nextCharacter))
                            {
                                location = getNextLocation().Value;
                            }
                        }
                        if ("eE".Contains(location.nextCharacter))
                        {
                            location = getNextLocation().Value;

                            if ("+-".Contains(location.nextCharacter))
                                location = getNextLocation().Value;
                            
                            while (char.IsDigit(location.nextCharacter))
                            {
                                location = getNextLocation().Value;
                            }
                        }
                        if ("fF".Contains(location.nextCharacter))
                            location = getNextLocation().Value;
                            
                        return (lastLexeme = new Literal(new(start, location)));
                    }
                    
                    if (start.nextCharacter == start.character && "<>".Contains(start.character) &&
                        start.nextNextCharacter == '=')
                    {
                        // Three-character operators: <<=, >>=
                        getNextLocation();
                        getNextLocation();
                        return (lastLexeme = new Operator(new(start, 3), lastLexeme));
                    }
                    else if (start.nextCharacter == start.character && "+-=<>&|".Contains(start.character) ||
                        start.nextCharacter == '=' && "+-*/%<>!&|^".Contains(start.character))
                    {
                        // Two-character operators:
                        // ++, --, ==, <<, >>, &&, ||, +=, -=, *=, /=, %=, <=, >=, !=, &=, |=, ^=
                        getNextLocation();
                        return (lastLexeme = new Operator(new(start, 2), lastLexeme));
                    } 
                    else
                    {
                        // One-character operators: (everything else)
                        return (lastLexeme = new Operator(new(start, 1), lastLexeme));
                    }
                }
            };
        }

        /// <summary>
        /// Parses the supplied expression as containing a limited subset of HLSL, returning a Dictionary of node
        /// definitions suitable for passing to QuickNode.CompoundOp and thence for conversion into MaterialX nodes.
        /// </summary>
        /// <param name="node">The node to use to determine input type mappings.</param>
        /// <param name="sgContext">The subgraph context in which the node exists.</param>
        /// <param name="expr">The expression string to parse.</param>
        /// <param name="inputTypeOverrides">An optional dictionary mapping input names to types
        /// that will override those inferred from the node inputs.</param>
        /// <returns>A dictionary mapping internal and output node names to their definitions.</returns>
        internal static Dictionary<string, NodeDef> Parse(
            AbstractMaterialNode node, SubGraphContext sgContext, string expr,
            Dictionary<string, string> inputTypeOverrides = null)
        {
            Dictionary<string, ParserInput> inputs = new();
            var inputSlots = new List<MaterialSlot>();
            node.GetInputSlots<MaterialSlot>(inputSlots);
            foreach (var inputSlot in inputSlots)
            {
                var inputName = NodeUtils.RemoveWhitespace(SlotUtils.GetName(inputSlot));
                if (inputTypeOverrides != null && inputTypeOverrides.TryGetValue(inputName, out var inputType))
                {
                    inputs.Add(inputName, new ExternalInput(inputType));
                }
                else if (inputSlot is SamplerStateMaterialSlot)
                {
                    var samplerState = SlotUtils.GetPropertyRedirectedTextureSamplerState(inputSlot, sgContext);
                    inputs.Add(inputName, new SamplerStateInput(samplerState));
                }
                else
                {
                    inputs.Add(inputName, new ExternalInput(SlotUtils.GetDataTypeName(inputSlot)));
                }
            }
            return Parse(inputs, expr);
        }

        /// <summary>
        /// Parses the supplied expression as containing a limited subset of HLSL, returning a Dictionary of node
        /// definitions suitable for passing to QuickNode.CompoundOp and thence for conversion into MaterialX nodes.
        /// </summary>
        /// <param name="inputs">Maps input slot names to their metadata.</param>
        /// <param name="expr">The expression string to parse.</param>
        /// <returns>A dictionary mapping internal and output node names to their definitions.</returns>
        internal static Dictionary<string, NodeDef> Parse(Dictionary<string, ParserInput> inputs, string expr)
        {
            // Uses the shunting yard algorithm: https://en.wikipedia.org/wiki/Shunting_yard_algorithm
            Stack<Operator> operatorStack = new();
            List<SyntaxNode> operands = new();

            // The pop action removes the topmost operator from the stack, combines it with its operands
            // (which are removed from the operand list), and adds a new node with the result to the
            // operand list.
            Action popOperator = () =>
            {
                var op = operatorStack.Pop();
                var arity = op.GetArity(operands.Count);
                if (operands.Count < arity)
                    throw new ParseException($"Expected {arity} operands", op.Span);

                var node = new SyntaxNode(op, operands.GetRange(operands.Count - arity, arity));
                operands.RemoveRange(operands.Count - arity, arity);
                operands.Add(node);
            };

            // We keep track of a single "pending" lexeme so that we can look ahead one token to disambiguate
            // modes of usage (variable references versus function calls, e.g.)
            Lexeme pendingLexeme = null; 
            var getNextLexeme = CreateLexer(expr);
            while (true)
            {
                Lexeme lexeme;
                if (pendingLexeme != null)
                {
                    lexeme = pendingLexeme;
                    pendingLexeme = null;
                }
                else
                {
                    lexeme = getNextLexeme();
                }
                if (lexeme == null)
                    break;
                
                switch (lexeme)
                {
                    case Symbol symbol:
                        // Promote to function call if next lexeme is (, variable definition if next is symbol.
                        pendingLexeme = getNextLexeme();
                        if (pendingLexeme is Operator && pendingLexeme.Span.contents == "(")
                            operatorStack.Push(new(symbol.Span, Operator.VariantType.FunctionCall));
                        else if (pendingLexeme is Symbol)
                            operatorStack.Push(new(symbol.Span, Operator.VariantType.VariableDefinition));
                        else
                            operands.Add(new(lexeme));
                        break;
                    
                    case Literal:
                        operands.Add(new(lexeme));
                        break;
                    
                    case Operator op:
                        switch (op.Span.contents)
                        {
                            case "(":
                            case "{":
                            case "[":
                                var closer = op.Span.contents switch 
                                {
                                    "(" => ")",
                                    "{" => "}",
                                    _ => "]",
                                };
                                // Special handling for empty brackets.
                                pendingLexeme = getNextLexeme();
                                if (pendingLexeme is Operator && pendingLexeme.Span.contents == closer)
                                    operatorStack.Push(new(op.Span, Operator.VariantType.Nullary));
                                else
                                    operatorStack.Push(op);
                                break;

                            case ")":
                            case "}":
                            case "]":
                            {
                                var opener = op.Span.contents switch 
                                {
                                    ")" => "(",
                                    "}" => "{",
                                    _ => "[",
                                };
                                while (operatorStack.Count > 0 && operatorStack.Peek().Span.contents != opener)
                                {
                                    popOperator();
                                }
                                if (operatorStack.Count == 0)
                                    throw new ParseException($"Mismatched {op.Span.contents}", op.Span);
                                popOperator();
                                if (operatorStack.Count > 0 &&
                                    operatorStack.Peek().Variant == Operator.VariantType.FunctionCall)
                                {
                                    popOperator();
                                }
                                break;
                            }
                            default:
                                while (operatorStack.Count > 0 && operatorStack.Peek().TakesPrecedenceOver(op))
                                {
                                    popOperator();
                                }
                                operatorStack.Push(op);
                                break;
                        }
                        break;
                }
            }
            while (operatorStack.Count > 0)
            {
                popOperator();
            }

            Dictionary<string, NodeDef> output = new();
            operands.ForEach(node => node.Compile(inputs, output));

            return output;
        }
    }
}