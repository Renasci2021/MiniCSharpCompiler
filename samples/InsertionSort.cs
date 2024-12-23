using System;

class Program
{
    public static void Main()
    {
        Console.WriteLine("Enter integers separated by spaces:");
        string input = Console.ReadLine();
        int[] numbers = Array.ConvertAll(input.Split(' '), int.Parse);

        for (int i = 1; i < numbers.Length; i++)
        {
            int key = numbers[i];
            int j = i - 1;

            while (numbers[j] > key)
            {
                numbers[j + 1] = numbers[j];
                j = j - 1;

                if (j < 0)
                {
                    break;
                }
            }
            numbers[j + 1] = key;
        }

        Console.WriteLine("Sorted numbers:");
        Console.WriteLine(string.Join(" ", numbers));
    }
}