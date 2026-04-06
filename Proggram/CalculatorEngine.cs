using System;
using System.Collections.Generic;

namespace ModernCalculator
{
    /// <summary>
    /// Pure calculation engine — no UI dependencies.
    /// Handles standard and scientific operations with expression history.
    /// </summary>
    internal class CalculatorEngine
    {
        // ── State ──────────────────────────────────────────────────────────
        private double _operand1;
        private double _operand2;
        private string _operator = string.Empty;
        private bool _newEntry = true;          // next digit starts a fresh number
        private bool _justEvaluated = false;    // last action was '='
        private string _currentDisplay = "0";
        private string _expressionDisplay = string.Empty;

        private readonly List<string> _history = new();

        // ── Public state accessors ─────────────────────────────────────────
        public string CurrentDisplay  => _currentDisplay;
        public string ExpressionDisplay => _expressionDisplay;
        public IReadOnlyList<string> History => _history;

        // ── Digit / decimal input ──────────────────────────────────────────
        public void InputDigit(string digit)
        {
            if (_justEvaluated)
            {
                _currentDisplay = digit;
                _expressionDisplay = string.Empty;
                _justEvaluated = false;
                _newEntry = false;
                return;
            }

            if (_newEntry)
            {
                _currentDisplay = digit;
                _newEntry = false;
            }
            else
            {
                if (_currentDisplay == "0" && digit != ".")
                    _currentDisplay = digit;
                else if (digit == "." && _currentDisplay.Contains('.'))
                    return;
                else
                    _currentDisplay += digit;
            }
        }

        // ── Operator ───────────────────────────────────────────────────────
        public void InputOperator(string op)
        {
            if (!_newEntry)
            {
                if (!string.IsNullOrEmpty(_operator) && !_justEvaluated)
                    Calculate(false);
                _operand1 = double.Parse(_currentDisplay);
            }

            _operator = op;
            _expressionDisplay = $"{FormatNumber(_operand1)} {OpSymbol(op)}";
            _newEntry = true;
            _justEvaluated = false;
        }

        // ── Equals ────────────────────────────────────────────────────────
        public void Equals()
        {
            if (string.IsNullOrEmpty(_operator)) return;

            _operand2 = double.Parse(_currentDisplay);
            string expr = $"{FormatNumber(_operand1)} {OpSymbol(_operator)} {FormatNumber(_operand2)} =";
            Calculate(true);
            AddHistory($"{expr} {_currentDisplay}");
            _expressionDisplay = expr;
            _justEvaluated = true;
        }

        private void Calculate(bool finalise)
        {
            double op2 = finalise ? _operand2 : double.Parse(_currentDisplay);

            double result = _operator switch
            {
                "+"  => _operand1 + op2,
                "-"  => _operand1 - op2,
                "*"  => _operand1 * op2,
                "/"  => op2 == 0 ? double.NaN : _operand1 / op2,
                "^"  => Math.Pow(_operand1, op2),
                "%"  => _operand1 * op2 / 100,
                _    => op2
            };

            _currentDisplay = double.IsNaN(result)
                ? "Error"
                : FormatNumber(result);

            if (finalise)
            {
                _operand1 = double.IsNaN(result) ? 0 : result;
                _newEntry = true;
            }
        }

        // ── Scientific operations ─────────────────────────────────────────
        public void ScientificOperation(string op)
        {
            if (!double.TryParse(_currentDisplay, out double val)) return;

            double result;
            string expr;

            switch (op)
            {
                case "sin":
                    result = Math.Sin(ToRadians(val));
                    expr   = $"sin({FormatNumber(val)})";
                    break;
                case "cos":
                    result = Math.Cos(ToRadians(val));
                    expr   = $"cos({FormatNumber(val)})";
                    break;
                case "tan":
                    result = val % 180 == 90 ? double.NaN : Math.Tan(ToRadians(val));
                    expr   = $"tan({FormatNumber(val)})";
                    break;
                case "log":
                    result = val <= 0 ? double.NaN : Math.Log10(val);
                    expr   = $"log({FormatNumber(val)})";
                    break;
                case "ln":
                    result = val <= 0 ? double.NaN : Math.Log(val);
                    expr   = $"ln({FormatNumber(val)})";
                    break;
                case "sqrt":
                    result = val < 0 ? double.NaN : Math.Sqrt(val);
                    expr   = $"√({FormatNumber(val)})";
                    break;
                case "sq":
                    result = val * val;
                    expr   = $"sqr({FormatNumber(val)})";
                    break;
                case "cube":
                    result = val * val * val;
                    expr   = $"cube({FormatNumber(val)})";
                    break;
                case "inv":
                    result = val == 0 ? double.NaN : 1 / val;
                    expr   = $"1/({FormatNumber(val)})";
                    break;
                case "fact":
                    result = Factorial(val);
                    expr   = $"fact({FormatNumber(val)})";
                    break;
                case "pi":
                    result = Math.PI;
                    expr   = "π";
                    break;
                case "e":
                    result = Math.E;
                    expr   = "e";
                    break;
                case "abs":
                    result = Math.Abs(val);
                    expr   = $"|{FormatNumber(val)}|";
                    break;
                default:
                    return;
            }

            _expressionDisplay = expr;
            _currentDisplay = double.IsNaN(result)
                ? "Error"
                : FormatNumber(result);

            AddHistory($"{expr} = {_currentDisplay}");
            _newEntry = true;
            _justEvaluated = true;
        }

        // ── Utility operations ────────────────────────────────────────────
        public void ToggleSign()
        {
            if (_currentDisplay == "Error" || _currentDisplay == "0") return;
            _currentDisplay = _currentDisplay.StartsWith('-')
                ? _currentDisplay[1..]
                : "-" + _currentDisplay;
        }

        public void Percentage()
        {
            if (!double.TryParse(_currentDisplay, out double val)) return;
            _currentDisplay = FormatNumber(val / 100);
        }

        public void Backspace()
        {
            if (_newEntry || _currentDisplay == "Error") { _currentDisplay = "0"; return; }
            _currentDisplay = _currentDisplay.Length <= 1 ? "0" : _currentDisplay[..^1];
            if (_currentDisplay == "-") _currentDisplay = "0";
        }

        public void Clear()
        {
            _operand1 = 0;
            _operand2 = 0;
            _operator = string.Empty;
            _currentDisplay = "0";
            _expressionDisplay = string.Empty;
            _newEntry = true;
            _justEvaluated = false;
        }

        public void ClearEntry()
        {
            _currentDisplay = "0";
            _newEntry = true;
        }

        public void ClearHistory() => _history.Clear();

        // ── Memory ────────────────────────────────────────────────────────
        private double _memory = 0;
        public void MemoryStore()   { if (double.TryParse(_currentDisplay, out double v)) _memory = v; }
        public void MemoryRecall()  { _currentDisplay = FormatNumber(_memory); _newEntry = true; }
        public void MemoryAdd()     { if (double.TryParse(_currentDisplay, out double v)) _memory += v; }
        public void MemoryClear()   { _memory = 0; }
        public string MemoryValue   => FormatNumber(_memory);
        public bool   HasMemory     => _memory != 0;

        // ── Helpers ───────────────────────────────────────────────────────
        private static double ToRadians(double deg) => deg * Math.PI / 180;

        private static double Factorial(double n)
        {
            if (n < 0 || n != Math.Floor(n) || n > 20) return double.NaN;
            double r = 1;
            for (int i = 2; i <= (int)n; i++) r *= i;
            return r;
        }

        private static string FormatNumber(double n)
        {
            if (double.IsNaN(n) || double.IsInfinity(n)) return "Error";
            // Use up to 10 significant figures, trim trailing zeros
            string s = n.ToString("G10");
            return s;
        }

        private static string OpSymbol(string op) => op switch
        {
            "+" => "+",
            "-" => "−",
            "*" => "×",
            "/" => "÷",
            "^" => "^",
            "%" => "%",
            _   => op
        };

        private void AddHistory(string entry)
        {
            _history.Insert(0, entry);
            if (_history.Count > 50) _history.RemoveAt(_history.Count - 1);
        }
    }
}
