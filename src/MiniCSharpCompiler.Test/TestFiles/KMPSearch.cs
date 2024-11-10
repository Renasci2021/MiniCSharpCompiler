using System;

public class KMP
{
    public static void KMPSearch(string pattern, string text)
    {
        int M = pattern.Length;
        int N = text.Length;

        // Create lps[] that will hold the longest prefix suffix values for pattern
        int[] lps = new int[M];
        int j = 0; // index for pattern[]

        // Preprocess the pattern (calculate lps[] array)
        ComputeLPSArray(pattern, M, lps);

        int i = 0; // index for text[]
        while (i < N)
        {
            if (pattern[j] == text[i])
            {
                j++;
                i++;
            }

            if (j == M)
            {
                Console.WriteLine("Found pattern at index " + (i - j));
                j = lps[j - 1];
            }
            else if (i < N && pattern[j] != text[i])
            {
                if (j != 0)
                    j = lps[j - 1];
                else
                    i = i + 1;
            }
        }
    }

    private static void ComputeLPSArray(string pattern, int M, int[] lps)
    {
        int length = 0; // length of the previous longest prefix suffix
        int i = 1;
        lps[0] = 0; // lps[0] is always 0

        while (i < M)
        {
            if (pattern[i] == pattern[length])
            {
                length++;
                lps[i] = length;
                i++;
            }
            else
            {
                if (length != 0)
                {
                    length = lps[length - 1];
                }
                else
                {
                    lps[i] = 0;
                    i++;
                }
            }
        }
    }

    public static void Main()
    {
        Console.WriteLine("Enter the text:");
        string text = Console.ReadLine();
        Console.WriteLine("Enter the pattern:");
        string pattern = Console.ReadLine();
        KMPSearch(pattern, text);
    }
}