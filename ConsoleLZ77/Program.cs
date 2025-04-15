using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LZ77;

namespace ConsoleLZ77
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.Clear();

            while (true)
            {
                bool exit = false;
                while (!exit)
                {
                    DisplayMenu();
                    Console.ForegroundColor = ConsoleColor.Magenta; 
                    string choice = Console.ReadLine();

                    try
                    {
                        switch (choice)
                        {
                            case "1":
                                EncodeString();
                                break;
                            case "2":
                                DecodeString();
                                break;
                            case "3":
                                ShowCompressionRatio();
                                break;
                            case "4":
                                ShowTokens();
                                break;
                            case "0":
                                exit = true; // Выход из внутреннего цикла
                                break;
                            default:
                                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }

                    if (!exit)
                    {
                        Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                        Console.ReadKey();
                        Console.Clear();
                    }
                }
                // После выхода из внутреннего цикла очищаем консоль и продолжаем
                Console.Clear();
            }
        }

        static void DisplayMenu()
        {
            Console.ForegroundColor = ConsoleColor.Blue; 
            Console.WriteLine("Меню:");
            Console.WriteLine("1. Закодировать строку из файла coding.txt");
            Console.WriteLine("2. Раскодировать строку из файла decoding.txt");
            Console.WriteLine("3. Показать степень сжатия");
            Console.WriteLine("4. Показать список меток и их количество");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите опцию: ");
        }

        static void EncodeString()
        {
            Console.ForegroundColor = ConsoleColor.Magenta; 
            if (!File.Exists("coding.txt"))
                throw new FileNotFoundException("Файл coding.txt не найден");

            string input = File.ReadAllText("coding.txt");
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Файл coding.txt пуст");

            LZ77Coder lz77 = new LZ77Coder(input);
            List<LZ77Coder.LZ77Token> tokens = lz77.Encode(input);

            using (StreamWriter writer = new StreamWriter("decoding.txt"))
            {
                foreach (var token in tokens)
                {
                    writer.WriteLine(token.ToString());
                }
            }

            Console.WriteLine("Строка успешно закодирована и сохранена в decoding.txt");
        }

        static void DecodeString()
        {
            Console.ForegroundColor = ConsoleColor.Magenta; 
            if (!File.Exists("decoding.txt"))
                throw new FileNotFoundException("Файл decoding.txt не найден");

            List<LZ77Coder.LZ77Token> tokens = new List<LZ77Coder.LZ77Token>();
            string[] lines = File.ReadAllLines("decoding.txt");

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Ожидаем формат (offset,length,nextChar) или (offset,length,eof)
                string cleaned = line.Trim('(', ')');
                string[] parts = cleaned.Split(',');

                if (parts.Length != 3)
                    throw new FormatException($"Неверный формат метки: {line}");

                if (!int.TryParse(parts[0], out int offset) ||
                    !int.TryParse(parts[1], out int length))
                    throw new FormatException($"Неверный формат метки: {line}");

                char nextChar = parts[2] == "eof" ? '\0' : parts[2][0];
                if (parts[2] != "eof" && parts[2].Length != 1)
                    throw new FormatException($"Неверный формат метки: {line}");

                tokens.Add(new LZ77Coder.LZ77Token(offset, length, nextChar));
            }

            if (tokens.Count == 0)
                throw new ArgumentException("Файл decoding.txt не содержит валидных меток");

            LZ77Coder lz77 = new LZ77Coder(" "); 
            string decoded = lz77.Decode(tokens);

            File.WriteAllText("coding.txt", decoded);
            Console.WriteLine("Строка успешно раскодирована и сохранена в coding.txt");
        }

        static void ShowCompressionRatio()
        {
            Console.ForegroundColor = ConsoleColor.Magenta; 
            if (!File.Exists("coding.txt"))
                throw new FileNotFoundException("Файл coding.txt не найден");

            if (!File.Exists("decoding.txt"))
                throw new FileNotFoundException("Файл decoding.txt не найден");

            string input = File.ReadAllText("coding.txt");
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Файл coding.txt пуст");

            List<LZ77Coder.LZ77Token> tokens = new List<LZ77Coder.LZ77Token>();
            string[] lines = File.ReadAllLines("decoding.txt");

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string cleaned = line.Trim('(', ')');
                string[] parts = cleaned.Split(',');

                if (parts.Length != 3)
                    throw new FormatException($"Неверный формат метки: {line}");

                if (!int.TryParse(parts[0], out int offset) ||
                    !int.TryParse(parts[1], out int length))
                    throw new FormatException($"Неверный формат метки: {line}");

                char nextChar = parts[2] == "eof" ? '\0' : parts[2][0];
                tokens.Add(new LZ77Coder.LZ77Token(offset, length, nextChar));
            }

            if (tokens.Count == 0)
                throw new ArgumentException("Файл decoding.txt не содержит валидных меток");

            LZ77Coder lz77 = new LZ77Coder(input);
            var (originalBitLength, encodedBitLength, ratio) = lz77.CalculateCompressionRatio(input, tokens);
            Console.WriteLine($"Количество битов в исходной строке: {originalBitLength}");
            Console.WriteLine($"Количество битов в закодированной строке: {encodedBitLength}");
            Console.WriteLine($"Коэффициент сжатия: {ratio:F2}");
        }

        static void ShowTokens()
        {
            Console.ForegroundColor = ConsoleColor.Magenta; 
            if (!File.Exists("decoding.txt"))
                throw new FileNotFoundException("Файл decoding.txt не найден");

            string[] lines = File.ReadAllLines("decoding.txt");
            List<string> validTokens = new List<string>();

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    validTokens.Add(line);
            }

            Console.WriteLine("Список меток:");
            foreach (string token in validTokens)
            {
                Console.WriteLine(token);
            }
            Console.WriteLine($"Общее количество меток: {validTokens.Count}");
        }
    }
}