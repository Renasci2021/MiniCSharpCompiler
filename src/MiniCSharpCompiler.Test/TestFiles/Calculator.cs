using System;
using System.Collections.Generic;

public class Stack<T>
{
    private List<T> _elements = new List<T>();

    public void Push(T item)
    {
        _elements.Add(item);
    }

    public T Pop()
    {
        if (_elements.Count == 0)
            throw new InvalidOperationException("Stack is empty.");
        var item = _elements[_elements.Count - 1];
        _elements.RemoveAt(_elements.Count - 1);
        return item;
    }

    public T Peek()
    {
        if (_elements.Count == 0)
            throw new InvalidOperationException("Stack is empty.");
        return _elements[_elements.Count - 1];
    }

    public int Count
    {
        get { return _elements.Count; }
    }
}

public class Calculator
{
    public int Evaluate(string expression)
    {
        var values = new Stack<int>();
        var operators = new Stack<char>();
        int i = 0;

        while (i < expression.Length)
        {
            if (char.IsWhiteSpace(expression[i]))
            {
                i++;
                continue;
            }

            if (char.IsDigit(expression[i]))
            {
                int value = 0;
                while (i < expression.Length && char.IsDigit(expression[i]))
                {
                    value = value * 10 + (expression[i] - '0');
                    i++;
                }
                values.Push(value);
                continue;
            }

            if (expression[i] == '(')
            {
                operators.Push(expression[i]);
            }
            else if (expression[i] == ')')
            {
                while (operators.Peek() != '(')
                {
                    values.Push(ApplyOperation(operators.Pop(), values.Pop(), values.Pop()));
                }
                operators.Pop();
            }
            else if (IsOperator(expression[i]))
            {
                while (operators.Count > 0 && HasPrecedence(expression[i], operators.Peek()))
                {
                    values.Push(ApplyOperation(operators.Pop(), values.Pop(), values.Pop()));
                }
                operators.Push(expression[i]);
            }
            i++;
        }

        while (operators.Count > 0)
        {
            values.Push(ApplyOperation(operators.Pop(), values.Pop(), values.Pop()));
        }

        return values.Pop();
    }

    private bool IsOperator(char op)
    {
        return op == '+' || op == '-' || op == '*' || op == '/';
    }

    private bool HasPrecedence(char op1, char op2)
    {
        if (op2 == '(' || op2 == ')')
            return false;
        if ((op1 == '*' || op1 == '/') && (op2 == '+' || op2 == '-'))
            return false;
        return true;
    }

    private int ApplyOperation(char op, int b, int a)
    {
        switch (op)
        {
            case '+': return a + b;
            case '-': return a - b;
            case '*': return a * b;
            case '/': return a / b;
            default: throw new ArgumentException("Invalid operator");
        }
    }
}

class Program
{
    static void Main()
    {
        var calculator = new Calculator();
        string expression = "3 + (2 * 2) - 1";
        int result = calculator.Evaluate(expression);
        Console.WriteLine($"Result of '{expression}' is: {result}");
    }
}