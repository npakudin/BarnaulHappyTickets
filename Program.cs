//Copyright 2018 Nikolay Pakudin
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


//
// MIT License
//

// number of different trees of N nodes is an N-th Catalan number
// each node can contains one of 6 operations
// so # of different formulas = # of different trees * # of different signs
//   = Catalan(n) * 6^n

// n => # of different formulas
//  0 =>     1 * 6^0  = 1
//  1 =>     1 * 6^1  = 6
//  2 =>     2 * 6^2  = 72
//  3 =>     5 * 6^3  = 1080        ~ 1.0e+3
//  4 =>    14 * 6^4  = 18144       ~ 1.8e+4
//  5 =>    42 * 6^5  = 326592      ~ 3.2e+5
//  6 =>   132 * 6^6  = 6158592     ~ 6.1e+6
//  7 =>   429 * 6^7  = 120092544   ~ 1.2e+8
//  8 =>  1430 * 6^8  = 2401850880  ~ 2.4e+9
//  9 =>  4862 * 6^9  = 48997757952 ~ 4.8e+10
// 10 => 16796 * 6^10               ~ 1.0155899e+12

// for 1..9
// total variants: <= operations * braces = 6^9 * 1430 ~= 1.4 * 10^10

// for 1..6
// total variants: <= operations * braces = 6^5 * 132 ~= 1.0 * 10^6

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    public enum Operation
    {
        Plus,
        Minus,
        Multiply,
        Div,
        Power,
        // let vertical line - "|" is concat operator
        Concat,
    }

    public abstract class Node
    {
        public Node Left;
        public Node Right;
        public bool IsNegative;

        public abstract double Evaluate();

        // returns length of string after concat
        // (8) -> 1
        // (6|9) -> 2
        // (5|5|5) -> 3
        // (5|5|5|5) -> 4
        public abstract int GetConcatLength();

        public abstract bool IsConcatenable();

        protected abstract string GetPrintValue();

        // Don't create too much strings and StringBuilders, use single StringBuilder
        private void PrintTo(StringBuilder sb)
        {
            if (IsNegative)
            {
                sb.Append("(-");
            }

            if (Left != null)
            {
                sb.Append("(");
                Left.PrintTo(sb);
            }

            sb.Append(GetPrintValue());

            if (Right != null)
            {
                Right.PrintTo(sb);
                sb.Append(")");
            }
            
            if (IsNegative)
            {
                sb.Append(")");
            }
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            PrintTo(sb);
            return sb.ToString();
        }
    }

    public class NumberNode : Node
    {
        public int Number;

        public override double Evaluate()
        {
            return Number * (IsNegative ? -1 : 1);
        }

        public override int GetConcatLength()
        {
            return 1;
        }

        public override bool IsConcatenable()
        {
            return !IsNegative;
        }

        protected override string GetPrintValue()
        {
            return Number.ToString();
        }
    }

    public class OperationNode : Node
    {
        public Operation Operation;

        private double _cached = double.NaN;
        private bool _isCached;

        public override double Evaluate()
        {
            if (!_isCached)
            {
                _isCached = true;
                _cached = InnerEvaluate() * (IsNegative ? -1 : 1);
            }

            return _cached;
        }

        private double InnerEvaluate()
        {
            var leftValue = Left.Evaluate();
            var rightValue = Right.Evaluate();

            switch (Operation)
            {
                case Operation.Plus:
                    return leftValue + rightValue;
                case Operation.Minus:
                    return leftValue - rightValue;
                case Operation.Multiply:
                    return leftValue * rightValue;
                case Operation.Div:
                    return leftValue / rightValue;
                case Operation.Power:
                    return Math.Pow(leftValue, rightValue);
                case Operation.Concat:
                    // 1|(2|3) = 1|(2*10^1 + 3) = 1|(23) = 1 * 10^2 + 23 = 123 
                    var log10 = Right.GetConcatLength();
                    return leftValue * Math.Pow(10, log10) + rightValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override string GetPrintValue()
        {
            switch (Operation)
            {
                case Operation.Plus:
                    return "+";
                case Operation.Minus:
                    return "-";
                case Operation.Multiply:
                    return "*";
                case Operation.Div:
                    return "/";
                case Operation.Power:
                    return "^";
                case Operation.Concat:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool IsConcatenable()
        {
            return Operation == Operation.Concat && Left.IsConcatenable() && Right.IsConcatenable() && !IsNegative;
        }

        public override int GetConcatLength()
        {
            return Left.GetConcatLength() + Right.GetConcatLength();
        }
    }

    public class BracesEnumerator
    {
        public const double Epsilon = 1.0E-20;
        private readonly string _str;

        public BracesEnumerator(string str)
        {
            _str = str;
        }

        private int DigitByIndex(int index)
        {
            return _str[index] - 48;
        }

        private readonly Dictionary<int, Dictionary<double, Node>> _cache = new Dictionary<int, Dictionary<double, Node>>();
        
        private void CacheSave(int rightN, int rightBegin, double value, Node node)
        {
            var key = (rightN << 16) | rightBegin;
            if (!_cache.TryGetValue(key, out var subCache))
            {
                subCache = new Dictionary<double, Node>();
                _cache[key] = subCache;
            }

            subCache[value] = node;
        }
        
        private bool IsCacheContains(int rightN, int rightBegin, double value)
        {
            var key = (rightN << 16) | rightBegin;
            if (!_cache.TryGetValue(key, out var subCache))
            {
                return false;
            }
            var isContains = subCache.ContainsKey(value);
            //return isContains;
            return false;
        }

        public IEnumerable<Node> Braces(int n, int begin, bool prohibitNegative, int level, int address)
        {
            if (n == 0)
            {
                yield return new NumberNode {Number = DigitByIndex(begin)};
                //if (!prohibitNegative)
                {
                    yield return new NumberNode {Number = DigitByIndex(begin), IsNegative = true};
                }
            }

            for (var i = 0; i < n; i++)
            {
                var rights = Braces(n - i - 1, begin + i + 1, false, level + 1, address | (1 << (level + 1)));
                foreach (var right in rights)
                {
                    // if right tree has the same value and generator have the same parameters
                    // don't generate right trees
                    // improve performance -17%
//                    var rightN = n - i - 1;
//                    var rightBegin = begin + i + 1;
                    var rightN = level;
                    var rightBegin = address;
                    // not completely correct - loosing data here
                    // supposing for integer result it's OK
                    var rightValue = right.Evaluate();

                    if (IsCacheContains(rightN, rightBegin, rightValue))
                    {
                        //continue;
                    }

                    var canSave = true;
                    
                    foreach (var value in Enum.GetValues(typeof(Operation)))
                    {
                        if ((Operation) value == Operation.Concat && (right.IsNegative || !right.IsConcatenable()))
                        {
                            // cannot concat 1|(2+3)
                            // cannot concat 1|(-2)
                            canSave = false;
                            continue;
                        }
                                                    
                        if ((Operation) value == Operation.Div && Math.Abs(right.Evaluate()) <= double.Epsilon)
                        {
                            // cannot divide by zero
                            canSave = false;
                            continue;
                        }

                        // improve performance ~2 times for 4, 5 and 6 digits
                        // don't return values which are already calculated
                        if ((Operation) value == Operation.Multiply || (Operation) value == Operation.Div)
                        {
                            if (right.IsNegative)
                            {
                                // a * b = (-a) * (-b)
                                // a / b = (-a) / (-b)

                                // (-a) * b = a * (-b)
                                // (-a) / b = -a / (-b)
                                canSave = false;
                                continue;
                            }

//                                // no performance effect
//                                if (right is OperationNode opRight)
//                                {
//                                    if (opRight.Operation == Operation.Multiply || opRight.Operation == Operation.Div)
//                                    {
//                                        if (opRight.Left.IsNegative || opRight.Right.IsNegative)
//                                        {
//                                            continue;
//                                        }
//                                    }
//                                }
                        }
                        
                        var prohibitLeftNegative = false;

                        var lefts = Braces(i, begin, prohibitLeftNegative,  level + 1, address | (0 << (level + 1)));
                        foreach (var left in lefts)
                        {
                            if ((Operation) value == Operation.Concat && !left.IsConcatenable())
                            {
                                // cannot concat 1|(2+3)
                                canSave = false;
                                continue;
                            }

                            if ((Operation) value == Operation.Power && Math.Abs(left.Evaluate()) <= double.Epsilon && Math.Abs(right.Evaluate()) <= double.Epsilon)
                            {
                                // cannot calculate 0^0
                                canSave = false;
                                continue;
                            }
                            yield return new OperationNode {Left = left, Right = right, Operation = (Operation) value};
                            
                            // improve performance ~10 times for 4, 5 and 6 digits
                            if ((Operation) value == Operation.Power || (Operation) value == Operation.Concat)
                            {
                                // -(a + b) = (-a) + (-b)
                                // -(a - b) = (-a) + b
                                // -(a * b) = (-a) * b = a * (-b)
                                // -(a / b) = (-a) / b = -a / (-b)
                                yield return new OperationNode {Left = left, Right = right, Operation = (Operation) value, IsNegative = true};
                            }
                        }
                    }

                    //if (canSave)
                    {
                        CacheSave(rightN, rightBegin, rightValue, right);
                    }
                }
            }
        }
    }

    internal class Program
    {
        public static void BarnaulHappyTickets(string filename, int from, int to)
        {
            const int signsNumber = 3; // not digits - places between them

            long totalExpr = 0;
            long matchedExpr = 0;

            var startTime = DateTime.Now;
            var fileInfo = new FileInfo(filename);
            var workingFileMode = FileMode.Create;

            // fail over (for continue work after program stop)
            // if the last valuable line starts with number, continue from 1st number in the line
            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                using (var sr = fileInfo.OpenRead())
                {
                    var bufSize = Math.Min(100, fileInfo.Length);
                    var buf = new byte[bufSize];
                    
                    sr.Seek(-buf.Length, SeekOrigin.End);
                    sr.Read(buf, 0, buf.Length);
                    
                    var str = Encoding.ASCII.GetString(buf);
                    var lines = str.Split("\n");
                    var lastLine = lines.Last(x => x.Length > 0);
                    var beginToken = lastLine.Split(' ', '\t', '\r').First();
                    if (beginToken.StartsWith("real"))
                    {
                        // new run
                        workingFileMode = FileMode.Create;
                    }
                    else
                    {
                        if (int.TryParse(beginToken, out var lastValue))
                        {
                            // continue previous run
                            from = lastValue; // don't use "lastValue + 1" - line can be uncompleted!
                            workingFileMode = FileMode.Append;
                        }
                    }
                }
            }

            
            using (var sw = new StreamWriter(fileInfo.Open(workingFileMode)))
            {
                sw.WriteLine("=============================");
                for (int i = from; i <= to; i++)
                {
                    var bracesEnumerator = new BracesEnumerator(i.ToString("000000"));
                    var trees = bracesEnumerator.Braces(signsNumber, 0, false, 0, 0);
                    foreach (var tree in trees)
                    {
                        totalExpr++;

                        var res = tree.Evaluate();

                        if (Math.Abs(res - 100) < 1.0E-1)
                        {
                            matchedExpr++;
                            var treeStr = tree.ToString();
                            sw.WriteLine($"{i} : {treeStr}");
                            break;
                        }

                        if (totalExpr % 1000000 == 0)
                        {
                            sw.Flush();
                            Console.WriteLine($"totalExpr = {totalExpr}");
                        }
                    }
                }

                var diffTime = DateTime.Now - startTime;

                sw.WriteLine(
                    $"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                sw.WriteLine(
                    $"real {matchedExpr} in {diffTime.TotalMilliseconds} => {matchedExpr/ diffTime.TotalMilliseconds} records/ms");

                Console.WriteLine(
                    $"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                Console.WriteLine(
                    $"real {matchedExpr} in {diffTime.TotalMilliseconds} => {matchedExpr / diffTime.TotalMilliseconds} records/ms");
            }
        }
        
        // signsNumber - not digits - places between them
        private static void Problem10598(string filename, int signsNumber)
        {
            var sb = new StringBuilder();
            for (var i = 1; i <= signsNumber + 1; i++)
            {
                sb.Append(i);
            }
            
            long totalExpr = 0;
            long matchedExpr = 0;
            
            var map = new Dictionary<double, string>();

            var startTime = DateTime.Now;
            var fileInfo = new FileInfo(filename);
            using (var sw = new StreamWriter(fileInfo.Open(FileMode.Create)))
            {
                sw.WriteLine("=============================");
                var bracesEnumerator = new BracesEnumerator(sb.ToString());
                var trees = bracesEnumerator.Braces(signsNumber, 0, false, 0, 0);
                foreach (var tree in trees)
                {
                    totalExpr++;

                    var res = tree.Evaluate();

                    
                    
                    if (Math.Abs(res - Math.Round(res)) > BracesEnumerator.Epsilon)
                    {
                        // ignore with fraction part
                        //continue;
                    }

                    if (Math.Abs(res) > 20_000)
                    {
                        // ignore too large
                        //continue;
                    }

                    if (Math.Abs(res) < BracesEnumerator.Epsilon)
                    {
                        // ignore numbers like 1.0E-117
                        // NOTE: zero is ignored too! 
                        //continue;
                    }

                    if (!map.ContainsKey(res))
                    {
                        matchedExpr++;
                        map[res] = tree.ToString();
                    }

                    
                    

                    if (totalExpr % 1000000 == 0)
                    {
                        sw.Flush();
                        Console.WriteLine($"totalExpr = {totalExpr}");
                    }
                }

                var diffTime = DateTime.Now - startTime;

                sw.WriteLine(
                    $"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                sw.WriteLine(
                    $"real {matchedExpr} in {diffTime.TotalMilliseconds} => {matchedExpr/ diffTime.TotalMilliseconds} records/ms");

                Console.WriteLine(
                    $"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                Console.WriteLine(
                    $"real {matchedExpr} in {diffTime.TotalMilliseconds} => {matchedExpr / diffTime.TotalMilliseconds} records/ms");
                Console.WriteLine(
                    $"total/real {(double)totalExpr / matchedExpr}");


                // print keys in sorted order
                {
                    sw.WriteLine("-------- The same sorted --------");
                    var keys = map.Keys.ToArray();
                    Array.Sort(keys);

                    var prev = 2.0;
                    var lastOk = 2.0;
                    foreach (var key in keys)
                    {
                        //Console.WriteLine($"{key} = {map[key]}");
                        sw.WriteLine($"{key} = {map[key]}");

                        if (key >= prev && Math.Abs(lastOk - 2.0) < BracesEnumerator.Epsilon)
                        {
                            if (Math.Abs(key - prev - 1.0) >= BracesEnumerator.Epsilon &&
                                Math.Abs(key - prev) >= BracesEnumerator.Epsilon)
                            {
                                // not OK
                                lastOk = prev;
                            }

                            prev = key;
                        }
                    }
                    sw.WriteLine($"lastOk: {lastOk}");
                }
            }
        }
        

        public static void Main(string[] args)
        {
            var signsNumber = 5;
            Problem10598($"res-{signsNumber}.txt", signsNumber);

//            var from = 000_000;
//            var to = 100_000;
//            var fromStr = from.ToString("000000");
//            var toStr = to.ToString("000000");
//            BarnaulHappyTickets($"barnaul-{fromStr}-{toStr}.txt", from, to);
        }
    }
}
