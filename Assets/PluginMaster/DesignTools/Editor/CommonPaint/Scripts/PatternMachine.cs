/*
Copyright (c) 2020 Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte, 2020.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
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
            COMMA,
            ASTERISK,
            MULTIPLIER,
            ELLIPSIS,
            END
        }

        public class Token
        {
            public readonly PatternState state = PatternState.START;
            protected Token(PatternState state) => this.state = state;
            public static Token START = new Token(PatternState.START);
            public static Token OPENING_PARENTHESIS = new Token(PatternState.OPENING_PARENTHESIS);
            public static Token CLOSING_PARENTHESIS = new Token(PatternState.CLOSING_PARENTHESIS);
            public static Token COMMA = new Token(PatternState.COMMA);
            public static Token ASTERISK = new Token(PatternState.ASTERISK);
            public static Token ELLIPSIS = new Token(PatternState.ELLIPSIS);
            public static Token END = new Token(PatternState.END);
        }

        public class IntToken : Token
        {
            public readonly int value = -1;
            public IntToken(int value, PatternState state) : base(state) => this.value = value;
        }

        public class MultiplierToken : IntToken
        {
            private int _count = 0;
            public int count => _count;
            public MultiplierToken(int value) : base(value, PatternState.MULTIPLIER) { }
            public int IncreaseCount() => ++_count;
            public void Reset() => _count = 0;
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
            MISPLACED_COMMA,
            UNPAIRED_PARENTHESIS,
            EMPTY_PARENTHESIS,
            INVALID_MULTIPLIER,
            INVALID_CHARACTER
        }

        public static ValidationResult Validate(string frecuencyPattern, int lastIndex, out Token[] tokens,
            out Token[] endTokens)
        {
            tokens = null;
            endTokens = null;
            frecuencyPattern = frecuencyPattern.Replace(" ", "");
            if (frecuencyPattern == string.Empty) return ValidationResult.EMPTY;
            var validCharactersRemoved = System.Text.RegularExpressions.Regex.Replace(frecuencyPattern, @"[\d,.*()]", "");
            if (validCharactersRemoved != string.Empty) return ValidationResult.INVALID_CHARACTER;
            var validBracketsRemoved = System.Text.RegularExpressions.Regex.Replace(frecuencyPattern,
                @"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)", "");
            if (System.Text.RegularExpressions.Regex.Match(validBracketsRemoved,
                @"\(|\)").Success) return ValidationResult.UNPAIRED_PARENTHESIS;
            if (System.Text.RegularExpressions.Regex.Match(frecuencyPattern,
                @"\(\)").Success) return ValidationResult.EMPTY_PARENTHESIS;
            var validMultiplicationsRemoved = System.Text.RegularExpressions.Regex.Replace(frecuencyPattern,
                @"(\d+|\))\*\d+", "");
            if(System.Text.RegularExpressions.Regex.Match(validMultiplicationsRemoved,
                @"\*").Success) return ValidationResult.MISPLACED_ASTERISK;
            var validCommasRemoved = System.Text.RegularExpressions.Regex.Replace(frecuencyPattern,
                @"(\)|\.\.\.|\d+)(,(\(|\d+))+", "");
            if (System.Text.RegularExpressions.Regex.Match(validCommasRemoved, @",").Success)
                return ValidationResult.MISPLACED_COMMA;
            var dotCount = frecuencyPattern.Count(c => c == '.');
                if(dotCount > 3) return ValidationResult.MISPLACED_PERIOD;
            var validDotsRemoved = System.Text.RegularExpressions.Regex.Replace(frecuencyPattern,
                @"(\d|\))\.\.\.,?", "");
            if (System.Text.RegularExpressions.Regex.Match(validDotsRemoved, @"\.").Success)
                return ValidationResult.MISPLACED_PERIOD;
            var matches = System.Text.RegularExpressions.Regex.Matches(frecuencyPattern, @"\d+|[(),*]|\.\.\.");
            var tokenList = new System.Collections.Generic.List<Token>();
            var endTokenList = new System.Collections.Generic.List<Token>();

            tokenList.Add(Token.START);
            bool addTokenstoEndList = false;
            void AddTokenToList(Token t)
            {
                if (addTokenstoEndList)
                {
                    if (endTokenList.Count > 0 ) endTokenList.Add(t);
                    else if (t != Token.COMMA) endTokenList.Add(t);
                }
                else
                {
                    tokenList.Add(t);
                    if (t == Token.ELLIPSIS) addTokenstoEndList = true;
                }
            }

            foreach(System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Value == "(") AddTokenToList(Token.OPENING_PARENTHESIS);
                else if (match.Value == ")") AddTokenToList(Token.CLOSING_PARENTHESIS);
                else if (match.Value == ",") AddTokenToList(Token.COMMA);
                else if (match.Value == "*") AddTokenToList(Token.ASTERISK);
                else if (match.Value == "...")
                {
                    if(tokenList.Last() is MultiplierToken) return ValidationResult.MISPLACED_PERIOD;
                    AddTokenToList(Token.ELLIPSIS);
                }
                else
                {
                    var value = int.Parse(match.Value);
                    var list = addTokenstoEndList ? endTokenList : tokenList;
                    var state = list.Count > 0 && list.Last() == Token.ASTERISK
                        ? PatternState.MULTIPLIER : PatternState.INDEX;
                    if (state == PatternState.INDEX && value > lastIndex) return ValidationResult.INDEX_OUT_OF_RANGE;
                    else if (state == PatternState.MULTIPLIER && value < 2) return ValidationResult.INVALID_MULTIPLIER;
                    AddTokenToList(state == PatternState.INDEX ? new IntToken(value, state) : new MultiplierToken(value));
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
            foreach(var token in _tokens) if(token is MultiplierToken) (token as MultiplierToken).Reset();
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
                switch(nextToken.state)
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
                                ? _lastParenthesis : _tokenIndex - 3;
                        break;
                    case PatternState.ELLIPSIS:
                        _tokenIndex = currentState == PatternState.CLOSING_PARENTHESIS
                            ? _lastParenthesis - 1 : _tokenIndex - 2;
                        break;
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
                                ? lastParenthesis : tokenIdx - 3;
                        break;
                    case PatternState.ELLIPSIS:
                        tokenIdx = currentState == PatternState.CLOSING_PARENTHESIS
                            ? lastParenthesis - 1 : tokenIdx - 2;
                        break;
                    default: break;
                }
                ++tokenIdx;
                currentState = nextToken.state;
            }
            return indexesList.ToArray();
        }
        #endregion
    }
}