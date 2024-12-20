using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Enter integers separated by spaces:");
        string input = Console.ReadLine();
        int[] numbers = Array.ConvertAll(input.Split(' '), int.Parse);

        InsertionSort(numbers);

        Console.WriteLine("Sorted numbers:");
        Console.WriteLine(string.Join(" ", numbers));
    }

    static void InsertionSort(int[] array)
    {
        for (int i = 1; i < array.Length; i++)
        {
            int key = array[i];
            int j = i - 1;

            while (j >= 0 && array[j] > key)
            {
                array[j + 1] = array[j];
                j--;
            }
            array[j + 1] = key;
        }
    }
}