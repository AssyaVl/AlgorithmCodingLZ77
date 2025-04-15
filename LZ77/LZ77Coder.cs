using System;
using System.Collections.Generic;
using System.Text;

namespace LZ77
{
    /// <summary>
    /// Класс для выполнения сжатия и декодирования методом LZ77
    /// </summary>
    public class LZ77Coder
    {

        /// <summary>
        /// Конструктор, который принимает только входную строку
        /// </summary>
        /// <param name="input">Входная строка для кодирования</param>
        public LZ77Coder(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Входная строка некорректна");
        }

        /// <summary>
        /// Структура для хранения результата кодирования (метки), (сдвиг в буфере, длина совпадения, последующий символ) 
        /// </summary>
        public struct LZ77Token
        {
            public int Offset; // Сдвиг в буфере
            public int Length; // Длина совпадения
            public char NextChar; // Следующий символ

            public LZ77Token(int offset, int length, char nextChar)
            {
                Offset = offset;
                Length = length;
                NextChar = nextChar;
            }

            public override string ToString()
            {
                return NextChar == '\0' ? $"({Offset},{Length},eof)" : $"({Offset},{Length},{NextChar})";
            }
        }

        /// <summary>
        /// Кодирует входную строку методом LZ77
        /// </summary>
        /// <param name="input">Входная строка для кодирования</param>
        /// <returns>Список меток (смещение, длина, символ)</returns>
        public List<LZ77Token> Encode(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Входная строка некорректна");

            List<LZ77Token> tokens = new List<LZ77Token>();
            int position = 0;

            while (position < input.Length)//проверяем каждую позицию в строке
            {
                // Буфер поиска — вся строка до текущей позиции
                int searchStart = 0; // Смотрим с самого начала строки

                // Ищем наилучшее совпадение (максимальная длина)
                int bestOffset = 0;//сдвиг в буфере
                int bestLength = 0;//длина совпадения 
                char nextChar = position < input.Length ? input[position] : '\0'; //следующий символ

                for (int i = searchStart; i < position; i++)//начинаем искать совпадения
                {
                    int length = 0;//пока длина 0
                    while (position + length < input.Length && // Не выходим за конец строки
                           i + length < position && // Не пересекаем текущую позицию символа
                           input[i + length] == input[position + length]) // Символы совпадают
                    {
                        length++;//увелмчиваем длину совпадения
                    }
                    if (length > bestLength)//если длина совпадения лучше, что мы встречали до этого
                    {
                        bestLength = length;
                        bestOffset = position - i;//сдвиг в буфере (позиция - количество символов назад, когда мы нашли первое сопвпадение)
                        nextChar = (position + length < input.Length) ? input[position + length] : '\0';//прововерка, что мы не вышли за пределеы строки
                    }
                }

                if (bestLength == 0)
                {
                    bestOffset = 0;
                    bestLength = 0;
                    nextChar = input[position]; //новый символ
                }

                tokens.Add(new LZ77Token(bestOffset, bestLength, nextChar)); //добавляем новую метку
                position += bestLength + 1; //проверяем следующй символ 
            }

            return tokens; //возвращаем список меток
        }

        /// <summary>
        /// Декодирует список меток в исходную строку
        /// </summary>
        /// <param name="tokens">Список меток</param>
        /// <returns>Раскодированная строка</returns>
        public string Decode(List<LZ77Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                throw new ArgumentException("Список меток некорректен");

            StringBuilder result = new StringBuilder();//раскодированная строка

            foreach (var token in tokens) //перебираем все метки
            {
                if (token.Length > 0) //если у метки длина совпадения не 0, значит там повторяются
                                      //символы из уже раскодированной строки
                {
                    int start = result.Length - token.Offset; //начало: длина уже раскодированной строки - сдвиг в буфере
                    for (int i = 0; i < token.Length; i++) //от 0 до длины совпадения добавляем повторы 
                    {
                        result.Append(result[start + i]); //добавляем в раскодированную строку повторение начиная
                                                          //от старта заканчивая на последнем повторяющемся символе
                    }
                }
                if (token.NextChar != '\0') //если не конец строки
                {
                    result.Append(token.NextChar);//добавляем в строку повторяющийся символ
                }
            }

            return result.ToString(); //возвращаем раскодированную строку
        }

        /// <summary>
        /// Вычисляет коэффициент сжатия
        /// </summary>
        /// <param name="input">Исходная строка</param>
        /// <param name="tokens">Список токенов</param>
        /// <returns>Коэффициент сжатия</returns>
        public (int originalBitLength, int encodedBitLength, double ratio) CalculateCompressionRatio(string input, List<LZ77Token> tokens)
        {
            if (string.IsNullOrEmpty(input) || tokens == null || tokens.Count == 0) // проверка на пустоту
                return (0,0,0.0);

            // Исходная длина строки в битах (8 бит на символ)
            int originalBitLength = input.Length * 8;

            // Находим максимальные значения offset и length среди всех токенов
            int maxOffset = 0;
            int maxLength = 0;

            foreach (var token in tokens)
            {
                if (token.Offset > maxOffset)
                    maxOffset = token.Offset;
                if (token.Length > maxLength)
                    maxLength = token.Length;
            }

            // Для offset = 0 и length = 0 нужно минимум 1 бит, чтобы закодировать 0
            maxOffset = Math.Max(1, maxOffset);
            maxLength = Math.Max(1, maxLength);

            // Вычисляем количество бит для offset и length
            int offsetBits = (int)Math.Ceiling(Math.Log2(maxOffset));
            int lengthBits = (int)Math.Ceiling(Math.Log2(maxLength));
            int charBits = 8; // Для nextChar всегда 8 бит

            // Общая длина закодированных данных
            int bitsPerToken = offsetBits + lengthBits + charBits;
            int encodedBitLength = tokens.Count * bitsPerToken;

            // Коэффициент сжатия
            return (originalBitLength, encodedBitLength, (double)originalBitLength / encodedBitLength);
        }
    }
}