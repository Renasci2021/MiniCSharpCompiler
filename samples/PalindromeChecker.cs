using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter a string to check if it is a palindrome:");
        string input = Console.ReadLine();

        bool flag = true;

        int left = 0;
        int right = input.Length - 1;

        while (left < right)
        {
            if (input[left] != input[right])
            {
                flag = false;
            }
            left = left + 1;
            right = right - 1;
        }

        if (flag == true)
        {
            Console.WriteLine("The string is a palindrome.");
        }
        else
        {
            Console.WriteLine("The string is not a palindrome.");
        }
    }
}