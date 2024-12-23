using System;

class Program
{
    public static void Main()
    {
        Console.WriteLine("Enter integers separated by spaces:");
        string input = Console.ReadLine();  // input = Console.ReadLine(); 未定义的变量 'input'
        int[] numbers = Array.ConvertAll(input.Split(' '), int.Parse);

        for (int i = 1; i < numbers.Length; i++)
        {
            int key = numbers[i];
            int j = i - 1;  // int j = input; 类型不匹配：无法将类型 'StringKeyword' 赋值给 'IntKeyword'

            while (numbers[j] > key)  //  while (numbers[j]) //while 循环条件必须是布尔类型
            {
                numbers[j + 1] = numbers[j];
                j = j - 1;  // int j = j - 1; 变量 'j' 已存在

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