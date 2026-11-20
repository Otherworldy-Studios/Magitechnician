/*
Copyright (c) Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#pragma warning disable UDR0001
using System.Linq;

namespace PluginMaster
{
    public class PatternMachine
    {
        #region STATES AND TOKENS
        public enum PatternState
        {
            START,
            INDEX,
            OPENING_PARENTHESIS,
            CLOSING_PARENTHESIS,
            MULTIPLIER,
            ELLIPSIS,
            RANDOM_INDEX,
            END
        }

        public class Token
        {
            public readonly PatternState state = PatternState.START;
            protected Token(PatternState state) => this.state = state;
            public static readonly Token START = new Token(PatternState.START);
            public static readonly Token OPENING_PARENTHESIS = new Token(PatternState.OPENING_PARENTHESIS);
            public static readonly Token CLOSING_PARENTHESIS = new Token(PatternState.CLOSING_PARENTHESIS);
            public static readonly Token ELLIPSIS = new Token(PatternState.ELLIPSIS);
            public static readonly Token END = new Token(PatternState.END);
        }

        public class IntToken : Token
        {
            public readonly int value = -1;
            public IntToken(int value) : base(PatternState.INDEX) => this.value = value;
        }

        public class MultiplierToken : Token
        {
            public readonly int value = -1;
            private int _count = 0;
            public int count => _count;
            public MultiplierToken(int value) : base(PatternState.MULTIPLIER) => this.value = value;
            public int IncreaseCount() => ++_count;
            public void Reset() => _count = 0;
        }

        public class RandomIndexToken : Token
        {
            private static readonly System.Random _random = new System.Random();
            public readonly (int index, float weight)[] weights;
            private readonly float _totalWeight;
            public int GetRandomIndex()
            {
                var randomValue = (float)(_random.NextDouble() * _totalWeight);
                foreach (var (index, weight) in weights)
                {
                    if (randomValue < weight) return index;
                    randomValue -= weight;
                }
                return weights.Last().index;
            }
            public RandomIndexToken((int index, float weight)[] weights) : base(PatternState.RANDOM_INDEX)
            {
                this.weights = weights;
                _totalWeight = weights.Sum(w => w.weight);
            }
        }
        #endregion
        #region VALIDATE
        public enum ValidationResult
        {
            VALID,
            EMPTY,
            INDEX_OUT_OF_RANGE,
            MISPLACED_PERIOD,
            MISPLACED_ASTERISK,
            MISSING_COMMA,
            MISPLACED_COMMA,
            UNPAIRED_PARENTHESIS,
            EMPTY_PARENTHESIS,
            INVALID_MULTIPLIER,
            UNPAIRED_BRACKET,
            EMPTY_BRACKET,
            INVALID_NESTED_BRACKETS,
            INVALID_PARENTHESES_WITHIN_BRACKETS,
            MISPLACED_VERTICAL_BAR,
            MISPLACED_COLON,
            INVALID_CHARACTER
        }

        public static ValidationResult Validate(string frequencyPattern, int lastIndex, out Token[] tokens,
            out Token[] endTokens)
        {
            tokens = null;
            endTokens = null;
            frequencyPattern = frequencyPattern.Replace(" ", "");

            if (frequencyPattern == string.Empty) return ValidationResult.EMPTY;

            // Validate allowed characters, now including brackets, vertical bars, and colons
            var validCharactersRemoved = System.Text.RegularExpressions.Regex.Replace(frequencyPattern,
                @"[0-9.,()\[\]:|*]+", "");
            if (validCharactersRemoved != string.Empty) return ValidationResult.INVALID_CHARACTER;

            // Validate unpaired parentheses
            var validBracketsRemoved = System.Text.RegularExpressions.Regex.Replace(frequencyPattern,
                @"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)", "");
            if (System.Text.RegularExpressions.Regex.Match(validBracketsRemoved, @"\(|\)").Success)
                return ValidationResult.UNPAIRED_PARENTHESIS;

            // Validate empty parentheses
            if (System.Text.RegularExpressions.Regex.Match(frequencyPattern, @"\(\)").Success)
                return ValidationResult.EMPTY_PARENTHESIS;

            // Validate unpaired brackets
            var validSquareBracketsRemoved = System.Text.RegularExpressions.Regex.Replace(frequencyPattern,
                @"\[(?>\[(?<c>)|[^\[\]]+|\](?<-c>))*(?(c)(?!))\]", "");
            if (System.Text.RegularExpressions.Regex.Match(validSquareBracketsRemoved, @"\[|\]").Success)
                return ValidationResult.UNPAIRED_BRACKET;

            // Validate empty brackets
            if (System.Text.RegularExpressions.Regex.Match(frequencyPattern, @"\[\]").Success)
                return ValidationResult.EMPTY_BRACKET;

            // Validate invalid nested structures in brackets
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\[\[|\]\]"))
                return ValidationResult.INVALID_NESTED_BRACKETS;
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\[\([^\]]*\)\]"))
                return ValidationResult.INVALID_PARENTHESES_WITHIN_BRACKETS;

            // Validate vertical bars outside brackets
            var noBrackets = System.Text.RegularExpressions.Regex.Replace(frequencyPattern, @"\[.*?\]", "");
            if (System.Text.RegularExpressions.Regex.IsMatch(noBrackets, @"\|"))
                return ValidationResult.MISPLACED_VERTICAL_BAR;

            // Validate vertical bars not between numbers within brackets
            var noValidBars = System.Text.RegularExpressions.Regex.Replace(frequencyPattern, @"(?<=\d)\|(?=\d)", "");
            if (System.Text.RegularExpressions.Regex.IsMatch(noValidBars, @"\|"))
                return ValidationResult.MISPLACED_VERTICAL_BAR;

            // Validate misplaced colons by removing valid index:weight patterns inside brackets
            var validColonsRemoved = System.Text.RegularExpressions.Regex.Replace(frequencyPattern,
                @"\[(?>\d+(:\d+)?(\|\d+(:\d+)?)*|[^:\[\]]+)*(?<!:)\]", "");
            if (System.Text.RegularExpressions.Regex.Match(validColonsRemoved, @":").Success)
                return ValidationResult.MISPLACED_COLON;

            // Validate multiplications, allowing them after brackets
            var validMultiplicationsRemoved = System.Text.RegularExpressions.Regex.Replace(frequencyPattern,
                @"(\d+|\)|\])\*\d+", "");
            if (System.Text.RegularExpressions.Regex.Match(validMultiplicationsRemoved, @"\*").Success)
                return ValidationResult.MISPLACED_ASTERISK;

            // Validate missing commas
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\]\(|\)\[|\]\[|\)\("))
                return ValidationResult.MISSING_COMMA;
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"(?<=[\]\)]|\d)(?=[\[\(])"))
                return ValidationResult.MISSING_COMMA;
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"(?<=[\]\)])(?=\d)"))
                return ValidationResult.MISSING_COMMA;

            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\[[^\]]*?,[^\]]*?\]"))
                return ValidationResult.MISPLACED_COMMA;
            var validCommasRemoved = System.Text.RegularExpressions.Regex.Replace(frequencyPattern,
                @"(?<=^|\)|\]|\d|\.\.\.),(?=\d|\(|\[)", "");
            if (System.Text.RegularExpressions.Regex.Match(validCommasRemoved, @",").Success)
                return ValidationResult.MISPLACED_COMMA;

            // Validate ellipses inside parentheses
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\([^\)]*?\.\.\.[^\)]*?\)"))
                return ValidationResult.MISPLACED_PERIOD;
            // Validate ellipses inside brackets
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\[[^\]]*?\.\.\.[^\]]*?\]"))
                return ValidationResult.MISPLACED_PERIOD;
            // Validate single ellipsis in the entire pattern
            var ellipsisMatches = System.Text.RegularExpressions.Regex.Matches(frequencyPattern, @"\.\.\.");
            if (ellipsisMatches.Count > 1)
                return ValidationResult.MISPLACED_PERIOD;
            // If there is a single ellipsis, validate its position
            if (ellipsisMatches.Count == 1)
            {
                var ellipsisPosition = ellipsisMatches[0].Index;
                // Validate ellipsis is after a number, closing parenthesis, or closing bracket
                if (!System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern.Substring(0, ellipsisPosition),
                    @".*(\d|\)|\])$"))
                    return ValidationResult.MISPLACED_PERIOD;
                // Validate ellipsis is at the end or before a comma
                if (!System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern.Substring(ellipsisPosition),
                    @"^\.\.\.(,|$)"))
                    return ValidationResult.MISPLACED_PERIOD;
            }
            // Validate points inside brackets
            if (System.Text.RegularExpressions.Regex.IsMatch(frequencyPattern, @"\[[^\]]*?(?<!:)\d+\.\d*[^\]]*?\]"))
                return ValidationResult.MISPLACED_PERIOD;

            // Validate points outside brackets by removing ellipses and brackets
            var noEllipsesOrBrackets = System.Text.RegularExpressions.Regex.Replace(frequencyPattern, @"\[.*?\]|\.\.\.", "");
            if (System.Text.RegularExpressions.Regex.IsMatch(noEllipsesOrBrackets, @"\."))
                return ValidationResult.MISPLACED_PERIOD;

            // Tokenize the input
            var matches = System.Text.RegularExpressions.Regex.Matches(frequencyPattern, @"\[.*?\]|\d+|\*\d+|[()|]|\.\.\.");
            var tokenList = new System.Collections.Generic.List<Token>();
            var endTokenList = new System.Collections.Generic.List<Token>();

            tokenList.Add(Token.START);
            bool addTokenstoEndList = false;
            void AddTokenToList(Token t)
            {
                if (addTokenstoEndList)
                {
                    endTokenList.Add(t);
                }
                else
                {
                    tokenList.Add(t);
                    if (t == Token.ELLIPSIS) addTokenstoEndList = true;
                }
            }

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Value == "(") AddTokenToList(Token.OPENING_PARENTHESIS);
                else if (match.Value == ")") AddTokenToList(Token.CLOSING_PARENTHESIS);
                else if (match.Value.Contains("*"))
                {
                    var value = int.Parse(match.Value.Substring(1));
                    if (value < 2) return ValidationResult.INVALID_MULTIPLIER;
                    AddTokenToList(new MultiplierToken(value));
                }
                else if (match.Value == "...") AddTokenToList(Token.ELLIPSIS);
                else if (match.Value.StartsWith("["))
                {
                    var content = match.Value.Substring(1, match.Value.Length - 2);
                    var entries = content.Split('|');
                    var weights = new System.Collections.Generic.List<(int index, float weight)>();

                    foreach (var entry in entries)
                    {
                        var parts = entry.Split(':');
                        int index = int.Parse(parts[0]);
                        float weight = parts.Length > 1 ? float.Parse(parts[1]) : 1.0f;
                        weights.Add((index, weight));
                    }

                    AddTokenToList(new RandomIndexToken(weights.ToArray()));
                }
                else
                {
                    var value = int.Parse(match.Value);
                    if (value > lastIndex) return ValidationResult.INDEX_OUT_OF_RANGE;
                    AddTokenToList(new IntToken(value));
                }
            }

            tokenList.Add(Token.END);
            endTokenList.Add(Token.END);
            tokens = tokenList.ToArray();
            endTokens = endTokenList.ToArray();
            return ValidationResult.VALID;
        }

        #endregion
        #region MACHINE
        private Token[] _tokens = null;
        private int _tokenIndex = 0;
        private System.Collections.Generic.Stack<int> _parenthesisStack = new System.Collections.Generic.Stack<int>();
        private int _lastParenthesis = -1;

        private Token[] _endTokens = null;

        public PatternMachine(Token[] tokens, Token[] endTokens) => (_tokens, _endTokens) = (tokens, endTokens);

        public void SetTokens(Token[] tokens, Token[] endTokens)
        {
            if (!Enumerable.SequenceEqual(tokens, _tokens)) _tokens = tokens;
            if (!Enumerable.SequenceEqual(endTokens, _endTokens)) _endTokens = endTokens;
        }

        public void Reset()
        {
            _tokenIndex = 0;
            foreach (var token in _tokens) if (token is MultiplierToken) (token as MultiplierToken).Reset();
        }

        public int nextIndex
        {
            get
            {
                if (_tokenIndex == -1) return -1;
                var currentState = _tokens[_tokenIndex].state;
                if (currentState == PatternState.END) return -1;
                ++_tokenIndex;
                var nextToken = _tokens[_tokenIndex];
                switch (nextToken.state)
                {
                    case PatternState.INDEX:
                        return (nextToken as IntToken).value;
                    case PatternState.OPENING_PARENTHESIS:
                        _parenthesisStack.Push(_tokenIndex);
                        break;
                    case PatternState.CLOSING_PARENTHESIS:
                        _lastParenthesis = _parenthesisStack.Pop();
                        break;
                    case PatternState.MULTIPLIER:
                        var mult = nextToken as MultiplierToken;
                        if (mult.IncreaseCount() < mult.value)
                            _tokenIndex = currentState == PatternState.CLOSING_PARENTHESIS
                                ? _lastParenthesis - 1 : _tokenIndex -= 2;
                        break;
                    case PatternState.ELLIPSIS:
                        _tokenIndex = currentState == PatternState.CLOSING_PARENTHESIS
                            ? _lastParenthesis - 1 : _tokenIndex -= 2;
                        break;
                    case PatternState.RANDOM_INDEX:
                        var randomIndexToken = nextToken as RandomIndexToken;
                        return randomIndexToken.GetRandomIndex();
                    case PatternState.END:
                        return -1;
                    default:
                        break;
                }
                return nextIndex;
            }
        }

        public int[] GetEndIndexes()
        {
            int tokenIdx = 0;
            foreach (var token in _endTokens) if (token is MultiplierToken) (token as MultiplierToken).Reset();
            var currentState = _endTokens[0].state;
            if (currentState == PatternState.END) return new int[0];
            var indexesList = new System.Collections.Generic.List<int>();
            var parenthesisStack = new System.Collections.Generic.Stack<int>();
            var lastParenthesis = -1;
            while (currentState != PatternState.END)
            {
                var nextToken = _endTokens[tokenIdx];
                switch (nextToken.state)
                {
                    case PatternState.INDEX:
                        indexesList.Add((nextToken as IntToken).value);
                        break;
                    case PatternState.OPENING_PARENTHESIS:
                        parenthesisStack.Push(tokenIdx);
                        break;
                    case PatternState.CLOSING_PARENTHESIS:
                        lastParenthesis = parenthesisStack.Pop();
                        break;
                    case PatternState.MULTIPLIER:
                        var mult = nextToken as MultiplierToken;
                        if (mult.IncreaseCount() < mult.value)
                            tokenIdx = currentState == PatternState.CLOSING_PARENTHESIS
                                ? lastParenthesis - 1 : tokenIdx = tokenIdx - 2;
                        break;
                    case PatternState.ELLIPSIS:
                        tokenIdx = currentState == PatternState.CLOSING_PARENTHESIS ? lastParenthesis - 1 : tokenIdx - 2;
                        break;
                    case PatternState.RANDOM_INDEX:
                        var randomIndexToken = nextToken as RandomIndexToken;
                        indexesList.Add(randomIndexToken.GetRandomIndex());
                        break;
                    default: break;
                }
                ++tokenIdx;
                currentState = nextToken.state;
            }
            return indexesList.ToArray();
        }

        public int tokenIndex => _tokenIndex;
        public void SetTokenIndex(int value)
        {
            if (value <= 0 || value >= _tokens.Length) return;
            _tokenIndex = value;
        }
        #endregion
    }
}
#pragma warning restore UDR0001
